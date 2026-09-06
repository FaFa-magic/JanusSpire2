using Godot;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interactions.RightClick;

namespace JanusSpire2.JanusSpire2Code.Nodes;

public partial class NDiaryCardPileScreen : Control, ICapstoneScreen
{
    public const string ScenePath = "res://JanusSpire2/scenes/screens/diary_card_pile_screen.tscn";
    private const int CardsPerSide = 9;
    private const int CardsPerSpread = CardsPerSide * 2;
    private readonly List<CardModel> _cards = [];
    private readonly List<Control> _slots = [];
    private readonly List<(GodotObject Source, StringName Signal, Callable Handler)> _focusConnections = [];
    private readonly NGridCardHolder?[] _holders = new NGridCardHolder?[CardsPerSpread];
    private readonly bool[] _glowing = new bool[CardsPerSpread];
    private ModCardPileOpenContext? _context;
    private NDiaryPageArrow _previous = null!;
    private NDiaryPageArrow _next = null!;
    private Button _back = null!;
    private Label _pageLabel = null!;
    private Label _countLabel = null!;
    private Label _emptyLabel = null!;
    private string[] _closeHotkeys = [];
    private int _page;
    private bool _refreshPending;
    private bool _closed;
    private NInspectCardScreen? _inspectScreen;

    public CardPile Pile => _context!.Pile;
    public NetScreenType ScreenType => NetScreenType.CardPile;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _back;

    public static NDiaryCardPileScreen Create(ModCardPileOpenContext context)
    {
        var screen = ResourceLoader.Load<PackedScene>(ScenePath).Instantiate<NDiaryCardPileScreen>();
        screen._context = context;
        return screen;
    }

    public override void _Ready()
    {
        _previous = GetNode<NDiaryPageArrow>("PreviousPage");
        _next = GetNode<NDiaryPageArrow>("NextPage");
        _back = GetNode<Button>("BackButton");
        _pageLabel = GetNode<Label>("PageLabel");
        _countLabel = GetNode<Label>("CardCount");
        _emptyLabel = GetNode<Label>("EmptyLabel");
        foreach (string side in new[] { "LeftPage", "RightPage" })
        {
            foreach (Control slot in GetNode<GridContainer>($"{side}/Margin/CardSlots").GetChildren())
            {
                _slots.Add(slot);
                slot.Resized += LayoutCards;
            }
        }

        // The scene can also be opened on its own for layout editing, without a combat model.
        if (_context == null)
            return;

        GetNode<Label>("Title").Text = PileText("title");
        GetNode<RichTextLabel>("BottomLabel").Text = "[center]" + PileText("info") + "[/center]";
        _emptyLabel.Text = PileText("empty");
        _back.Text = PileText("back");
        _previous.TooltipText = PileText("previous_page");
        _next.TooltipText = PileText("next_page");
        _back.Pressed += Close;
        _back.GuiInput += HandleBackInput;
        _previous.Connect(NClickableControl.SignalName.Released,
            Callable.From<NClickableControl>(_ => PageLeft()));
        _next.Connect(NClickableControl.SignalName.Released,
            Callable.From<NClickableControl>(_ => PageRight()));

        Pile.ContentsChanged += QueueRefresh;
        _closeHotkeys = new[] { MegaInput.cancel.ToString(), MegaInput.pauseAndBack.ToString(), MegaInput.back.ToString() }
            .Concat(_context.Definition.Hotkeys ?? []).Distinct().ToArray();
        foreach (string hotkey in _closeHotkeys)
            NHotkeyManager.Instance?.PushHotkeyReleasedBinding(hotkey, Close);
        NHotkeyManager.Instance?.PushHotkeyReleasedBinding(MegaInput.viewDeckAndTabLeft, PageLeft);
        NHotkeyManager.Instance?.PushHotkeyReleasedBinding(MegaInput.viewExhaustPileAndTabRight, PageRight);
        RefreshCards();
    }

    private string PileText(string suffix) =>
        new LocString(ModCardPileSpec.HoverTipLocTable, $"{_context!.Definition.Id}.{suffix}").GetFormattedText();

    private bool CanInteract => !_closed && IsVisibleInTree() && ActiveScreenContext.Instance.IsCurrent(this);

