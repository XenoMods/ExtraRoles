using System.Linq;
using ExtraRoles.Helpers;
using ExtraRoles.Roles;
using HarmonyLib;
using UnhollowerBaseLib;
using XenoCore.Utils;

namespace ExtraRoles {
	[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
	public class AssignRolesPatch {
		public static void Postfix(Il2CppReferenceArray<GameData.PlayerInfo> JPGEIBIBJPJ) {
			AssignRoles(true);
			AssignRoles(false);

			{
				ExtraNetwork.Send(CustomRPC.ResetStartingRoles);
				EndGameCentral.ResetStartingRoles();
			}
		}

		private static void AssignRoles(bool Impostor) {
			var Peoples = PlayerControl.AllPlayerControls.ToArray().Shuffle().ToList();
			Peoples.RemoveAll(x => x.Data.IsImpostor != Impostor);

			var FreeRoles = Role.ROLES.Where(Role => Role.Enable
			                                         && Role.ImpostorRole == Impostor).ToList();
			foreach (var SomeGuy in Peoples) {
				if (FreeRoles.Count == 0) {
					break;
				}
				
				var Role = FreeRoles.RandomItem();
				Role.Player = SomeGuy;
				FreeRoles.Remove(Role);

				ExtraNetwork.Send(CustomRPC.SetRole, Writer => {
					Writer.Write(Role.RoleNo);
					Writer.Write(Role.Player.PlayerId);
				});

				Role.PlayerAssigned();
			}
		}
	}
}