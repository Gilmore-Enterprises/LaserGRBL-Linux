using LaserGRBL.CSV;
using Xunit;

namespace LaserGRBL.Tests
{
	// Phase 1 regression test: the GRBL error/alarm/setting code tables load from
	// embedded resources in LaserGRBL.Core and resolve known codes. Proves both the
	// CSV parser port and the embedded-resource naming (LaserGRBL.CSV.*) on .NET 10.
	public class CsvDictionaryTests
	{
		[Fact]
		public void ErrorCodes_ResolveKnownCode()
		{
			var errors = new CsvDictionary("LaserGRBL.CSV.error_codes.csv", 2);
			Assert.Equal("Expected command letter", errors.GetItem("1", 0));
			Assert.Equal(
				"G-code words consist of a letter and a value. Letter was not found.",
				errors.GetItem("1", 1));
		}

		[Fact]
		public void AlarmCodes_ResolveKnownCode()
		{
			var alarms = new CsvDictionary("LaserGRBL.CSV.alarm_codes.csv", 2);
			Assert.Equal("Hard limit", alarms.GetItem("1", 0));
		}

		[Fact]
		public void SettingCodes_v11_ResolveKnownCode()
		{
			// $0 is the step-pulse-time setting in Grbl v1.1; len=3 (label, unit, description).
			var settings = new CsvDictionary("LaserGRBL.CSV.setting_codes.v1.1.csv", 3);
			Assert.NotNull(settings.GetItem("0", 0));
		}

		[Fact]
		public void UnknownKey_ReturnsNull()
		{
			var errors = new CsvDictionary("LaserGRBL.CSV.error_codes.csv", 2);
			Assert.Null(errors.GetItem("999999", 0));
		}

		[Fact]
		public void StringList_RoundTripsEscapedSeparators()
		{
			// The "," split path used when parsing CSV rows must survive escaped tokens.
			var list = StringList.FromMessage("a,b,c", ",");
			Assert.Equal(3, list.Count);
			Assert.Equal("a", list[0]);
			Assert.Equal("c", list[2]);
		}
	}
}
