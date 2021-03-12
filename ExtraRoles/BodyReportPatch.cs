using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExtraRoles.Roles;
using XenoCore.Locale;

namespace ExtraRoles {
	[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.LocalPlayer.Die))]
	public class DiePatch {
		public static void Postfix(PlayerControl __instance, DeathReason OECOPGMHMKC) {
			if (EndGameCentral.RoleVictory != null) return;
			
			foreach (var Role in Role.ROLES) {
				Role.OnLocalDie(__instance, OECOPGMHMKC);
			}
		}
	}
	
	[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.LocalPlayer.CmdReportDeadBody))]
	public class BodyReportPatch {
		public static void Postfix(PlayerControl __instance, GameData.PlayerInfo CAKODNGLPDF) {
			var Body = ExtraRoles.KilledPlayers
				.FirstOrDefault(x => x.PlayerId == CAKODNGLPDF.PlayerId);
			if (Body == null) return;

			foreach (var Role in Role.ROLES) {
				Role.ReportBody(__instance, Body);
			}
		}
	}
	
	public class DeadPlayer {
		public byte KillerId { get; set; }
		public byte PlayerId { get; set; }
		public DateTime KillTime { get; set; }
		public DeathReason DeathReason { get; set; }
	}
	
	public class BodyReport {
		public DeathReason DeathReason { get; set; }
		public PlayerControl Killer { get; set; }
		public PlayerControl Reporter { get; set; }
		public float KillAge { get; set; }

		public static string ParseBodyReport(BodyReport br) {
			var VisualTime = Math.Round(br.KillAge / 1000).ToString(CultureInfo.InvariantCulture);
			var Medic = MedicRole.INSTANCE;

			if (br.KillAge > Medic.KillerColorDuration * 1000) {
				return LanguageManager.Get("er.medic.report.old")
					.Replace("%t", VisualTime);
			} else if (br.DeathReason == (DeathReason) 3) {
				return LanguageManager.Get("er.medic.report.officer")
					.Replace("%t", VisualTime);
			} else if (br.DeathReason == (DeathReason) 4) {
				return LanguageManager.Get("er.medic.report.zombie")
					.Replace("%t", VisualTime);
			} else if (br.KillAge < Medic.KillerNameDuration * 1000) {
				return LanguageManager.Get("er.medic.report.name")
					.Replace("%t", VisualTime)
					.Replace("%n", br.Killer.name);
			} else {
				var Darker = LanguageManager.Get("er.darker");
				var Lighter = LanguageManager.Get("er.lighter");
				
				var Colors = new Dictionary<byte, string> {
					{0, Darker},
					{1, Darker},
					{2, Darker},
					{3, Lighter},
					{4, Lighter},
					{5, Lighter},
					{6, Darker},
					{7, Lighter},
					{8, Darker},
					{9, Darker},
					{10, Lighter},
					{11, Lighter},
				};
				var ColorType = Colors[br.Killer.Data.ColorId];

				return LanguageManager.Get("er.medic.report.color")
					.Replace("%t", VisualTime)
					.Replace("%c", ColorType);
			}
		}
	}
}