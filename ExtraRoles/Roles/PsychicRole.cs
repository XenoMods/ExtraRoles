using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class PsychicRole : Role {
		public static readonly PsychicRole INSTANCE = new PsychicRole();

		// Settings

		// Runtime
		public bool IsActive { get; set; }
		
		public GameObject AuraObject { get; set; }
		
		// Actions
		public CooldownController EffectCooldown { get; }

		// 6a0dad
		private PsychicRole() : base("psychic",
			new Color(106 / 255f, 13f / 255f, 173f / 255f, 1)) {
			CreateDefaultCooldown();
			EffectCooldown = new CooldownController(Prefix, "effect",
				10f, 5f, 60f, 2.5f);
		}

		protected override void ResetRuntime() {
			IsActive = false;
			AuraObject = null;
			EffectCooldown.Reset();
		}

		protected override void InitForLocalPlayer() {
			var Primary = ModActions.Primary;
			Primary.Visible = true;
			Primary.Icon = ExtraResources.SPIRITS;
			Primary.Targeter = NoneTargeter.INSTANCE;
			Primary.Saturator = ActiveSaturator.INSTANCE;
			Primary.Cooldown = Cooldown;
		}

		public void EnableAura() {
			if (AuraObject == null && Player != null) {
				AuraObject = Object.Instantiate(ExtraResources.AURA,
					Player.transform);
				AuraObject.SetActive(false);
			}

			if (Player != null) {
				var LocalPlayer = PlayerControl.LocalPlayer;

				if (LocalPlayer.PlayerId == Player.PlayerId
				    || Show
				    || LocalPlayer.Data.IsDead) {
					AuraObject.SetActive(true);
				}
			}
			
			Cooldown.Use();
			IsActive = true;
		}

		public void DisableAura() {
			if (AuraObject != null) {
				AuraObject.SetActive(false);
			}

			IsActive = false;
		}

		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (Player != null) {
				if (IsLocalPlayer()) {
					var Primary = ModActions.Primary;
					Primary.Active = !IsActive;
					Primary.Update();

					if (IsActive) {
						foreach (var PlayerControl in PlayerControl.AllPlayerControls) {
							PlayerControl.Visible = true;
						}
					}
				} else {
					if (IsActive) {
						Player.Visible = true;
					}
				}
			}

			if (IsActive) {
				EffectCooldown.ForceSetLastUsedFrom(Cooldown);

				if (EffectCooldown.GetKD(false) == 0) {
					DisableAura();
				}
			}
		}

		public override void DoAction(ActionType Type, bool Dead, ref bool Acted) {
			if (Type != ActionType.PRIMARY
			    || !IsLocalPlayer()|| IsActive || !Cooldown.IsReady()) return;
			
			ExtraNetwork.Send(CustomRPC.PsychicEffect);
			EnableAura();
			Acted = true;
		}

		public override void MeetingEnd(ExileController Exile) {
			base.MeetingEnd(Exile);
			IsActive = false;
		}
	}
}