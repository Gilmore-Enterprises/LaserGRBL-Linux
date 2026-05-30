//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify it under the terms of
// the GPLv3 General Public License as published by the Free Software Foundation; either version 3
// of the License, or (at your option) any later version.

// Ported from the WinForms version: System.Drawing.Color → CoreColor.
// SystemColors.Control / ControlText replaced with CoreColor.SystemControl / SystemControlText.

using System.Collections.Generic;
using static LaserGRBL.ColorScheme;

namespace LaserGRBL
{
	public interface IColorScheme
	{
		Scheme Scheme { get; }
		bool IsDark { get; }
		CoreColor FormBackColor { get; }
		CoreColor FormForeColor { get; }
		CoreColor PreviewBackColor { get; }
		CoreColor PreviewText { get; }
		CoreColor PreviewRuler { get; }
		CoreColor PreviewGrid { get; }
		CoreColor PreviewGridMinor { get; }
		CoreColor PreviewJobRange { get; }
		CoreColor PreviewFirstMovement { get; }
		CoreColor PreviewOtherMovement { get; }
		CoreColor PreviewLaserPower { get; }
		CoreColor PreviewCross { get; }
		CoreColor PreviewCommandOK { get; }
		CoreColor PreviewCommandKO { get; }
		CoreColor PreviewCommandWait { get; }
		CoreColor PreviewCrossCursor { get; }
		CoreColor LogBackColor { get; }
		CoreColor LogLeftCOMMAND { get; }
		CoreColor LogLeftSTARTUP { get; }
		CoreColor LogLeftALARM { get; }
		CoreColor LogLeftCONFIG { get; }
		CoreColor LogLeftFEEDBACK { get; }
		CoreColor LogLeftPOSITION { get; }
		CoreColor LogLeftOTHERS { get; }
		CoreColor LogRightGOOD { get; }
		CoreColor LogRightBAD { get; }
		CoreColor LogRightOTHERS { get; }
		CoreColor TextBoxColorOverride { get; }
		CoreColor LinkColor { get; }
		CoreColor VisitedLinkColor { get; }
		CoreColor ControlsBorder { get; }
		CoreColor ControlsBackDisabled { get; }
		CoreColor DisabledButtons { get; }
		CoreColor PressedButtons { get; }
	}

	// CAD style color scheme
	public class SchemeCADStyle : IColorScheme
	{
		public Scheme Scheme => Scheme.CADStyle;
		public bool IsDark => false;
		public CoreColor FormBackColor => CoreColor.FromArgb(248, 248, 248);
		public CoreColor FormForeColor => CoreColor.SystemControlText;
		public CoreColor PreviewBackColor => CoreColor.FromArgb(33, 40, 48);
		public CoreColor PreviewText => CoreColor.FromArgb(220, 220, 220);
		public CoreColor PreviewRuler => CoreColor.FromArgb(69, 78, 101);
		public CoreColor PreviewGrid => CoreColor.FromArgb(49, 55, 70);
		public CoreColor PreviewGridMinor => CoreColor.FromArgb(38, 45, 55);
		public CoreColor PreviewJobRange => CoreColor.FromArgb(200, 200, 140);
		public CoreColor PreviewFirstMovement => CoreColor.Blue;
		public CoreColor PreviewOtherMovement => CoreColor.FromArgb(60, 82, 85);
		public CoreColor PreviewLaserPower => CoreColor.FromArgb(220, 220, 220);
		public CoreColor PreviewCross => CoreColor.FromArgb(230, 230, 20);
		public CoreColor PreviewCommandOK => CoreColor.FromArgb(55, 199, 116);
		public CoreColor PreviewCommandKO => CoreColor.FromArgb(240, 50, 50);
		public CoreColor PreviewCommandWait => CoreColor.LightPink;
		public CoreColor PreviewCrossCursor => CoreColor.FromArgb(103, 107, 117);
		public CoreColor LogBackColor => CoreColor.White;
		public CoreColor LogLeftCOMMAND => CoreColor.Black;
		public CoreColor LogLeftSTARTUP => CoreColor.DarkGreen;
		public CoreColor LogLeftALARM => CoreColor.Crimson;
		public CoreColor LogLeftCONFIG => CoreColor.DimGray;
		public CoreColor LogLeftFEEDBACK => CoreColor.DodgerBlue;
		public CoreColor LogLeftPOSITION => CoreColor.OrangeRed;
		public CoreColor LogLeftOTHERS => CoreColor.Purple;
		public CoreColor LogRightGOOD => CoreColor.DarkBlue;
		public CoreColor LogRightBAD => CoreColor.Red;
		public CoreColor LogRightOTHERS => CoreColor.Black;
		public CoreColor TextBoxColorOverride => CoreColor.Black;
		public CoreColor LinkColor => CoreColor.DodgerBlue;
		public CoreColor VisitedLinkColor => CoreColor.Purple;
		public CoreColor ControlsBorder => CoreColor.FromArgb(220, 220, 220);
		public CoreColor ControlsBackDisabled => CoreColor.FromArgb(180, 180, 180);
		public CoreColor DisabledButtons => CoreColor.FromArgb(140, 140, 140);
		public CoreColor PressedButtons => CoreColor.FromArgb(222, 230, 7);
	}

