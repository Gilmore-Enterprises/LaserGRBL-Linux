using System;
using System.Diagnostics;
using System.IO;

namespace LaserGRBL.Platform.Linux
{
	/// <summary>
	/// Plays WAV files on Linux using paplay (PulseAudio) or aplay (ALSA) as fallback.
	/// Both are available on all major desktop distros.
	/// </summary>
	public class LinuxSoundPlayer : ISoundPlayer
	{
		public void Play(string wavPath)
		{
			if (string.IsNullOrEmpty(wavPath) || !File.Exists(wavPath))
				return;

			// Try paplay (PulseAudio/PipeWire) first, then aplay (ALSA)
			foreach (string player in new[] { "paplay", "aplay" })
			{
				try
				{
					var psi = new ProcessStartInfo(player, $"\"{wavPath}\"")
					{
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true
					};
					using var p = Process.Start(psi);
					if (p != null)
					{
						p.WaitForExit(3000);
						return; // success
					}
				}
				catch (Exception ex) when (ex is FileNotFoundException || ex is System.ComponentModel.Win32Exception)
				{
					// player not available, try next
				}
			}
		}
	}
}
