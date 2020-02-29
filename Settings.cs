using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace No_Forced_Slowdown
{
	public class Settings : ModSettings
	{
		public enum SlowdownDegree
		{
			RegularGameplay,
			DisableGamespeedLock,
			DisableGamespeedSlowdown
		}

		private static string minSecondsTextBuffer = "60"; // Unsaved

		public static SlowdownDegree CurrentModFunction = SlowdownDegree.DisableGamespeedSlowdown;

		public static int MinSecondsToNextSlowdown = 60;

		public static float LastRealTimeSlowdown; // Unsaved

		public static void DoSettingsWindowContents(Rect rect)
		{
			IEnumerable<SlowdownDegree> allDegrees = Enum.GetValues(typeof(SlowdownDegree)).Cast<SlowdownDegree>();
			Listing_Standard modOptions = new Listing_Standard();

			modOptions.Begin(rect);
			modOptions.Gap(20f);

			modOptions.Label("NFS.ModFunctions".Translate());

			// Toggle how much the mod interferes with vanilla TimeSlower
			foreach (SlowdownDegree degree in allDegrees)
			{
				string name = "NFS." + degree.ToString();
				string label = name.Translate();
				bool active = CurrentModFunction == degree;
				string tooltip = (name + ".Tooltip").Translate();

				if (modOptions.RadioButton(label, active, default, tooltip))
				{
					CurrentModFunction = degree;

#if DEBUG
					Log.Message($"No Forced Slowdown :: [{CurrentModFunction.ToString()}] has been selected.");
#endif
				}
			}

			modOptions.Gap(30f);

			// Determine real time seconds timer for Settings.MinSecondsToNextSlowdown
			Rect TextFieldRect = modOptions.GetRect(Text.LineHeight);
			Rect labelRect = TextFieldRect.LeftPart(0.75f);
			Rect inputRect = TextFieldRect.RightPart(0.20f);

			Widgets.Label(labelRect, "NFS.MinSecondsToNextSlowdown".Translate());
			Widgets.DrawHighlightIfMouseover(labelRect);
			TooltipHandler.TipRegion(labelRect, "NFS.MinSecondsToNextSlowdown.Tooltip".Translate());
			Widgets.TextFieldNumeric(inputRect, ref MinSecondsToNextSlowdown, ref minSecondsTextBuffer);

			modOptions.End();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref CurrentModFunction, "NFS_CurrentModFunction", SlowdownDegree.DisableGamespeedSlowdown);
			Scribe_Values.Look(ref MinSecondsToNextSlowdown, "NFS_MinSecondsToNextSlowdown", 60);

			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				// Set unsaved values
				LastRealTimeSlowdown = -1 * MinSecondsToNextSlowdown;
				minSecondsTextBuffer = MinSecondsToNextSlowdown.ToString();
			}
		}
	}
}
