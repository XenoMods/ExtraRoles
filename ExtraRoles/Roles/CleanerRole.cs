using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.CustomOptions;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class CleanerRole : Role {
		public static readonly CleanerRole INSTANCE = new CleanerRole();

		public override bool ImpostorRole => true;

		// Settings

		// Runtime

		// ff7070
		private CleanerRole() : base("cleaner",
			new Color(1f, 112f / 255f, 112f / 255f, 1)) {
			CreateDefaultCooldown();
		}

		protected override void ResetRuntime() {
		}

		protected override void InitForLocalPlayer() {
			var Side = ModActions.Side;
			Side.Visible = true;
			Side.Saturator = ActiveSaturator.INSTANCE;
			Side.Cooldown = Cooldown;
			Side.Icon = ExtraResources.CLEAN;
		}

		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (!IsLocalPlayer()) return;

			var Side = ModActions.Side;
			Side.Active = !Dead;
			Side.Update();
		}

		public void Clean(byte ParentId) {
			var Bodies = Object.FindObjectsOfType<DeadBody>();
			foreach (var DeadBody in Bodies) {
				if (DeadBody.ParentId != ParentId) continue;
				
				DeadBody.gameObject.SetActive(false);
				Object.Destroy(DeadBody);
				return;
			}
		}

		public override void DoAction(ActionType Type, bool Dead, ref bool Acted) {
			if (Dead || Type != ActionType.SIDE || !IsLocalPlayer()) return;
			if (!Cooldown.IsReady()) return;

			var Bodies = Object.FindObjectsOfType<DeadBody>();
			var PlayerPosition = PlayerControl.LocalPlayer.GetTruePosition();
			
			foreach (var DeadBody in Bodies) {
				var Distance = (PlayerPosition - DeadBody.TruePosition).magnitude;
				if (Distance > PlayerControl.LocalPlayer.MaxReportDistance) continue;

				var ParentId = DeadBody.ParentId;
				ExtraNetwork.Send(CustomRPC.CleanerClean, Writer => {
					Writer.Write(ParentId);
				});
				Clean(ParentId);

				Cooldown.Use();
				Acted = true;
				return;
			}
		}
	}
}