    private void QueueRefresh()
    {
        // A pile can change several times within one action. Rebuild after those mutations,
        // never while a right-click signal is still dispatching through one of our holders.
        if (_refreshPending || _closed)
            return;
        _refreshPending = true;
        Callable.From(() =>
        {
            _refreshPending = false;
            if (!_closed && IsInsideTree())
                RefreshCards();
        }).CallDeferred();
    }

    private void RefreshCards()
    {
        // The inspect screen owns the current hover tips and focus. Catch up after it closes.
        if (_inspectScreen is { Visible: true })
            return;
        foreach (CardModel card in _cards)
            card.Upgraded -= QueueRefresh;
        _cards.Clear();
        // Sort a presentation snapshot only; the synchronized pile keeps its original order.
        _cards.AddRange(Pile.Cards.OrderBy(card => RarityOrder(card.Rarity))
            .ThenBy(card => TypeOrder(card.Type)).ThenBy(card => card.Id.Entry, StringComparer.Ordinal));
        foreach (CardModel card in _cards)
            card.Upgraded += QueueRefresh;
        _page = Math.Clamp(_page, 0, PageCount - 1);
        ShowPage();
    }

    private int PageCount => Math.Max(1, (_cards.Count + CardsPerSpread - 1) / CardsPerSpread);

    private void PageLeft() => ChangePage(-1);
    private void PageRight() => ChangePage(1);

    private void ChangePage(int direction)
    {
        if (!CanInteract)
            return;
        int next = Math.Clamp(_page + direction, 0, PageCount - 1);
        if (next == _page)
            return;
        Control? focus = GetViewport().GuiGetFocusOwner();
        if (focus != null && IsAncestorOf(focus))
            focus.ReleaseFocus();
        _page = next;
        SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
        ShowPage();
        // Keep controller navigation on the page controls, never on a newly assigned card.
        if (NControllerManager.Instance?.IsUsingDirectionalNavigation == true)
        {
            NDiaryPageArrow arrow = direction < 0 ? _previous : _next;
            (arrow.IsEnabled ? (Control)arrow : _back).GrabFocus();
        }
    }

    private void ShowPage()
    {
        NHoverTipSet.Clear();
        Control? focus = GetViewport().GuiGetFocusOwner();
        for (int i = 0; i < CardsPerSpread; i++)
        {
            var holder = _holders[i];
            int index = _page * CardsPerSpread + i;
            if (index >= _cards.Count)
            {
                if (holder != null)
                    holder.Hide();
                continue;
            }

            CardModel card = _cards[index];
            if (holder == null)
            {
                // Only presentation nodes are created. No card is generated, moved, or registered.
                NCard? cardNode = NCard.Create(card);
                if (cardNode == null)
                    continue;
                holder = NGridCardHolder.Create(cardNode);
                if (holder == null)
                {
                    cardNode.QueueFreeSafely();
                    continue;
                }
                _holders[i] = holder;
                holder.Connect(NCardHolder.SignalName.Pressed, Callable.From<NCardHolder>(OnHolderPressed));
                holder.Connect(NCardHolder.SignalName.AltPressed, Callable.From<NCardHolder>(OnHolderAltPressed));
                Control slot = _slots[i];
                slot.GetNode<Control>("CardAnchor").AddChildSafely(holder);
                ConnectFocus(holder.Hitbox, NClickableControl.SignalName.Focused,
                    Callable.From<NClickableControl>(_ => slot.ZIndex = 1));
                ConnectFocus(holder.Hitbox, NClickableControl.SignalName.Unfocused,
                    Callable.From<NClickableControl>(_ => slot.ZIndex = 0));
                ConnectFocus(holder, Control.SignalName.FocusEntered, Callable.From(() => slot.ZIndex = 1));
                ConnectFocus(holder, Control.SignalName.FocusExited, Callable.From(() => slot.ZIndex = 0));
            }
            else
            {
                holder.Show();
                holder.ReassignToCard(card, Pile.Type, null, MegaCrit.Sts2.Core.Entities.UI.ModelVisibility.Visible);
            }

            holder.Scale = holder.SmallScale;
            holder.CardNode!.UpdateVisuals(Pile.Type, CardPreviewMode.Normal);
            holder.CardNode.CardHighlight.AnimHideInstantly();
            _glowing[i] = false;
        }

        _previous.SetEnabled(_page > 0);
        _next.SetEnabled(_page < PageCount - 1);
        _previous.Modulate = _previous.IsEnabled ? Colors.White : new Color(1, 1, 1, 0.25f);
        _next.Modulate = _next.IsEnabled ? Colors.White : new Color(1, 1, 1, 0.25f);
        _pageLabel.Text = $"{_page + 1} / {PageCount}";
        var countText = new LocString("card_library", "CARD_COUNT");
        countText.Add("Amount", _cards.Count);
        _countLabel.Text = countText.GetFormattedText();
        _emptyLabel.Visible = _cards.Count == 0;
        LayoutCards();
        UpdateNavigation();
        RefreshGlow();
        if (focus != null && IsAncestorOf(focus) && CanInteract &&
            (!focus.IsVisibleInTree() || focus.FocusMode == FocusModeEnum.None))
            _back.GrabFocus();
    }

