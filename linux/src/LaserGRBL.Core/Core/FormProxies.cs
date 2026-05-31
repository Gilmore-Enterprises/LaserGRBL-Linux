// Placeholder proxy types replacing WinForms Form parameters in GrblCore's constructor.
// The App layer provides concrete Avalonia-backed implementations in Phase 4.

namespace LaserGRBL
{
	/// <summary>Proxy for PreviewForm — the G-code visualizer panel passed to GrblCore.</summary>
	public class PreviewFormProxy
	{
		// Callbacks set by GrblCore; App layer overrides to hook the Avalonia control.
		public virtual void UpdateFrom(GrblCore core) { }
		/// <summary>Simulate pressing a custom image button. App layer overrides to trigger the Avalonia button.</summary>
		public virtual void EmulateCustomButtonDown(int index) { }
		/// <summary>Simulate releasing a custom image button. App layer overrides to trigger the Avalonia button.</summary>
		public virtual void EmulateCustomButtonUp(int index) { }
	}

	/// <summary>Proxy for JogForm — the jog control panel passed to GrblCore.</summary>
	public class JogFormProxy
	{
		public virtual void UpdateFrom(GrblCore core) { }
		/// <summary>Change jog step size by delta index. App layer overrides to update the step slider.</summary>
		public virtual void ChangeJogStepIndexBy(int delta) { }
		/// <summary>Change jog speed by delta index. App layer overrides to update the speed slider.</summary>
		public virtual void ChangeJogSpeedIndexBy(int delta) { }
	}
}