	// CAD dark color scheme
	public class SchemeCADDark : IColorScheme
	{
		public Scheme Scheme => Scheme.CADDark;
		public bool IsDark => true;
		public CoreColor FormBackColor => CoreColor.FromArgb(35, 40, 51);
		public CoreColor FormForeColor => CoreColor.FromArgb(190, 190, 190);
		public CoreColor PreviewBackColor => CoreColor.FromArgb(23, 30, 38);
		public CoreColor PreviewText => CoreColor.FromArgb(220, 220, 220);
		public CoreColor PreviewRuler => CoreColor.FromArgb(69, 78, 101);
		public CoreColor PreviewGrid => CoreColor.FromArgb(39, 45, 60);
		public CoreColor PreviewGridMinor => CoreColor.FromArgb(28, 35, 45);
		public CoreColor PreviewJobRange => CoreColor.FromArgb(200, 200, 140);
		public CoreColor PreviewFirstMovement => CoreColor.Blue;
		public CoreColor PreviewOtherMovement => CoreColor.FromArgb(60, 82, 85);
		public CoreColor PreviewLaserPower => CoreColor.FromArgb(220, 220, 220);
		public CoreColor PreviewCross => CoreColor.FromArgb(230, 230, 20);
		public CoreColor PreviewCommandOK => CoreColor.FromArgb(55, 199, 116);
		public CoreColor PreviewCommandKO => CoreColor.FromArgb(240, 50, 50);
		public CoreColor PreviewCommandWait => CoreColor.LightPink;
		public CoreColor PreviewCrossCursor => CoreColor.FromArgb(103, 107, 117);
		public CoreColor LogBackColor => CoreColor.FromArgb(70, 79, 92);
		public CoreColor LogLeftCOMMAND => CoreColor.White;
		public CoreColor LogLeftSTARTUP => CoreColor.FromArgb(121, 214, 58);
		public CoreColor LogLeftALARM => CoreColor.Crimson;
		public CoreColor LogLeftCONFIG => CoreColor.LightGray;
		public CoreColor LogLeftFEEDBACK => CoreColor.DodgerBlue;
		public CoreColor LogLeftPOSITION => CoreColor.OrangeRed;
		public CoreColor LogLeftOTHERS => CoreColor.Pink;
		public CoreColor LogRightGOOD => CoreColor.DarkBlue;
		public CoreColor LogRightBAD => CoreColor.Red;
		public CoreColor LogRightOTHERS => CoreColor.Black;
		public CoreColor TextBoxColorOverride => CoreColor.Black;
		public CoreColor LinkColor => CoreColor.Pink;
		public CoreColor VisitedLinkColor => CoreColor.Purple;
		public CoreColor ControlsBorder => CoreColor.FromArgb(64, 67, 85);
		public CoreColor ControlsBackDisabled => CoreColor.FromArgb(180, 180, 180);
		public CoreColor DisabledButtons => CoreColor.FromArgb(85, 90, 105);
		public CoreColor PressedButtons => CoreColor.FromArgb(84, 96, 122);
	}

