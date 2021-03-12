using System.Reflection;
using Reactor.Extensions;
using Reactor.Unstrip;
using UnityEngine;
using XenoCore.Utils;

namespace ExtraRoles.Helpers {
	public static class ExtraResources {
		public static readonly ResourceLoader LOADER = new ResourceLoader("ExtraRoles.Resources.",
			Assembly.GetExecutingAssembly());
		
		// Comic Sans MS 20
		public static readonly Sprite REPAIR = LOADER.Sprite("ButtonRepair.png");
		public static readonly Sprite SHIELD = LOADER.Sprite("ButtonShield.png");
		public static readonly Sprite SPIRITS = LOADER.Sprite("Spirits.png");
		public static readonly Sprite TASKS = LOADER.Sprite("Tasks.png");
		public static readonly Sprite TIME = LOADER.Sprite("Time.png");
		public static readonly Sprite INVISIBLE = LOADER.Sprite("Invisible.png");
		public static readonly Sprite DISGUISE = LOADER.Sprite("Disguise.png");
		public static readonly Sprite INFECT = LOADER.Sprite("Infect.png");
		public static readonly Sprite CHECK = LOADER.Sprite("Check.png");
		public static readonly Sprite BOOK_RED = LOADER.Sprite("BookRed.png");
		public static readonly Sprite BOOK_GREEN = LOADER.Sprite("BookGreen.png");
		public static readonly Sprite BOOK_BLUE = LOADER.Sprite("BookBlue.png");
		public static readonly Sprite BOOK_BROWN = LOADER.Sprite("BookBrown.png");
		public static readonly Sprite CLEAN = LOADER.Sprite("Clean.png");

		public static readonly BundleDefinition BUNDLE = LOADER.Bundle("extra");
		
		public static readonly AudioClip BREAK_SHIELD = BUNDLE.Audio("BreakShield");
		
		public static readonly GameObject AURA = BUNDLE.Object("AuraPrefab");
		public static readonly GameObject TIME_WARP = BUNDLE.Object("TimeWarpPrefab");
		public static readonly GameObject INVISIBLE_PREFAB = BUNDLE.Object("InvisiblePrefab");
		public static readonly GameObject STONE_AURA = BUNDLE.Object("StoneAuraPrefab");
		public static readonly GameObject TRANSPOSITION = BUNDLE.Object("TranspositionPrefab");
	}
}