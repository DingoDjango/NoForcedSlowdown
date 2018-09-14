using UnityEngine;
using Verse;

namespace NoForcedSlowdown
{
	public class Main : Mod
	{
		public Main(ModContentPack content) : base(content)
		{
			this.GetSettings<Settings>();
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
