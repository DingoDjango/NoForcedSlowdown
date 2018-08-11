﻿using Harmony;
using UnityEngine;
using Verse;

namespace NoForcedSlowdown
{
	[HarmonyPatch(typeof(TimeSlower), nameof(TimeSlower.ForcedNormalSpeed), MethodType.Getter)]
	public static class Patch_TimeSlower_ForcedNormalSpeed
	{
		// Disables TimeSlower mechanics completely
		public static bool Prefix()
		{
			if (Settings.CurrentModFunction == Settings.SlowdownDegree.DisableGamespeedSlowdown)
			{
				return false;
			}

			return true;
		}

		// Allows slowdown to x1 but retains player-driven time controls
		public static void Postfix(ref bool __result)
		{
			if (Settings.CurrentModFunction == Settings.SlowdownDegree.DisableGamespeedLock && __result)
			{
				__result = false;

				if (Find.TickManager.CurTimeSpeed != TimeSpeed.Paused && Settings.LastRealTimeSlowdown + Settings.MinSecondsToNextSlowdown < Time.realtimeSinceStartup)
				{
					Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
					Settings.LastRealTimeSlowdown = Time.realtimeSinceStartup;
				}
			}
		}
	}
}
