using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using XenoCore.Buttons;

namespace ExtraRoles.Roles.Actions {
	public enum ActionType {
		PRIMARY,
		SIDE,
	}

	[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
	public class ModActions {
		public static UniversalButton Primary;
		public static UniversalButton Side;

		public static void Postfix() {
			Primary = UniversalButton.FromKillButton();
			Side = Make(Vector2.zero, ActionType.SIDE);
		}

		private static UniversalButton Make(Vector2 Offset, ActionType Type) {
			return UniversalButton.Create(Offset, () => DoAction(Type));
		}

		public static void ResetAll() {
			Primary.Reset();
			Side.Reset();
		}

		public static UniversalButton GetByType(ActionType Type) {
			return Type switch {
				ActionType.PRIMARY => Primary,
				ActionType.SIDE => Side,
				_ => null
			};
		}

		public static List<KeyboardAction> MakeActions() {
			return new List<KeyboardAction> {
				new KeyboardAction(KeyCode.Q, ActionType.PRIMARY),
				new KeyboardAction(KeyCode.F, ActionType.SIDE),
			};
		}

		public static bool DoAction(ActionType Type) {
			var Dead = PlayerControl.LocalPlayer.Data.IsDead;
			var Acted = false;

			foreach (var Role in Role.ROLES) {
				Role.DoAction(Type, Dead, ref Acted);

				if (Acted) {
					return false;
				}
			}

			return true;
		}
	}

	public static class KeyboardController {
		private static readonly List<KeyboardAction> Actions = ModActions.MakeActions();
		
		public static void Update() {
			var IsImpostor = PlayerControl.LocalPlayer.Data.IsImpostor;
			
			foreach (var Action in Actions) {
				if (!(IsImpostor && Action.Type == ActionType.PRIMARY)
				    && !Action.Last && Input.GetKeyDown(Action.Key)) {
					ModActions.DoAction(Action.Type);
				}

				Action.Last = Input.GetKeyUp(Action.Key);
			}
		}
	}

	public class KeyboardAction {
		public readonly KeyCode Key;
		public readonly ActionType Type;
		public bool Last;

		public KeyboardAction(KeyCode Key, ActionType Type) {
			this.Key = Key;
			this.Type = Type;
		}
	}
}