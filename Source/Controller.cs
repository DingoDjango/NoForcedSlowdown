using System.Reflection;
using Harmony;
using Verse;

namespace No_Forced_Slowdown
{
	public class Controller : Mod
	{
		public Controller(ModContentPack content) : base(content)
		{
			HarmonyInstance harmony = HarmonyInstance.Create("dingo.rimworld.no_forced_slowdown");

			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}
