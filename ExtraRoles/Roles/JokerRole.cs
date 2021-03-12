using UnityEngine;
using XenoCore.Locale;

namespace ExtraRoles.Roles {
	public sealed class JokerRole : Role {
		public static readonly JokerRole INSTANCE = new JokerRole();

		public override bool OwnTeam => true;

		// Runtime
		public bool RepairUsed { get; set; }
		public bool SabotageActive { get; set; }

		private string Tasks;

		// 838383
		private JokerRole() : base("joker",
			new Color(138f / 255f, 138f / 255f, 138f / 255f, 1)) {
		}

		protected override void ResetRuntime() {
			RepairUsed = false;
			SabotageActive = false;
			Tasks = LanguageManager.Get(MakeOptionId(TASKS));
		}

		public override void PreUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (Player == null) return;
			
			ClearTasks();
		}

		public override void Intro(IntroCutscene.CoBegin__d Cutscene) {
			if (!IsLocalPlayer()) return;
			
			SetupOwnIntroTeam(Cutscene);
		}

		public override void MeetingProceed(MeetingHud Meeting) {
			if (Player != null && Meeting.exiledPlayer != null
					&& Meeting.exiledPlayer.PlayerId == Player.PlayerId) {
				SingleVictory();
			}
		}

		public override void UpdateTasksVisual(HudManager Manager) {
			if (!IsLocalPlayer()) return;
			
			Manager.TaskText.Text = Tasks;
		}
	}
}