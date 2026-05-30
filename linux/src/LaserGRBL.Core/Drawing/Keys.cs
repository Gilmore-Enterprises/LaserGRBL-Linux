// Cross-platform Keys enum with the same integer values as System.Windows.Forms.Keys,
// covering the subset used by HotKeysManager. Same values = serialization compatibility.
// The App layer maps CoreKeys ↔ Avalonia.Input.Key when dispatching hotkeys.
using System;

namespace LaserGRBL
{
	[Flags]
	public enum Keys
	{
		None      = 0,
		// Modifier flags (same bit positions as WinForms)
		Shift     = 0x10000,
		Control   = 0x20000,
		Alt       = 0x40000,
		Modifiers = unchecked((int)0xFFFF0000),

		// Letter keys
		A = 0x41, H = 0x48, O = 0x4F, R = 0x52,
		S = 0x53, U = 0x55, X = 0x58, Z = 0x5A,

		// Digit keys
		D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33, D4 = 0x34,
		D5 = 0x35, D6 = 0x36, D7 = 0x37, D8 = 0x38, D9 = 0x39,

		// Function keys
		F1  = 0x70, F5  = 0x74, F6  = 0x75, F7  = 0x76,
		F12 = 0x7B, F24 = 0x87,

		// Numpad
		NumPad0 = 0x60, NumPad1 = 0x61, NumPad2 = 0x62, NumPad3 = 0x63,
		NumPad4 = 0x64, NumPad5 = 0x65, NumPad6 = 0x66, NumPad7 = 0x67,
		NumPad8 = 0x68, NumPad9 = 0x69,
		Multiply = 0x6A, Add = 0x6B, Subtract = 0x6D, Divide = 0x6F,
	}
}
