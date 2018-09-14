using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace NoForcedSlowdown
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		private static readonly MethodInfo Call_ShouldTriggerForcedNormalSpeed = AccessTools.DeclaredProperty(typeof(HarmonyPatches), nameof(HarmonyPatches.ShouldTriggerForcedNormalSpeed)).GetGetMethod();

		static HarmonyPatches()
		{
#if DEBUG
			HarmonyInstance.DEBUG = true;
#endif

			// Default:	RimWorld.TimeControls	-	draws a horizontal line over the time controls UI when ForcedNormalSpeed
			// Default:	Verse.TickManager		-	forces speed to 0 (paused) or x1 when ForcedNormalSpeed
			HarmonyInstance harmony = HarmonyInstance.Create("dingo.rimworld.no_forced_slowdown");
			MethodInfo doTimeControlsGUI = AccessTools.Method(typeof(TimeControls), nameof(TimeControls.DoTimeControlsGUI));
			MethodInfo tickRateMultiplier = AccessTools.DeclaredProperty(typeof(TickManager), nameof(TickManager.TickRateMultiplier)).GetGetMethod();

#if DEBUG
			// Check for null references
			//	Log.Message($"No Forced Slowdown :: MethodInfo _ForcedNormalSpeed = {AccessTools.DeclaredProperty(typeof(TimeSlower), nameof(TimeSlower.ForcedNormalSpeed)).GetGetMethod()}");
			Log.Message($"No Forced Slowdown :: MethodInfo _ModdedForcedNormalSpeed = {Call_ShouldTriggerForcedNormalSpeed.ToString()}");
			Log.Message($"No Forced Slowdown :: MethodInfo doTimeControlsGUI = {doTimeControlsGUI.ToString()}");
			Log.Message($"No Forced Slowdown :: MethodInfo tickRateMultiplier = {tickRateMultiplier.ToString()}");
#endif

			// Since Verse.TimeSlower.ForcedNormalSpeed gets inlined, we use HarmonyTranspiler patches
			harmony.Patch(doTimeControlsGUI, null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_TimeControls_DoTimeControlsGUI)));
			harmony.Patch(tickRateMultiplier, null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_TickManager_TickRateMultiplier)));

#if DEBUG
			Log.Message("No Forced Slowdown :: Injected Harmony patches.");
#endif
		}

		// Remove calls to a TimeSlower instance in a method, use HarmonyPatches.ShouldTriggerForcedNormalSpeed instead.
		private static IEnumerable<CodeInstruction> ReplaceTimeSlowerCall(this IEnumerable<CodeInstruction> source, string methodIdentifier)
		{
			int startMethodCall = -1, endMethodCall = -1;

			List<CodeInstruction> codes = new List<CodeInstruction>(source);

			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("TimeSlower"))
				{
					startMethodCall = i - 1;

					for (int j = i + 1; j < codes.Count; j++)
					{
						if (codes[j].opcode == OpCodes.Brfalse)
						{
							endMethodCall = j;
							break;
						}
					}

					break;
				}
			}

#if DEBUG
			Log.Error($"{methodIdentifier} : StartIndex = {startMethodCall.ToString()} / EndIndex = {endMethodCall.ToString()}");
#endif

			if (startMethodCall > -1 && endMethodCall > -1)
			{
				codes[startMethodCall].opcode = OpCodes.Call;
				codes[startMethodCall].operand = Call_ShouldTriggerForcedNormalSpeed;
				codes.RemoveRange(startMethodCall + 1, endMethodCall - startMethodCall - 1);
			}

			return codes.AsEnumerable();
		}

		// Replacement method for Verse.TimeSlower.ForcedNormalSpeed
		public static bool ShouldTriggerForcedNormalSpeed
		{
			get
			{
				// Case: user disabled slowdown mechanic.	Result: always return false.
				if (Settings.CurrentModFunction == Settings.SlowdownDegree.DisableGamespeedSlowdown)
				{
					return false;
				}

				// Case: user disabled time controls lock.	Result: force slowdown, let user change game speed.
				bool slowdownTriggered = Find.TickManager.slower.ForcedNormalSpeed;

				if (Settings.CurrentModFunction == Settings.SlowdownDegree.DisableGamespeedLock && slowdownTriggered)
				{
					// If enough time has passed since last slowdown (setting-dependent), mimic vanilla behaviour. Returning slowdownTriggered would only work for 1 tick.
					if (Find.TickManager.CurTimeSpeed != TimeSpeed.Paused && Settings.LastRealTimeSlowdown + Settings.MinSecondsToNextSlowdown < Time.realtimeSinceStartup)
					{
						Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
						Settings.LastRealTimeSlowdown = Time.realtimeSinceStartup;
					}

					return false;
				}

				return slowdownTriggered;
			}
		}

		public static IEnumerable<CodeInstruction> Patch_TimeControls_DoTimeControlsGUI(IEnumerable<CodeInstruction> instructions)
		{
			return instructions.ReplaceTimeSlowerCall("Patch_TimeControls_DoTimeControlsGUI");
		}

		public static IEnumerable<CodeInstruction> Patch_TickManager_TickRateMultiplier(IEnumerable<CodeInstruction> instructions)
		{
			return instructions.ReplaceTimeSlowerCall("Patch_TickManager_TickRateMultiplier");
		}
	}
}
