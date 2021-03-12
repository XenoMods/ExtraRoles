using ExtraRoles.Helpers;
using ExtraRoles.Roles;
using Hazel;
using XenoCore.Network;
using XenoCore.Utils;

namespace ExtraRoles {
	public enum RPC {
		PlayAnimation = 0,
		CompleteTask = 1,
		SyncSettings = 2,
		SetInfected = 3,
		Exiled = 4,
		CheckName = 5,
		SetName = 6,
		CheckColor = 7,
		SetColor = 8,
		SetHat = 9,
		SetSkin = 10,
		ReportDeadBody = 11,
		MurderPlayer = 12,
		SendChat = 13,
		StartMeeting = 14,
		SetScanner = 15,
		SendChatNote = 16,
		SetPet = 17,
		SetStartCounter = 18,
		EnterVent = 19,
		ExitVent = 20,
		SnapTo = 21,
		Close = 22,
		VotingComplete = 23,
		CastVote = 24,
		ClearVote = 25,
		AddVote = 26,
		CloseDoorsOfType = 27,
		RepairSystem = 28,
		SetTasks = 29,
		UpdateGameData = 30,
	}

	public enum CustomRPC : byte {
		SetRole = 43,
		SetProtected = 44,
		ShieldBreak = 45,
		OfficerKill = 46,
		FixLights = 47,
		ResetStartingRoles = 49,
		SingleWin = 50,
		ProtectedMurderAttempt = 51,
		ZombieInfect = 52,
		PsychicEffect = 53,
		ScientistEffect = 54,
		SpyEffect = 55,
		ZombieChainKill = 56,
		MageStonefication = 57,
		MageTransposition = 58,
		CleanerClean = 59,
		
		DebugVictory = 80
	}
	
	public class RPCExtraRoles : RPCListener {
		public void Handle(byte PacketId, MessageReader Reader) {
			switch (PacketId) {
				case (byte) CustomRPC.ShieldBreak: {
					MedicRole.INSTANCE.BreakShield();
					break;
				}
				case (byte) CustomRPC.FixLights: {
					SabotageCentralPatch.FixLights();
					break;
				}
				case (byte) CustomRPC.ResetStartingRoles: {
					// EndGameCentral.SetPeaceful(Reader.ReadBytesAndSize());
					EndGameCentral.ResetStartingRoles();
					break;
				}
				case (byte) CustomRPC.SetProtected: {
					if (XenoCore.Utils.Extensions.TryGetPlayerById(Reader.ReadByte(), out var Player)) {
						MedicRole.INSTANCE.Protected = Player;
					}
					break;
				}
				case (byte) CustomRPC.SetRole: {
					var Role = Roles.Role.ROLES[Reader.ReadByte()];
					Role.SetPlayerById(Reader.ReadByte());
					Role.PlayerAssigned();
					break;
				}
				case (byte) CustomRPC.OfficerKill: {
					var Killer = PlayerTools.GetPlayerById(Reader.ReadByte());
					var Target = PlayerTools.GetPlayerById(Reader.ReadByte());
					Killer.MurderPlayer(Target);
					break;
				}
				case (byte) CustomRPC.SingleWin: {
					EndGameCentral.RoleVictory = Role.ROLES[Reader.ReadByte()];
					
					foreach (var player in PlayerControl.AllPlayerControls) {
						player.RemoveInfected();
					}

					PlayerControl.LocalPlayer.SetInfected(new[] {
						Reader.ReadByte()
					});
					break;
				}
				case (byte) CustomRPC.ProtectedMurderAttempt: {
					var PlayerId = Reader.ReadByte();

					if (PlayerId == PlayerControl.LocalPlayer.PlayerId) {
						ExtraResources.BREAK_SHIELD.PlayGlobally(
							AudioManager.EffectsScale(3f));
					}

					break;
				}
				case (byte) CustomRPC.PsychicEffect: {
					var Psychic = PsychicRole.INSTANCE;
					Psychic.EnableAura();
					break;
				}
				case (byte) CustomRPC.ScientistEffect: {
					var Scientist = ScientistRole.INSTANCE;
					Scientist.EnableTimeWarp();
					break;
				}
				case (byte) CustomRPC.SpyEffect: {
					var Spy = SpyRole.INSTANCE;
					Spy.EnableInvisibility();
					break;
				}
				case (byte) CustomRPC.ZombieInfect: {
					var Zombie = ZombieRole.INSTANCE;
					var Target = PlayerTools.GetPlayerById(Reader.ReadByte());

					if (Target != null) {
						Zombie.Infect(Target);
					}

					break;
				}
				case (byte) CustomRPC.ZombieChainKill: {
					ZombieRole.INSTANCE.KillTargets(Reader.ReadBytesAndSize());
					break;
				}
				case (byte) CustomRPC.DebugVictory: {
					DebugTools.DebugWin(PlayerTools.GetPlayerById(Reader.ReadByte()));
					break;
				}
				case (byte) CustomRPC.MageStonefication: {
					MageRole.INSTANCE.EnableStonefication(Reader.ReadBytesAndSize());
					break;
				}
				case (byte) CustomRPC.MageTransposition: {
					var Partner = PlayerTools.GetPlayerById(Reader.ReadByte());
					if (Partner != null) {
						MageRole.INSTANCE.Transposition(Partner);
					}

					break;
				}
				case (byte) CustomRPC.CleanerClean: {
					CleanerRole.INSTANCE.Clean(Reader.ReadByte());
					break;
				}
			}
		}
	}
}