	// blue laser color scheme
	public class SchemeBlueLaser : IColorScheme
	{
		public Scheme Scheme => Scheme.BlueLaser;
		public bool IsDark => false;
		public CoreColor FormBackColor => CoreColor.SystemControl;
		public CoreColor FormForeColor => CoreColor.SystemControlText;
		public CoreColor PreviewBackColor => CoreColor.LightYellow;
		public CoreColor PreviewText => CoreColor.Black;
		public CoreColor PreviewRuler => CoreColor.DarkGray;
		public CoreColor PreviewGrid => CoreColor.FromArgb(242, 242, 200);
		public CoreColor PreviewGridMinor => CoreColor.FromArgb(248, 248, 220);
		public CoreColor PreviewJobRange => CoreColor.DarkGray;
		public CoreColor PreviewFirstMovement => CoreColor.Blue;
		public CoreColor PreviewOtherMovement => CoreColor.LightGray;
		public CoreColor PreviewLaserPower => CoreColor.Red;
		public CoreColor PreviewCross => CoreColor.Blue;
		public CoreColor PreviewCommandOK => CoreColor.DarkGreen;
		public CoreColor PreviewCommandKO => CoreColor.DarkRed;
		public CoreColor PreviewCommandWait => CoreColor.LightPink;
		public CoreColor PreviewCrossCursor => CoreColor.FromArgb(184, 184, 184);
		public CoreColor LogBackColor => CoreColor.White;
		public CoreColor LogLeftCOMMAND => CoreColor.Black;
		public CoreColor LogLeftSTARTUP => CoreColor.DarkGreen;
		public CoreColor LogLeftALARM => CoreColor.Crimson;
		public CoreColor LogLeftCONFIG => CoreColor.DimGray;
		public CoreColor LogLeftFEEDBACK => CoreColor.DodgerBlue;
		public CoreColor LogLeftPOSITION => CoreColor.OrangeRed;
		public CoreColor LogLeftOTHERS => CoreColor.Purple;
		public CoreColor LogRightGOOD => CoreColor.DarkBlue;
		public CoreColor LogRightBAD => CoreColor.Red;
		public CoreColor LogRightOTHERS => CoreColor.Black;
		public CoreColor TextBoxColorOverride => CoreColor.Black;
		public CoreColor LinkColor => CoreColor.DodgerBlue;
		public CoreColor VisitedLinkColor => CoreColor.Purple;
		public CoreColor ControlsBorder => CoreColor.FromArgb(220, 220, 220);
		public CoreColor ControlsBackDisabled => CoreColor.FromArgb(180, 180, 180);
		public CoreColor DisabledButtons => CoreColor.FromArgb(70, 70, 70);
		public CoreColor PressedButtons => CoreColor.Crimson;
	}

