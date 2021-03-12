using System.Collections.Generic;
using System.Linq;
using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class MageRole : Role {
		public static readonly MageRole INSTANCE = new MageRole();

		// Settings

		// Runtime
		public bool StoneficationActive { get; set; }
		
		// Actions
		public CooldownController StoneficationCooldown { get; }
		public CooldownController StoneficationEffect { get; }
		
		public CooldownController TranspositionCooldown { get; }

		// ecf005
		private MageRole() : base("mage",
			new Color(236f / 255f, 240f / 255f, 5f / 255f, 1)) {
			StoneficationCooldown = CreateCooldown("stonefication.cooldown");
			StoneficationEffect = CreateCooldown("stonefication.effect");
			TranspositionCooldown = CreateCooldown("transposition.cooldown");
		}

		protected override void ResetRuntime() {
			TranspositionCooldown.Reset();
			StoneficationCooldown.Reset();
			StoneficationEffect.Reset();
			StoneficationActive = false;
		}

		protected override void InitForLocalPlayer() {
			var Primary = ModActions.Primary;
			Primary.Visible = true;
			Primary.Icon = ExtraResources.BOOK_RED;
			Primary.Cooldown = StoneficationCooldown;
			Primary.Saturator = ActiveSaturator.INSTANCE;
			
			var Side = ModActions.Side;
			Side.Visible = true;
			Side.Icon = ExtraResources.BOOK_BLUE;
			Side.Cooldown = TranspositionCooldown;
			Side.Saturator = ActiveSaturator.INSTANCE;
		}

		private List<byte> CalculateStoneficationTargets() {
			var Center = Player.GetTruePosition();
			var MaxDistance = GameOptionsData.KillDistances[2];
			var Result = new List<byte>();
			
			foreach (var Control in PlayerControl.AllPlayerControls) {
				var Distance = (Control.GetTruePosition() - Center).magnitude;

				if (Distance <= MaxDistance) {
					Result.Add(Control.PlayerId);
				}
			}

			return Result;
		}

		private static void RecolorPlayer(PlayerControl Control, bool Stoned) {
			if (!Stoned) {
				Control.SetColor(Control.Data.ColorId);
				return;
			}
			
			var Renderer = Control.GetComponent<SpriteRenderer>();
			PlayerControl.SetPlayerMaterialColors(Color.gray, Renderer);
			PlayerControl.SetPlayerMaterialColors(Color.gray, Control.HatRenderer.FrontLayer);
			PlayerControl.SetPlayerMaterialColors(Color.gray, Control.HatRenderer.BackLayer);

			if (Control.CurrentPet != null) {
				PlayerControl.SetPlayerMaterialColors(Color.gray, Control.CurrentPet.rend);
			}
		}

		public void EnableStonefication(IEnumerable<byte> Targets) {
			ExtraResources.STONE_AURA.OneTimeAnimate(Player.transform);

			foreach (var TargetId in Targets) {
				var Target = PlayerTools.GetPlayerById(TargetId);
				if (Target == null || Target.Compare(Player)) continue;

				Target.moveable = false;
				RecolorPlayer(Target, true);
			}
			
			StoneficationCooldown.Use();
			StoneficationActive = true;
		}

		public void DisableStonefication() {
			foreach (var Control in PlayerControl.AllPlayerControls) {
				Control.moveable = true;
				RecolorPlayer(Control, false);
			}

			StoneficationActive = false;
		}

		public void Transposition(PlayerControl Partner) {
			var First = Player.MyPhysics.body.position;
			var Second = Partner.MyPhysics.body.position;

			Player.MyPhysics.body.position = Second;
			Partner.MyPhysics.body.position = First;
			
			ExtraResources.TRANSPOSITION.OneTimeAnimate(Player.transform);
			ExtraResources.TRANSPOSITION.OneTimeAnimate(Partner.transform);
			
			TranspositionCooldown.Use();
		}
		
		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (StoneficationActive) {
				StoneficationEffect.ForceSetLastUsedFrom(StoneficationCooldown);

				if (StoneficationEffect.GetKD(false) == 0) {
					DisableStonefication();
				}
			}
			
			if (!IsLocalPlayer()) return;

			var Primary = ModActions.Primary;
			Primary.Active = !Dead;
			Primary.Update();
			
			var Side = ModActions.Side;
			Side.Active = !Dead;
			Side.Update();
		}

		public override void DoAction(ActionType Type, bool Dead, ref bool Acted) {
			if (Dead || !IsLocalPlayer()) return;

			if (Type == ActionType.PRIMARY) {
				if (!StoneficationCooldown.IsReady()) return;

				var Targets = CalculateStoneficationTargets().ToArray();
				ExtraNetwork.Send(CustomRPC.MageStonefication, Writer => {
					Writer.WriteBytesAndSize(Targets);
				});
				EnableStonefication(Targets);
				Acted = true;
			} else if (Type == ActionType.SIDE) {
				if (!TranspositionCooldown.IsReady()) return;

				var Partner = PlayerControl.AllPlayerControls.ToArray()
					.Where(Control => Control.PlayerId != Player.PlayerId
						&& !Control.Data.IsDead)
					.ToList().RandomItem();
				if (Partner == null) return;
				
				ExtraNetwork.Send(CustomRPC.MageTransposition, Writer => {
					Writer.Write(Partner.PlayerId);
				});
				Transposition(Partner);
			}
		}

		public override void Intro(IntroCutscene.CoBegin__d Cutscene) {
			base.Intro(Cutscene);
			
			StoneficationCooldown.UpdateForIntro(Cutscene);
			StoneficationEffect.UpdateForIntro(Cutscene);
			TranspositionCooldown.UpdateForIntro(Cutscene);
		}

		public override void MeetingEnd(ExileController Exile) {
			base.MeetingEnd(Exile);

			StoneficationActive = false;
			StoneficationCooldown.UpdateForExile(Exile);
			StoneficationEffect.UpdateForExile(Exile);
			TranspositionCooldown.UpdateForExile(Exile);
			
			DisableStonefication();
		}
	}
}