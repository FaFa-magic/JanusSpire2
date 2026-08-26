using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards;

/// <summary>
/// Presentation-only handle for a Record card that remains in the Diary.
/// It deliberately owns no Record keywords, dynamic variables, enchantments, or card hooks.
/// </summary>
public sealed class JanusRecordMappingCard : JanusCardModel,
    ICardEnergyCostContributor,
    ICardStarCostContributor,
    ICardPlayStateContributor
{
    [SavedProperty]
    public int OriginalRecordMappingKey { get; set; }

    public JanusRecordCardModel? Original { get; private set; }

    public override string Title => Original?.Title ?? string.Empty;

    public override CardType Type => Original?.Type ?? CardType.Skill;

    public override CardRarity Rarity => Original?.Rarity ?? CardRarity.Token;

    public override TargetType TargetType => Original?.TargetType ?? TargetType.None;

    public override CardPoolModel VisualCardPool => Original?.VisualCardPool ?? base.VisualCardPool;

    public override CardMultiplayerConstraint MultiplayerConstraint =>
        Original?.MultiplayerConstraint ?? CardMultiplayerConstraint.None;

    // Original.HoverTips below already supplies the Block tip when needed.
    public override bool GainsBlock => false;

    public override int CanonicalStarCost => Original?.CanonicalStarCost ?? -1;

    public override int CurrentStarCost => Original?.GetStarCostWithModifiers() ?? -1;

    public override bool HasStarCostX => Original?.HasStarCostX ?? false;

    public override CardAssetProfile AssetProfile => Original?.AssetProfile ?? CardAssetProfile.Empty;

    protected override int CanonicalEnergyCost => Original?.EnergyCost.Canonical ?? 0;

    protected override bool HasEnergyCostX => Original?.EnergyCost.CostsX ?? false;

    protected override bool IsPlayable => IsOriginalAvailable();

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => Original?.HoverTips ?? [];

    public JanusRecordMappingCard()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.None, false)
    {
    }

    internal void Bind(JanusRecordCardModel original)
    {
        Original = original;
        OriginalRecordMappingKey = original.RecordMappingKey;
        // Keep the presentation card's own cost in step with the Diary card as well.
        // Extra-hand UI and targeting can inspect the model's base cost before RitsuLib's
        // contributor is applied, so only delegating ModifyEnergyCost is not sufficient.
        EnergyCost.SetCustomBaseCost(original.EnergyCost.GetWithModifiers(CostModifiers.Local));
        this.RequestVisualReload();
    }

    public int ModifyEnergyCost(CardModel card, int currentCost, CostModifiers modifiers)
    {
        return Original?.EnergyCost.GetWithModifiers(modifiers) ?? currentCost;
    }

    public int ModifyStarCost(CardModel card, int currentCost)
    {
        return Original?.GetStarCostWithModifiers() ?? currentCost;
    }

    public bool? CanPlay(CardModel card)
    {
        return IsOriginalAvailable() && Original!.CanPlay();
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        string mappedDescription = Original?.GetDescriptionForPile(PileType.Hand) ?? string.Empty;
        description.Add("MappedDescription", mappedDescription);
    }

    private bool IsOriginalAvailable()
    {
        return Original is { CanTake: true } original && original.Pile?.Type == MainFile.Diary;
    }
}