	// red laser color scheme
	public class SchemeRedLaser : IColorScheme
	{
		public Scheme Scheme => Scheme.RedLaser;
		public bool IsDark => false;
		public CoreColor FormBackColor => CoreColor.SystemControl;
		public CoreColor FormForeColor => CoreColor.SystemControlText;
		public CoreColor PreviewBackColor => CoreColor.LightYellow;
		public CoreColor PreviewText => CoreColor.Black;
		public CoreColor PreviewRuler => CoreColor.DarkGray;
		public CoreColor PreviewGrid => CoreColor.FromArgb(242, 242, 200);
		public CoreColor PreviewGridMinor => CoreColor.FromArgb(248, 248, 220);
		public CoreColor PreviewJobRange => CoreColor.DarkGray;
		public CoreColor PreviewFirstMovement => CoreColor.Blue;
		public CoreColor PreviewOtherMovement => CoreColor.LightGray;
		public CoreColor PreviewLaserPower => CoreColor.DarkBlue;
		public CoreColor PreviewCross => CoreColor.DarkViolet;
		public CoreColor PreviewCommandOK => CoreColor.DarkGreen;
		public CoreColor PreviewCommandKO => CoreColor.DarkRed;
		public CoreColor PreviewCommandWait => CoreColor.LightBlue;
		public CoreColor PreviewCrossCursor => CoreColor.FromArgb(184, 184, 184);
		public CoreColor LogBackColor => CoreColor.White;
		public CoreColor LogLeftCOMMAND => CoreColor.Black;
		public CoreColor LogLeftSTARTUP => CoreColor.DarkGreen;
		public CoreColor LogLeftALARM => CoreColor.Crimson;
		public CoreColor LogLeftCONFIG => CoreColor.DimGray;
		public CoreColor LogLeftFEEDBACK => CoreColor.DodgerBlue;
		public CoreColor LogLeftPOSITION => CoreColor.OrangeRed;
		public CoreColor LogLeftOTHERS => CoreColor.Purple;
		public CoreColor LogRightGOOD => CoreColor.DarkGreen;
		public CoreColor LogRightBAD => CoreColor.Red;
		public CoreColor LogRightOTHERS => CoreColor.Black;
		public CoreColor TextBoxColorOverride => CoreColor.Black;
		public CoreColor LinkColor => CoreColor.DodgerBlue;
		public CoreColor VisitedLinkColor => CoreColor.Purple;
		public CoreColor ControlsBorder => CoreColor.FromArgb(220, 220, 220);
		public CoreColor ControlsBackDisabled => CoreColor.FromArgb(180, 180, 180);
		public CoreColor DisabledButtons => CoreColor.FromArgb(100, 100, 100);
		public CoreColor PressedButtons => CoreColor.Crimson;
	}

	// dark color scheme
	public class SchemeDark : IColorScheme
	{
		public Scheme Scheme => Scheme.Dark;
		public bool IsDark => true;
		public CoreColor FormBackColor => CoreColor.FromArgb(29, 44, 75);
		public CoreColor FormForeColor => CoreColor.White;
		public CoreColor PreviewBackColor => CoreColor.FromArgb(220, 220, 220);
		public CoreColor PreviewText => CoreColor.Black;
		public CoreColor PreviewRuler => CoreColor.DarkGray;
		public CoreColor PreviewGrid => CoreColor.FromArgb(210, 210, 210);
		public CoreColor PreviewGridMinor => CoreColor.FromArgb(220, 220, 220);
		public CoreColor PreviewJobRange => CoreColor.DimGray;
		public CoreColor PreviewFirstMovement => CoreColor.Blue;
		public CoreColor PreviewOtherMovement => CoreColor.FromArgb(180, 118, 0);
		public CoreColor PreviewLaserPower => CoreColor.Red;
		public CoreColor PreviewCross => CoreColor.DarkMagenta;
		public CoreColor PreviewCommandOK => CoreColor.DarkGreen;
		public CoreColor PreviewCommandKO => CoreColor.DarkRed;
		public CoreColor PreviewCommandWait => CoreColor.LightBlue;
		public CoreColor PreviewCrossCursor => CoreColor.Gray;
		public CoreColor LogBackColor => CoreColor.FromArgb(220, 220, 220);
		public CoreColor LogLeftCOMMAND => CoreColor.Black;
		public CoreColor LogLeftSTARTUP => CoreColor.DarkGreen;
		public CoreColor LogLeftALARM => CoreColor.DarkRed;
		public CoreColor LogLeftCONFIG => CoreColor.DarkSlateGray;
		public CoreColor LogLeftFEEDBACK => CoreColor.DarkBlue;
		public CoreColor LogLeftPOSITION => CoreColor.OrangeRed;
		public CoreColor LogLeftOTHERS => CoreColor.Purple;
		public CoreColor LogRightGOOD => CoreColor.Lime;
		public CoreColor LogRightBAD => CoreColor.OrangeRed;
		public CoreColor LogRightOTHERS => CoreColor.White;
		public CoreColor TextBoxColorOverride => CoreColor.White;
		public CoreColor LinkColor => CoreColor.Yellow;
		public CoreColor VisitedLinkColor => CoreColor.Violet;
		public CoreColor ControlsBorder => CoreColor.FromArgb(39, 54, 85);
		public CoreColor ControlsBackDisabled => CoreColor.FromArgb(180, 180, 180);
		public CoreColor DisabledButtons => CoreColor.FromArgb(100, 100, 100);
		public CoreColor PressedButtons => CoreColor.Crimson;
	}

