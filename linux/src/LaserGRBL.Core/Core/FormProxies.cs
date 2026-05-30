// Placeholder proxy types replacing WinForms Form parameters in GrblCore's constructor.
// The App layer provides concrete Avalonia-backed implementations in Phase 4.

namespace LaserGRBL
{
	/// <summary>Proxy for PreviewForm — the G-code visualizer panel passed to GrblCore.</summary>
	public class PreviewFormProxy
	{
		// Callbacks set by GrblCore; App layer overrides to hook the Avalonia control.
		public virtual void UpdateFrom(GrblCore core) { }
	}

	/// <summary>Proxy for JogForm — the jog control panel passed to GrblCore.</summary>
	public class JogFormProxy
	{
		public virtual void UpdateFrom(GrblCore core) { }
	}
}
