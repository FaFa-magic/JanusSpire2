using System.Reflection;
using MegaCrit.Sts2.addons.mega_text;
using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Basic;
using JanusSpire2.JanusSpire2Code.Configs;
using JanusSpire2.JanusSpire2Code.Patches;
using JanusSpire2.JanusSpire2Code.Relics;
using JanusSpire2.Scripts.Telemetry;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
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
	
	public static void Initialize()
	{
		var assembly = Assembly.GetExecutingAssembly();
		
		Logger = RitsuLibFramework.CreateLogger(ModId);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
		RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

		JanusConfigPage.Register();
		JanusTelemetry.Register();
		
		RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<BlackCatAssault, BlackCatUnleash>();
		RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<Coronet, ShiningCrown>();
		
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
			VisibleWhen = ctx => ctx.Player != null,
		}).PileType;
		
		ModPatcher patcher = RitsuLibFramework.CreatePatcher(ModId, "janus_patches");
		patcher.RegisterPatch<CheckForEmptyHandPatch>();
		patcher.RegisterPatch<SkipPlayerFlushPatch>();
		patcher.RegisterPatch<EnemyTurnFlushPatch>();
		patcher.RegisterPatch<DiaryHasEnoughResourcesPatch>();
		patcher.RegisterPatch<DiarySpendResourcesPatch>();
		patcher.RegisterPatch<PlayerPopulateCombatStatePatch>();
		patcher.RegisterPatch<DiaryOnPlayWrapperPatch>();
		patcher.RegisterPatch<PerkDiarySelectionRightClickPatch>();
		patcher.RegisterPatch<HolyNightDiaryLocationRecoveryPatch>();
		patcher.RegisterPatch<PreventSingleCardGenerationPatch>();
		patcher.RegisterPatch<PreventMultipleCardGenerationPatch>();
		patcher.RegisterPatch<SwiftStatusAndCurseEnchantPatch>();
		patcher.RegisterPatch<SwiftCombatStackVisualRefreshPatch>();
		patcher.RegisterPatch<RemovedRelicRewardAnimationPatch>();
		patcher.RegisterPatch<UnceasingTopEmptyHandPatch>();
		patcher.RegisterPatch<UnceasingTopCombatStartPatch>();
		patcher.RegisterPatch<ForcedPotionTargetingPatch>();

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
	}
}
