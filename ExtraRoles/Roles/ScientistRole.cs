using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.CustomOptions;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class ScientistRole : Role {
		public static readonly ScientistRole INSTANCE = new ScientistRole();

		private float Speed = 4.5f;
		private float GhostSpeed = 3f;

		// Settings
		private readonly CustomNumberOption _TimeWarpSpeed;
		private readonly CustomToggleOption _TimeWarpSelf;

		public float TimeWarpSpeed => _TimeWarpSpeed.GetValue();
		public bool TimeWarpSelf => _TimeWarpSelf.GetValue();

		// Runtime
		public bool IsActive { get; set; }
		
		public GameObject TimeWarpObject { get; set; }

		// Actions
		public CooldownController TimeWarpCooldown { get; }
		public CooldownController TimeWarpEffect { get; }
		public CooldownController TasksCooldown { get; }

		// e67300
		private ScientistRole() : base("scientist",
			new Color(230f / 255f, 115f / 255f, 0f / 255f, 1)) {
			TimeWarpCooldown = new CooldownController(Prefix, "timewarp.cooldown");
			TimeWarpEffect = new CooldownController(Prefix, "timewarp.effect",
				10f, 5f, 60f, 2.5f);
			_TimeWarpSpeed = MakeNumber("timewarp.speed", 0.75f,
				0.25f, 10.0f, 0.25f);
			_TimeWarpSelf = MakeToggle("timewarp.self", true);
			TasksCooldown = new CooldownController(Prefix, "tasks.cooldown");
		}

		protected override void ResetRuntime() {
			TimeWarpCooldown.Reset();
			TasksCooldown.Reset();
			TimeWarpEffect.Reset();
		}

		public override void PlayerAssigned() {
			base.PlayerAssigned();

			var Physics = Player.MyPhysics;
			Speed = Physics.Speed;
			GhostSpeed = Physics.GhostSpeed;
		}

		protected override void InitForLocalPlayer() {
			var Primary = ModActions.Primary;
			Primary.Visible = true;
			Primary.Icon = ExtraResources.TIME;
			Primary.Cooldown = TimeWarpCooldown;
			Primary.Saturator = ActiveSaturator.INSTANCE;

			var Side = ModActions.Side;
			Side.Visible = true;
			Side.Icon = ExtraResources.TASKS;
			Side.Cooldown = TasksCooldown;
			Side.Saturator = ActiveSaturator.INSTANCE;
			Side.CooldownStrategy = ActiveDependentCooldownStrategy.INSTANCE;
		}

		public void EnableTimeWarp() {
			if (TimeWarpObject == null && Player != null) {
				TimeWarpObject = Object.Instantiate(ExtraResources.TIME_WARP,
					Player.transform);
				TimeWarpObject.SetActive(false);
			}

			if (Player != null) {
				var LocalPlayer = PlayerControl.LocalPlayer;

				if (LocalPlayer.PlayerId == Player.PlayerId
				    || Show) {
					TimeWarpObject.SetActive(true);
				}
			}

			var Mod = 1f / PlayerControl.GameOptions.PlayerSpeedMod * TimeWarpSpeed;
			var SpeedNew = Speed * Mod;
			var GhostSpeedNew = GhostSpeed * Mod;
			foreach (var PlayerControl in PlayerControl.AllPlayerControls) {
				var Physics = PlayerControl.MyPhysics;
				
				if (!TimeWarpSelf && PlayerControl.Compare(Player)) {
					Physics.Speed = Speed;
					Physics.GhostSpeed = GhostSpeed;
				} else {
					Physics.Speed = SpeedNew;
					Physics.GhostSpeed = GhostSpeedNew;
				}
			}

			TimeWarpCooldown.Use();
			IsActive = true;
		}

		public void DisableTimeWarp() {
			if (TimeWarpObject != null) {
				TimeWarpObject.SetActive(false);
			}

			foreach (var PlayerControl in PlayerControl.AllPlayerControls) {
				var Physics = PlayerControl.MyPhysics;
				Physics.Speed = Speed;
				Physics.GhostSpeed = GhostSpeed;
			}

			IsActive = false;
		}
		
		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (IsActive) {
				TimeWarpEffect.ForceSetLastUsedFrom(TimeWarpCooldown);

				if (TimeWarpEffect.GetKD(false) == 0) {
					DisableTimeWarp();
				}
			}
			
			if (!IsLocalPlayer()) return;

			var Primary = ModActions.Primary;
			Primary.Active = !Dead;
			Primary.Update();

			var Active = false;
			foreach (var Task in Player.myTasks) {
				if (Task.IsComplete) continue;
				Active = true;
				break;
			}

			var Side = ModActions.Side;
			Side.Active = Active && !Dead;
			Side.Update();
		}

		private PlayerTask GetRandomTask() {
			if (Player == null) return null;
			
			foreach (var Task in Player.myTasks) {
				if (!Task.IsComplete) {
					return Task;
				}
			}

			return null;
		}

		public override void DoAction(ActionType Type, bool Dead, ref bool Acted) {
			if (Dead || !IsLocalPlayer()) return;

			if (Type == ActionType.PRIMARY) {
				if (!TimeWarpCooldown.IsReady()) return;
				
				ExtraNetwork.Send(CustomRPC.ScientistEffect);
				EnableTimeWarp();
				Acted = true;
			} else if (Type == ActionType.SIDE) {
				if (!TasksCooldown.IsReady()) return;
				
				var Task = GetRandomTask();

				if (Task == null) return;
				Player.CompleteTask(Task.Id);
				Player.RpcCompleteTask(Task.Id);

				TasksCooldown.Use();
				Acted = true;
			}
		}

		public override void Intro(IntroCutscene.CoBegin__d Cutscene) {
			base.Intro(Cutscene);
			
			TimeWarpCooldown.UpdateForIntro(Cutscene);
			TasksCooldown.UpdateForIntro(Cutscene);
		}

		public override void MeetingEnd(ExileController Exile) {
			base.MeetingEnd(Exile);

			IsActive = false;
			TimeWarpCooldown.UpdateForExile(Exile);
			TasksCooldown.UpdateForExile(Exile);
		}
	}
}