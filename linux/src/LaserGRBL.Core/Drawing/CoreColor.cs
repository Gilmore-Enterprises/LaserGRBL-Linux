using System;

namespace LaserGRBL
{
	/// <summary>
	/// Platform-neutral ARGB color. Replaces System.Drawing.Color in Core so the
	/// layer compiles on .NET 10/Linux without GDI+. The App layer maps to Avalonia.Media.Color.
	/// </summary>
	public readonly struct CoreColor : IEquatable<CoreColor>
	{
		public readonly byte A;
		public readonly byte R;
		public readonly byte G;
		public readonly byte B;

		public CoreColor(byte r, byte g, byte b, byte a = 255)
		{
			A = a; R = r; G = g; B = b;
		}

		public static CoreColor FromArgb(byte a, byte r, byte g, byte b) => new CoreColor(r, g, b, a);
		public static CoreColor FromArgb(byte r, byte g, byte b) => new CoreColor(r, g, b);
		public static CoreColor FromArgb(int argb)
		{
			return new CoreColor(
				r: (byte)((argb >> 16) & 0xFF),
				g: (byte)((argb >> 8) & 0xFF),
				b: (byte)(argb & 0xFF),
				a: (byte)((argb >> 24) & 0xFF));
		}

		public int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;

		// Common named colors matching System.Drawing.Color ARGB values.
		public static readonly CoreColor Black          = new CoreColor(0,   0,   0  );
		public static readonly CoreColor White          = new CoreColor(255, 255, 255);
		public static readonly CoreColor Red            = new CoreColor(255, 0,   0  );
		public static readonly CoreColor Green          = new CoreColor(0,   128, 0  );
		public static readonly CoreColor Blue           = new CoreColor(0,   0,   255);
		public static readonly CoreColor Gray           = new CoreColor(128, 128, 128);
		public static readonly CoreColor DarkGray       = new CoreColor(169, 169, 169);
		public static readonly CoreColor LightGray      = new CoreColor(211, 211, 211);
		public static readonly CoreColor Orange         = new CoreColor(255, 165, 0  );
		public static readonly CoreColor Yellow         = new CoreColor(255, 255, 0  );
		public static readonly CoreColor Transparent    = new CoreColor(0,   0,   0,  0);
		public static readonly CoreColor DarkGreen      = new CoreColor(0,   100, 0  );
		public static readonly CoreColor Crimson        = new CoreColor(220, 20,  60 );
		public static readonly CoreColor DimGray        = new CoreColor(105, 105, 105);
		public static readonly CoreColor DodgerBlue     = new CoreColor(30,  144, 255);
		public static readonly CoreColor OrangeRed      = new CoreColor(255, 69,  0  );
		public static readonly CoreColor Purple         = new CoreColor(128, 0,   128);
		public static readonly CoreColor DarkBlue       = new CoreColor(0,   0,   139);
		public static readonly CoreColor LightPink      = new CoreColor(255, 182, 193);
		public static readonly CoreColor LightYellow    = new CoreColor(255, 255, 224);
		public static readonly CoreColor Pink           = new CoreColor(255, 192, 203);
		public static readonly CoreColor DarkRed        = new CoreColor(139, 0,   0  );
		public static readonly CoreColor DarkSlateGray  = new CoreColor(47,  79,  79 );
		public static readonly CoreColor Lime           = new CoreColor(0,   255, 0  );
		public static readonly CoreColor Violet         = new CoreColor(238, 130, 238);
		public static readonly CoreColor LimeGreen      = new CoreColor(50,  205, 50 );
		public static readonly CoreColor LightBlue      = new CoreColor(173, 216, 230);
		public static readonly CoreColor Aqua           = new CoreColor(0,   255, 255);
		public static readonly CoreColor DarkOrange     = new CoreColor(255, 140, 0  );
		public static readonly CoreColor MediumVioletRed = new CoreColor(199, 21, 133);
		public static readonly CoreColor MediumPurple   = new CoreColor(147, 112, 219);
		public static readonly CoreColor DarkViolet     = new CoreColor(148, 0,   211);
		public static readonly CoreColor DarkMagenta    = new CoreColor(139, 0,   139);
		// Platform-agnostic equivalents for WinForms SystemColors used in color schemes.
		public static readonly CoreColor SystemControl     = new CoreColor(240, 240, 240); // SystemColors.Control
		public static readonly CoreColor SystemControlText = new CoreColor(0,   0,   0  ); // SystemColors.ControlText

		public bool Equals(CoreColor other) => A == other.A && R == other.R && G == other.G && B == other.B;
		public override bool Equals(object obj) => obj is CoreColor c && Equals(c);
		public override int GetHashCode() => ToArgb();
		public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

		public static bool operator ==(CoreColor a, CoreColor b) => a.Equals(b);
		public static bool operator !=(CoreColor a, CoreColor b) => !a.Equals(b);
	}
}
