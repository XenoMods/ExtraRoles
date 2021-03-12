using ExtraRoles.Helpers;
using ExtraRoles.Roles;
using HarmonyLib;

namespace ExtraRoles {
	[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowInfectedMap))]
	public class MapOpenPatch {
		public static void Postfix(MapBehaviour __instance) {
			foreach (var Role in Role.ROLES) {
				Role.ShowInfectedMap(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
	public class MapUpdatePatch {
		public static void Postfix(MapBehaviour __instance) {
			foreach (var Role in Role.ROLES) {
				Role.UpdateMap(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(MapRoom), nameof(MapRoom.Method_41))]
	public class SabotageButtonDeactivatePatch {
		public static bool Prefix(MapRoom __instance, float DCEFKAOFGOG) {
			return !EngineerRole.INSTANCE.IsLocalPlayer();
		}
	}

	public static class SabotageCentralPatch {
		public static bool HandleSabotage(MapRoom Map, SabotageType Type) {
			foreach (var Role in Role.ROLES) {
				if (Role.ActivateSabotage(Map, Type)) {
					// If that role handles sabotage activation then cancel default
					return false;
				}
			}

			return true;
		}

		public static void FixLights() {
			var SwitchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical]
				.Cast<SwitchSystem>();
			SwitchSystem.ActualSwitches = SwitchSystem.ExpectedSwitches;
		}
	}

	#region SABOTAGE_TYPES
	[HarmonyPatch(typeof(MapRoom), nameof(MapRoom.SabotageReactor))]
	public class SabotageReactorPatch {
		public static bool Prefix(MapRoom __instance) {
			return SabotageCentralPatch.HandleSabotage(__instance, SabotageType.Reactor);
		}
	}

	[HarmonyPatch(typeof(MapRoom), nameof(MapRoom.SabotageLights))]
	public class SabotageLightsPatch {
		public static bool Prefix(MapRoom __instance) {
			return SabotageCentralPatch.HandleSabotage(__instance, SabotageType.Lights);
		}
	}

	[HarmonyPatch(typeof(MapRoom), nameof(MapRoom.SabotageComms))]
	public class SabotageCommsPatch {
		public static bool Prefix(MapRoom __instance) {
			return SabotageCentralPatch.HandleSabotage(__instance, SabotageType.Comms);
		}
	}

	[HarmonyPatch(typeof(MapRoom), nameof(MapRoom.SabotageOxygen))]
	public class SabotageOxyPatch {
		public static bool Prefix(MapRoom __instance) {
			return SabotageCentralPatch.HandleSabotage(__instance, SabotageType.Oxygen);
		}
	}

	[HarmonyPatch(typeof(MapRoom), nameof(MapRoom.SabotageSeismic))]
	public class SabotageSeismicPatch {
		public static bool Prefix(MapRoom __instance) {
			return SabotageCentralPatch.HandleSabotage(__instance, SabotageType.Seismic);
		}
	}
	#endregion
}