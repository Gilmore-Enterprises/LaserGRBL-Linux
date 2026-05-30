using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace LaserGRBL.Tests
{
	// Phase 1 regression tests for the type-tagged JSON settings store that replaces
	// BinaryFormatter. Verifies heterogeneous values round-trip with their exact CLR type.
	public class TaggedJsonStoreTests
	{
		private enum Sample { A = 0, B = 7, C = 42 }

		public class Poco
		{
			public string Name { get; set; }
			public int Count { get; set; }
		}

		private static Dictionary<string, object> RoundTrip(Dictionary<string, object> input)
		{
			string json = TaggedJsonStore.Serialize(input);
			return TaggedJsonStore.Deserialize(json);
		}

		[Fact]
		public void Scalars_RoundTripWithExactTypes()
		{
			var input = new Dictionary<string, object>
			{
				["b"] = true,
				["i"] = 123,
				["l"] = 9000000000L,
				["d"] = 3.5,
				["m"] = 12.34m,
				["s"] = "hello",
			};

			var output = RoundTrip(input);

			Assert.IsType<bool>(output["b"]);
			Assert.Equal(true, output["b"]);
			Assert.IsType<int>(output["i"]);
			Assert.Equal(123, output["i"]);
			Assert.IsType<long>(output["l"]);
			Assert.Equal(9000000000L, output["l"]);
			Assert.Equal(3.5, output["d"]);
			Assert.IsType<decimal>(output["m"]);
			Assert.Equal(12.34m, output["m"]);
			Assert.Equal("hello", output["s"]);
		}

		[Fact]
		public void Version_RoundTrips()
		{
			var output = RoundTrip(new Dictionary<string, object> { ["v"] = new Version(1, 2, 3, 4) });
			Assert.IsType<Version>(output["v"]);
			Assert.Equal(new Version(1, 2, 3, 4), output["v"]);
		}

		[Fact]
		public void CultureInfo_RoundTrips()
		{
			var output = RoundTrip(new Dictionary<string, object> { ["c"] = new CultureInfo("zh-CN") });
			var ci = Assert.IsType<CultureInfo>(output["c"]);
			Assert.Equal("zh-CN", ci.Name);
		}

		[Fact]
		public void Enum_RoundTripsToExactType()
		{
			var output = RoundTrip(new Dictionary<string, object> { ["e"] = Sample.C });
			Assert.IsType<Sample>(output["e"]);
			Assert.Equal(Sample.C, output["e"]);
		}

		[Fact]
		public void ObjectArray_RoundTripsMixedElements()
		{
			var input = new Dictionary<string, object>
			{
				["arr"] = new object[] { 10, "x", true, new Version(2, 0) },
			};

			var output = RoundTrip(input);

			var arr = Assert.IsType<object[]>(output["arr"]);
			Assert.Equal(4, arr.Length);
			Assert.Equal(10, arr[0]);
			Assert.Equal("x", arr[1]);
			Assert.Equal(true, arr[2]);
			Assert.Equal(new Version(2, 0), arr[3]);
		}

		[Fact]
		public void Poco_RoundTripsViaFallback()
		{
			var input = new Dictionary<string, object> { ["p"] = new Poco { Name = "jog", Count = 5 } };
			var output = RoundTrip(input);
			var p = Assert.IsType<Poco>(output["p"]);
			Assert.Equal("jog", p.Name);
			Assert.Equal(5, p.Count);
		}

		[Fact]
		public void Null_RoundTrips()
		{
			var output = RoundTrip(new Dictionary<string, object> { ["n"] = null });
			Assert.True(output.ContainsKey("n"));
			Assert.Null(output["n"]);
		}
	}
}
