using System;
using ExtraRoles.Roles;
using ExtraRoles.Roles.Actions;
using HarmonyLib;

namespace ExtraRoles {
	[HarmonyPatch(typeof(UseButtonManager), nameof(UseButtonManager.DoClick))]
	public class ActionSecondaryPatch {
		public static void Prefix() {
		}
	}

	[HarmonyPatch(typeof(KillButtonManager), nameof(KillButtonManager.PerformKill))]
	public class ActionPrimaryPatch {
		public static bool Prefix() {
			return ModActions.DoAction(ActionType.PRIMARY);
		}
	}

	[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
	public static class MurderPlayerPatch {
		public static void Prefix(PlayerControl __instance, PlayerControl CAKODNGLPDF) {
			foreach (var Role in Role.ROLES) {
				Role.PreMurderPlayer(__instance, CAKODNGLPDF);
			}
		}
		
		public static void Postfix(PlayerControl __instance, PlayerControl CAKODNGLPDF) {
			var DeadBody = new DeadPlayer {
				PlayerId = CAKODNGLPDF.PlayerId,
				KillerId = __instance.PlayerId,
				KillTime = DateTime.UtcNow,
				DeathReason = DeathReason.Kill
			};
			
			foreach (var Role in Role.ROLES) {
				Role.PostMurderPlayer(__instance, CAKODNGLPDF, DeadBody);
			}

			ExtraRoles.KilledPlayers.Add(DeadBody);
		}
	}
}