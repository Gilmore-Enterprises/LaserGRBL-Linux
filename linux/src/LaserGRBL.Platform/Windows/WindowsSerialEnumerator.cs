using System.Collections.Generic;

namespace LaserGRBL.Platform.Windows
{
	/// <summary>
	/// Windows COM-port enumeration. Uses System.IO.Ports.SerialPort.GetPortNames()
	/// as the primary source; WMI (System.Management) provides friendly names when available.
	/// </summary>
	public class WindowsSerialEnumerator : ISerialEnumerator
	{
		public IEnumerable<SerialPortInfo> EnumeratePorts()
		{
			var result = new List<SerialPortInfo>();

			// Primary: .NET built-in (always works, no WMI)
			foreach (string port in System.IO.Ports.SerialPort.GetPortNames())
			{
				string friendly = TryGetFriendlyName(port);
				result.Add(new SerialPortInfo(port, friendly ?? port));
			}

			return result;
		}

		private static string TryGetFriendlyName(string portName)
		{
#if WINDOWS
			try
			{
				using var searcher = new System.Management.ManagementObjectSearcher(
					$"SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%{portName}%'");
				foreach (System.Management.ManagementObject obj in searcher.Get())
				{
					string caption = obj["Caption"] as string;
					if (!string.IsNullOrEmpty(caption)) return caption;
				}
			}
			catch { }
#endif
			return null;
		}
	}
}
