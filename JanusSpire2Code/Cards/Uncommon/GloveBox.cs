using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class GloveBox() : JanusCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self), ICardDescriptionContributor
{
    private const int StickerThreshold = 5;
    private const string StickersPlayedKey = "StickersPlayed";

    private int _stickersPlayed;

    [SavedProperty]
    public int StickersPlayed
    {
        get => _stickersPlayed;
        private set
        {
            AssertMutable();
            _stickersPlayed = Math.Clamp(value, 0, StickerThreshold - 1);
            DynamicVars[StickersPlayedKey].BaseValue = _stickersPlayed;
            this.RequestVisualReload();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(JanusKeywords.Sticker)];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(StickerThreshold),
        new DynamicVar(StickersPlayedKey, 0)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2F);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Pile?.Type != MainFile.Diary ||
            cardPlay.Card.Owner != Owner ||
            !cardPlay.Card.Keywords.Contains(JanusKeywords.Sticker) ||
            Owner.Creature.IsDead ||
            CombatState == null)
        {
            return;
        }

        int nextProgress = StickersPlayed + 1;
        if (nextProgress < StickerThreshold)
        {
            StickersPlayed = nextProgress;
            return;
        }

        StickersPlayed = 0;
        CardModel? sticker = CardFactory.GetDistinctForCombat(
                Owner,
                ModelDb.AllCards.Where(card => card.Keywords.Contains(JanusKeywords.Sticker)),
                1,
                Owner.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (sticker != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(sticker, PileType.Hand, Owner);
        }
    }

    public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
    {
        bool isInDiary = Pile?.Type == MainFile.Diary ||
                         (Pile == null && context.PileType == MainFile.Diary);
        if (!isInDiary)
        {
            return [];
        }

        return
        [
            new CardDescriptionFragment(
                new LocString("cards", $"{Id.Entry}.progressDescription"),
                CardDescriptionFragmentPlacement.AfterBase)
        ];
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
