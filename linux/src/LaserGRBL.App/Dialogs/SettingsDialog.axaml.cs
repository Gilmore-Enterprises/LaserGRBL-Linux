// Settings dialog. Ports SettingsForm.cs.
// Mirrors the 8 settings tabs: Hardware, Raster Import, Vector Import,
// Jog Control, Auto Cooling, G-Code, Sound, Options.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using LaserGRBL;

namespace LaserGRBL.Dialogs
{
	public partial class SettingsDialog : Window
	{
		private readonly GrblCore _core;

		public SettingsDialog(GrblCore core)
		{
			_core = core;
			InitializeComponent();
			Opened += OnOpened;
		}

		private void OnOpened(object sender, EventArgs e)
		{
			LoadSettings();
		}

		private void LoadSettings()
		{
			// Hardware
			var wrapper = Settings.GetObject("ComWrapper Protocol", ComWrapper.WrapperType.UsbSerial);
			RbProtocolUsb.IsChecked    = wrapper == ComWrapper.WrapperType.UsbSerial;
			RbProtocolUsb2.IsChecked   = wrapper == ComWrapper.WrapperType.UsbSerial2;
			RbProtocolRjcp.IsChecked   = wrapper == ComWrapper.WrapperType.RJCPSerial;
			RbProtocolTelnet.IsChecked = wrapper == ComWrapper.WrapperType.Telnet;
			RbProtocolWifi.IsChecked   = wrapper == ComWrapper.WrapperType.LaserWebESP8266;

			var fw = Settings.GetObject("Firmware Type", Firmware.Grbl);
			RbFwGrbl.IsChecked     = fw == Firmware.Grbl;
			RbFwSmoothie.IsChecked = fw == Firmware.Smoothie;
			RbFwMarlin.IsChecked   = fw == Firmware.Marlin;

			NudTableWidth.Value  = (decimal)Settings.GetObject("Bed Size X", 200.0);
			NudTableHeight.Value = (decimal)Settings.GetObject("Bed Size Y", 200.0);

			// GCode
			TxtHeader.Text = Settings.GetObject("GCode.CustomHeader", GrblCore.GCODE_STD_HEADER);
			TxtFooter.Text = Settings.GetObject("GCode.CustomFooter", GrblCore.GCODE_STD_FOOTER);
			CbDisableFastSkip.IsChecked = Settings.GetObject("Disable G0 fast skip", false);

			// Laser on/off
			TxtLaserOn.Text  = Settings.GetObject("GCode.LaserOn",  "M3");
			TxtLaserOff.Text = Settings.GetObject("GCode.LaserOff", "M5");

			// Sound
			CbSoundSuccess.IsChecked    = Settings.GetObject("Sound.Success.Enabled",    true);
			CbSoundWarning.IsChecked    = Settings.GetObject("Sound.Warning.Enabled",    true);
			CbSoundFatal.IsChecked      = Settings.GetObject("Sound.Fatal.Enabled",      true);
			CbSoundConnect.IsChecked    = Settings.GetObject("Sound.Connect.Enabled",    true);
			CbSoundDisconnect.IsChecked = Settings.GetObject("Sound.Disconnect.Enabled", true);

			// Color scheme
			var scheme = Settings.GetObject("ColorScheme", ColorScheme.Scheme.RedLaser);
			CbColorScheme.SelectedIndex = (int)scheme;
		}

		private void BtnOK_Click(object sender, RoutedEventArgs e)
		{
			SaveSettings();
			Close();
		}

		private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

		private void SaveSettings()
		{
			// Hardware: protocol
			var wrapper = ComWrapper.WrapperType.UsbSerial;
			if (RbProtocolUsb2.IsChecked   == true) wrapper = ComWrapper.WrapperType.UsbSerial2;
			if (RbProtocolRjcp.IsChecked   == true) wrapper = ComWrapper.WrapperType.RJCPSerial;
			if (RbProtocolTelnet.IsChecked == true) wrapper = ComWrapper.WrapperType.Telnet;
			if (RbProtocolWifi.IsChecked   == true) wrapper = ComWrapper.WrapperType.LaserWebESP8266;
			Settings.SetObject("ComWrapper Protocol", wrapper);

			// Firmware
			var fw = Firmware.Grbl;
			if (RbFwSmoothie.IsChecked == true) fw = Firmware.Smoothie;
			if (RbFwMarlin.IsChecked   == true) fw = Firmware.Marlin;
			Settings.SetObject("Firmware Type", fw);

			// Bed size
			Settings.SetObject("Bed Size X", (double)(NudTableWidth.Value  ?? 200m));
			Settings.SetObject("Bed Size Y", (double)(NudTableHeight.Value ?? 200m));

			// GCode
			Settings.SetObject("GCode.CustomHeader", TxtHeader.Text ?? GrblCore.GCODE_STD_HEADER);
			Settings.SetObject("GCode.CustomFooter", TxtFooter.Text ?? GrblCore.GCODE_STD_FOOTER);
			Settings.SetObject("Disable G0 fast skip", CbDisableFastSkip.IsChecked == true);

			// Laser commands
			Settings.SetObject("GCode.LaserOn",  TxtLaserOn.Text  ?? "M3");
			Settings.SetObject("GCode.LaserOff", TxtLaserOff.Text ?? "M5");

			// Sound
			Settings.SetObject("Sound.Success.Enabled",    CbSoundSuccess.IsChecked    == true);
			Settings.SetObject("Sound.Warning.Enabled",    CbSoundWarning.IsChecked    == true);
			Settings.SetObject("Sound.Fatal.Enabled",      CbSoundFatal.IsChecked      == true);
			Settings.SetObject("Sound.Connect.Enabled",    CbSoundConnect.IsChecked    == true);
			Settings.SetObject("Sound.Disconnect.Enabled", CbSoundDisconnect.IsChecked == true);

			// Color scheme
			var scheme = (ColorScheme.Scheme)(CbColorScheme.SelectedIndex);
			Settings.SetObject("ColorScheme", scheme);
			ColorScheme.CurrentScheme = scheme;
		}
	}
}
