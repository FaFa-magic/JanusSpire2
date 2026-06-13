using System.Reflection;
using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Basic;
using JanusSpire2.JanusSpire2Code.Configs;
using JanusSpire2.JanusSpire2Code.Relics;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace JanusSpire2.JanusSpire2Code;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
	public const string ModId = "JanusSpire2";

	public static Logger Logger { get; private set; } = null!;
		
	public static void Initialize()
	{
		var assembly = Assembly.GetExecutingAssembly();
		
		Logger = RitsuLibFramework.CreateLogger(ModId);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
		RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

		JanusConfigPage.Register();
		
		RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<BlackCatAssault, BlackCatUnleash>();
		RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<Coronet, ShiningCrown>();
		// var patcher = RitsuLibFramework.CreatePatcher(ModId, "core-patches");
		// patcher.RegisterPatch<JanusPatches>();
		//
		// if (!patcher.PatchAll())
		// 	throw new InvalidOperationException("Critical patches failed.");
	}
	
	private static void DisableMod()
	{
		// Mark your own mod disabled when a required patch cannot apply.
	}
}
