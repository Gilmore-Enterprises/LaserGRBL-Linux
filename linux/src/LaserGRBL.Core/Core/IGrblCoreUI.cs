// UI interaction interface injected into GrblCore so the Core project has no
// dependency on WinForms, Avalonia, or any UI toolkit.
// The App layer provides a real implementation; tests use a stub.

namespace LaserGRBL
{
	public interface IGrblCoreUI
	{
		// --- Thread marshalling (replaces System.Windows.Forms.Control.BeginInvoke) ---

		/// <summary>Post an action to the UI thread. Safe to call from any thread.</summary>
		void BeginInvoke(System.Action action);

		// --- Modal dialogs ---

		/// <summary>Show a non-fatal error message to the user.</summary>
		void ShowError(string message, string title);

		/// <summary>Ask a yes/no question. Returns true if user confirmed.</summary>
		bool AskConfirmation(string message, string title);

		// --- File dialogs ---

		/// <summary>Show an open-file dialog. Returns null if user cancelled.</summary>
		string ShowOpenFileDialog(string lastFile, string filter);

		/// <summary>Show a save-file dialog for G-code. Returns null if user cancelled.</summary>
		string ShowSaveGCodeDialog(string defaultExt, string filter);

		/// <summary>Show a save-file dialog for a LaserGRBL project (.lps). Returns null if user cancelled.</summary>
		string ShowSaveProjectDialog();

		// --- Import flow launchers (GrblCore calls these; the UI drives the actual dialog) ---

		void ShowRasterImport(GrblCore core, string filename, bool append);
		void ShowVectorImport(GrblCore core, string filename, bool append);

		// --- Job-run dialogs (return values consumed by GrblCore logic) ---

		/// <summary>
		/// Show the "Run from position" dialog.
		/// Returns the selected line index, or -1 if user cancelled.
		/// </summary>
		int ShowRunFromPosition(int totalCount, bool homingEnabled, out bool homing);

		/// <summary>
		/// Show the "Resume job" dialog.
		/// Returns the resume line, or -1 if user cancelled.
		/// </summary>
		int ShowResumeJob(int executed, int sent, int target, object lastIssue, bool homingEnabled,
			bool homing, out bool homingOut, bool setwco, out bool setwcoOut, object lastKnownWCO);

		// --- Laser selector ---

		/// <summary>Show the laser selector dialog. Returns the selected GUID or null.</summary>
		string ShowLaserSelector();
	}
}