	// hacker color scheme
	public class SchemeHacker : IColorScheme
	{
		public Scheme Scheme => Scheme.Hacker;
		public bool IsDark => true;
		public CoreColor FormBackColor => CoreColor.FromArgb(0, 10, 35);
		public CoreColor FormForeColor => CoreColor.LimeGreen;
		public CoreColor PreviewBackColor => CoreColor.FromArgb(220, 220, 220);
		public CoreColor PreviewText => CoreColor.Black;
		public CoreColor PreviewRuler => CoreColor.DarkGray;
		public CoreColor PreviewGrid => CoreColor.FromArgb(210, 210, 210);
		public CoreColor PreviewGridMinor => CoreColor.FromArgb(220, 220, 220);
		public CoreColor PreviewJobRange => CoreColor.DimGray;
		public CoreColor PreviewFirstMovement => CoreColor.Blue;
		public CoreColor PreviewOtherMovement => CoreColor.FromArgb(180, 118, 0);
		public CoreColor PreviewLaserPower => CoreColor.DarkGreen;
		public CoreColor PreviewCross => CoreColor.DarkMagenta;
		public CoreColor PreviewCommandOK => CoreColor.DarkBlue;
		public CoreColor PreviewCommandKO => CoreColor.OrangeRed;
		public CoreColor PreviewCommandWait => CoreColor.Pink;
		public CoreColor PreviewCrossCursor => CoreColor.Gray;
		public CoreColor LogBackColor => CoreColor.FromArgb(20, 20, 20);
		public CoreColor LogLeftCOMMAND => CoreColor.LimeGreen;
		public CoreColor LogLeftSTARTUP => CoreColor.Pink;
		public CoreColor LogLeftALARM => CoreColor.Red;
		public CoreColor LogLeftCONFIG => CoreColor.LightGray;
		public CoreColor LogLeftFEEDBACK => CoreColor.LightBlue;
		public CoreColor LogLeftPOSITION => CoreColor.OrangeRed;
		public CoreColor LogLeftOTHERS => CoreColor.Pink;
		public CoreColor LogRightGOOD => CoreColor.LightBlue;
		public CoreColor LogRightBAD => CoreColor.Red;
		public CoreColor LogRightOTHERS => CoreColor.White;
		public CoreColor TextBoxColorOverride => CoreColor.White;
		public CoreColor LinkColor => CoreColor.Yellow;
		public CoreColor VisitedLinkColor => CoreColor.Violet;
		public CoreColor ControlsBorder => CoreColor.FromArgb(15, 25, 50);
		public CoreColor ControlsBackDisabled => CoreColor.FromArgb(180, 180, 180);
		public CoreColor DisabledButtons => CoreColor.FromArgb(100, 100, 100);
		public CoreColor PressedButtons => CoreColor.Crimson;
	}

