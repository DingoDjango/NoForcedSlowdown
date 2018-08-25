using System.Reflection;
using Harmony;
using UnityEngine;
using Verse;

namespace NoForcedSlowdown
{
	public class Main : Mod
	{
		public Main(ModContentPack content) : base(content)
		{
			this.GetSettings<Settings>();

#if DEBUG
			HarmonyInstance.DEBUG = true;
#endif

			HarmonyInstance harmony = HarmonyInstance.Create("dingo.rimworld.no_forced_slowdown");
			harmony.PatchAll(Assembly.GetExecutingAssembly());

#if DEBUG
			Log.Message("No Forced Slowdown :: Injected Harmony patches.");
#endif
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);

			Settings.DoSettingsWindowContents(inRect.LeftHalf());
		}

		public override string SettingsCategory()
		{
			return "No Forced Slowdown";
		}
	}
}
