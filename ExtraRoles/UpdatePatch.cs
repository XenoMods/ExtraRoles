using ExtraRoles.Roles;
using ExtraRoles.Roles.Actions;
using HarmonyLib;
using UnityEngine;
using XenoCore.Utils;

namespace ExtraRoles {
	[HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Method_24))]
	public class GameOptionsData_ToHudString {
		public static void Postfix(ref string __result) {
			DestroyableSingleton<HudManager>.Instance.GameSettings.scale = 0.27f;
		}
	}

	[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
	public class HudUpdateManager {
		public static void Postfix(HudManager __instance) {
			if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
			
			if (!__instance.Chat.isActiveAndEnabled) {
				__instance.Chat.SetVisible(true);
			}

			var LocalPlayer = PlayerControl.LocalPlayer;
			var Dead = LocalPlayer.Data.IsDead;

			KeyboardController.Update();

			var UseButtonActiveEnabled = __instance.UseButton != null
			                             && __instance.UseButton.isActiveAndEnabled;
			PlayerTools.CalculateClosest(PlayerControl.LocalPlayer);

			foreach (var Role in Role.ROLES) {
				Role.PreUpdate(__instance, UseButtonActiveEnabled, Dead);
			}

			#region FILL_NAME

			foreach (var Player in PlayerControl.AllPlayerControls) {
				Player.nameText.Color = Color.white;
			}

			if (PlayerControl.LocalPlayer.Data.IsImpostor) {
				foreach (var player in PlayerControl.AllPlayerControls) {
					if (player.Data.IsImpostor) {
						player.nameText.Color = Color.red;
					}
				}
			}

			foreach (var Role in Role.ROLES) {
				Role.FillPlayerName();
			}

			#endregion

			foreach (var Role in Role.ROLES) {
				Role.PostUpdate(__instance, UseButtonActiveEnabled, Dead);
			}
			
			foreach (var Role in Role.ROLES) {
				Role.UpdateTasksVisual(__instance);
			}
		}
	}
}