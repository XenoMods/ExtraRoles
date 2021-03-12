using System.Collections.Generic;
using System.Linq;
using ExtraRoles.Roles;
using HarmonyLib;
using XenoCore.Locale;

namespace ExtraRoles {
	public static class EndGameCentral {
		public static readonly Dictionary<byte, Role> StartingRoles
			= new Dictionary<byte, Role>();

		public static readonly List<PlayerControl> StartingPlayers = new List<PlayerControl>();
		public static Role RoleVictory;
		public static PlayerControl LocalPlayer;

		public static WinnerSide WinnerSide;

		public static void ResetStartingRoles() {
			StartingRoles.Clear();
			foreach (var Role in Role.ROLES) {
				if (Role.Player == null) continue;
				StartingRoles.Add(Role.Player.PlayerId, Role);
			}

			StartingPlayers.Clear();
			StartingPlayers.AddRange(PlayerControl.AllPlayerControls.ToArray());

			RoleVictory = null;
			LocalPlayer = PlayerControl.LocalPlayer;
		}

		public static void RecalculateWinnerSide() {
			if (RoleVictory != null) {
				WinnerSide = WinnerSide.Role;
			} else if (TempData.DidHumansWin(TempData.EndReason)) {
				WinnerSide = WinnerSide.Crewmate;
			} else {
				WinnerSide = WinnerSide.Impostor;
			}
		}
	}

	public enum WinnerSide {
		Crewmate,
		Impostor,
		Role
	}

	[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
	public static class EndGameStartPatch {
		public static void Postfix(EndGameManager __instance) {
			foreach (var Role in Role.ROLES) {
				Role.EndGame(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
	public static class EndGameSetPatch {
		private static bool IsLocalWin;

		private static void AddWinner(PlayerControl Player) {
			if (EndGameCentral.LocalPlayer.Data.PlayerId == Player.Data.PlayerId) {
				IsLocalWin = true;
			}

			TempData.winners.Add(new WinningPlayerData(Player.Data));
		}

		private static void AddWinners(IReadOnlyCollection<PlayerControl> Players) {
			var Ordered = new List<PlayerControl>();
			var LocalPlayerId = EndGameCentral.LocalPlayer.Data.PlayerId;
			Ordered.AddRange(Players.Where(Player => Player.Data.PlayerId == LocalPlayerId));
			Ordered.AddRange(Players.Where(Player => Player.Data.PlayerId != LocalPlayerId));

			foreach (var Player in Ordered) {
				AddWinner(Player);
			}
		}

		private static List<PlayerControl> GetStandardTeam(bool IsImpostor) {
			return EndGameCentral.StartingPlayers.Where(Player => {
				if (EndGameCentral.StartingRoles.ContainsKey(Player.PlayerId)) {
					return !EndGameCentral.StartingRoles[Player.PlayerId].OwnTeam
					       && Player.Data.IsImpostor == IsImpostor;
				}

				return Player.Data.IsImpostor == IsImpostor;
			}).ToList();
		}

		public static bool Prefix(EndGameManager __instance) {
			EndGameCentral.RecalculateWinnerSide();
			TempData.winners.Clear();
			IsLocalWin = false;

			switch (EndGameCentral.WinnerSide) {
				case WinnerSide.Role: {
					AddWinner(EndGameCentral.RoleVictory.Player);
					break;
				}
				case WinnerSide.Crewmate: {
					AddWinners(GetStandardTeam(false));
					break;
				}
				case WinnerSide.Impostor: {
					AddWinners(GetStandardTeam(true));
					break;
				}
			}

			return true;
		}

		public static void Postfix(EndGameManager __instance) {
			if (IsLocalWin) {
				__instance.WinText.Text = XenoLang.VICTORY.Get();
				__instance.WinText.Color = Palette.CrewmateBlue;
				__instance.BackgroundBar.material.color = Palette.CrewmateBlue;
			} else {
				__instance.WinText.Text = XenoLang.DEFEAT.Get();
				__instance.WinText.Color = Palette.ImpostorRed;
				__instance.BackgroundBar.material.color = Palette.ImpostorRed;
			}

			foreach (var Role in Role.ROLES) {
				Role.EndGame(__instance);
			}
		}
	}
}