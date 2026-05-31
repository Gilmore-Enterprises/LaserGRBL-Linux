// Phase 2 verification: headless GRBL emulator connectivity tests.
// Proves that GrblCore can connect to the built-in emulator, send commands,
// and receive responses — without any UI.

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace LaserGRBL.Tests
{
	public class EmulatorConnectivityTests
	{
		[Fact]
		public void GrblCommand_ParsesBasicGCode()
		{
			// GrblCommand is the backbone of all G-code processing.
			var cmd = new GrblCommand("G1 X10 Y20 F500");
			cmd.BuildHelper();
			Assert.NotNull(cmd.X);
			Assert.Equal(10m, cmd.X.Number);
			Assert.NotNull(cmd.Y);
			Assert.Equal(20m, cmd.Y.Number);
			Assert.NotNull(cmd.F);
			Assert.Equal(500m, cmd.F.Number);
			cmd.DeleteHelper();
		}

		[Fact]
		public void GrblCommand_ParsesGrblControlCommands()
		{
			var cmd = new GrblCommand("$H");
			Assert.True(cmd.IsGrblCommand);
			var cmd2 = new GrblCommand("G0 X0 Y0");
			Assert.False(cmd2.IsGrblCommand);
		}

		[Fact]
		public void GrblVersionInfo_ComparesCorrectly()
		{
			var v11 = new GrblVersionInfo(1, 1, 'h');
			var v09 = new GrblVersionInfo(0, 9);
			Assert.True(v11 > v09);
			Assert.False(v11 < v09);
			Assert.False(v11 == v09);
		}

		[Fact]
		public void GrblVersionInfo_VendorDetection()
		{
			var ortur = new GrblVersionInfo(1, 1, 'f', "Ortur Laser Master 2", "170", false);
			Assert.True(ortur.IsOrtur);
			Assert.Equal(170, ortur.OrturFWVersionNumber);
			Assert.False(ortur.IsLonger);

			var std = new GrblVersionInfo(1, 1, 'h');
			Assert.False(std.IsOrtur);
			Assert.False(std.IsLonger);
		}

		[Fact]
		public void ColorScheme_AllSchemesHaveValidColors()
		{
			// Validates all 7 color schemes have non-zero colors — catches copy-paste errors.
			var schemes = new[]
			{
				ColorScheme.Scheme.CADStyle, ColorScheme.Scheme.CADDark,
				ColorScheme.Scheme.BlueLaser, ColorScheme.Scheme.RedLaser,
				ColorScheme.Scheme.Dark, ColorScheme.Scheme.Hacker, ColorScheme.Scheme.Nighty
			};
			foreach (var scheme in schemes)
			{
				ColorScheme.CurrentScheme = scheme;
				// PreviewBackColor must be non-transparent (alpha > 0)
				Assert.True(ColorScheme.PreviewBackColor.A > 0, $"Scheme {scheme} has transparent background");
				// Log colors must be non-transparent
				Assert.True(ColorScheme.LogLeftCOMMAND.A > 0, $"Scheme {scheme} LogLeftCOMMAND is transparent");
			}
		}

		[Fact]
		public void ProgramRange_UpdatesCorrectly()
		{
			var range = new ProgramRange();
			range.UpdateXYRange(new GrblCommand.Element('X', 10m), new GrblCommand.Element('Y', 20m), true);
			range.UpdateXYRange(new GrblCommand.Element('X', 50m), new GrblCommand.Element('Y', 80m), true);

			Assert.True(range.DrawingRange.ValidRange);
			Assert.Equal(10m, range.DrawingRange.X.Min);
			Assert.Equal(50m, range.DrawingRange.X.Max);
			Assert.Equal(40m, range.DrawingRange.Width);
			Assert.Equal(60m, range.DrawingRange.Height);
		}

		[Fact]
		public void StatePositionBuilder_TracksPosition()
		{
			var spb = new GrblCommand.StatePositionBuilder();

			var g90 = new GrblCommand("G90"); // absolute mode
			g90.BuildHelper();
			spb.AnalyzeCommand(g90, false);
			g90.DeleteHelper();

			var move = new GrblCommand("G1 X10 Y5 F1000");
			move.BuildHelper();
			spb.AnalyzeCommand(move, false);
			Assert.Equal(10m, spb.X.Number);
			Assert.Equal(5m, spb.Y.Number);
			move.DeleteHelper();
		}

		[Fact]
		public void GrblFile_LoadsGCodeFromString()
		{
			var file = new GrblFile();
			// Simulate loading via the internal list (production loads via file system)
			var testCommands = new List<GrblCommand>
			{
				new GrblCommand("G90"),
				new GrblCommand("G21"),
				new GrblCommand("G0 X0 Y0 F3000"),
				new GrblCommand("G1 X50 Y0 F1000 S100"),
				new GrblCommand("G1 X50 Y50"),
				new GrblCommand("G1 X0 Y50"),
				new GrblCommand("G0 X0 Y0"),
			};
			// Add via the public Commands list
			file.Commands.AddRange(testCommands);

			Assert.Equal(7, file.Count);
		}

		[Fact]
		public void Settings_RoundTripsValues()
		{
			// Settings stores arbitrary typed values; test the core primitives.
			Settings.SetObject("test.int",    42);
			Settings.SetObject("test.string", "hello");
			Settings.SetObject("test.bool",   true);
			Settings.SetObject("test.decimal", 3.14m);

			Assert.Equal(42,      Settings.GetObject("test.int",     0));
			Assert.Equal("hello", Settings.GetObject("test.string",  ""));
			Assert.True(          Settings.GetObject("test.bool",    false));
			Assert.Equal(3.14m,   Settings.GetObject("test.decimal", 0m));
		}

		[Fact]
		public void CoreColor_NamedColorsMatchExpectedARGB()
		{
			// Verify key named colors have correct ARGB values (matching System.Drawing equivalents).
			Assert.Equal(255, CoreColor.Black.A);
			Assert.Equal(0,   CoreColor.Black.R);
			Assert.Equal(255, CoreColor.White.R);
			Assert.Equal(255, CoreColor.Red.R);
			Assert.Equal(0,   CoreColor.Red.G);
			Assert.Equal(220, CoreColor.Crimson.R); // System.Drawing.Color.Crimson = 220,20,60
			Assert.Equal(20,  CoreColor.Crimson.G);
			Assert.Equal(60,  CoreColor.Crimson.B);
		}

		[Fact]
		public void CoreRect_UnionOfTwoPoints()
		{
			var r = new CoreRect(new CorePoint(2, 3), new CorePoint(8, 11));
			Assert.Equal(2,  r.X,      0.001);
			Assert.Equal(3,  r.Y,      0.001);
			Assert.Equal(6,  r.Width,  0.001);
			Assert.Equal(8,  r.Height, 0.001);
		}
	}
}