    private void LayoutCards()
    {
        foreach (Control slot in _slots)
        {
            var anchor = slot.GetNode<Control>("CardAnchor");
            anchor.Position = slot.Size * 0.5f;
            // NCardHolder animates its own scale between 0.8 and 1. Scale its parent so
            // hovering cannot reset these smaller three-row cards to full-size cards.
            float scale = Math.Max(0.1f, Math.Min(slot.Size.X / 315f, slot.Size.Y / 355f));
            anchor.Scale = Vector2.One * scale;
        }
    }

    public override void _Process(double delta)
    {
        if (_context != null && !_closed)
            RefreshGlow();
    }

    private void RefreshGlow()
    {
        for (int i = 0; i < _holders.Length; i++)
        {
            if (_holders[i] is not { Visible: true, CardNode: { } cardNode } holder)
                continue;
            bool glow = InspirationKeyword.ShouldGlow(holder.CardModel);
            if (_glowing[i] == glow)
                continue;
            _glowing[i] = glow;
            if (glow)
            {
                cardNode.CardHighlight.Modulate = NCardHighlight.playableColor;
                cardNode.CardHighlight.AnimShow();
            }
            else
                cardNode.CardHighlight.AnimHide();
        }
    }

    private void OnHolderPressed(NCardHolder holder)
    {
        if (CanInteract && holder.CardModel is { } card && Pile.Cards.Contains(card))
            Inspect(card);
    }

    private void OnHolderAltPressed(NCardHolder holder)
    {
        if (!CanInteract || holder.CardModel is not { } card || !ReferenceEquals(card.Pile, Pile))
            return;
        var viewport = GetViewport();
        var player = LocalContext.GetMe(card.CombatState);
        if (player != null && NPlayerHand.Instance is { InCardPlay: false } &&
            NTargetManager.Instance is { IsInSelection: false })
        {
            var trigger = new ModRightClickTrigger(
                NControllerManager.Instance?.IsUsingDirectionalNavigation ?? false,
                null, ModRightClickSource.CombatPileCard, Pile.Type);
            if (ModRightClickRegistry.TryDispatch(new(player, card, trigger)))
            {
                // Inspiration closes this screen before submitting its synchronized action.
                viewport.SetInputAsHandled();
                return;
            }
        }
        if (!_closed)
            Inspect(card);
    }

    private void Inspect(CardModel card)
    {
        var snapshot = _cards.Where(Pile.Cards.Contains).ToList();
        int index = snapshot.IndexOf(card);
        if (index < 0 || NGame.Instance == null)
            return;
        _inspectScreen = NGame.Instance.GetInspectCardScreen();
        _inspectScreen.VisibilityChanged -= OnInspectVisibilityChanged;
        _inspectScreen.VisibilityChanged += OnInspectVisibilityChanged;
        _back.Disabled = true;
        _previous.Disable();
        _next.Disable();
        _inspectScreen.Open(snapshot, index);
    }

    private void OnInspectVisibilityChanged()
    {
        if (_inspectScreen is not { Visible: false })
            return;
        _inspectScreen.VisibilityChanged -= OnInspectVisibilityChanged;
        _inspectScreen = null;
        if (_closed)
            return;
        _back.Disabled = false;
        RefreshCards();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!CanInteract || inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
            return;
        if (key.Keycode == Key.Pageup)
            PageLeft();
        else if (key.Keycode == Key.Pagedown)
            PageRight();
        else
            return;
        GetViewport().SetInputAsHandled();
    }

    private void HandleBackInput(InputEvent input)
    {
        if (!CanInteract || input.IsEcho() || !input.IsActionPressed(MegaInput.select) || input.IsActionPressed("ui_accept"))
            return;
        Close();
        _back.AcceptEvent();
    }

