using ExtraRoles.Roles;
using HarmonyLib;
using XenoCore.Locale;

namespace ExtraRoles {
	[HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
	public class IntroCutscenePath {
		public static void Prefix(IntroCutscene.CoBegin__d __instance) {
			foreach (var Role in Role.ROLES) {
				Role.Intro(__instance);
			}
		}

		public static void Postfix(IntroCutscene.CoBegin__d __instance) {
			foreach (var Role in Role.ROLES) {
				if (Role.Player == PlayerControl.LocalPlayer) {
					__instance.__this.Title.Text = LanguageManager.Get($"er.{Role.Id}");
					__instance.__this.Title.Color = Role.Color;
					__instance.__this.ImpostorText.Text = LanguageManager.Get($"er.{Role.Id}.desc");
					__instance.__this.BackgroundBar.material.color = Role.Color;
				}
			}
			
			HudManager.Instance.Chat.SetVisible(true);
		}
	}
}