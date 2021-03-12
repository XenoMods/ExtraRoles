using System.Collections.Generic;
using System.Linq;
using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnhollowerBaseLib;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.CustomOptions;
using XenoCore.Locale;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class ZombieRole : Role {
		public static readonly ZombieRole INSTANCE = new ZombieRole();

		public override bool OwnTeam => true;
		
		// Settings
		private readonly CustomToggleOption _ImpostorCanSeeAlpha;

		public bool ImpostorCanSeeAlpha => _ImpostorCanSeeAlpha.GetValue();

		// Runtime
		public readonly HashSet<byte> Infected = new HashSet<byte>();
		
		private string Tasks;

		// 1a5718
		private ZombieRole() : base("zombie",
			new Color(26f / 255f, 87f / 255f, 24f / 255f, 1)) {
			CreateDefaultCooldown();
			_ImpostorCanSeeAlpha = MakeToggle("impostor_can_see_alpha", false);
		}

		protected override void ResetRuntime() {
			Infected.Clear();
			
			Tasks = LanguageManager.Get(MakeOptionId(TASKS));
		}

		public override void PlayerAssigned() {
			base.PlayerAssigned();
			if (Player == null) return;

			Infected.Add(Player.PlayerId);
		}

		protected override void InitForLocalPlayer() {
			var Primary = ModActions.Primary;
			Primary.Visible = true;
			Primary.Icon = ExtraResources.INFECT;
			Primary.Targeter = ZombieTargeter.TINSTANCE;
			Primary.Saturator = ActiveAndTargetSaturator.INSTANCE;
			Primary.Cooldown = Cooldown;
			Primary.TargetOutlineColor = Color;
		}

		public override void PreUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (Player == null) return;

			ClearTasks();
		}

		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			var LocalPlayerId = PlayerControl.LocalPlayer.PlayerId;
			
			foreach (var Control in PlayerControl.AllPlayerControls) {
				var IsInfected = Infected.Contains(Control.PlayerId);
				var IsAlpha = false;
				
				if (IsInfected) {
					if (Control.PlayerId == Player.PlayerId) {
						// Alpha zombie

						if (Control.PlayerId == LocalPlayerId) {
							// Player can see himself as alpha
							IsAlpha = true;
						} else if (ImpostorCanSeeAlpha && PlayerControl.LocalPlayer.Data.IsImpostor) {
							// If impostors can see alpha
							IsAlpha = true;
						} else {
							IsInfected = false;
						}
					} else if (Control.PlayerId == LocalPlayerId) {
						// Player can't see himself
						IsInfected = false;
					}
				}
				
				SetInfectedVisual(Control.myRend.material, IsInfected, IsAlpha);

				if (MeetingHud.Instance == null) continue;
				foreach (var MeetingPlayer in MeetingHud.Instance.playerStates) {
					if (MeetingPlayer.TargetPlayerId != Control.PlayerId) continue;
					
					SetInfectedVisual(MeetingPlayer.PlayerIcon.Body.material, IsInfected, IsAlpha);
				}
			}
			
			if (!IsLocalPlayer()) return;

			var Primary = ModActions.Primary;
			Primary.Active = !Dead;
			Primary.Update();
		}

		private void SetInfectedVisual(Material Material, bool IsInfected, bool IsAlpha) {
			if (IsInfected) {
				Material.SetColor(Globals.VISOR_COLOR, IsAlpha ? Color.red : Color);
			} else {
				Material.SetColor(Globals.VISOR_COLOR, Palette.VisorColor);
			}
		}

		public void Infect(PlayerControl Target) {
			Infected.Add(Target.PlayerId);
		}

		private void CheckVictory() {
			var Result = new List<ZombieData>();
			foreach (var Control in PlayerControl.AllPlayerControls) {
				Result.Add(new ZombieData {
					Dead = Control.Data.IsDead,
					Infected = Infected.Contains(Control.PlayerId)
				});
			}

			if (Result.All(Data => Data.Dead || Data.Infected)) {
				SingleVictory();
			}
		}

		public override void DoAction(ActionType Type, bool Dead, ref bool Acted) {
			if (!IsLocalPlayer() || Dead) return;

			if (Type == ActionType.PRIMARY) {
				var Target = ModActions.Primary.CurrentTarget;
				if (Target == null) return;

				if (Infected.Contains(Target.PlayerId)) return;
				
				Infect(Target);
				ExtraNetwork.Send(CustomRPC.ZombieInfect, Writer => {
					Writer.Write(Target.PlayerId);					
				});

				Cooldown.Use();
				CheckVictory();
				Acted = true;
			}
		}

		public override void OnLocalDie(PlayerControl Victim, DeathReason Reason) {
			if (!IsLocalPlayer()) return;
			if (Victim.PlayerId != Player.PlayerId) return;

			Infected.Remove(Player.PlayerId);
			var TargetIds = new Il2CppStructArray<byte>(Infected.Count);
			var Index = 0;
			
			foreach (var InfectedId in Infected) {
				TargetIds[Index] = InfectedId;
				Index++;
			}
			ConsoleTools.Info(TargetIds.Count.ToString());

			ExtraNetwork.Send(CustomRPC.ZombieChainKill, Writer => {
				Writer.WriteBytesAndSize(TargetIds);				
			});

			KillTargets(TargetIds);
		}

		public void KillTargets(IEnumerable<byte> TargetIds) {
			if (Player == null) return;

			var ZombieData = Player.Data;
			ZombieData.IsDead = false;
			ZombieData.IsImpostor = true;
			
			foreach (var TargetId in TargetIds) {
				var Target = PlayerTools.GetPlayerById(TargetId);
				if (Target == null) continue;
				
				Player.MurderPlayer(Target);
			}

			ZombieData.IsDead = true;
			ZombieData.IsImpostor = false;
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
			
			Body.DeathReason = (DeathReason) 4;
		}
		
		public override void UpdateTasksVisual(HudManager Manager) {
			if (!IsLocalPlayer()) return;
			
			Manager.TaskText.Text = Tasks;
		}

		private class ZombieData {
			public bool Dead;
			public bool Infected;
		}
		
		private class ZombieTargeter : ButtonTargeter {
			public static readonly ZombieTargeter TINSTANCE = new ZombieTargeter();
		
			private ZombieTargeter() {
			}

			public PlayerControl GetTarget(bool Active) {
				var Infected = ZombieRole.INSTANCE.Infected;
				
				PlayerTools.CalculateClosest(PlayerControl.LocalPlayer,
					out var ClosestPlayer, out var ClosestDistance,
					SomePlayer => !Infected.Contains(SomePlayer.PlayerId));
				
				return Active && ClosestDistance.IsInKillRange()
					? ClosestPlayer
					: null;
			}
		}
	}
}