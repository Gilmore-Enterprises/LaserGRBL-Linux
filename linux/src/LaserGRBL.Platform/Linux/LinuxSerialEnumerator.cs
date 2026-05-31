using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace LaserGRBL.Platform.Linux
{
	/// <summary>
	/// Enumerates serial ports on Linux by scanning /dev for USB-serial and ACM devices.
	/// Attempts to read friendly names from /sys/bus/usb-serial/devices or udev symlinks.
	/// CH340/CP210x/FTDI are in-kernel on Linux so no driver install is needed.
	/// </summary>
	public class LinuxSerialEnumerator : ISerialEnumerator
	{
		public IEnumerable<SerialPortInfo> EnumeratePorts()
		{
			var result = new List<SerialPortInfo>();

			// Standard USB-serial patterns
			string[] patterns = { "/dev/ttyUSB*", "/dev/ttyACM*", "/dev/ttyS*", "/dev/ttyAMA*" };

			foreach (string pattern in patterns)
			{
				string dir = Path.GetDirectoryName(pattern);
				string filePattern = Path.GetFileName(pattern);

				if (!Directory.Exists(dir)) continue;

				foreach (string device in Directory.GetFiles(dir, filePattern))
				{
					string friendlyName = TryGetFriendlyName(device) ?? device;
					result.Add(new SerialPortInfo(device, friendlyName));
				}
			}

			// Sort: USB* first, then ACM, then ttyS
			result.Sort((a, b) => string.Compare(a.Device, b.Device, StringComparison.Ordinal));
			return result;
		}

		private static string TryGetFriendlyName(string devicePath)
		{
			try
			{
				// /dev/ttyUSB0 → look up via /sys/class/tty/<name>/device/
				string name = Path.GetFileName(devicePath);
				string sysPath = $"/sys/class/tty/{name}/device";

				if (Directory.Exists(sysPath))
				{
					// Read USB product string from parent hub
					string productFile = Path.Combine(sysPath, "../product");
					string manufacturerFile = Path.Combine(sysPath, "../manufacturer");

					string product = ReadSysFile(productFile);
					string manufacturer = ReadSysFile(manufacturerFile);

					if (!string.IsNullOrWhiteSpace(product) && !string.IsNullOrWhiteSpace(manufacturer))
						return $"{manufacturer} {product} ({name})";
					if (!string.IsNullOrWhiteSpace(product))
						return $"{product} ({name})";
				}

				// Fallback: try /dev/serial/by-id/ symlinks for a human-readable name
				string byId = "/dev/serial/by-id";
				if (Directory.Exists(byId))
				{
					foreach (string symlink in Directory.GetFiles(byId))
					{
						try
						{
							string target = Path.GetFullPath(Path.Combine(byId, File.ReadAllText($"/proc/self/fd/{symlink}")));
							if (string.Equals(target, devicePath, StringComparison.OrdinalIgnoreCase))
								return $"{Path.GetFileName(symlink)} ({name})";
						}
						catch { /* skip malformed symlinks */ }
					}

					// Simpler approach: readlink via udevadm if available, otherwise just use by-id name lookup
					foreach (string symlink in Directory.GetFiles(byId))
					{
						try
						{
							var info = new FileInfo(symlink);
							// Resolve symlink target
							string resolved = ResolveSysFsSymlink(symlink);
							if (resolved != null && Path.GetFileName(resolved) == name)
								return $"{Path.GetFileName(symlink).Replace("usb-", "").Split('_')[0]} ({name})";
						}
						catch { }
					}
				}
			}
			catch { /* Don't fail enumeration if we can't get a friendly name */ }

			return null;
		}

		private static string ReadSysFile(string path)
		{
			try
			{
				string full = Path.GetFullPath(path);
				if (File.Exists(full))
					return File.ReadAllText(full).Trim();
			}
			catch { }
			return null;
		}

		private static string ResolveSysFsSymlink(string symlink)
		{
			try
			{
				// Use readlink -f equivalent
				var psi = new System.Diagnostics.ProcessStartInfo("readlink", $"-f \"{symlink}\"")
				{
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				using var p = System.Diagnostics.Process.Start(psi);
				if (p == null) return null;
				string result = p.StandardOutput.ReadToEnd().Trim();
				p.WaitForExit(500);
				return result;
			}
			catch { return null; }
		}
	}
}
