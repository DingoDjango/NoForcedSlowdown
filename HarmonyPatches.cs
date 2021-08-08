using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace No_Forced_Slowdown
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
#if TRUE // NEW CODE
		static HarmonyPatches()
		{
			Harmony harmony = new Harmony("dingo.rimworld.no_forced_slowdown");

			harmony.Patch(
				AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeed)), 
				new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SignalForceNormalSpeed_Prefix)));
			harmony.Patch(
				AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeedShort)), 
				new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SignalForceNormalSpeed_Prefix)));
		}

		public static bool Patch_SignalForceNormalSpeed_Prefix(TimeSlower __instance)
		{
			switch (Settings.CurrentModFunction)
			{
				case Settings.SlowdownDegree.RegularGameplay:
					break;
				case Settings.SlowdownDegree.DisableGamespeedLock:
					{
						// if enough time has passed since last slowdown (depending on settings), slow down gamespeed, but do not lock it
						if (Find.TickManager.CurTimeSpeed != TimeSpeed.Paused && Settings.LastRealTimeSlowdown + Settings.MinSecondsToNextSlowdown < Time.realtimeSinceStartup)
						{
							// execute slowdown
							Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
							// remember last slowdown timestamp
							Settings.LastRealTimeSlowdown = Time.realtimeSinceStartup;
						}

						// skip original function - do not lock gamespeed
						return false;
					}
				case Settings.SlowdownDegree.DisableGamespeedSlowdown:
					{
						// set forceNormalSpeedUntil (private variable) to 0 just in case
						Traverse.Create(__instance).Field("forceNormalSpeedUntil").SetValue(0);

						// skip original function - do not slowdown & do not lock gamespeed
						return false;
					}
			}
			// execute original function - trigger slowdown & lock gamespeed
			return true;
		}

#else // OLD CODE

		private static readonly MethodInfo Call_ShouldTriggerForcedNormalSpeed = AccessTools.DeclaredProperty(typeof(HarmonyPatches), nameof(HarmonyPatches.ShouldTriggerForcedNormalSpeed)).GetGetMethod();

		static HarmonyPatches()
		{
#if DEBUG
			Harmony.DEBUG = true;
#endif

			// Default: RimWorld.TimeControls - draws a horizontal line over the time controls UI when ForcedNormalSpeed.
			// Default: Verse.TickManager     - forces speed to 0 (paused) or x1 when ForcedNormalSpeed.
			Harmony harmony = new Harmony("dingo.rimworld.no_forced_slowdown");
			MethodInfo doTimeControlsGUI = AccessTools.Method(typeof(TimeControls), nameof(TimeControls.DoTimeControlsGUI));
			MethodInfo tickRateMultiplier = AccessTools.DeclaredProperty(typeof(TickManager), nameof(TickManager.TickRateMultiplier)).GetGetMethod();

#if DEBUG
			// Check for null references
			// Log.Message($"No Forced Slowdown :: MethodInfo _ForcedNormalSpeed = {AccessTools.DeclaredProperty(typeof(TimeSlower), nameof(TimeSlower.ForcedNormalSpeed)).GetGetMethod()}");
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

		/* Example IL from RimWorld.TimeControls.DoTimeControlsGUI:
		 * IL_00E5: call      class Verse.TickManager Verse.Find::get_TickManager()
		 * IL_00EA: ldfld     class Verse.TimeSlower Verse.TickManager::slower
		 * IL_00EF: callvirt  instance bool Verse.TimeSlower::get_ForcedNormalSpeed()
		 * IL_00F4: brfalse   IL_0125 */
		private static IEnumerable<CodeInstruction> ReplaceTimeSlowerCall(this IEnumerable<CodeInstruction> source)
		{
			FieldInfo tickManagerSlower = AccessTools.Field(typeof(TickManager), nameof(TickManager.slower));
			bool replacedMethodCall = false; // Used to prevent ArgumentOutOfRange for codes[i + 1].
			List<CodeInstruction> codes = source.ToList();

			for (int i = 0; i < codes.Count; i++)
			{
				CodeInstruction instruction = codes[i];

				if (!replacedMethodCall && codes[i + 1].operand == tickManagerSlower)
				{
					instruction = new CodeInstruction(OpCodes.Call, Call_ShouldTriggerForcedNormalSpeed); // Replace the start of the method call chain.
					replacedMethodCall = true;
					i+=2; // The next iteration will reach brfalse.
				}

				yield return instruction;
			}
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
			return instructions.ReplaceTimeSlowerCall();
		}

		public static IEnumerable<CodeInstruction> Patch_TickManager_TickRateMultiplier(IEnumerable<CodeInstruction> instructions)
		{
			return instructions.ReplaceTimeSlowerCall();
		}
#endif
	}
}
