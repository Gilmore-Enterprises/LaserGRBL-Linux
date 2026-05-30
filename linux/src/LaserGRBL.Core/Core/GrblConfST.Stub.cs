// Minimal stub for GrblConfST to allow GrblCommand to compile while GrblCore.cs
// is ported in a later Phase 1 step. The full class lands when GrblCore.cs is added.
using System.Text.RegularExpressions;

namespace LaserGRBL
{
	public partial class GrblConfST
	{
		private static readonly Regex ConfRegEX = new Regex(@"^[$](\d+)\s*=(.*)");

		public static bool IsSetConf(string p) => ConfRegEX.IsMatch(p);

		// Stubs for properties used by StateBuilder. Full implementation added with GrblCore port.
		public virtual decimal MaxRateX => 4000m;
		public virtual decimal MaxRateY => 4000m;
	}
}
