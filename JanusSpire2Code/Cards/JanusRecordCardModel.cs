using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards;

public abstract class JanusRecordCardModel : JanusCardModel, ICardOverlayContributor
{
    private const string CanTakeIconPath =
        "res://JanusSpire2/images/combatui/collection.png";
    private const string CanTakeOverlayId = "janus_record_can_take";
    private const float CanTakeIconSize = 108F;

    private static readonly Vector2 CanTakeIconPosition = new(70F, -230F);

    private static Texture2D? _canTakeTexture;
    private bool _canTake;

    [SavedProperty]
    public int RecordMappingKey { get; set; }

    [SavedProperty]
    public bool CanTake
    {
        get => _canTake;
        protected set
        {
            AssertMutable();

            if (_canTake == value)
            {
                return;
            }

            _canTake = value;
            this.RequestVisualReload();
        }
    }

    public JanusRecordCardModel(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public async Task EnableTake()
    {
        CanTake = true;
        await RecordExtraHandManager.SyncFor(this);
    }

    public async Task DisableTake()
    {
        CanTake = false;
        await RecordExtraHandManager.SyncFor(this);
    }

    public IEnumerable<CardOverlayContribution> GetCardOverlays(CardOverlayContext context)
    {
        if (!CanTake || !ReferenceEquals(context.Card, this))
        {
            return [];
        }

        return
        [
            CardOverlayContribution.FromFactory(
                CanTakeOverlayId,
                static _ => CreateCanTakeIcon(),
                order: 1000,
                fullRect: false)
        ];
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
        {
            await DisableTake();
        }
    }

    private static TextureRect CreateCanTakeIcon()
    {
        return new TextureRect
        {
            Name = "JanusRecordCanTakeIcon",
            Position = CanTakeIconPosition,
            Size = new Vector2(CanTakeIconSize, CanTakeIconSize),
            PivotOffset = new Vector2(CanTakeIconSize * 0.5F, CanTakeIconSize * 0.5F),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = LoadCanTakeTexture()
        };
    }

    private static Texture2D? LoadCanTakeTexture()
    {
        if (_canTakeTexture == null || !GodotObject.IsInstanceValid(_canTakeTexture))
        {
            _canTakeTexture = ResourceLoader.Load<Texture2D>(
                CanTakeIconPath,
                null,
                ResourceLoader.CacheMode.Ignore);
        }

        return _canTakeTexture;
    }
}