    private void UpdateNavigation()
    {
        var visible = _holders.Select((holder, index) => (holder, index))
            .Where(entry => entry.holder is { Visible: true }).ToList();
        foreach (var (holder, index) in visible)
        {
            var (row, column) = Cell(index);
            Control Neighbor(int dr, int dc, Control fallback) => visible
                .FirstOrDefault(entry => Cell(entry.index) == (row + dr, column + dc)).holder ?? fallback;
            holder!.FocusNeighborLeft = Neighbor(0, -1, _previous.IsEnabled ? _previous : holder).GetPath();
            holder.FocusNeighborRight = Neighbor(0, 1, _next.IsEnabled ? _next : holder).GetPath();
            holder.FocusNeighborTop = Neighbor(-1, 0, holder).GetPath();
            holder.FocusNeighborBottom = Neighbor(1, 0, _back).GetPath();
        }
        if (visible.FirstOrDefault().holder is { } first)
        {
            _back.FocusNeighborTop = first.GetPath();
            _previous.FocusNeighborRight = first.GetPath();
            _next.FocusNeighborLeft = first.GetPath();
        }
    }

    private static (int Row, int Column) Cell(int index) =>
        (index % CardsPerSide / 3, index % 3 + index / CardsPerSide * 3);

    private void Close()
    {
        if (CanInteract && ReferenceEquals(NCapstoneContainer.Instance?.CurrentCapstoneScreen, this))
            NCapstoneContainer.Instance.Close();
    }

    public void AfterCapstoneOpened() => Show();

    public void AfterCapstoneClosed()
    {
        _closed = true;
        Hide();
        DetachEvents();
        this.QueueFreeSafely();
    }

    public override void _ExitTree()
    {
        _closed = true;
        DetachEvents();
        foreach (Control slot in _slots)
            slot.Resized -= LayoutCards;
        foreach (var (source, signal, handler) in _focusConnections)
            if (GodotObject.IsInstanceValid(source))
                source.Disconnect(signal, handler);
        _focusConnections.Clear();
        // These are pooled vanilla holders. Disconnect callbacks before returning them.
        foreach (var holder in _holders)
        {
            if (holder == null || !GodotObject.IsInstanceValid(holder))
                continue;
            holder.Disconnect(NCardHolder.SignalName.Pressed, Callable.From<NCardHolder>(OnHolderPressed));
            holder.Disconnect(NCardHolder.SignalName.AltPressed, Callable.From<NCardHolder>(OnHolderAltPressed));
            holder.QueueFreeSafely();
        }
    }

    private void ConnectFocus(GodotObject source, StringName signal, Callable handler)
    {
        source.Connect(signal, handler);
        _focusConnections.Add((source, signal, handler));
    }

    private void DetachEvents()
    {
        if (_context == null)
            return;
        Pile.ContentsChanged -= QueueRefresh;
        foreach (CardModel card in _cards)
            card.Upgraded -= QueueRefresh;
        foreach (string hotkey in _closeHotkeys)
            NHotkeyManager.Instance?.RemoveHotkeyReleasedBinding(hotkey, Close);
        NHotkeyManager.Instance?.RemoveHotkeyReleasedBinding(MegaInput.viewDeckAndTabLeft, PageLeft);
        NHotkeyManager.Instance?.RemoveHotkeyReleasedBinding(MegaInput.viewExhaustPileAndTabRight, PageRight);
        if (_inspectScreen != null && GodotObject.IsInstanceValid(_inspectScreen))
            _inspectScreen.VisibilityChanged -= OnInspectVisibilityChanged;
    }

    private static int RarityOrder(CardRarity rarity) => rarity switch
    {
        CardRarity.Ancient => 0, CardRarity.Rare => 1, CardRarity.Uncommon => 2,
        CardRarity.Common => 3, CardRarity.Basic => 4, CardRarity.Status => 5,
        CardRarity.Curse => 6, CardRarity.Event => 7, CardRarity.Quest => 8,
        CardRarity.Token => 9, _ => 10
    };

    private static int TypeOrder(CardType type) => type switch
    {
        CardType.Power => 0, CardType.Attack => 1, CardType.Skill => 2,
        CardType.Status => 3, CardType.Curse => 4, CardType.Quest => 5, _ => 6
    };
}
