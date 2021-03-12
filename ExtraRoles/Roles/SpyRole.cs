using ExtraRoles.Helpers;
using ExtraRoles.Roles.Actions;
using UnityEngine;
using XenoCore.Buttons;
using XenoCore.Buttons.Strategy;
using XenoCore.CustomOptions;
using XenoCore.Utils;

namespace ExtraRoles.Roles {
	public sealed class SpyRole : Role {
		public static readonly SpyRole INSTANCE = new SpyRole();

		private static readonly Color HALF_INVISIBLE = new Color(1f, 1f, 1f, 0.5f);

		// Settings
		private readonly CustomToggleOption _ResetOnMeeting;
		
		public bool ResetOnMeeting => _ResetOnMeeting.GetValue();
		
		// Runtime
		public bool IsActive { get; set; }

		private PersonInfo OriginalSkin;
		
		// Actions
		public CooldownController InvisibleCooldown { get; }
		public CooldownController InvisibleEffect { get; }
		public CooldownController DisguiseCooldown { get; }

		// ed05af
		private SpyRole() : base("spy",
			new Color(237f / 255f, 5f / 255f, 175f / 255f, 1)) {
			InvisibleCooldown = CreateCooldown("invisible.cooldown");
			InvisibleEffect = CreateCooldown("invisible.effect", 10f);
			DisguiseCooldown = CreateCooldown("disguise.cooldown");
			
			_ResetOnMeeting = MakeToggle("disguise.reset_on_meeting", true);
		}

		protected override void ResetRuntime() {
			InvisibleCooldown.Reset();
			InvisibleEffect.Reset();
			DisguiseCooldown.Reset();
		}

		public override void PlayerAssigned() {
			base.PlayerAssigned();

			OriginalSkin = PersonInfo.From(Player.Data);
		}

		protected override void InitForLocalPlayer() {
			var Primary = ModActions.Primary;
			Primary.Visible = true;
			Primary.Icon = ExtraResources.INVISIBLE;
			Primary.Cooldown = InvisibleCooldown;
			Primary.Saturator = ActiveSaturator.INSTANCE;

			var Side = ModActions.Side;
			Side.Visible = true;
			Side.Icon = ExtraResources.DISGUISE;
			Side.Cooldown = DisguiseCooldown;
			Side.Saturator = ActiveSaturator.INSTANCE;
		}

		public void EnableInvisibility() {
			var Aura = Object.Instantiate(ExtraResources.INVISIBLE_PREFAB,
				Player.transform);
			Object.Destroy(Aura, Aura.GetComponent<Animator>()
				.GetCurrentAnimatorStateInfo(0).length);
			
			
			InvisibleCooldown.Use();
			IsActive = true;
		}

		public void DisableInvisibility() {
			if (Player != null && !Player.Data.IsDead) {
				Player.Visible = true;
			}
			
			IsActive = false;
		}
		
		public override void PostUpdate(HudManager Manager, bool UseEnabled, bool Dead) {
			if (IsActive) {
				if (Player != null && !IsLocalPlayer()) {
					Player.Visible = false;
				}
				
				InvisibleEffect.ForceSetLastUsedFrom(InvisibleCooldown);

				if (InvisibleEffect.GetKD(false) == 0) {
					DisableInvisibility();
				}
			}
			
			if (!IsLocalPlayer()) return;

			Player.myRend.color = IsActive ? HALF_INVISIBLE : Color.white;

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
				if (!InvisibleCooldown.IsReady()) return;
				
				ExtraNetwork.Send(CustomRPC.SpyEffect);
				EnableInvisibility();
				Acted = true;
			} else if (Type == ActionType.SIDE) {
				if (!DisguiseCooldown.IsReady()) return;
				
				var Players = PlayerControl.AllPlayerControls;
				var Random = new System.Random();
				var RandomPlayerId = Players[Random.Next(0, Players.Count)].PlayerId;

				Disguise(RandomPlayerId);
				DisguiseCooldown.Use();
				Acted = true;
			}
		}

		public void Disguise(byte AsPlayerId) {
			if (Player == null) return;
			if (Player.PlayerId == AsPlayerId) {
				OriginalSkin.Setup(Player);
				return;
			}
			
			var AsPlayer = PlayerTools.GetPlayerById(AsPlayerId);
			if (AsPlayer == null) return;

			var NewPerson = PersonInfo.From(AsPlayer.Data);
			NewPerson.Setup(Player);
		}

		public override void Intro(IntroCutscene.CoBegin__d Cutscene) {
			base.Intro(Cutscene);
			
			InvisibleCooldown.UpdateForIntro(Cutscene);
			DisguiseCooldown.UpdateForIntro(Cutscene);
		}

		public override void MeetingStart(MeetingHud Meeting) {
			base.MeetingStart(Meeting);

			if (ResetOnMeeting && IsLocalPlayer()) {
				OriginalSkin.Setup(Player);
			}
		}

		public override void MeetingEnd(ExileController Exile) {
			base.MeetingEnd(Exile);

			IsActive = false;
			InvisibleCooldown.UpdateForExile(Exile);
			DisguiseCooldown.UpdateForExile(Exile);
		}

		public override void MeetingBeforeCreatePlayer(MeetingHud Meeting, GameData.PlayerInfo PlayerInfo) {
			if (!ResetOnMeeting) return;
			if (Player == null) return;
			if (PlayerInfo.PlayerId != Player.PlayerId) return;
			
			OriginalSkin.Set(PlayerInfo);
		}

		private class PersonInfo {
			private byte Color;
			private uint Hat;
			private uint Skin;
			private uint Pet;
			private string Name;

			public static PersonInfo From(GameData.PlayerInfo PlayerInfo) {
				return new PersonInfo {
					Color = PlayerInfo.ColorId,
					Hat = PlayerInfo.HatId,
					Skin = PlayerInfo.SkinId,
					Pet = PlayerInfo.PetId,
					Name = PlayerInfo.PlayerName
				};
			}

			public void Setup(PlayerControl To) {
				To.RpcSetColor(Color);
				To.RpcSetHat(Hat);
				To.RpcSetSkin(Skin);
				To.RpcSetPet(Pet);
				To.RpcSetName(Name);
			}
			
			public void Set(GameData.PlayerInfo To) {
				To.ColorId = Color;
				To.HatId = Hat;
				To.SkinId = Skin;
				To.PetId = Pet;
				To.PlayerName = Name;
			}
		}
	}
}