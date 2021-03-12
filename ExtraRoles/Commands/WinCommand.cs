using System.Collections.Generic;
using ExtraRoles.Helpers;
using XenoCore.Commands;
using XenoCore.Utils;

namespace ExtraRoles.Commands {
	public class WinCommand : ICommand {
		public string Name() => "win";
		public string Usage() => "/win";

		public void Run(ChatCallback Callback, List<string> Args) {
			var Player = PlayerControl.LocalPlayer;

			ExtraNetwork.Send(CustomRPC.DebugVictory, Writer => {
				Writer.Write(Player.PlayerId);
			});

			DebugTools.DebugWin(Player);
		}
	}
}