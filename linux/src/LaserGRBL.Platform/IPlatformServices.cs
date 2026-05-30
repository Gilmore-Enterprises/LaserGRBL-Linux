// Phase 0 scaffolding: OS-abstraction interfaces. Implementations land in Phase 1-2.
using System.Collections.Generic;

namespace LaserGRBL.Platform
{
	/// <summary>Discovers available serial ports (Windows: WMI; Linux: /dev/ttyUSB*, /dev/ttyACM*).</summary>
	public interface ISerialEnumerator
	{
		IEnumerable<SerialPortInfo> EnumeratePorts();
	}

	public readonly struct SerialPortInfo
	{
		public SerialPortInfo(string device, string friendlyName)
		{
			Device = device;
			FriendlyName = friendlyName;
		}
		public string Device { get; }        // e.g. "COM3" or "/dev/ttyUSB0"
		public string FriendlyName { get; }  // human-readable description, when available
	}

	/// <summary>Per-user data/config directory (already cross-platform via SpecialFolder.ApplicationData).</summary>
	public interface IPathProvider
	{
		string DataPath { get; }
		string AssetPath { get; } // bundled resources (sounds, csv, firmware)
	}

	/// <summary>Plays the app's notification sounds (Windows: SoundPlayer; Linux: paplay/aplay).</summary>
	public interface ISoundPlayer
	{
		void Play(string wavPath);
	}

	/// <summary>Encrypts/decrypts small secrets (Windows: DPAPI; Linux: AES keyfile / libsecret).</summary>
	public interface ISecretStore
	{
		string Encrypt(string plaintext);
		string Decrypt(string ciphertext, string fallback);
	}

	/// <summary>Locates and launches external tools (autotrace, avrdude) from system packages.</summary>
	public interface IExternalTool
	{
		bool TryResolve(string toolName, out string executablePath);
		int Run(string executablePath, string arguments, out string stdout, out string stderr);
	}

	/// <summary>OS/runtime identification (replaces registry/WMI-based OSHelper).</summary>
	public interface IOsInfo
	{
		bool Is64BitProcess { get; }
		string Describe(); // single-line OS + CLR description for diagnostics
	}

	/// <summary>Registers file association (Windows: registry; Linux: .desktop + MIME).</summary>
	public interface IFileAssociation
	{
		void RegisterGcodeAssociation();
	}
}
