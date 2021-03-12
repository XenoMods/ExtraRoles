using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.CustomOptions;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class OfficerRole : Role {
		public static readonly OfficerRole INSTANCE = new OfficerRole();

		// Settings
		private readonly CustomToggleOption _CanKillJoker;
		private readonly CustomToggleOption _CanKillZombie;
		
		public bool CanKillJoker => _CanKillJoker.GetValue();
		public bool CanKillZombie => _CanKillZombie.GetValue();

		// Runtime

		// 0028c6
		private OfficerRole() : base("officer",
			new Color(0, 40f / 255f, 198f / 255f, 1)) {
			CreateDefaultCooldown();
			_CanKillJoker = MakeToggle("can_kill_joker", true);
			_CanKillZombie = MakeToggle("can_kill_zombie", true);
		}

		protected override void ResetRuntime() {
		}

		protected override void InitForLocalPlayer() {
			var Primary = ModActions.Primary;
			Primary.Visible = true;
			Primary.Targeter = ClosestActiveTargeter.INSTANCE;
			Primary.Saturator = ActiveAndTargetSaturator.INSTANCE;
			Primary.Cooldown = Cooldown;
			Primary.Icon = ExtraResources.CHECK;
			Primary.TargetOutlineColor = Color;
		}

		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (!IsLocalPlayer()) return;

			var Primary = ModActions.Primary;
			Primary.Active = !Dead;
			Primary.Update();
		}

		private void OfficerKill(PlayerControl LocalPlayer, PlayerControl Target) {
			ExtraNetwork.Send(CustomRPC.OfficerKill, Writer => {
				Writer.Write(LocalPlayer.PlayerId);
				Writer.Write(Target.PlayerId);
			});
			LocalPlayer.MurderPlayer(Target);
			Cooldown.Use();
		}

		public override void DoAction(ActionType Type, bool Dead, ref bool Acted) {
			if (Dead || Type != ActionType.PRIMARY) return;

			var Target = ModActions.Primary.CurrentTarget;
			if (Target == null) return;
			if (!IsLocalPlayer() || !Cooldown.IsReady()) return;

			var Medic = MedicRole.INSTANCE;
			var Joker = JokerRole.INSTANCE;
			var Zombie = ZombieRole.INSTANCE;
			var LocalPlayer = PlayerControl.LocalPlayer;
			
			if (Target.Compare(Medic.Protected)) {
				OfficerKill(LocalPlayer, LocalPlayer);
				Acted = true;
				return;
			}
			if (CanKillJoker && Target.Compare(Joker.Player)) {
				OfficerKill(LocalPlayer, Target);
				Acted = true;
				return;
			}
			if (CanKillZombie && Target.Compare(Zombie.Player)) {
				OfficerKill(LocalPlayer, Target);
				Acted = true;
				return;
			}
			if (Target.Data.IsImpostor) {
				OfficerKill(LocalPlayer, Target);
				Acted = true;
				return;
			}
			
			OfficerKill(LocalPlayer, LocalPlayer);
			Acted = true;
		}

		public override void PreMurderPlayer(PlayerControl Killer, PlayerControl Target) {
			if (Player == null) return;
			
			if (Killer == Player) {
				Killer.Data.IsImpostor = true;
			}
		}

		public override void PostMurderPlayer(PlayerControl Killer, PlayerControl Target, DeadPlayer Body) {
			if (Player == null) return;
			
			if (Killer == Player) {
				Killer.Data.IsImpostor = false;
			}

			if (Killer.Compare(Target)) {
				Body.DeathReason = (DeathReason) 3;
			}
		}
	}
}