	// nighty color scheme
	public class SchemeNighty : IColorScheme
	{
		public Scheme Scheme => Scheme.Nighty;
		public bool IsDark => true;
		public CoreColor FormBackColor => CoreColor.FromArgb(25, 25, 25);
		public CoreColor FormForeColor => CoreColor.Aqua;
		public CoreColor PreviewBackColor => CoreColor.FromArgb(25, 25, 25);
		public CoreColor PreviewText => CoreColor.Aqua;
		public CoreColor PreviewRuler => CoreColor.Aqua;
		public CoreColor PreviewGrid => CoreColor.FromArgb(34, 34, 34);
		public CoreColor PreviewGridMinor => CoreColor.FromArgb(28, 28, 28);
		public CoreColor PreviewJobRange => CoreColor.LightPink;
		public CoreColor PreviewFirstMovement => CoreColor.DarkOrange;
		public CoreColor PreviewOtherMovement => CoreColor.FromArgb(150, 0, 120);
		public CoreColor PreviewLaserPower => CoreColor.FromArgb(0, 125, 140);
		public CoreColor PreviewCross => CoreColor.Pink;
		public CoreColor PreviewCommandOK => CoreColor.DarkGreen;
		public CoreColor PreviewCommandKO => CoreColor.DarkRed;
		public CoreColor PreviewCommandWait => CoreColor.LightBlue;
		public CoreColor PreviewCrossCursor => CoreColor.Gray;
		public CoreColor LogBackColor => CoreColor.FromArgb(25, 25, 25);
		public CoreColor LogLeftCOMMAND => CoreColor.Aqua;
		public CoreColor LogLeftSTARTUP => CoreColor.FromArgb(220, 30, 220);
		public CoreColor LogLeftALARM => CoreColor.FromArgb(0, 127, 139);
		public CoreColor LogLeftCONFIG => CoreColor.MediumVioletRed;
		public CoreColor LogLeftFEEDBACK => CoreColor.MediumVioletRed;
		public CoreColor LogLeftPOSITION => CoreColor.MediumPurple;
		public CoreColor LogLeftOTHERS => CoreColor.Purple;
		public CoreColor LogRightGOOD => CoreColor.DarkGreen;
		public CoreColor LogRightBAD => CoreColor.Red;
		public CoreColor LogRightOTHERS => CoreColor.LimeGreen;
		public CoreColor TextBoxColorOverride => CoreColor.LimeGreen;
		public CoreColor LinkColor => CoreColor.Yellow;
		public CoreColor VisitedLinkColor => CoreColor.Violet;
		public CoreColor ControlsBorder => CoreColor.FromArgb(40, 40, 40);
		public CoreColor ControlsBackDisabled => CoreColor.FromArgb(180, 180, 180);
		public CoreColor DisabledButtons => CoreColor.FromArgb(100, 100, 100);
		public CoreColor PressedButtons => CoreColor.Crimson;
	}

	public class ColorScheme
	{
		public enum Scheme
		{
			CADStyle,
			CADDark,
			BlueLaser,
			RedLaser,
			Dark,
			Hacker,
			Nighty
		}

		private static Dictionary<Scheme, IColorScheme> mDefaultSchemas;

		static ColorScheme()
		{
			mDefaultSchemas = new Dictionary<Scheme, IColorScheme>();
			AddSchema(new SchemeCADStyle());
			AddSchema(new SchemeCADDark());
			AddSchema(new SchemeBlueLaser());
			AddSchema(new SchemeRedLaser());
			AddSchema(new SchemeDark());
			AddSchema(new SchemeHacker());
			AddSchema(new SchemeNighty());
			CurrentScheme = Scheme.RedLaser;
		}

		public static void AddSchema(IColorScheme colorSchema)
		{
			mDefaultSchemas.Add(colorSchema.Scheme, colorSchema);
		}

		public static Scheme CurrentScheme { get; set; }

		private static IColorScheme CurrentSchemeColors => mDefaultSchemas[CurrentScheme];

		public static bool DarkScheme => CurrentSchemeColors.IsDark;

