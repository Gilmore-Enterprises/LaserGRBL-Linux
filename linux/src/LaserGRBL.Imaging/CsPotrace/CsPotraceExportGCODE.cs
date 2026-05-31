using LaserGRBL;
//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using System;
using System.Collections.Generic;
using CsPotrace.BezierToBiarc;
using System.Collections;
using CsPotrace;

namespace CsPotrace
{
	/// <summary>
	/// Description of CsPotraceExportGCODE.
	/// </summary>
	public partial class Potrace
	{

		/// <summary>
		/// Exports a figure, created by Potrace from a Bitmap to a svg-formatted string
		/// </summary>
		/// <param name="list">Arraylist, which contains vectorinformations about the Curves</param>
		/// <param name="Width">Width of the exportd cvg-File</param>
		/// <param name="Height">Height of the exportd cvg-File</param>
		/// <returns></returns>
		public static List<string> Export2GCode(List<List<CsPotrace.Curve>> list, float oX, float oY, double scale, string lOn, string lOff, CoreSize originalImageSize, string skipcmd)
		{
			// Debug rendering (GDI+) removed — Phase 5 handles visual debug via Avalonia.
			bool debug = false;

			List<string> rv = new List<string>();
			object g = null; // Phase 5: debug graphics object, null = no rendering
			foreach (List<CsPotrace.Curve> Curves in list)
				rv.AddRange(GetPathGC(Curves, lOn, lOff, oX * scale, oY * scale, scale, g, skipcmd));
			return rv;
		}

		private static List<string> GetPathGC(List<CsPotrace.Curve> Curves, string lOn, string lOff, double oX, double oY, double scale, object /* Graphics: Phase 5 */ g, string skipcmd)
		{
			List<string> rv = new List<string>();

			OnPathBegin(Curves, lOn, oX, oY, scale, rv, skipcmd);

			foreach (CsPotrace.Curve Curve in Curves)
				OnPathSegment(Curve, oX, oY, scale, rv, g);

			OnPathEnd(Curves, lOff, oX, oY, scale, rv);

			return rv;
		}

		private static void OnPathSegment(CsPotrace.Curve Curve, double oX, double oY, double scale, List<string> rv, object /* Graphics: Phase 5 */ g)
		{
			if (double.IsNaN(Curve.LinearLenght)) // problem?
				return;

			if (Curve.Kind == CsPotrace.CurveKind.Line)
			{
				//trace line
				if (g != null)
				
				rv.Add(String.Format("G1 X{0} Y{1}", formatnumber(Curve.B.X + oX, scale), formatnumber(Curve.B.Y + oY, scale)));
			}

			if (Curve.Kind == CsPotrace.CurveKind.Bezier)
			{
				CubicBezier cb = new CubicBezier(new Vector2((float)Curve.A.X, (float)Curve.A.Y),
												 new Vector2((float)Curve.ControlPointA.X, (float)Curve.ControlPointA.Y),
												 new Vector2((float)Curve.ControlPointB.X, (float)Curve.ControlPointB.Y),
												 new Vector2((float)Curve.B.X, (float)Curve.B.Y));

				try
				{
					List<BiArc> bal = Algorithm.ApproxCubicBezier(cb, 5, 2);
					if (bal != null)	//può ritornare null se ha troppi punti da processare
					{
						foreach (BiArc ba in bal)
						{
							if (!double.IsNaN(ba.A1.Length) && !double.IsNaN(ba.A1.LinearLength))
								rv.Add(GetArcGC(ba.A1, oX, oY, scale, g));

							if (!double.IsNaN(ba.A2.Length) && !double.IsNaN(ba.A2.LinearLength))
								rv.Add(GetArcGC(ba.A2, oX, oY, scale, g));
						}
					}
					else //same as exception
					{
						rv.Add(String.Format("G1 X{0} Y{1}", formatnumber(Curve.B.X + oX, scale), formatnumber(Curve.B.Y + oY, scale)));
					}
				}
				catch
				{
					rv.Add(String.Format("G1 X{0} Y{1}", formatnumber(Curve.B.X + oX, scale), formatnumber(Curve.B.Y + oY, scale)));
				}

			}
		}

		private static void OnPathBegin(List<CsPotrace.Curve> Curves, string lOn, double oX, double oY, double scale, List<string> rv, string skipcmd)
		{
			if (Curves.Count > 0)
			{
				//fast go to position
				rv.Add(String.Format("{0} X{1} Y{2}", skipcmd, formatnumber(Curves[0].A.X + oX, scale), formatnumber(Curves[0].A.Y + oY, scale)));
				//turn on laser
				rv.Add(lOn);
			}
		}

		private static void OnPathEnd(List<CsPotrace.Curve> Curves, string lOff, double oX, double oY, double scale, List<string> rv)
		{
			//turn off laser
			if (Curves.Count > 0)
				rv.Add(lOff);
		}

		private static string GetArcGC(Arc arc, double oX, double oY, double scale, object /* Graphics: Phase 5 */ g)
		{
			//http://www.cnccookbook.com/CCCNCGCodeArcsG02G03.htm
			//https://www.tormach.com/g02_g03.html

			if (!CasoLimite(arc, scale))
			{
				return String.Format("G{0} X{1} Y{2} I{3} J{4}", !arc.IsClockwise ? 2 : 3, formatnumber(arc.P2.X + oX, scale), formatnumber(arc.P2.Y + oY, scale), formatnumber(arc.C.X - arc.P1.X, scale), formatnumber(arc.C.Y - arc.P1.Y, scale));
			}
			else //approximate with a line
			{
				return String.Format("G1 X{0} Y{1}", formatnumber(arc.P2.X + oX, scale), formatnumber(arc.P2.Y + oY, scale));
			}
		}

		private static bool CasoLimite(Arc arc, double scale)
		{
			if (arc.r == 0 || arc.LinearLength == 0)
				return true;
			if ((arc.LinearLength / scale) <= 0.5)
				return true;
			if (arc.r / arc.LinearLength > 1000)
				return true;

			return false;
		}

		private static string formatnumber(double number, double scale)
		{
			double num = number / scale;
			if (!double.IsNaN(num))
				return num.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
			else
				return "0";
		}

		public static CorePoint AsPointF(Vector2 v)
		{
			return new CorePoint(v.X, v.Y);
		}

	}
}
