using System.Reflection;
using MegaCrit.Sts2.addons.mega_text;
using JanusSpire2.JanusSpire2Code.Cards;
using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Basic;
using JanusSpire2.JanusSpire2Code.Cards.Uncommon;
using JanusSpire2.JanusSpire2Code.Configs;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Patches;
using JanusSpire2.JanusSpire2Code.Powers;
using JanusSpire2.JanusSpire2Code.Relics;
using JanusSpire2.Scripts.Telemetry;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace JanusSpire2.JanusSpire2Code;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
	public const string ModId = "JanusSpire2";

	public static Logger Logger { get; private set; } = null!;
		
	public static PileType Diary;
	public static PileType RecordMappingStorage;
	public static PileType RecordExtraHand;
	
	public static void Initialize()
	{
		var assembly = Assembly.GetExecutingAssembly();
		
		Logger = RitsuLibFramework.CreateLogger(ModId);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
		RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
		JanusKeywords.RegisterPersistence();

		JanusConfigPage.Register();
		JanusTelemetry.Register();
		
		RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<BlackCatAssault, BlackCatUnleash>();
		RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<Coronet, ShiningCrown>();
		BlackCatSealPower.RegisterSynchronizedRightClick();
		InspirationKeyword.RegisterSynchronizedRightClick();
		StickerMergeAction.Register();
		
		var registry = ModCardPileRegistry.For(ModId);
		Diary = registry.RegisterOwned("diary_pile", new ModCardPileSpec
		{
			// CombatOnly：每次战斗创建，战斗结束时销毁
			// RunPersistent：同一局游戏内可跨战斗保留（仅存于内存，需自行写入存档）
			Scope = ModCardPileScope.CombatOnly,
			// Headless：不可见
			// TopBarDeck：顶栏牌组按钮旁
			// BottomLeft：战斗UI左下（抽牌堆附近）
			// BottomRight：战斗UI右下（消耗堆附近）
			// ExtraHand：额外手牌容器
			Style = ModCardPileUiStyle.BottomLeft,
			Anchor = ModCardPileAnchor.Default,
			IconPath = "res://JanusSpire2/images/piles/Diary.png",
			// 点击打开
			OnOpen = OpenDiaryPile,
			VisibleWhen = ctx => ctx.Player != null && ctx.Pile is { Cards.Count: > 0 },
		}).PileType;

		RecordMappingStorage = registry.RegisterOwned("record_mapping_storage", new ModCardPileSpec
		{
			Scope = ModCardPileScope.CombatOnly,
			Style = ModCardPileUiStyle.Headless,
			CardShouldBeVisible = false,
		}).PileType;

		RecordExtraHand = registry.RegisterOwned("record_extra_hand", new ModCardPileSpec
		{
			Scope = ModCardPileScope.CombatOnly,
			Style = ModCardPileUiStyle.ExtraHand,
			Anchor = new ModCardPileAnchor(ModCardPileAnchorKind.ExtraHandAbove, default),
			CardShouldBeVisible = true,
			ExtraHand = new ModCardPileExtraHandSpec
			{
				Direction = ModExtraHandLayoutDirection.VanillaHand,
				ShowPlayableGlow = true,
				AllowCardPlay = true,
				// The projection is playable through RitsuLib, but it is not a real hand card:
				// do not apply hand end-of-turn rules or flush it with the vanilla hand.
				Behaviors = ModExtraHandBehavior.None,
			},
			VisibleWhen = ctx => ctx.Player != null,
		}).PileType;
		
		ModPatcher patcher = RitsuLibFramework.CreatePatcher(ModId, "janus_patches");
		patcher.RegisterPatch<CheckForEmptyHandPatch>();
		patcher.RegisterPatch<DiaryHasEnoughResourcesPatch>();
		patcher.RegisterPatch<DiarySpendResourcesPatch>();
		patcher.RegisterPatch<PlayerPopulateCombatStatePatch>();
		patcher.RegisterPatch<DiaryOnPlayWrapperPatch>();
		patcher.RegisterPatch<PerkDiarySelectionRightClickPatch>();
		patcher.RegisterPatch<PreventSingleCardGenerationPatch>();
		patcher.RegisterPatch<PreventMultipleCardGenerationPatch>();
		patcher.RegisterPatch<SwiftStatusAndCurseEnchantPatch>();
		patcher.RegisterPatch<SwiftCombatStackVisualRefreshPatch>();
		patcher.RegisterPatch<RemovedRelicRewardAnimationPatch>();
		patcher.RegisterPatch<UnceasingTopEmptyHandPatch>();
		patcher.RegisterPatch<UnceasingTopCombatStartPatch>();
		patcher.RegisterPatch<RecordMappingAllCardsPatch>();
		patcher.RegisterPatch<RecordMappingHookListenersPatch>();
		patcher.RegisterPatch<RecordMappingPileHookPatch>();
		patcher.RegisterPatch<RecordMappingCanPlayPatch>();
		patcher.RegisterPatch<RecordMappingSpendResourcesPatch>();
		patcher.RegisterPatch<RecordMappingPlayBridgePatch>();
		patcher.RegisterPatch<RecordMappingDescriptionPatch>();
		patcher.RegisterPatch<RecordMappingDynamicVarPreviewPatch>();
		patcher.RegisterPatch<RecordMappingDynamicVarPreviewResetPatch>();
		patcher.RegisterPatch<RecordMappingEnchantmentVisualPatch>();
		patcher.RegisterPatch<InspirationPileGlowInitPatch>();
		patcher.RegisterPatch<InspirationPileGlowPatch>();

		if (!patcher.PatchAll())
			throw new InvalidOperationException("Critical patches failed.");
	}

	private static void OpenDiaryPile(ModCardPileOpenContext context)
	{
		context.ShowDefaultPileScreen();
		if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is not NCardPileScreen screen)
		{
			return;
		}

		MegaRichTextLabel bottomLabel = screen.GetNode<MegaRichTextLabel>("%BottomLabel");
		bottomLabel.Text = "[center]" + new LocString(
			ModCardPileSpec.HoverTipLocTable,
			$"{context.Definition.Id}.info").GetFormattedText();
		bottomLabel.Visible = true;

		NCardGrid grid = screen.GetNode<NCardGrid>("CardGrid");
		void RefreshDiaryViewOrder()
		{
			if (!Godot.GodotObject.IsInstanceValid(grid))
			{
				return;
			}

			List<CardModel> cards = context.Pile.Cards
				.OrderBy(card => GetDiaryRarityOrder(card.Rarity))
				.ThenBy(card => GetDiaryTypeOrder(card.Type))
				.ThenBy(card => card.Id.Entry, StringComparer.Ordinal)
				.ToList();
			grid.SetCards(cards, Diary, [SortingOrders.Ascending]);
		}

		void OnScreenTreeExiting()
		{
			context.Pile.ContentsChanged -= RefreshDiaryViewOrder;
			screen.TreeExiting -= OnScreenTreeExiting;
		}

		context.Pile.ContentsChanged += RefreshDiaryViewOrder;
		screen.TreeExiting += OnScreenTreeExiting;
		RefreshDiaryViewOrder();
	}

	private static int GetDiaryRarityOrder(CardRarity rarity)
	{
		return rarity switch
		{
			CardRarity.Ancient => 0,
			CardRarity.Rare => 1,
			CardRarity.Uncommon => 2,
			CardRarity.Common => 3,
			CardRarity.Basic => 4,
			CardRarity.Status => 5,
			CardRarity.Curse => 6,
			CardRarity.Event => 7,
			CardRarity.Quest => 8,
			CardRarity.Token => 9,
			_ => 10,
		};
	}

	private static int GetDiaryTypeOrder(CardType type)
	{
		return type switch
		{
			CardType.Power => 0,
			CardType.Attack => 1,
			CardType.Skill => 2,
			CardType.Status => 3,
			CardType.Curse => 4,
			CardType.Quest => 5,
			_ => 6,
		};
	}
}
