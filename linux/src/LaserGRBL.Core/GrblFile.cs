//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
// System.Drawing / Drawing2D / WinForms removed — see CoreColor, CorePoint, CoreSize.
// CsPotrace, SvgConverter, Obj3D, SharpGL — stubs here; full bodies in Phase 3 (Imaging).
using System.Linq;
using System.Threading.Tasks;
using CsPotrace;
using LaserGRBL.SvgConverter;
using LaserGRBL.RasterConverter;
using System.Threading;

namespace LaserGRBL
{
	public class GrblFile : IEnumerable<GrblCommand>
	{
		public enum CartesianQuadrant { I, II, III, IV, Mix, Unknown }

		public delegate void OnFileLoadedDlg(long elapsed, string filename);
		public event OnFileLoadedDlg OnFileLoading;
		public event OnFileLoadedDlg OnFileLoaded;

		private List<GrblCommand> list = new List<GrblCommand>();
		private ProgramRange mRange = new ProgramRange();
		private TimeSpan mEstimatedTotalTime;
		public List<GrblCommand> Commands => list;
		private Thread mLoadingThread = null;

		public GrblFile()
		{

		}

		public GrblFile(decimal x, decimal y, decimal x1, decimal y1)
		{
			mRange.UpdateXYRange(new GrblCommand.Element('X', x), new GrblCommand.Element('Y', y), false);
			mRange.UpdateXYRange(new GrblCommand.Element('X', x1), new GrblCommand.Element('Y', y1), false);
        }

        private void ClearList()
        {
            foreach (GrblCommand command in list)
            {
                command.Dispose();
            }
            list.Clear();
        }

