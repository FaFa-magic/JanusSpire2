using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Networking.ManagedActions;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class HolyNight() : JanusCardModel(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private static readonly RitsuLibManagedNetActionDescriptor<RightClickPayload> RightClickDescriptor = new(
        MainFile.ModId,
        "holy_night_take_from_diary",
        SerializeRightClickPayload,
        DeserializeRightClickPayload,
        ExecuteManagedRightClick,
        GameActionType.CombatPlayPhaseOnly);

    private static readonly HolyNightRightClickHandler RightClickHandler = new();
    private static int _rightClickRegistered;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Int("StrengthLoss", 4),
        new CardsVar(3),
        new DamageVar(4m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<StrengthPower>()    
    ];
    
    internal static void RegisterSynchronizedRightClick()
    {
        if (Interlocked.Exchange(ref _rightClickRegistered, 1) != 0)
        {
            return;
        }

        RitsuLibManagedNetActions.Register(RightClickDescriptor);
        ModRightClickRegistry.Register(RightClickHandler);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<HolyNightPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["StrengthLoss"].BaseValue,
            Owner.Creature,
            this);
    }

    private static bool IsSupportedRightClickPile(PileType pileType)
    {
        return pileType is PileType.Draw or PileType.Discard or PileType.Exhaust ||
               pileType == MainFile.Diary;
    }

    private bool CanExecuteRightClick(PileType expectedPile)
    {
        if (Owner.PlayerCombatState == null ||
            Pile?.Type != expectedPile ||
            !IsSupportedRightClickPile(expectedPile) ||
            Pile.Type == PileType.Hand)
        {
            return false;
        }

        CardPile hand = PileType.Hand.GetPile(Owner);
        CardPile? diaryPile = Owner.PlayerCombatState.AllPiles
            .FirstOrDefault(pile => pile.Type == MainFile.Diary);
        return hand.Cards.Count < CardPile.MaxCardsInHand &&
               diaryPile != null &&
               diaryPile.Cards.Count >= DynamicVars.Cards.IntValue;
    }

    private async Task ExecuteRightClick(
        GameActionPlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player actionPlayer,
        PileType expectedPile)
    {
        if (!ReferenceEquals(actionPlayer, Owner) || !CanExecuteRightClick(expectedPile))
        {
            return;
        }

        CardPile diaryPile = MainFile.Diary.GetPile(Owner);
        if (LocalContext.IsMe(actionPlayer) &&
            NCapstoneContainer.Instance is { InUse: true } capstone)
        {
            capstone.Close();
        }

        // FromCombatPile serializes mutable cards by NetCombatCard runtime ID. Record projection
        // models are local presentation state, so those IDs are not stable between peers. A fixed
        // Diary snapshot makes the official choice synchronizer transmit positional indexes.
        List<CardModel> diarySnapshot = diaryPile.Cards.ToList();
        int cardCount = DynamicVars.Cards.IntValue;
        List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            diarySnapshot,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, cardCount))).ToList();

        if (selected.Count != cardCount ||
            selected.Distinct().Count() != cardCount ||
            selected.Any(card => card.Pile?.Type != MainFile.Diary))
        {
            return;
        }

        foreach (CardModel card in selected)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        await CardPileCmd.Add(this, PileType.Hand);
    }

    private static byte[] SerializeRightClickPayload(RightClickPayload payload)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        writer.WriteEnum(payload.SourcePile);
        writer.WriteInt(payload.CardIndex);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static RightClickPayload DeserializeRightClickPayload(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        return new(reader.ReadEnum<PileType>(), reader.ReadInt());
    }

    private static async Task ExecuteManagedRightClick(
        RitsuLibManagedNetActionContext<RightClickPayload> context)
    {
        CardPile? sourcePile = context.Player.PlayerCombatState?.AllPiles
            .FirstOrDefault(pile => pile.Type == context.Message.SourcePile);
        if (sourcePile == null ||
            context.Message.CardIndex < 0 ||
            context.Message.CardIndex >= sourcePile.Cards.Count ||
            sourcePile.Cards[context.Message.CardIndex] is not HolyNight card)
        {
            return;
        }

        await card.ExecuteRightClick(
            context.PlayerChoiceContext,
            context.Player,
            context.Message.SourcePile);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars["StrengthLoss"].UpgradeValueBy(2M);
    }

    private readonly record struct RightClickPayload(PileType SourcePile, int CardIndex);

    private sealed class HolyNightRightClickHandler : IModRightClickHandler
    {
        public int Priority => 100;

        public bool TryHandle(ModRightClickContext context)
        {
            if (context.Model is not HolyNight card ||
                !ReferenceEquals(context.Player, card.Owner) ||
                context.Trigger.Source != ModRightClickSource.CombatPileCard ||
                context.Trigger.ExpectedCardPile is not { } expectedPile ||
                !IsSupportedRightClickPile(expectedPile) ||
                card.Pile?.Type != expectedPile)
            {
                return false;
            }

            int cardIndex = card.Pile.Cards.ToList().IndexOf(card);
            if (cardIndex < 0)
            {
                return false;
            }

            return RitsuLibManagedNetActions.Request(
                RunManager.Instance,
                RightClickDescriptor,
                new(expectedPile, cardIndex),
                context.Player.NetId);
        }
    }
}
