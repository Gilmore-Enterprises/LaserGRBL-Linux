using System;
using System.Diagnostics;
using System.IO;

namespace LaserGRBL.Platform.Linux
{
	/// <summary>
	/// Locates and runs external tools (autotrace, avrdude) from system packages.
	/// On Linux these are expected to be installed via apt/dnf rather than bundled.
	/// </summary>
	public class LinuxExternalTool : IExternalTool
	{
		public bool TryResolve(string toolName, out string executablePath)
		{
			// Try 'which' to locate the tool on PATH
			executablePath = RunWhich(toolName);
			if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
				return true;

			// Check common install locations as fallback
			string[] commonPaths = {
				$"/usr/bin/{toolName}",
				$"/usr/local/bin/{toolName}",
				$"/bin/{toolName}"
			};
			foreach (string p in commonPaths)
			{
				if (File.Exists(p)) { executablePath = p; return true; }
			}

			executablePath = null;
			return false;
		}

		public int Run(string executablePath, string arguments, out string stdout, out string stderr)
		{
			try
			{
				var psi = new ProcessStartInfo(executablePath, arguments)
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				};
				using var p = Process.Start(psi)!;
				stdout = p.StandardOutput.ReadToEnd();
				stderr = p.StandardError.ReadToEnd();
				p.WaitForExit(30000);
				return p.ExitCode;
			}
			catch (Exception ex)
			{
				stdout = "";
				stderr = ex.Message;
				return -1;
			}
		}

		private static string RunWhich(string name)
		{
			try
			{
				var psi = new ProcessStartInfo("which", name)
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				};
				using var p = Process.Start(psi);
				if (p == null) return null;
				string result = p.StandardOutput.ReadToEnd().Trim();
				p.WaitForExit(1000);
				return result;
			}
			catch { return null; }
		}
	}
}
