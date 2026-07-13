using System.Reflection;
using Godot;
using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Basic;
using JanusSpire2.JanusSpire2Code.Configs;
using JanusSpire2.JanusSpire2Code.Patches;
using JanusSpire2.JanusSpire2Code.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Modding;
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
			Style = ModCardPileUiStyle.TopBarDeck,
			Anchor = new ModCardPileAnchor(
				ModCardPileAnchorKind.BottomLeftSecondary,
				new Vector2(0, -2)),
			IconPath = "res://JanusSpire2/images/piles/Diary.png",
			// 点击打开
			OnOpen = ctx => ctx.ShowDefaultPileScreen(),
			VisibleWhen = ctx => ctx.Player != null,
		}).PileType;
		
		ModPatcher patcher = RitsuLibFramework.CreatePatcher(ModId, "janus_patches");
		patcher.RegisterPatch<CheckForEmptyHandPatch>();
		patcher.RegisterPatch<SkipPlayerFlushPatch>();
		patcher.RegisterPatch<EnemyTurnFlushPatch>();
		patcher.RegisterPatch<DiaryHasEnoughResourcesPatch>();
		patcher.RegisterPatch<DiarySpendResourcesPatch>();

		if (!patcher.PatchAll())
			throw new InvalidOperationException("Critical patches failed.");
	}
}
