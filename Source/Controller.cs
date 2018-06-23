using System.Reflection;
using Harmony;
using Verse;

namespace NoForcedSlowdown
{
	public class Controller : Mod
	{
		public Controller(ModContentPack content) : base(content)
		{
			HarmonyInstance.Create("dingo.rimworld.no_forced_slowdown").PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}