        public void SaveGCODE(string filename, bool header, bool footer, bool between, int cycles, bool useLFLineEndings, GrblCore core)
		{
			try
			{
				using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filename))
				{
					if (useLFLineEndings)
						sw.NewLine = "\n";

					if (header)
						EvaluateAddLines(core, sw, Settings.GetObject("GCode.CustomHeader", GrblCore.GCODE_STD_HEADER));

					for (int i = 0; i < cycles; i++)
					{
						foreach (GrblCommand cmd in list)
							sw.WriteLine(cmd.Command);


						if (between && i < cycles - 1)
							EvaluateAddLines(core, sw, Settings.GetObject("GCode.CustomPasses", GrblCore.GCODE_STD_PASSES));
					}

					if (footer)
						EvaluateAddLines(core, sw, Settings.GetObject("GCode.CustomFooter", GrblCore.GCODE_STD_FOOTER));

					sw.Close();
				}
			}
			catch { }
		}

		private static void EvaluateAddLines(GrblCore core, System.IO.StreamWriter sw, string lines)
		{
			string[] arr = lines.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			foreach (string line in arr)
			{
				if (line.Trim().Length > 0)
				{
					string command = core.EvaluateExpression(line);
					if (!string.IsNullOrEmpty(command))
						sw.WriteLine(command);
				}
			}
		}

		private void SafeLoadFile(ThreadStart loadFileAction)
        {
            if (CheckInUse()) return;
            mLoadingThread = new Thread(new ThreadStart(() =>
            {
				try
				{
					loadFileAction.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.LogException("SafeLoadFile", ex);
                }
                finally
                {
                    mLoadingThread = null;
                }
            }));
            mLoadingThread.Start();
        }

		public void LoadFile(string filename, bool append)
        {
            SafeLoadFile(() =>
            {
				RiseOnFileLoading(filename);

				long start = Tools.HiResTimer.TotalMilliseconds;

				if (!append)
                    ClearList();

				mRange.ResetRange();
				if (System.IO.File.Exists(filename))
				{
					using (System.IO.StreamReader sr = new System.IO.StreamReader(filename))
					{
						string line = null;
						while ((line = sr.ReadLine()) != null)
							if ((line = line.Trim()).Length > 0)
							{
								GrblCommand cmd = new GrblCommand(line);
								if (!cmd.IsEmpty)
									list.Add(cmd);
							}
					}
				}
				Analyze();
				long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

				RiseOnFileLoaded(filename, elapsed);
            });
		}

		public void LoadImportedSVG(string filename, bool append, GrblCore core, ColorFilter filter)
        {
            SafeLoadFile(() =>
            {
				RiseOnFileLoading(filename);

				long start = Tools.HiResTimer.TotalMilliseconds;

				if (!append)
                    ClearList();

				mRange.ResetRange();

				SvgConverter.GCodeFromSVG converter = new SvgConverter.GCodeFromSVG();
				converter.GCodeXYFeed = Settings.GetObject("GrayScaleConversion.VectorizeOptions.BorderSpeed", 1000);
				converter.UseLegacyBezier = !Settings.GetObject($"Vector.UseSmartBezier", true);

				string gcode = converter.convertFromFile(filename, core, filter);
				string[] lines = gcode.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				foreach (string l in lines)
				{
					string line = l;
					if ((line = line.Trim()).Length > 0)
					{
						GrblCommand cmd = new GrblCommand(line);
						if (!cmd.IsEmpty)
							list.Add(cmd);
					}
				}

				Analyze();
				long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

				RiseOnFileLoaded(filename, elapsed);
            });
        }


		private abstract class ColorSegment
		{
			public int mColor { get; set; }
			protected int mPixLen;

			public ColorSegment(int col, int len, bool rev)
			{
				mColor = col;
				mPixLen = rev ? -len : len;
			}

			public virtual bool IsSeparator
			{ get { return false; } }

			public bool Fast(L2LConf c)
			{ return c.pwm ? mColor == 0 : mColor <= 125; }

			public string formatnumber(int number, float offset, L2LConf c)
			{
				double dval = Math.Round(number / (c.vectorfilling ? c.fres : c.res) + offset, 3);
				return dval.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}

			// Format laser power value
			// grbl                    with pwm : color can be between 0 and configured SMax - S128
			// smoothiware             with pwm : Value between 0.00 and 1.00    - S0.50
			// Marlin : Laser power can not be defined as switch (Add in comment hard coded changes)
			public string FormatLaserPower(int color, L2LConf c)
			{
				if (c.firmwareType == Firmware.Smoothie)
					return string.Format(System.Globalization.CultureInfo.InvariantCulture, "S{0:0.00}", color / 255.0); //maybe scaling to UI maxpower VS config maxpower instead of fixed / 255.0 ?
																														 //else if (c.firmwareType == Firmware.Marlin)
																														 //	return "";
				else
					return string.Format(System.Globalization.CultureInfo.InvariantCulture, "S{0}", color);
			}

			public abstract string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c);
		}

		private class XSegment : ColorSegment
		{
			public XSegment(int col, int len, bool rev) : base(col, len, rev) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c)
			{
				cumX += mPixLen;

				if (c.pwm)
					return string.Format("X{0} {1}", formatnumber(cumX, c.oX, c), FormatLaserPower(mColor, c));
				else
					return string.Format("X{0} {1}", formatnumber(cumX, c.oX, c), Fast(c) ? c.lOff : c.lOn);
			}
		}

		private class YSegment : ColorSegment
		{
			public YSegment(int col, int len, bool rev) : base(col, len, rev) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c)
			{
				cumY += mPixLen;

				if (c.pwm)
					return string.Format("Y{0} {1}", formatnumber(cumY, c.oY, c), FormatLaserPower(mColor, c));
				else
					return string.Format("Y{0} {1}", formatnumber(cumY, c.oY, c), Fast(c) ? c.lOff : c.lOn);
			}
		}

		private class DSegment : ColorSegment
		{
			public DSegment(int col, int len, bool rev) : base(col, len, rev) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, GrblFile.L2LConf c)
			{
				cumX += mPixLen;
				cumY -= mPixLen;

				if (c.pwm)
					return string.Format("X{0} Y{1} {2}", formatnumber(cumX, c.oX, c), formatnumber(cumY, c.oY, c), FormatLaserPower(mColor, c));
				else
					return string.Format("X{0} Y{1} {2}", formatnumber(cumX, c.oX, c), formatnumber(cumY, c.oY, c), Fast(c) ? c.lOff : c.lOn);
			}
		}

		private class VSeparator : ColorSegment
		{
			public VSeparator() : base(0, 1, false) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c)
			{
				if (mPixLen < 0)
					throw new Exception();

				cumY += mPixLen;
				return string.Format("Y{0}", formatnumber(cumY, c.oY, c));
			}

			public override bool IsSeparator
			{ get { return true; } }
		}

		private class HSeparator : ColorSegment
		{
			public HSeparator() : base(0, 1, false) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c)
			{
				if (mPixLen < 0)
					throw new Exception();

				cumX += mPixLen;
				return string.Format("X{0}", formatnumber(cumX, c.oX, c));
			}

			public override bool IsSeparator
			{ get { return true; } }
		}

		public static bool RasterFilling(RasterConverter.ImageProcessor.Direction dir)
		{
			return dir == RasterConverter.ImageProcessor.Direction.Diagonal || dir == RasterConverter.ImageProcessor.Direction.Horizontal || dir == RasterConverter.ImageProcessor.Direction.Vertical;
		}
		public static bool VectorFilling(RasterConverter.ImageProcessor.Direction dir)
		{
			return dir == RasterConverter.ImageProcessor.Direction.NewDiagonal ||
			dir == RasterConverter.ImageProcessor.Direction.NewHorizontal ||
			dir == RasterConverter.ImageProcessor.Direction.NewVertical ||
			dir == RasterConverter.ImageProcessor.Direction.NewReverseDiagonal ||
			dir == RasterConverter.ImageProcessor.Direction.NewGrid ||
			dir == RasterConverter.ImageProcessor.Direction.NewDiagonalGrid ||
			dir == RasterConverter.ImageProcessor.Direction.NewCross ||
			dir == RasterConverter.ImageProcessor.Direction.NewDiagonalCross ||
			dir == RasterConverter.ImageProcessor.Direction.NewSquares ||
			dir == RasterConverter.ImageProcessor.Direction.NewZigZag ||
			dir == RasterConverter.ImageProcessor.Direction.NewHilbert ||
			dir == RasterConverter.ImageProcessor.Direction.NewInsetFilling;
		}

		public static bool TimeConsumingFilling(RasterConverter.ImageProcessor.Direction dir)
		{
			return
			dir == RasterConverter.ImageProcessor.Direction.NewCross ||
			dir == RasterConverter.ImageProcessor.Direction.NewDiagonalCross ||
			dir == RasterConverter.ImageProcessor.Direction.NewSquares;
		}

		public bool CheckInUse(bool showMessageBox = true)
		{
            if (mLoadingThread != null || InUse)
            {
                if(showMessageBox)
					Logger.LogMessage("GrblFile", Strings.AlreadyLoading);

				return true;
            }
			return false;
        }


		public void LoadImagePotrace(ICoreBitmap bmp, string filename, bool UseSpotRemoval, int SpotRemoval, bool UseSmoothing, decimal Smoothing, bool UseOptimize, decimal Optimize, bool useOptimizeFast, L2LConf c, bool append, GrblCore core)
		{
			// Phase 3 — Potrace vectorization pipeline; re-implemented with SkiaSharp.
			_ = bmp; _ = filename; _ = c; _ = append; _ = core;
		}

		private void RiseOnFileLoaded(string filename, long elapsed)
		{
			if (OnFileLoaded != null)
				OnFileLoaded(elapsed, filename);
		}

		private void RiseOnFileLoading(string filename)
		{
			if (OnFileLoading != null)
				OnFileLoading(0, filename);
		}

		public class L2LConf
		{
			public double res;
			public float oX;
			public float oY;
			public int markSpeed;
			public int borderSpeed;
			public int minPower;
			public int maxPower;
			public string lOn;
			public string lOff;
			public RasterConverter.ImageProcessor.Direction dir;
			public bool pwm;
			public double fres;
			public bool vectorfilling;
			public Firmware firmwareType;
		}

		private string skipcmd = "G0";
		public void LoadImageL2L(ICoreBitmap bmp, string filename, L2LConf c, bool append, GrblCore core)
        {
            if (CheckInUse()) return;

            skipcmd = Settings.GetObject("Disable G0 fast skip", false) ? "G1" : "G0";

			RiseOnFileLoading(filename);

			// Phase 3: ICoreBitmap.RotateFlip handled in imaging layer

			long start = Tools.HiResTimer.TotalMilliseconds;

			if (!append)
                ClearList();

			mRange.ResetRange();

			//absolute
			//list.Add(new GrblCommand("G90")); //(Moved to custom Header)

			//move fast to offset (or slow if disable G0) and set mark speed
			list.Add(new GrblCommand(String.Format("{0} X{1} Y{2} F{3}", skipcmd, formatnumber(c.oX), formatnumber(c.oY), c.markSpeed)));
			if (c.pwm)
				list.Add(new GrblCommand(String.Format("{0} S0", c.lOn))); //laser on and power to zero
			else
				list.Add(new GrblCommand($"{c.lOff} S{GrblCore.Configuration.MaxPWM}")); //laser off and power to maxpower

			//set speed to markspeed						
			// For marlin, need to specify G1 each time :
			//list.Add(new GrblCommand(String.Format("G1 F{0}", c.markSpeed)));
			//list.Add(new GrblCommand(String.Format("F{0}", c.markSpeed))); //replaced by the first move to offset and set speed

			ImageLine2Line(bmp, c);

			//laser off
			list.Add(new GrblCommand(c.lOff));

			//move fast to origin
			//list.Add(new GrblCommand("G0 X0 Y0")); //moved to custom footer

			Analyze();
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);
		}

		internal void GenerateCuttingTest(int f_col, int f_start, int f_end, int p_start, int p_end, int s_fixed, int f_text, int s_text, string title, string ton)
        {
            if (CheckInUse()) return;

            int p_row = p_end - p_start + 1;
			double ox = 3;
			double oy = 3;
			string filename = "Cutting Test";

			int x_size = f_col * 14 - 4;
			int y_size = p_row * 14 - 4;

			RiseOnFileLoading(filename);

			long start = Tools.HiResTimer.TotalMilliseconds;

            ClearList();
			mRange.ResetRange();

			double f_delta = f_col > 1 ? (f_end - f_start) / (double)(f_col - 1) : 0;

			//back to origin
			list.Add(new GrblCommand(String.Format("G0 X{0} Y{1} S{2}", formatnumber(ox), formatnumber(oy), formatnumber(s_fixed))));
			list.Add(new GrblCommand($"G1 {ton} F{formatnumber(f_start)}"));

			double cx = ox;
			double cy = oy;

			for (int p = 0; p < p_row; p++) // rows
			{
				cy = oy + 14 * p;
				for (int f = 0; f < f_col; f += 1) //cols
				{
					cx = ox + 14 * f;
					for (int pass = 0; pass < p_start + p; pass++)
					{
						//move to position
						list.Add(new GrblCommand(String.Format("G0 X{0} Y{1}", formatnumber(cx), formatnumber(cy))));
						//now draw rectangle
						list.Add(new GrblCommand(String.Format("G1 X{0} F{1} {2}", formatnumber(cx+10), formatnumber(f_start + f_delta * f), ton))); //Laser ON
						list.Add(new GrblCommand(String.Format("G1 Y{0}", formatnumber(cy+10))));
						list.Add(new GrblCommand(String.Format("G1 X{0}", formatnumber(cx))));
						list.Add(new GrblCommand(String.Format("G1 Y{0}", formatnumber(cy))));
						list.Add(new GrblCommand("M5")); // Laser OFF
					}
				}
			}
			
			//back to origin
			list.Add(new GrblCommand(String.Format("G0 X{0} Y{1} S0", formatnumber(ox), formatnumber(oy))));
			list.Add(new GrblCommand($"G1 {ton} F{formatnumber(f_text)}"));


			for (int x = 0; x < f_col; x++)
				list.AddRange(Hershey.Hershey.CreateString($"F{(int)(f_start + (x * f_delta))}", (x * 14) + (10 / 2) + ox, oy / 2, f_text, s_text, true, "M4"));
			for (int y = 0; y < p_row; y++)
				list.AddRange(Hershey.Hershey.CreateString($"{(int)(p_start + y)}pass", ox / 2, (y * 14) + (10 / 2) + oy, f_text, s_text, false, "M4"));

			string srange = $"S{s_fixed}";
			string frange = (f_start != f_end) ? $"F{f_start} - F{f_end}" : $"F{f_end}";
			string prange = (p_start != p_end) ? $"{p_start} - F{p_end} pass" : $"{p_end} pass";


			if (string.IsNullOrEmpty(title))
				title = "LaserGRBL cutting test";
			else
				title = $"LaserGRBL cutting test [{title}]";
			string parmessage = $"{srange}, {frange}, {prange}, {ton}";
			list.AddRange(Hershey.Hershey.CreateString(title, x_size / 2 + ox, y_size + oy + oy + oy / 2, f_text, s_text, true, "M4"));
			list.AddRange(Hershey.Hershey.CreateString(parmessage, x_size / 2 + ox, y_size + oy + oy / 2, f_text, s_text, true, "M4"));

			//laser off
			list.Add(new GrblCommand("M5"));


			Analyze();
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);
		}

		public void GenerateGreyscaleTest(int f_row, int s_col, int f_start, int f_end, int s_start, int s_end, int x_size, int y_size, double resolution, int f_grid, int s_grid, string title, int f_text, int s_text, string ton)
        {
            if (CheckInUse()) return;

            //string text = Hershey.Hershey.Test();

            double ox = 3;
			double oy = 3;
			string filename = "PowerSpeed Test";

			RiseOnFileLoading(filename);

			long start = Tools.HiResTimer.TotalMilliseconds;

            ClearList();
			mRange.ResetRange();

			bool forward = true;
			double f_delta = f_row > 1 ? (f_end - f_start) / (double)(f_row-1) : 0;
			double s_delta = s_col > 1 ? (s_end - s_start) / (double)(s_col-1) : 0;

			double x_step = x_size / (double)s_col;
			double y_step = y_size / (double)f_row;
			double filling_step = 1 / resolution;

			//back to origin
			list.Add(new GrblCommand(String.Format("G0 X{0} Y{1} S0", formatnumber(ox), formatnumber(oy))));
			list.Add(new GrblCommand($"G1 {ton} F{formatnumber(f_start)}"));

			//draw filling
			double prevF = double.NaN, curF = double.NaN;
			forward = true;
			for (double y = 0; y <= y_size; y += filling_step)
			{
				curF = f_start + ((int)(y / y_step)) * f_delta;

				if (curF != prevF)
				{
					list.Add(new GrblCommand($"F{formatnumber(curF)}"));
					prevF = curF;
				}

				list.Add(new GrblCommand($"Y{formatnumber(oy + y)} S0"));

				for (int x = 0; x < s_col; x++)
				{
					double cx = forward ? ((x + 1) * x_step) : x_size - ((x + 1) * x_step);
					double cs = forward ? s_start + (x * s_delta) : s_end - (x * s_delta);

					list.Add(new GrblCommand(String.Format("X{0} S{1}", formatnumber(ox + cx), formatnumber(cs))));
				}

				forward = !forward;
			}

			//back to origin
			list.Add(new GrblCommand(String.Format("G0 X{0} Y{1} S0", formatnumber(ox), formatnumber(oy))));
			list.Add(new GrblCommand($"G1 {ton} F{formatnumber(f_grid)}"));

			// draw grid X
			forward = true;
			for (int y = 0; y < f_row + 1; y++)
			{
				double cy = y * y_step;
				list.Add(new GrblCommand(String.Format("Y{0} S0", formatnumber(oy + cy))));

				for (int x = 0; x < s_col + 1; x++)
				{
					double cx = forward ? x * x_step : x_size - x * x_step;
					list.Add(new GrblCommand(String.Format("X{0} S{1}", formatnumber(ox + cx), formatnumber(s_grid))));
				}

				forward = !forward;
			}

			//back to origin
			list.Add(new GrblCommand(String.Format("G0 X{0} Y{1} S0", formatnumber(ox), formatnumber(oy))));
			list.Add(new GrblCommand($"G1 {ton} F{formatnumber(f_grid)}"));

			// draw grid Y
			forward = true;
			for (int x = 0; x < s_col + 1; x++)
			{
				double cx = x * x_step;
				list.Add(new GrblCommand(String.Format("X{0} S0", formatnumber(ox + cx))));

				for (int y = 0; y < f_row + 1; y++)
				{
					double cy = forward ? y * y_step : y_size - y * y_step;
					list.Add(new GrblCommand(String.Format("Y{0} S{1}", formatnumber(oy + cy), formatnumber(s_grid))));
				}

				forward = !forward;
			}

			for (int x = 0; x < s_col; x++)
				list.AddRange( Hershey.Hershey.CreateString($"S{(int)(s_start + (x * s_delta))}", (x*x_step) + (x_step/2) + ox, oy / 2, f_text, s_text, true, "M4"));
			for (int y = 0; y < f_row; y++)
				list.AddRange(Hershey.Hershey.CreateString($"F{(int)(f_start + (y * f_delta))}", ox / 2, (y * y_step) + (y_step / 2) + oy, f_text, s_text, false, "M4"));

			string srange = (s_start != s_end) ? $"S{s_start} - S{s_end}" : $"S{s_end}";
			string frange = (f_start != f_end) ? $"F{f_start} - F{f_end}" : $"F{f_end}";

			
			if (string.IsNullOrEmpty(title))
				title = "LaserGRBL power/speed test";
			else
				title = $"LaserGRBL power/speed test [{title}]";
			string parmessage = $"{srange}, {frange}, {ton},  {formatnumber(resolution)} line/mm";
			list.AddRange(Hershey.Hershey.CreateString(title, x_size / 2 + ox, y_size + oy + oy + oy/2, f_text, s_text, true, "M4"));
			list.AddRange(Hershey.Hershey.CreateString(parmessage, x_size / 2 + ox, y_size + oy + oy / 2, f_text, s_text, true, "M4"));

			//laser off
			list.Add(new GrblCommand("M5"));


			Analyze();
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);
		}

		internal void GenerateShakeTest(string axis, int flimit, int axislen, int cpower, int cspeed)
        {
            if (CheckInUse()) return;

            string filename = $"Shake Test {axis}";

			RiseOnFileLoading(filename);

			long start = Tools.HiResTimer.TotalMilliseconds;

            ClearList();
			mRange.ResetRange();

			list.Add(new GrblCommand("M5"));                    //laser OFF
			list.Add(new GrblCommand("G1 F1000 X0 Y0 S0"));     //move to origin (slowly)
			list.Add(new GrblCommand($"G1 F{cspeed} X7 Y10"));        //positioning
			list.Add(new GrblCommand("M4"));                    //laser ON
			list.Add(new GrblCommand($"G1 F{cspeed} S{cpower} X13 Y10"));        //drow cross
			list.Add(new GrblCommand("M5"));                    //laser OFF
			list.Add(new GrblCommand($"G1 F{cspeed} X10 Y7"));        //positioning
			list.Add(new GrblCommand("M4"));                    //laser ON
			list.Add(new GrblCommand($"G1 F{cspeed} S{cpower} X10 Y13"));        //drow cross
			list.Add(new GrblCommand("M5"));                    //laser OFF
			list.Add(new GrblCommand("G1 F1000 X10 Y10 S0"));     //move to cross center (slowly)

			GenerateShakeTest2(axis, flimit, axislen, 10, 50, 0.5);
			GenerateShakeTest2(axis, flimit, axislen, 10, 100, 2);
			GenerateShakeTest2(axis, flimit, axislen, 10, 200, 4);
			GenerateShakeTest2(axis, flimit, axislen, 10, 400, 8);

			list.Add(new GrblCommand($"G1 F{cspeed} X10 Y10 S0"));     //move to cross center (fast)

			list.Add(new GrblCommand($"G1 F{cspeed} X7 Y10"));        //positioning
			list.Add(new GrblCommand("M4"));                    //laser ON
			list.Add(new GrblCommand($"G1 F{cspeed} S{cpower} X13 Y10"));        //drow cross
			list.Add(new GrblCommand("M5"));                    //laser OFF
			list.Add(new GrblCommand($"G1 F{cspeed} X10 Y7"));        //positioning
			list.Add(new GrblCommand("M4"));                    //laser ON
			list.Add(new GrblCommand($"G1 F{cspeed} S{cpower} X10 Y13"));        //drow cross
			list.Add(new GrblCommand("M5"));                    //laser OFF
			list.Add(new GrblCommand("G1 F1000 X0 Y0 S0"));     //move to origin (slowly)

			Analyze();
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);
		}

		long map(long x, long in_min, long in_max, long out_min, long out_max)
		{
			return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
		}

		private void GenerateShakeTest2(string axis, int flimit, int axislen, int o, int trip, double step)
        {
            for (int c = trip / 2; c < axislen - trip / 2; c += trip) //centro dei punti di oscillazione
			{
				for (double i = 0; i < trip / 3; i += step)
				{
					list.Add(new GrblCommand($"G1 F{flimit} {axis}{formatnumber(o + c + i)}"));
					list.Add(new GrblCommand($"G1 F{flimit} {axis}{formatnumber(o + c - i)}"));
				}
			}
		}

        //internal void GenerateShakeTest(string axis, int flimit, int axislen, int cpower, int cspeed)
        //{
        //	string filename = $"Shake Test {axis}";

        //	RiseOnFileLoading(filename);

        //	long start = Tools.HiResTimer.TotalMilliseconds;

        //	ClearList();
        //	mRange.ResetRange();

        //	list.Add(new GrblCommand("M5"));                    //laser OFF
        //	list.Add(new GrblCommand("G1 F1000 X0 Y0 S0"));     //move to origin (slowly)
        //	list.Add(new GrblCommand($"G1 F{cspeed} X7 Y10"));        //positioning
        //	list.Add(new GrblCommand("M4"));                    //laser ON
        //	list.Add(new GrblCommand($"G1 F{cspeed} S{cpower} X13 Y10"));        //drow cross
        //	list.Add(new GrblCommand("M5"));                    //laser OFF
        //	list.Add(new GrblCommand($"G1 F{cspeed} X10 Y7"));        //positioning
        //	list.Add(new GrblCommand("M4"));                    //laser ON
        //	list.Add(new GrblCommand($"G1 F{cspeed} S{cpower} X10 Y13"));        //drow cross
        //	list.Add(new GrblCommand("M5"));                    //laser OFF
        //	list.Add(new GrblCommand("G1 F1000 X10 Y10 S0"));     //move to cross center (slowly)

        //	int ca = 10;
        //	int da = 1;
        //	while ((ca + da) < axislen)
        //	{
        //		ca += da;
        //		list.Add(new GrblCommand($"G1 F{flimit} {axis}{ca}"));
        //		ca -= da;
        //		list.Add(new GrblCommand($"G1 F{flimit} {axis}{ca}"));
        //		da += 5;
        //	}

        //	list.Add(new GrblCommand($"G1 F{cspeed} X7 Y10"));        //positioning
        //	list.Add(new GrblCommand("M4"));                    //laser ON
        //	list.Add(new GrblCommand($"G1 F{cspeed} S{cpower} X13 Y10"));        //drow cross
        //	list.Add(new GrblCommand("M5"));                    //laser OFF
        //	list.Add(new GrblCommand($"G1 F{cspeed} X10 Y7"));        //positioning
        //	list.Add(new GrblCommand("M4"));                    //laser ON
        //	list.Add(new GrblCommand($"G1 F{cspeed} S{cpower} X10 Y13"));        //drow cross
        //	list.Add(new GrblCommand("M5"));                    //laser OFF

        //	Analyze();
        //	long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

        //	RiseOnFileLoaded(filename, elapsed);
        //}

        // For Marlin, as we sen M106 command, we need to know last color send
        //private int lastColorSend = 0;
        private void ImageLine2Line(ICoreBitmap bmp, L2LConf c)
		{
			bool fast = true;
			List<ColorSegment> segments = GetSegments(bmp, c);
			List<GrblCommand> temp = new List<GrblCommand>();

			int cumX = 0;
			int cumY = 0;

			foreach (ColorSegment seg in segments)
			{
				bool changeGMode = (fast != seg.Fast(c)); //se veloce != dafareveloce

				if (seg.IsSeparator && !fast) //fast = previous segment contains S0 color
				{
					if (c.pwm)
						temp.Add(new GrblCommand("S0"));
					else
						temp.Add(new GrblCommand(c.lOff)); //laser off
				}

				fast = seg.Fast(c);

				// For marlin firmware, we must defined laser power before moving (unsing M106 or M107)
				// So we have to speficy gcode (G0 or G1) each time....
				//if (c.firmwareType == Firmware.Marlin)
				//{
				//	// Add M106 only if color has changed
				//	if (lastColorSend != seg.mColor)
				//		temp.Add(new GrblCommand(String.Format("M106 P1 S{0}", fast ? 0 : seg.mColor)));
				//	lastColorSend = seg.mColor;
				//	temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? "G0" : "G1", seg.ToGCodeNumber(ref cumX, ref cumY, c))));
				//}
				//else
				//{

				if (changeGMode)
					temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? skipcmd : "G1", seg.ToGCodeNumber(ref cumX, ref cumY, c))));
				else
					temp.Add(new GrblCommand(seg.ToGCodeNumber(ref cumX, ref cumY, c)));

				//}
			}

			temp = OptimizeLine2Line(temp, c);
			list.AddRange(temp);
		}


		private List<GrblCommand> OptimizeLine2Line(List<GrblCommand> temp, L2LConf c)
		{
			List<GrblCommand> rv = new List<GrblCommand>();

			decimal curX = (decimal)c.oX;
			decimal curY = (decimal)c.oY;
			bool cumulate = false;

			foreach (GrblCommand cmd in temp)
			{
				try
				{
					cmd.BuildHelper();

					bool oldcumulate = cumulate;

					if (c.pwm)
					{
						if (cmd.S != null) //is S command
						{
							if (cmd.S.Number == 0) //is S command with zero power
								cumulate = true;   //begin cumulate
							else
								cumulate = false;  //end cumulate
						}
					}
					else
					{
						if (cmd.IsLaserOFF)
							cumulate = true;   //begin cumulate
						else if (cmd.IsLaserON)
							cumulate = false;  //end cumulate
					}


					if (oldcumulate && !cumulate) //cumulate down front -> flush
					{
						if (c.pwm)
							rv.Add(new GrblCommand(string.Format("{0} X{1} Y{2} S0", skipcmd, formatnumber((double)curX), formatnumber((double)curY))));
						else
							rv.Add(new GrblCommand(string.Format("{0} X{1} Y{2} {3}", skipcmd, formatnumber((double)curX), formatnumber((double)curY), c.lOff)));

						//curX = curY = 0;
					}

					if (cmd.IsMovement)
					{
						if (cmd.X != null) curX = cmd.X.Number;
						if (cmd.Y != null) curY = cmd.Y.Number;
					}

					if (!cmd.IsMovement || !cumulate)
						rv.Add(cmd);
				}
				catch (Exception ex) { throw ex; }
				finally { cmd.DeleteHelper(); }
			}

			return rv;
		}

		private List<ColorSegment> GetSegments(ICoreBitmap bmp, L2LConf c)
		{
			bool uni = Settings.GetObject("Unidirectional Engraving", false);

			List<ColorSegment> rv = new List<ColorSegment>();
			if (c.dir == RasterConverter.ImageProcessor.Direction.Horizontal || c.dir == RasterConverter.ImageProcessor.Direction.Vertical)
			{
				bool h = (c.dir == RasterConverter.ImageProcessor.Direction.Horizontal); //horizontal/vertical

				for (int i = 0; i < (h ? bmp.Height : bmp.Width); i++)
				{
					bool d = uni || IsEven(i); //direct/reverse
					int prevCol = -1;
					int len = -1;

					for (int j = d ? 0 : (h ? bmp.Width - 1 : bmp.Height - 1); d ? (j < (h ? bmp.Width : bmp.Height)) : (j >= 0); j = (d ? j + 1 : j - 1))
						ExtractSegment(bmp, h ? j : i, h ? i : j, !d, ref len, ref prevCol, rv, c); //extract different segments

					if (h)
						rv.Add(new XSegment(prevCol, len + 1, !d)); //close last segment
					else
						rv.Add(new YSegment(prevCol, len + 1, !d)); //close last segment

					if (uni) // add "go back"
					{
						if (h) rv.Add(new XSegment(0, bmp.Width, true));
						else rv.Add(new YSegment(0, bmp.Height, true));
					}

					if (i < (h ? bmp.Height - 1 : bmp.Width - 1))
					{
						if (h)
							rv.Add(new VSeparator()); //new line
						else
							rv.Add(new HSeparator()); //new line
					}
				}
			}
			else if (c.dir == RasterConverter.ImageProcessor.Direction.Diagonal)
			{
				//based on: http://stackoverflow.com/questions/1779199/traverse-matrix-in-diagonal-strips
				//based on: http://stackoverflow.com/questions/2112832/traverse-rectangular-matrix-in-diagonal-strips

				/*

				+------------+
				|  -         |
				|  -  -      |
				+-------+    |
				|  -  - |  - |
				+-------+----+

				*/


				//the algorithm runs along the matrix for diagonal lines (slice index)
				//z1 and z2 contains the number of missing elements in the lower right and upper left
				//the length of the segment can be determined as "slice - z1 - z2"
				//my modified version of algorithm reverses travel direction each slice

				rv.Add(new VSeparator()); //new line

				int w = bmp.Width;
				int h = bmp.Height;
				for (int slice = 0; slice < w + h - 1; ++slice)
				{
					bool d = uni || IsEven(slice); //direct/reverse

					int prevCol = -1;
					int len = -1;

					int z1 = slice < h ? 0 : slice - h + 1;
					int z2 = slice < w ? 0 : slice - w + 1;

					for (int j = (d ? z1 : slice - z2); d ? j <= slice - z2 : j >= z1; j = (d ? j + 1 : j - 1))
						ExtractSegment(bmp, j, slice - j, !d, ref len, ref prevCol, rv, c); //extract different segments
					rv.Add(new DSegment(prevCol, len + 1, !d)); //close last segment

					//System.Diagnostics.Debug.WriteLine(String.Format("sl:{0} z1:{1} z2:{2}", slice, z1, z2));

					if (uni) // add "go back"
					{
						int slen = (slice - z1 - z2) + 1;
						rv.Add(new DSegment(0, slen, true));
						//System.Diagnostics.Debug.WriteLine(slen);
					}

					if (slice < Math.Min(w, h) - 1) //first part of the image
					{
						if (d && !uni)
							rv.Add(new HSeparator()); //new line
						else
							rv.Add(new VSeparator()); //new line
					}
					else if (slice >= Math.Max(w, h) - 1) //third part of image
					{
						if (d && !uni)
							rv.Add(new VSeparator()); //new line
						else
							rv.Add(new HSeparator()); //new line
					}
					else //central part of the image
					{
						if (w > h)
							rv.Add(new HSeparator()); //new line
						else
							rv.Add(new VSeparator()); //new line
					}
				}
			}

			return rv;
		}

		private void ExtractSegment(ICoreBitmap image, int x, int y, bool reverse, ref int len, ref int prevCol, List<ColorSegment> rv, L2LConf c)
		{
			len++;
			int col = GetColor(image, x, y, c.minPower, c.maxPower, c.pwm);
			if (prevCol == -1)
				prevCol = col;

			if (prevCol != col)
			{
				if (c.dir == RasterConverter.ImageProcessor.Direction.Horizontal)
					rv.Add(new XSegment(prevCol, len, reverse));
				else if (c.dir == RasterConverter.ImageProcessor.Direction.Vertical)
					rv.Add(new YSegment(prevCol, len, reverse));
				else if (c.dir == RasterConverter.ImageProcessor.Direction.Diagonal)
					rv.Add(new DSegment(prevCol, len, reverse));

				len = 0;
			}

			prevCol = col;
		}

		private List<List<Curve>> ParallelOptimizePaths(List<List<Curve>> list, double changecost)
		{
			if (list == null || list.Count <= 1)
				return list;

			int maxblocksize = 2048;    //max number of List<Curve> to process in a single OptimizePaths operation

			int blocknum = (int)Math.Ceiling(list.Count / (double)maxblocksize);
			if (blocknum <= 1)
				return OptimizePaths(list, changecost);

			System.Diagnostics.Debug.WriteLine("Count: " + list.Count);

			Task<List<List<Curve>>>[] taskArray = new Task<List<List<Curve>>>[blocknum];
			for (int i = 0; i < taskArray.Length; i++)
				taskArray[i] = Task.Factory.StartNew((data) => OptimizePaths((List<List<Curve>>)data, changecost), GetTaskJob(i, taskArray.Length, list));
			Task.WaitAll(taskArray);

			List<List<Curve>> rv = new List<List<Curve>>();
			for (int i = 0; i < taskArray.Length; i++)
			{
				List<List<Curve>> lc = taskArray[i].Result;
				rv.AddRange(lc);
			}

			return rv;
		}

		private List<List<Curve>> GetTaskJob(int threadIndex, int threadCount, List<List<Curve>> list)
		{
			int from = (threadIndex * list.Count) / threadCount;
			int to = ((threadIndex + 1) * list.Count) / threadCount;

			List<List<Curve>> rv = list.GetRange(from, to - from);
			System.Diagnostics.Debug.WriteLine($"Thread {threadIndex}/{threadCount}: {rv.Count} [from {from} to {to}]");
			return rv;
		}

		private List<List<Curve>> OptimizePaths(List<List<Curve>> list, double changecost)
		{
			if (list.Count <= 1)
				return list;


			dPoint Origin = new dPoint(0, 0);
			int nearestToZero = 0;
			double bestDistanceToZero = Double.MaxValue;

			double[,] costs = new double[list.Count, list.Count];   //array bidimensionale dei costi di viaggio dal punto finale della curva 1 al punto iniziale della curva 2
			for (int c1 = 0; c1 < list.Count; c1++)                 //ciclo due volte sulla lista di curve
			{
				dPoint c1fa = list[c1].First().A;	//punto iniziale del primo segmento del percorso (per calcolo distanza dallo zero)
				//dPoint c1la = list[c1].Last().A;	//punto iniziale dell'ulimo segmento del percorso (per calcolo direzione di uscita)
				dPoint c1lb = list[c1].Last().B;	//punto finale dell'ultimo segmento del percorso (per calcolo distanza tra percorsi e direzione di uscita e ingresso)
				

				for (int c2 = 0; c2 < list.Count; c2++)             //con due indici diversi c1, c2
				{
					dPoint c2fa = list[c2].First().A;     //punto iniziale del primo segmento del percorso (per calcolo distanza tra percorsi e direzione di ingresso)
					//dPoint c2fb = list[c2].First().B;     //punto finale del primo segmento del percorso (per calcolo direzione di continuazione)

					if (c1 == c2)
						costs[c1, c2] = double.MaxValue;  //distanza del punto con se stesso (caso degenere)
					else
						costs[c1, c2] = SquareDistance(c1lb, c2fa); //TravelCost(c1la, c1lb, c2fa, c2fb, changecost);
				}

				//trova quello che parte più vicino allo zero
				double distZero = SquareDistanceZero(c1fa);
				if (distZero < bestDistanceToZero)
				{
					nearestToZero = c1;
					bestDistanceToZero = distZero;
				}
			}

			//Create a list of unvisited places
			List<int> unvisited = Enumerable.Range(0, list.Count).ToList();

			//Pick nearest points
			List<List<CsPotrace.Curve>> bestPath = new List<List<Curve>>();

			//parti da quello individuato come "il più vicino allo zero"
			bestPath.Add(list[nearestToZero]);
			unvisited.Remove(nearestToZero);
			int lastIndex = nearestToZero;
			
			while (unvisited.Count > 0)
			{
				int bestIndex = 0;
				double bestDistance = double.MaxValue;

				foreach (int nextIndex in unvisited)                    //cicla tutti gli "unvisited" rimanenti
				{
					double dist = costs[lastIndex, nextIndex];
					if (dist < bestDistance)
					{
						bestIndex = nextIndex;                    //salva il bestIndex
						bestDistance = dist;                      //salva come risultato migliore                        
					}
				}

				bestPath.Add(list[bestIndex]);
				unvisited.Remove(bestIndex);

				//Save nearest point
				lastIndex = bestIndex;                   //l'ultimo miglior indice trovato diventa il prossimo punto da analizzare			
			}

			return bestPath;
		}

		////questa funzione calcola il "costo" di un cambio di direzione
		////in termini di distanza che sarebbe possibile percorrere
		////nel tempo di una decelerazione da velocità di marcatura, a zero 
		//private double ComputeDirectionChangeCost(L2LConf c, GrblCore core, bool border)
		//{
		//	double speed = (border ? c.borderSpeed : c.markSpeed) / 60.0; //velocità di marcatura (mm/sec)
		//	double accel = core.Configuration != null ? (double)core.Configuration.AccelerationXY : 2000; //acceleration (mm/sec^2)
		//	double cost = (speed * speed) / (2 * accel); //(mm)
		//	cost = cost * c.res; //mm tradotti nella risoluzione immagine

		//	return cost;
		//}

		//private double TravelCost(dPoint s1a, dPoint s1b, dPoint s2a, dPoint s2b, double changecost)
		//{
		//	double d = Math.Sqrt(SquareDistance(s1b, s2a));
		//	double a1 = DirectionChange(s1a, s1b, s2a);
		//	double a2 = DirectionChange(s1b, s2a, s2b);
		//	double cd = d + changecost * a1 + changecost * a2;

		//	//System.Diagnostics.Debug.WriteLine($"{d}\t{a1}\t{a2}\t{cd}");
		//	return cd;
		//}

		private static double SquareDistance(dPoint a, dPoint b)
		{
			double dX = b.X - a.X;
			double dY = b.Y - a.Y;
			return ((dX * dX) + (dY * dY));
		}
		private static double SquareDistanceZero(dPoint a)
		{
			return ((a.X * a.X) + (a.Y * a.Y));
		}

		//questo metodo ritorna un fattore 0 se c'è continuità di direzione, 0.5 su angolo 90°, 1 se c'è inversione totale (180°)
		private double DirectionChange(dPoint p1, dPoint p2, dPoint p3)
		{
			double angleA = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X); //angolo del segmento corrente
			double angleB = Math.Atan2(p3.Y - p2.Y, p3.X - p2.X); //angolo della retta congiungente

			double angleAB = Math.Abs(Math.Abs(angleB) - Math.Abs(angleA)) ; //0 se stessa direzione, pigreco se inverte direzione
			double factor = angleAB / Math.PI;
			return factor;
		}


		private int GetColor(ICoreBitmap I, int X, int Y, int min, int max, bool pwm)
		{
			// Phase 3: ICoreBitmap.GetPixelGray returns pre-computed gray (0-255, 0=white, 255=black laser-on).
			int rv = I.GetPixelGray(X, Y);
			if (rv == 0)
				return 0;
			else if (pwm)
				return rv * (max - min) / 255 + min;
			else
				return rv;
		}

		public string formatnumber(double number)
		{ return number.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }

		private static bool IsEven(int value)
		{ return value % 2 == 0; }

		public int Count
		{ get { return list.Count; } }

		public TimeSpan EstimatedTime { get { return mEstimatedTotalTime; } }


		//  II | I
		// ---------
		// III | IV
		public CartesianQuadrant Quadrant
		{
			get
			{
				if (!mRange.DrawingRange.ValidRange)
					return CartesianQuadrant.Unknown;
				else if (mRange.DrawingRange.X.Min >= 0 && mRange.DrawingRange.Y.Min >= 0)
					return CartesianQuadrant.I;
				else if (mRange.DrawingRange.X.Max <= 0 && mRange.DrawingRange.Y.Min >= 0)
					return CartesianQuadrant.II;
				else if (mRange.DrawingRange.X.Max <= 0 && mRange.DrawingRange.Y.Max <= 0)
					return CartesianQuadrant.III;
				else if (mRange.DrawingRange.X.Min >= 0 && mRange.DrawingRange.Y.Max <= 0)
					return CartesianQuadrant.IV;
				else
					return CartesianQuadrant.Mix;
			}
		}

		internal void DrawOnGraphics(object g, CoreSize size)
		{ /* Phase 5 — re-implemented in GrblPanel using Avalonia DrawingContext. */ }

		private void DrawJobPreview(object /* Graphics: Phase 5 */ g, GrblCommand.StatePositionBuilder spb, float zoom)
		{
			// Phase 5 — re-implemented in GrblPanel using Avalonia DrawingContext.
		}

		internal void LoadImageCenterline(ICoreBitmap bmp, string filename, bool useCornerThreshold, int cornerThreshold, bool useLineThreshold, int lineThreshold, L2LConf conf, bool append, GrblCore core)
		{
			// Phase 3 — Autotrace + SvgConverter pipeline; re-implemented with SkiaSharp.
			_ = bmp; _ = filename; _ = conf; _ = append; _ = core;
		}


		private void Analyze() //analyze the file and build global range and timing for each command
		{
			GrblCommand.StatePositionBuilder spb = new GrblCommand.StatePositionBuilder();

			mRange.ResetRange();
			mRange.UpdateXYRange("X0", "Y0", false);
			mEstimatedTotalTime = TimeSpan.Zero;

			foreach (GrblCommand cmd in list)
			{
				try
				{
					GrblConfST conf =  GrblCore.Configuration;
					TimeSpan delay = spb.AnalyzeCommand(cmd, true, conf);

					mRange.UpdateSRange(spb.S);

					if (spb.LastArcHelperResult != null)
						mRange.UpdateXYRange(spb.LastArcHelperResult.BBox.X, spb.LastArcHelperResult.BBox.Y, spb.LastArcHelperResult.BBox.Width, spb.LastArcHelperResult.BBox.Height, spb.LaserBurning);
					else
						mRange.UpdateXYRange(spb.X, spb.Y, spb.LaserBurning);

					mEstimatedTotalTime += delay;
					cmd.SetOffset(mEstimatedTotalTime);
				}
				catch (Exception ex) { throw ex; }
				finally { cmd.DeleteHelper(); }
			}
		}

		private void ScaleAndPosition(object g, CoreSize s, ProgramRange.XYRange scaleRange, float zoom)
		{ /* Phase 5 stub */ }

		System.Collections.Generic.IEnumerator<GrblCommand> IEnumerable<GrblCommand>.GetEnumerator()
		{ return list.GetEnumerator(); }

		public System.Collections.IEnumerator GetEnumerator()
		{ return list.GetEnumerator(); }

		public ProgramRange Range { get { return mRange; } }

		public bool InUse { get; internal set; }

		public GrblCommand this[int index]
		{ get { return list[index]; } }
	}
}
