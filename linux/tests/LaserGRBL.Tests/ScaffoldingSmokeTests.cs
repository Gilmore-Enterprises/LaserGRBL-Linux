using Xunit;

namespace LaserGRBL.Tests
{
	// Phase 0 smoke test: proves the layered solution wires together and the
	// test runner executes on Linux CI. Replaced by real Core/Imaging regression
	// tests starting in Phase 1.
	public class ScaffoldingSmokeTests
	{
		[Fact]
		public void CoreLayer_IsReferenced()
		{
			Assert.Equal("LaserGRBL.Core", LaserGRBL.CoreAssemblyMarker.Layer);
		}

		[Fact]
		public void ImagingLayer_IsReferenced()
		{
			Assert.Equal("LaserGRBL.Imaging", LaserGRBL.Imaging.ImagingAssemblyMarker.Layer);
		}
	}
}
