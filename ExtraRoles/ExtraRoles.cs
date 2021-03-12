using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.IL2CPP;
using ExtraRoles.Commands;
using ExtraRoles.Roles;
using HarmonyLib;
using Reactor;
using XenoCore;
using XenoCore.Commands;
using XenoCore.CustomOptions;
using XenoCore.Events;
using XenoCore.Locale;
using XenoCore.Network;
using XenoCore.Utils;

namespace ExtraRoles {
	[BepInPlugin(Id)]
	[BepInProcess(Globals.PROCESS)]
	[BepInDependency(ReactorPlugin.Id)]
	[BepInDependency(XenoPlugin.Id)]
	// ReSharper disable once ClassNeverInstantiated.Global
	public class ExtraRoles : BasePlugin {
		public const string Id = "com.mishin870.extraroles";
		public static readonly string Version = "1.0.4";

		public Harmony Harmony { get; } = new Harmony(Id);
		
		public static readonly List<DeadPlayer> KilledPlayers = new List<DeadPlayer>();

		public static readonly OptionGroup ER_GROUP = CustomOption.DEFAULT_GROUP.Up(20);

		public static readonly CustomToggleOption ConfirmExtraRoles = CustomOption
			.AddToggle("er.exile.confirm", true);
		public static readonly CustomToggleOption ShowExtraRoles = CustomOption
			.AddToggle("er.role.show", false);

		public override void Load() {
			Harmony.PatchAll();

			ConfirmExtraRoles.Group = ER_GROUP;
			ShowExtraRoles.Group = ER_GROUP;

			LanguageManager.Load(Assembly.GetExecutingAssembly(), "ExtraRoles.Lang.");
			Role.Load();
			VersionsList.Add("ExtraRoles", Version, true);
			
			CommandsController.Register(new WinCommand());
			
			HandleRpcPatch.AddListener(new RPCExtraRoles());
			
			EventsController.RESET_ALL.Register(() => {
				Role.ResetAll();
				KilledPlayers.Clear();
			});
		}
	}
	
	[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
	public class SingleStartGamePatch {
		public static void Postfix(GameStartManager __instance) {
			__instance.MinPlayers = 1;
			__instance.StartButton.color = Palette.EnabledColor;
		}
	}
}