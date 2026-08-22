using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;
using Godot;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards;

public abstract class JanusRecordCardModel :
    JanusCardModel,
    IModRightClickableCard,
    ICardOverlayContributor
{
    private const string CanTakeIconPath =
        "res://JanusSpire2/images/combatui/collection.png";
    private const string CanTakeOverlayId = "janus_record_can_take";
    private const float CanTakeIconSize = 96F;

    private static readonly Vector2 CanTakeIconPosition = new(70F, -254F);

    private static Texture2D? _canTakeTexture;
    private bool _canTake;

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

    public void EnableTake()
    {
        CanTake = true;
    }

    public void DisableTake()
    {
        CanTake = false;
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

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return context.Model == this &&
               context.Player == Owner &&
               context.Trigger.Source == ModRightClickSource.CombatPileCard &&
               context.Trigger.ExpectedCardPile == MainFile.Diary;
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context)
    {
        return context.Model == this &&
               context.Player == Owner &&
               context.Trigger.Source == ModRightClickSource.CombatPileCard &&
               Pile?.Type == MainFile.Diary &&
               CanTake &&
               HasRoomInHand();
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (!CanExecuteRightClick(context))
        {
            return;
        }

        await CardPileCmd.Add(this, PileType.Hand);
        DisableTake();
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
        {
            DisableTake();
        }

        return Task.CompletedTask;
    }

    private bool HasRoomInHand()
    {
        return PileType.Hand.GetPile(Owner).Cards.Count < CardPile.MaxCardsInHand;
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
            Texture = LoadCanTakeTexture(),
            ZIndex = 100
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
