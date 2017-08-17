using Harmony;
using Verse;

namespace No_Forced_Slowdown
{
	[HarmonyPatch(typeof(TimeSlower))]
	[HarmonyPatch(nameof(TimeSlower.SignalForceNormalSpeed))]
	public class Patch_TimeSlower_SignalForceNormalSpeed
	{
		public static bool Prefix()
		{
			return false;
		}
	}

	[HarmonyPatch(typeof(TimeSlower))]
	[HarmonyPatch(nameof(TimeSlower.SignalForceNormalSpeedShort))]
	public class Patch_TimeSlower_SignalForceNormalSpeedShort
	{
		public static bool Prefix()
		{
			return false;
		}
	}
}
