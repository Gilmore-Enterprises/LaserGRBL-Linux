using System;
using System.IO;

namespace LaserGRBL.Platform.Linux
{
	/// <summary>
	/// Registers .nc / .gcode file association on Linux via XDG .desktop file and MIME type.
	/// Writes to ~/.local/share/ (user-level, no root required).
	/// </summary>
	public class LinuxFileAssociation : IFileAssociation
	{
		public void RegisterGcodeAssociation()
		{
			try
			{
				string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string mimeDir = Path.Combine(home, ".local", "share", "mime", "packages");
				string appsDir = Path.Combine(home, ".local", "share", "applications");

				Directory.CreateDirectory(mimeDir);
				Directory.CreateDirectory(appsDir);

				// MIME type definition
				string mimeXml = Path.Combine(mimeDir, "lasergrbl-gcode.xml");
				File.WriteAllText(mimeXml, @"<?xml version=""1.0"" encoding=""UTF-8""?>
<mime-info xmlns='http://www.freedesktop.org/standards/shared-mime-info'>
  <mime-type type=""application/x-gcode"">
    <comment>G-Code file</comment>
    <glob pattern=""*.nc""/>
    <glob pattern=""*.gcode""/>
    <glob pattern=""*.ngc""/>
    <glob pattern=""*.cnc""/>
    <glob pattern=""*.tap""/>
  </mime-type>
</mime-info>");

				// .desktop entry
				string desktop = Path.Combine(appsDir, "lasergrbl.desktop");
				string exec = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "lasergrbl";
				File.WriteAllText(desktop, $@"[Desktop Entry]
Type=Application
Name=LaserGRBL
Comment=Laser engraver G-code controller
Exec={exec} %f
Icon=lasergrbl
MimeType=application/x-gcode;
Categories=Graphics;Engineering;
StartupNotify=true
");
				// Update MIME and desktop databases (ignore errors if tools not present)
				RunSilent("update-mime-database", $"\"{Path.Combine(home, ".local", "share", "mime")}\"");
				RunSilent("update-desktop-database", $"\"{appsDir}\"");
			}
			catch { /* Non-fatal — association is a nicety, not required */ }
		}

		private static void RunSilent(string tool, string args)
		{
			try
			{
				var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tool, args)
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				});
				p?.WaitForExit(3000);
			}
			catch { }
		}
	}
}
