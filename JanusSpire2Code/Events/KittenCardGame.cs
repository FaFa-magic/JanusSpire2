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
    private const float DefaultCardWeight = 1F;
    private const float BasicStrikeOrDefendWeight = 0.5F;

    private CardModel? _randomCardToCopy;
    private HashSet<ModelId>? _shownCardIds;

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

    private HashSet<ModelId> ShownCardIds
    {
        get
        {
            AssertMutable();
            return _shownCardIds ??= [];
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
               runState.Players.All(player =>
                   player.Gold >= RefreshCost && player.Deck.Cards.Count > 0);
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
        List<CardModel> deckCards = EventOwner.Deck.Cards.ToList();
        if (deckCards.Count == 0)
        {
            throw new InvalidOperationException($"Event '{Id.Entry}' cannot select a card from an empty deck.");
        }

        HashSet<ModelId> shownCardIds = ShownCardIds;
        List<CardModel> candidates = deckCards
            .Where(card => !shownCardIds.Contains(card.Id))
            .DistinctBy(card => card.Id)
            .ToList();

        if (candidates.Count == 0)
        {
            shownCardIds.Clear();
            candidates = deckCards
                .DistinctBy(card => card.Id)
                .ToList();
        }

        List<CardModel> nonStatusOrCurseCandidates = candidates
            .Where(card => card.Type is not CardType.Status and not CardType.Curse)
            .ToList();
        if (nonStatusOrCurseCandidates.Count > 0)
        {
            candidates = nonStatusOrCurseCandidates;
        }

        RandomCardToCopy = Rng.WeightedNextItem(candidates, GetSelectionWeight)
            ?? throw new InvalidOperationException($"Event '{Id.Entry}' failed to select a card.");
        shownCardIds.Add(RandomCardToCopy.Id);
        ((StringVar)DynamicVars[RandomCardKey]).StringValue = RandomCardToCopy.Title;
    }

    private static float GetSelectionWeight(CardModel? card)
    {
        if (card == null)
        {
            return 0F;
        }

        bool isBasicStrikeOrDefend = card.Rarity == CardRarity.Basic &&
                                     (card.Tags.Contains(CardTag.Strike) ||
                                      card.Tags.Contains(CardTag.Defend));
        return isBasicStrikeOrDefend ? BasicStrikeOrDefendWeight : DefaultCardWeight;
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
