using System.IO;

namespace LaserGRBL.Platform.Windows
{
	/// <summary>Plays WAV files using System.Media.SoundPlayer (Windows-only).</summary>
	public class WindowsSoundPlayer : ISoundPlayer
	{
		public void Play(string wavPath)
		{
			if (string.IsNullOrEmpty(wavPath) || !File.Exists(wavPath)) return;
#if WINDOWS
			try
			{
				using var player = new System.Media.SoundPlayer(wavPath);
				player.PlaySync();
			}
			catch { }
#endif
		}
	}
}
