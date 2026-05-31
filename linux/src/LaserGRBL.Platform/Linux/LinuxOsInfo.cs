using System;
using System.Runtime.InteropServices;

namespace LaserGRBL.Platform.Linux
{
	public class LinuxOsInfo : IOsInfo
	{
		public bool Is64BitProcess => Environment.Is64BitProcess;

		public string Describe()
		{
			string rid = RuntimeInformation.RuntimeIdentifier;
			string clr = RuntimeInformation.FrameworkDescription;
			string osVer = RuntimeInformation.OSDescription;
			return $"{osVer}|{clr}|{rid}";
		}
	}
}
