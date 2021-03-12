using System;
using System.Collections.Generic;
using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.CustomOptions;
using XenoCore.Locale;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public abstract class Role {
		protected static readonly string TASKS = "tasks";
		
		public static readonly OptionGroup GROUP_ENABLE = ExtraRoles.ER_GROUP.Down();
		public static readonly OptionGroup GROUP_COOLDOWN = GROUP_ENABLE.Down();

		// Settings
		public readonly string Id;
		protected string Prefix => $"er.{Id}";

		public Color Color { get; }
		public virtual bool OwnTeam => false;
		public virtual bool ImpostorRole => false;
		public virtual bool CanVent => false;

		private readonly CustomToggleOption _Enable;

		public bool Show => ExtraRoles.ShowExtraRoles.GetValue();
		public bool Enable => _Enable.GetValue();

		// Hard runtime
		public byte RoleNo { get; private set; }

		// Runtime
		public PlayerControl Player { get; set; }
		public bool DidSingleVictory { get; set; }

		// Actions
		protected CooldownController Cooldown { get; private set; }

		public static readonly List<Role> ROLES = new List<Role>();

		protected Role(string Id, Color RoleColor) {
			this.Id = Id;
			Color = RoleColor;

			var RoleHexColor = $"[{RoleColor.ToHexRGBA()}]";
			var Arguments = new Dictionary<string, Func<string>> {
				{"%c", () => RoleHexColor},
				{"%w", () => LanguageManager.Get($"er.{Id}.whom")}
			};

			_Enable = MakeRoleToggle(Id, "enable", Arguments, GROUP_ENABLE);

			var RoleTitle = CustomOption.AddTitle("er.role.title");
			RoleTitle.LocalizationArguments = new Dictionary<string, Func<string>> {
				{"%c", () => RoleHexColor},
				{"%n", () => LanguageManager.Get($"er.{Id}")},
				{"%r", () => Globals.FORMAT_WHITE},
			};
		}

		private static CustomToggleOption MakeRoleToggle(string Id, string Type,
			Dictionary<string, Func<string>> Arguments, OptionGroup Group) {
			var Result = CustomOption.AddToggle($"er.{Id}.{Type}",
				$"er.role.{Type}", false);
			Result.LocalizationArguments = Arguments;
			Result.Group = Group;
			return Result;
		}

		#region HELPERS

		protected CooldownController CreateCooldown(string OptionName = "cooldown",
			float Value = 30f, float Min = 5f, float Max = 60f, float Increment = 2.5f) {
			return CooldownController.FromOption(Prefix, OptionName, Value, Min, Max, Increment);
		}
		
		protected void CreateDefaultCooldown() {
			Cooldown = CreateCooldown();
		}

		protected void ClearTasks() {
			var ToRemove = new List<PlayerTask>();

			foreach (var Task in Player.myTasks) {
				if (!Task.TaskType.IsSabotage()) {
					ToRemove.Add(Task);
				}
			}

			foreach (var Task in ToRemove) {
				Player.RemoveTask(Task);
			}
		}

		protected void SingleVictory() {
			if (Player == null) return;

			EndGameCentral.RoleVictory = this;

			ExtraNetwork.Send(CustomRPC.SingleWin, Writer => {
				Writer.Write(RoleNo);
				Writer.Write(Player.PlayerId);
			});

			foreach (var SomePlayer in PlayerControl.AllPlayerControls) {
				if (SomePlayer != Player) {
					SomePlayer.RemoveInfected();
					SomePlayer.MurderPlayer(SomePlayer);
					SomePlayer.Data.IsDead = true;
					SomePlayer.Data.IsImpostor = false;
				} else {
					SomePlayer.Revive();
					SomePlayer.Data.IsDead = false;
					SomePlayer.Data.IsImpostor = true;
				}
			}
		}

		protected void RoleVictory(EndGameManager Manager) {
			Manager.BackgroundBar.material.color = Color;
			Manager.WinText.Text = LanguageManager.Get($"er.{Id}.win");
		}

		protected static void SetupOwnIntroTeam(IntroCutscene.CoBegin__d Cutscene) {
			var Team = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
			Team.Add(PlayerControl.LocalPlayer);
			Cutscene.yourTeam = Team;
		}

		#endregion

		#region SETTINGS

		protected string MakeOptionId(string Name) {
			return $"{Prefix}.{Name}";
		}

		protected CustomToggleOption MakeToggle(string Name, bool Value) {
			return CustomOption.AddToggle(MakeOptionId(Name), Value);
		}

		protected CustomNumberOption MakeNumber(string Name, float Value, float Min,
			float Max, float Increment) {
			return CustomOption.AddNumber(MakeOptionId(Name), Value, Min, Max, Increment);
		}

		#endregion

		#region RESET

		public void Reset() {
			Player = null;
			DidSingleVictory = false;
			Cooldown?.Reset();

			ResetRuntime();
		}

		protected abstract void ResetRuntime();

		#endregion

		#region EVENTS

		public virtual void PlayerAssigned() {
			if (IsLocalPlayer()) {
				InitForLocalPlayer();
			}
		}

		protected virtual void InitForLocalPlayer() {
		}

		public virtual void FillPlayerName() {
			if (Player == null) return;
			if (Player != PlayerControl.LocalPlayer && !Show) return;

			var RoleColor = Color;
			Player.nameText.Color = RoleColor;
			if (MeetingHud.Instance == null) return;

			foreach (var MeetingPlayer in MeetingHud.Instance.playerStates) {
				if (MeetingPlayer.NameText != null
				    && Player.PlayerId == MeetingPlayer.TargetPlayerId) {
					MeetingPlayer.NameText.Color = RoleColor;
				}
			}
		}

		public virtual void PreUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
		}

		public virtual void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
		}

		public virtual void DoAction(ActionType Type, bool Dead, ref bool Acted) {
		}

		public virtual void ShowInfectedMap(MapBehaviour Map) {
		}

		public virtual void UpdateMap(MapBehaviour Map) {
		}

		public virtual bool ActivateSabotage(MapRoom Map, SabotageType Type) {
			return false;
		}

		public virtual void PreMurderPlayer(PlayerControl Killer, PlayerControl Target) {
		}

		public virtual void PostMurderPlayer(PlayerControl Killer, PlayerControl Target,
			DeadPlayer Body) {
		}

		public virtual void ReportBody(PlayerControl Reporter, DeadPlayer Body) {
		}

		public virtual void Intro(IntroCutscene.CoBegin__d Cutscene) {
			Cooldown?.UpdateForIntro(Cutscene);
		}

		public virtual void MeetingStart(MeetingHud Meeting) {
		}

		public virtual void MeetingProceed(MeetingHud Meeting) {
		}

		public virtual void MeetingEnd(ExileController Exile) {
			Cooldown?.UpdateForExile(Exile);
		}

		public virtual void EndGame(EndGameManager Manager) {
			if (EndGameCentral.RoleVictory == this) {
				RoleVictory(Manager);
			}
		}

		public virtual void OnLocalDie(PlayerControl Victim, DeathReason Reason) {
		}

		public virtual void MeetingBeforeCreatePlayer(MeetingHud Meeting,
			GameData.PlayerInfo PlayerInfo) {
		}
		
		public virtual void UpdateTasksVisual(HudManager Manager) {
		}

		#endregion

		public bool IsLocalPlayer() {
			return Player != null && Player.PlayerId == PlayerControl.LocalPlayer.PlayerId;
		}

		public static void ResetAll() {
			foreach (var SomeRole in ROLES) {
				SomeRole.Reset();
			}

			ModActions.ResetAll();
		}

		public static void Load() {
			ROLES.Add(MedicRole.INSTANCE);
			ROLES.Add(OfficerRole.INSTANCE);
			ROLES.Add(EngineerRole.INSTANCE);
			ROLES.Add(JokerRole.INSTANCE);
			ROLES.Add(PsychicRole.INSTANCE);
			ROLES.Add(ZombieRole.INSTANCE);
			ROLES.Add(ScientistRole.INSTANCE);
			ROLES.Add(SpyRole.INSTANCE);
			ROLES.Add(MageRole.INSTANCE);
			ROLES.Add(CleanerRole.INSTANCE);

			for (var No = 0; No < ROLES.Count; No++) {
				ROLES[No].RoleNo = (byte) No;
			}
		}

		public void SetPlayerById(byte PlayerId) {
			if (XenoCore.Utils.Extensions.TryGetPlayerById(PlayerId, out var NewPlayer)) {
				Player = NewPlayer;
			}
		}
	}
}