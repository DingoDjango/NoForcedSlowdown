using HarmonyLib;
using UnityEngine;
using Verse;

namespace No_Forced_Slowdown
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		static HarmonyPatches()
		{
#if DEBUG
			Harmony.DEBUG = true;
#endif
			
			Harmony harmony = new Harmony("dingo.rimworld.no_forced_slowdown");

			// Patch: Verse.TimeSlower.SignalForceNormalSpeed
			harmony.Patch(
				original: AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeed)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SignalForceNormalSpeed_Prefix))
				{
					priority = Priority.First
				});

			// Patch: Verse.TimeSlower.SignalForceNormalSpeedShort
			harmony.Patch(
				original: AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeedShort)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SignalForceNormalSpeed_Prefix))
				{
					priority = Priority.First
				});
		}

		public static bool Patch_SignalForceNormalSpeed_Prefix(TimeSlower __instance)
		{
			// User disabled slowdown mechanic. Utilize vanilla debug setting
			if (Settings.CurrentModFunction == Settings.SlowdownDegree.DisableGamespeedSlowdown)
			{
				DebugViewSettings.neverForceNormalSpeed = true;

				// Do not run vanilla method
				return false;
			}

			// User disabled time controls lock. Only force game speed to x1 once
			if (Settings.CurrentModFunction == Settings.SlowdownDegree.DisableGamespeedLock)
			{
				// If enough real time has passed (in seconds, setting-dependent), trigger slowdown
				if (Find.TickManager.CurTimeSpeed != TimeSpeed.Paused && Settings.LastRealTimeSlowdown + Settings.MinSecondsToNextSlowdown < Time.realtimeSinceStartup)
				{
					Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
					Settings.LastRealTimeSlowdown = Time.realtimeSinceStartup;
				}

				// Do not run vanilla method
				return false;
			}

			// User opted for regular gameplay. Run vanilla method
			// Boldly assuming no fringe cases of users enabling the debug setting along with this mod
			DebugViewSettings.neverForceNormalSpeed = false;
			return true;
		}
	}
}
