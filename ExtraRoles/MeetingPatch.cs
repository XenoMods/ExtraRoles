using HarmonyLib;
using ExtraRoles.Roles;
using XenoCore.Locale;

namespace ExtraRoles {
	[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Method_7))]
	public class MeetingPopulatePlayersPatch {
		public static void Prefix(MeetingHud __instance, GameData.PlayerInfo PPIKPNJEAKJ) {
			foreach (var Role in Role.ROLES) {
				Role.MeetingBeforeCreatePlayer(__instance, PPIKPNJEAKJ);
			}
		}
	}
	
	[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
	public class MeetingStartPatch {
		public static void Postfix(MeetingHud __instance) {
			foreach (var Role in Role.ROLES) {
				Role.MeetingStart(__instance);
			}
		}
	}
	
	[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleProceed))]
	public class MeetingProceedPatch {
		public static void Postfix(MeetingHud __instance) {
			foreach (var Role in Role.ROLES) {
				Role.MeetingProceed(__instance);
			}
		}
	}
	
	[HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
	public class MeetingEnd {
		public static void Postfix(ExileController __instance) {
			if (ExtraRoles.ConfirmExtraRoles.GetValue() && !__instance.exiled.IsImpostor) {
				foreach (var Role in Role.ROLES) {
					if (Role.Player == null) continue;
						
					if (Role.Player.PlayerId == __instance.exiled.PlayerId) {
						__instance.completeString += LanguageManager.Get($"er.{Role.Id}.exile");
					}
				}
			}
			
			foreach (var Role in Role.ROLES) {
				Role.MeetingEnd(__instance);
			}
		}
	}
}