using JanusSpire2.JanusSpire2Code.Configs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Events;

[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Glory))]
public sealed class KittenCardGame : ModEventTemplate
{
    private const string RandomCardKey = "RandomCard";
    private const int RefreshCost = 30;

    private CardModel? _randomCardToCopy;

    private Player EventOwner => Owner ?? throw new InvalidOperationException(
        $"Event '{Id.Entry}' has not been assigned an owner.");

    private CardModel RandomCardToCopy
    {
        get => _randomCardToCopy ?? throw new InvalidOperationException(
            $"Event '{Id.Entry}' has not selected a card to copy.");
        set
        {
            AssertMutable();
            _randomCardToCopy = value;
        }
    }

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://JanusSpire2/images/characters/char_select_bg_janus.jpg");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar(RandomCardKey),
        new GoldVar(RefreshCost)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return JanusConfigPage.KittenCardGameEnabledBinding.Read() &&
               runState.CurrentActIndex is 1 or 2 &&
               runState.Players.All(player => player.Deck.Cards.Count > 0);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        SelectRandomCard();
        return CreateOptions();
    }

    private IReadOnlyList<EventOption> CreateOptions()
    {
        EventOption refreshOption = EventOwner.Gold >= RefreshCost
            ? new EventOption(this, Refresh, InitialOptionKey("REFRESH"))
            : new EventOption(this, null, InitialOptionKey("REFRESH_LOCKED"));

        return
        [
            new EventOption(
                this,
                Copy,
                InitialOptionKey("COPY"),
                HoverTipFactory.FromCard(RandomCardToCopy)),
            refreshOption
        ];
    }

    private void SelectRandomCard()
    {
        List<CardModel> candidates = EventOwner.Deck.Cards
            .Where(card => _randomCardToCopy == null || card.GetType() != _randomCardToCopy.GetType())
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = EventOwner.Deck.Cards.ToList();
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"Event '{Id.Entry}' cannot select a card from an empty deck.");
        }

        RandomCardToCopy = Rng.NextItem(candidates)!;
        ((StringVar)DynamicVars[RandomCardKey]).StringValue = RandomCardToCopy.Title;
    }

    private async Task Copy()
    {
        CardModel copy = EventOwner.RunState.CloneCard(RandomCardToCopy);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(copy, PileType.Deck),
            1.2F,
            CardPreviewStyle.EventLayout);
        SetEventFinished(PageDescription("COPY"));
    }

    private async Task Refresh()
    {
        Player eventOwner = EventOwner;
        if (eventOwner.Gold < RefreshCost)
        {
            SetEventState(PageDescription("REFRESH"), CreateOptions());
            return;
        }

        await PlayerCmd.LoseGold(RefreshCost, eventOwner, GoldLossType.Spent);
        SelectRandomCard();
        SetEventState(PageDescription("REFRESH"), CreateOptions());
    }
}
