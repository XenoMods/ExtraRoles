using System;
using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.CustomOptions;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class MedicRole : Role {
		public static readonly MedicRole INSTANCE = new MedicRole();

		public static readonly Color PROTECTED_COLOR = new Color(0, 1, 1, 1);

		// Settings
		private readonly CustomNumberOption _KillerNameDuration;
		private readonly CustomNumberOption _KillerColorDuration;
		private readonly CustomToggleOption _ShowProtected;
		private readonly CustomToggleOption _ShieldKillAttemptIndicator;
		private readonly CustomToggleOption _ShowReports;

		public int KillerNameDuration => (int) _KillerNameDuration.GetValue();
		public int KillerColorDuration => (int) _KillerColorDuration.GetValue();
		public bool ShowProtected => _ShowProtected.GetValue();
		public bool ShieldKillAttemptIndicator => _ShieldKillAttemptIndicator.GetValue();
		public bool ShowReports => _ShowReports.GetValue();

		// Runtime
		public PlayerControl Protected { get; set; }
		public bool ShieldUsed { get; set; }

		// 24b720
		private MedicRole() : base("medic",
			new Color(36f / 255f, 183f / 255f, 32f / 255f, 1)) {
			_ShowProtected = MakeToggle("shielded.show", true);
			_ShieldKillAttemptIndicator = MakeToggle("shielded.indicator", true);
			_KillerNameDuration = MakeNumber("report.name.time",
				5, 0, 60, 2.5f);
			_KillerColorDuration = MakeNumber("report.color.time",
				20, 0, 120, 2.5f);
			_ShowReports = MakeToggle("report.show", true);
		}

		protected override void ResetRuntime() {
			Protected = null;
			ShieldUsed = false;
		}

		protected override void InitForLocalPlayer() {
			var Primary = ModActions.Primary;
			Primary.Visible = true;
			Primary.Icon = ExtraResources.SHIELD;
			Primary.Targeter = ClosestActiveTargeter.INSTANCE;
			Primary.Saturator = ActiveAndTargetSaturator.INSTANCE;
			Primary.TargetOutlineColor = Color;
		}

		public void BreakShield() {
			if (Protected != null) {
				Protected.myRend.material.SetColor(Globals.VISOR_COLOR, Palette.VisorColor);
				Protected.myRend.material.SetFloat(Globals.OUTLINE, 0f);
			}

			Protected = null;
		}

		public override void FillPlayerName() {
			base.FillPlayerName();

			if (Protected == null) return;

			if (Protected == PlayerControl.LocalPlayer
			    || Player == PlayerControl.LocalPlayer || ShowProtected) {
				var Material = Protected.myRend.material;

				Material.SetColor(Globals.VISOR_COLOR, PROTECTED_COLOR);
				Material.SetFloat(Globals.OUTLINE, 1f);
				Material.SetColor(Globals.OUTLINE_COLOR, PROTECTED_COLOR);
			}
		}

		public override void PreUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (Protected == null) return;
			if (Player != null && !Player.Data.IsDead && !Protected.Data.IsDead) return;

			ExtraNetwork.Send(CustomRPC.ShieldBreak);

			var Material = Protected.myRend.material;
			Material.SetColor(Globals.VISOR_COLOR, Palette.VisorColor);
			Material.SetFloat(Globals.OUTLINE, 0f);
			Protected = null;
		}

		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (!IsLocalPlayer()) return;

			var Primary = ModActions.Primary;
			Primary.Active = !Dead && !ShieldUsed;
			Primary.Update();
		}

		public override void DoAction(ActionType Type, bool Dead, ref bool Acted) {
			if (Dead || Type != ActionType.PRIMARY) return;

			var Target = ModActions.Primary.CurrentTarget;

			if (Target != null) {
				if (!IsLocalPlayer()) return;
				
				Protected = Target;
				ShieldUsed = true;

				ExtraNetwork.Send(CustomRPC.SetProtected, Writer => {
					Writer.Write(Protected.PlayerId);
				});
				Acted = true;
				return;
			}
			
			if (PlayerControl.LocalPlayer.Data.IsImpostor
			    && PlayerTools.ClosestPlayer.Compare(Protected)) {
				// Detect when impostor tries to kill shielded player
				
				if (ShieldKillAttemptIndicator && PlayerTools.ClosestDistance.IsInKillRange()) {
					ExtraNetwork.Send(CustomRPC.ProtectedMurderAttempt, Writer => {
						Writer.Write(Protected.PlayerId);
					});
				}

				Acted = true;
			}
		}

		public override void ReportBody(PlayerControl Reporter, DeadPlayer Body) {
			if (!ShowReports) return;
			if (!IsLocalPlayer()) return;
			if (Reporter.PlayerId != Player.PlayerId) return;
			
			var ReportMessage = BodyReport.ParseBodyReport(new BodyReport {
				Killer = PlayerTools.GetPlayerById(Body.KillerId),
				Reporter = Reporter,
				KillAge = (float) (DateTime.UtcNow - Body.KillTime).TotalMilliseconds,
				DeathReason = Body.DeathReason
			});

			if (!string.IsNullOrWhiteSpace(ReportMessage)) {
				HudManager.Instance.Chat.AddSimpleChat(ReportMessage);

				if (ReportMessage.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0) {
					DestroyableSingleton<Telemetry>.Instance.SendWho();
				}
			}
		}
	}
}