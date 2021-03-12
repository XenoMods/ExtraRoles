using System;
using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class EngineerRole : Role {
		public static readonly EngineerRole INSTANCE = new EngineerRole();
		
		public override bool CanVent => IsLocalPlayer();

		// Runtime
		public bool RepairUsed { get; set; }
		public bool SabotageActive { get; set; }

		// 972e00
		private EngineerRole() : base("engineer",
			new Color(151f / 255f, 46f / 255f, 0, 1)) {
		}

		protected override void ResetRuntime() {
			RepairUsed = false;
			SabotageActive = false;
		}

		protected override void InitForLocalPlayer() {
			var Primary = ModActions.Primary;
			Primary.Visible = true;
			Primary.Icon = ExtraResources.REPAIR;
			Primary.Saturator = ActiveSaturator.INSTANCE;
		}

		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			SabotageActive = false;

			if (!IsLocalPlayer()) return;

			var Primary = ModActions.Primary;
			Primary.Active = !Dead && !RepairUsed;
			Primary.Update();

			if (!Primary.Active) return;

			foreach (var Task in PlayerControl.LocalPlayer.myTasks) {
				if (Task.TaskType.IsSabotage()) {
					SabotageActive = true;
				}
			}
		}

		public override void DoAction(ActionType Type, bool Dead, ref bool Acted) {
			if (Type != ActionType.PRIMARY) return;
			if (!IsLocalPlayer() || Dead || RepairUsed) return;

			DestroyableSingleton<HudManager>.Instance.ShowMap((Action<MapBehaviour>) delegate(MapBehaviour m) {
				m.ShowInfectedMap();
				m.ColorControl.baseColor = SabotageActive ? Color.gray : Color;
			});
			Acted = true;
		}

		public override void ShowInfectedMap(MapBehaviour Map) {
			if (!IsLocalPlayer()) return;
			if (!Map.IsOpen) return;
			
			Map.ColorControl.baseColor = Color;
			foreach (var Room in Map.infectedOverlay.rooms) {
				var Door = Room.door;
				if (Door == null) continue;

				var DoorObject = Door.gameObject;
				Door.enabled = false;
				DoorObject.SetActive(false);
				DoorObject.active = false;
			}
		}

		public override void UpdateMap(MapBehaviour Map) {
			if (!IsLocalPlayer()) return;
			if (!Map.IsOpen || !Map.infectedOverlay.gameObject.active) return;
			
			Map.ColorControl.baseColor = SabotageActive ? Color : Color.gray;
			var Percent = RepairUsed ? 1f : 0f;
			
			foreach (var Room in Map.infectedOverlay.rooms) {
				var Special = Room.special;
				if (Special == null) continue;
				
				Special.material.SetFloat(Globals.DESAT, SabotageActive ? 0f : 1f);
				Special.enabled = true;

				var SpecialObject = Special.gameObject;
				SpecialObject.SetActive(true);
				SpecialObject.active = true;

				Room.special.material.SetFloat(Globals.PERCENT,
					PlayerControl.LocalPlayer.Data.IsDead ? 1f : Percent);
			}
		}

		public override bool ActivateSabotage(MapRoom Map, SabotageType Type) {
			if (!IsLocalPlayer()) return false;
			if (PlayerControl.LocalPlayer.Data.IsDead) return true;
			if (RepairUsed || !SabotageActive) return true;

			RepairUsed = true;

			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (Type) {
				case SabotageType.Lights:
					SabotageCentralPatch.FixLights();
					ExtraNetwork.Send(CustomRPC.FixLights);
					break;
				case SabotageType.Reactor:
					ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 16);
					break;
				case SabotageType.Comms:
					ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 16 | 0);
					ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 16 | 1);
					break;
				case SabotageType.Oxygen:
					ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 0 | 64);
					ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 1 | 64);
					break;
				case SabotageType.Seismic:
					ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 16);
					break;
			}
			
			
			return true;
		}
	}
}