		public static CoreColor FormBackColor => CurrentSchemeColors.FormBackColor;
		public static CoreColor FormForeColor => CurrentSchemeColors.FormForeColor;
		public static CoreColor PreviewBackColor => CurrentSchemeColors.PreviewBackColor;
		public static CoreColor PreviewText => CurrentSchemeColors.PreviewText;
		public static CoreColor PreviewRuler => CurrentSchemeColors.PreviewRuler;
		public static CoreColor PreviewGrid => CurrentSchemeColors.PreviewGrid;
		public static CoreColor PreviewGridMinor => CurrentSchemeColors.PreviewGridMinor;
		public static CoreColor PreviewJobRange => CurrentSchemeColors.PreviewJobRange;
		public static CoreColor PreviewFirstMovement => CurrentSchemeColors.PreviewFirstMovement;
		public static CoreColor PreviewOtherMovement => CurrentSchemeColors.PreviewOtherMovement;
		public static CoreColor PreviewLaserPower => CurrentSchemeColors.PreviewLaserPower;
		public static CoreColor PreviewCross => CurrentSchemeColors.PreviewCross;
		public static CoreColor PreviewCommandOK => CurrentSchemeColors.PreviewCommandOK;
		public static CoreColor PreviewCommandKO => CurrentSchemeColors.PreviewCommandKO;
		public static CoreColor PreviewCommandWait => CurrentSchemeColors.PreviewCommandWait;
		public static CoreColor PreviewCrossCursor => CurrentSchemeColors.PreviewCrossCursor;
		public static CoreColor LogBackColor => CurrentSchemeColors.LogBackColor;
		public static CoreColor LogLeftCOMMAND => CurrentSchemeColors.LogLeftCOMMAND;
		public static CoreColor LogLeftSTARTUP => CurrentSchemeColors.LogLeftSTARTUP;
		public static CoreColor LogLeftALARM => CurrentSchemeColors.LogLeftALARM;
		public static CoreColor LogLeftCONFIG => CurrentSchemeColors.LogLeftCONFIG;
		public static CoreColor LogLeftFEEDBACK => CurrentSchemeColors.LogLeftFEEDBACK;
		public static CoreColor LogLeftPOSITION => CurrentSchemeColors.LogLeftPOSITION;
		public static CoreColor LogLeftOTHERS => CurrentSchemeColors.LogLeftOTHERS;
		public static CoreColor LogRightGOOD => CurrentSchemeColors.LogRightGOOD;
		public static CoreColor LogRightBAD => CurrentSchemeColors.LogRightBAD;
		public static CoreColor LogRightOTHERS => CurrentSchemeColors.LogRightOTHERS;
		public static CoreColor TextBoxColorOverride => CurrentSchemeColors.TextBoxColorOverride;
		public static CoreColor LinkColor => CurrentSchemeColors.LinkColor;
		public static CoreColor VisitedLinkColor => CurrentSchemeColors.VisitedLinkColor;
		public static CoreColor ControlsBorder => CurrentSchemeColors.ControlsBorder;
		public static CoreColor ControlsBackDisabled => CurrentSchemeColors.ControlsBackDisabled;
		public static CoreColor DisabledButtons => CurrentSchemeColors.DisabledButtons;
		public static CoreColor PressedButtons => CurrentSchemeColors.PressedButtons;

		public static CoreColor ChangeColorBrightness(CoreColor color, float correctionFactor)
		{
			float red   = color.R;
			float green = color.G;
			float blue  = color.B;

			if (correctionFactor < 0)
			{
				correctionFactor = 1 + correctionFactor;
				red   *= correctionFactor;
				green *= correctionFactor;
				blue  *= correctionFactor;
			}
			else
			{
				red   = (255 - red)   * correctionFactor + red;
				green = (255 - green) * correctionFactor + green;
				blue  = (255 - blue)  * correctionFactor + blue;
			}

			return CoreColor.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
		}

		public static CoreColor FormButtonsColor
		{
			get
			{
				if (DarkScheme)
					return ChangeColorBrightness(FormBackColor, +0.1f);
				else
					return ChangeColorBrightness(FormBackColor, -0.1f);
			}
		}

		public static CoreColor MenuHighlightColor
		{
			get
			{
				if (DarkScheme)
					return ChangeColorBrightness(FormBackColor, +0.2f);
				else
					return ChangeColorBrightness(FormBackColor, -0.1f);
			}
		}

		public static CoreColor MenuSeparatorColor
		{
			get
			{
				if (DarkScheme)
					return ChangeColorBrightness(FormBackColor, +0.15f);
				else
					return ChangeColorBrightness(FormBackColor, -0.1f);
			}
		}
	}
}
