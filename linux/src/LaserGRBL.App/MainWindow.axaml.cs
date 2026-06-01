using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LaserGRBL.Platform;

namespace LaserGRBL
{
	public partial class MainWindow : Window, IGrblCoreUI
	{
		private GrblCore _core;
		private readonly List<string> _commandHistory = new();
		private int _historyIndex = -1;

		public MainWindow()
		{
			InitializeComponent();
			Opened  += OnOpened;
			Closing += OnClosing;
		}

		private void OnOpened(object sender, EventArgs e)
		{
			PlatformServices.Initialize();
			var syncCtx = SynchronizationContext.Current ?? new SynchronizationContext();
			_core = new GrblCore(syncCtx, this, new PreviewFormProxy(), new JogFormProxy());
			GrblCore.StaticUI = this;
			_core.MachineStatusChanged += OnMachineStatusChanged;
			_core.OnFileLoaded         += OnFileLoaded;
			RefreshPortList();
			int savedBaud = Settings.GetObject("Port BaudRate", 115200);
			var baudItem = CbBaud.Items.Cast<ComboBoxItem>()
				.FirstOrDefault(i => i.Content?.ToString() == savedBaud.ToString());
			CbBaud.SelectedItem = baudItem ?? CbBaud.Items[0];
			Title = $"LaserGRBL {CoreApp.CurrentVersion}";
		}

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_core?.CloseCom(true);
			Settings.Exiting();
		}

		private void RefreshPortList()
		{
			var ports = PlatformServices.Serial?.EnumeratePorts() ?? Enumerable.Empty<SerialPortInfo>();
			CbCOM.Items.Clear();
			foreach (var p in ports)
				CbCOM.Items.Add(new ComboBoxItem { Content = p.Device });
			string last = Settings.GetObject<string>("Port ComName", null);
			if (last != null)
				CbCOM.SelectedItem = CbCOM.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content?.ToString() == last);
			if (CbCOM.SelectedItem == null && CbCOM.Items.Count > 0)
				CbCOM.SelectedIndex = 0;
		}

		private void MnConnect_Click(object sender, RoutedEventArgs e)
		{
			if (_core.IsConnected) _core.CloseCom(true);
			else DoConnect();
		}
		private void MnDisconnect_Click(object s, RoutedEventArgs e) => _core?.CloseCom(true);

		private void DoConnect()
		{
			string port = (CbCOM.SelectedItem as ComboBoxItem)?.Content?.ToString();
			if (string.IsNullOrEmpty(port)) { (this as IGrblCoreUI).ShowError("No COM port selected.", "Connect"); return; }
			if (!int.TryParse((CbBaud.SelectedItem as ComboBoxItem)?.Content?.ToString(), out int baud)) baud = 115200;
			Settings.SetObject("Port ComName", port);
			Settings.SetObject("Port BaudRate", baud);
			_core.Configure(ComWrapper.WrapperType.UsbSerial, port, baud);
			_core.OpenCom();
		}

		private void OnMachineStatusChanged()
		{
			Dispatcher.UIThread.Post(() =>
			{
				bool c = _core.IsConnected, r = _core.InProgram;
				MnDisconnect.IsEnabled  = c;
				BtnDisconnect.IsEnabled = c;
				BtnConnectMain.Content  = c ? "Stop" : "Connect";
				MnGrblReset.IsEnabled   = c; MnGoHome.IsEnabled = c && GrblCore.Configuration.HomingEnabled;
				MnUnlock.IsEnabled      = c; MnGrblConfig.IsEnabled = c;
				BtnHome.IsEnabled  = c && GrblCore.Configuration.HomingEnabled;
				BtnUnlock.IsEnabled = BtnReset.IsEnabled = c;
				BtnRun.IsEnabled    = c && _core.HasProgram && !r;
				BtnSave.IsEnabled   = MnSave.IsEnabled = MnSaveProject.IsEnabled = _core.HasProgram;
				BtnFeedHold.IsEnabled = BtnResume.IsEnabled = r;
				BtnSendCommand.IsEnabled = TxtCommand.IsEnabled = c;
				SlOvLinear.IsEnabled = SlOvSpeed.IsEnabled = SlOvPower.IsEnabled = c;
				foreach (var btn in new[]{BtnJogXPlus,BtnJogXMinus,BtnJogYPlus,BtnJogYMinus,BtnJogHome})
					btn.IsEnabled = c;
				StatusMachine.Text = _core.MachineStatus.ToString();
				LblConnStatus.Text = c ? $"Connected - {_core.GrblVersion}" : "Not connected";
			});
		}

		private void OnFileLoaded(long elapsed, string filename)
		{
			Dispatcher.UIThread.Post(() =>
			{
				Title = filename != null ? $"LaserGRBL - {System.IO.Path.GetFileName(filename)}" : "LaserGRBL";
				BtnSave.IsEnabled = MnSave.IsEnabled = _core.HasProgram;
				BtnRun.IsEnabled  = _core.IsConnected && _core.HasProgram;
				PreviewHint.IsVisible = !_core.HasProgram;
			});
		}

		// Override sliders: real-time override sent as GRBL realtime chars in Phase 7.
		private void SlOvLinear_Changed(object s, RangeBaseValueChangedEventArgs e) { LblOvLinear.Text=$"{(int)e.NewValue}%"; }
		private void SlOvSpeed_Changed (object s, RangeBaseValueChangedEventArgs e) { LblOvSpeed.Text =$"{(int)e.NewValue}%"; }
		private void SlOvPower_Changed (object s, RangeBaseValueChangedEventArgs e) { LblOvPower.Text =$"{(int)e.NewValue}%"; }

		private void JogXPlus_Click (object s, RoutedEventArgs e) => _core?.JogToDirection(GrblCore.JogDirection.E, false);
		private void JogXMinus_Click(object s, RoutedEventArgs e) => _core?.JogToDirection(GrblCore.JogDirection.W, false);
		private void JogYPlus_Click (object s, RoutedEventArgs e) => _core?.JogToDirection(GrblCore.JogDirection.N, false);
		private void JogYMinus_Click(object s, RoutedEventArgs e) => _core?.JogToDirection(GrblCore.JogDirection.S, false);
		private void JogHome_Click  (object s, RoutedEventArgs e) => _core?.JogToPosition(new CorePoint(0,0), true);

		private void TxtCommand_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return) BtnSendCommand_Click(sender, e);
		}
		private void BtnSendCommand_Click(object s, RoutedEventArgs e)
		{
			string cmd = TxtCommand.Text?.Trim();
			if (string.IsNullOrEmpty(cmd)) return;
			_core?.EnqueueCommand(new GrblCommand(cmd));
			_commandHistory.Add(cmd); _historyIndex = _commandHistory.Count;
			TxtCommand.Text = "";
		}

		private void PreviewCanvas_PointerPressed (object s, PointerPressedEventArgs e) {}
		private void PreviewCanvas_PointerMoved  (object s, PointerEventArgs e) {}
		private void PreviewCanvas_PointerReleased(object s, PointerReleasedEventArgs e) {}

		private void BtnRun_Click     (object s, RoutedEventArgs e) => _core?.RunProgram(null);
		private void BtnFeedHold_Click(object s, RoutedEventArgs e) => _core?.FeedHold(false);
		private void BtnResume_Click  (object s, RoutedEventArgs e) => _core?.CycleStartResume(false);

		private async void MnOpen_Click(object s, RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog { Title = "Open file",
				Filters = new() { new() { Name="All supported", Extensions=new[]{"nc","cnc","gcode","ngc","tap","bmp","png","jpg","jpeg","gif","svg","lps"}.ToList() } } };
			var f = await dlg.ShowAsync(this);
			if (f?.Length > 0) _core.OpenFile(f[0]);
		}
		private async void MnSave_Click(object s, RoutedEventArgs e)
		{
			var dlg = new SaveFileDialog { Title="Save G-code", DefaultExtension="nc",
				Filters = new(){ new(){ Name="G-code", Extensions=new[]{"nc"}.ToList() } } };
			if (!string.IsNullOrEmpty(await dlg.ShowAsync(this))) _core.SaveProgram(null, true, true, false, 1, false);
		}
		private async void MnSaveProject_Click(object s, RoutedEventArgs e)
		{
			var dlg = new SaveFileDialog { Title="Save Project", DefaultExtension="lps",
				Filters=new(){ new(){ Name="LaserGRBL Project", Extensions=new[]{"lps"}.ToList() } } };
			if (!string.IsNullOrEmpty(await dlg.ShowAsync(this))) _core.SaveProject(null);
		}
		private void MnImportRaster_Click(object s, RoutedEventArgs e) => _core?.OpenFile(null);
		private void MnImportSVG_Click   (object s, RoutedEventArgs e) => _core?.OpenFile(null);

		private void MnGrblReset_Click    (object s, RoutedEventArgs e) => _core?.GrblReset();
		private void MnGoHome_Click       (object s, RoutedEventArgs e) => _core?.EnqueueCommand(new GrblCommand("$H"));
		private void MnUnlock_Click       (object s, RoutedEventArgs e) => _core?.EnqueueCommand(new GrblCommand("$X"));
		private void MnGrblConfig_Click   (object s, RoutedEventArgs e) { }
		private void MnSettings_Click     (object s, RoutedEventArgs e) { }
		private void MnMaterialDB_Click   (object s, RoutedEventArgs e) { }
		private void MnWiFiDiscovery_Click(object s, RoutedEventArgs e) { }
		private void MnExit_Click         (object s, RoutedEventArgs e) => Close();
		private void MnJog_Click          (object s, RoutedEventArgs e) {}
		private void MnCustomButtons_Click (object s, RoutedEventArgs e) { }
		private void MnHotkeys_Click       (object s, RoutedEventArgs e) { }
		private void MnLaserSelector_Click (object s, RoutedEventArgs e) {}
		private void MnLaserLife_Click     (object s, RoutedEventArgs e) { }
		private void MnPowerVsSpeed_Click  (object s, RoutedEventArgs e) { }
		private void MnShakeTest_Click     (object s, RoutedEventArgs e) { }
		private void MnCuttingTest_Click   (object s, RoutedEventArgs e) { }
		private void MnFlashGrbl_Click     (object s, RoutedEventArgs e) { }
		private void MnEmulator_Click      (object s, RoutedEventArgs e) => GrblEmulator.EmulatorUI.ShowUI("Grbl Emulator v1.1#");
		private void MnWebsite_Click(object s, RoutedEventArgs e)
		{
			try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://lasergrbl.com"){ UseShellExecute=true }); } catch {}
		}
		private void MnUpdate_Click(object s, RoutedEventArgs e) => GitHub.InitUpdate();
		private void MnAbout_Click (object s, RoutedEventArgs e) { }

		void IGrblCoreUI.BeginInvoke(Action action) => Dispatcher.UIThread.Post(action);
		void IGrblCoreUI.ShowError(string message, string title)
		{
			Dispatcher.UIThread.Post(async () =>
			{
				var dlg = new Window { Title = title ?? "Error", Width = 380, Height = 140, CanResize = false };
				var btn = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
				btn.Click += (_, _) => dlg.Close();
				dlg.Content = new StackPanel { Margin = new Thickness(16), Spacing = 12,
					Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, btn } };
				await dlg.ShowDialog(this);
			});
		}
		bool IGrblCoreUI.AskConfirmation(string message, string title)
		{
			var tcs = new TaskCompletionSource<bool>();
			Dispatcher.UIThread.Post(async () =>
			{
				bool result = false;
				var dlg = new Window { Title = title ?? "Confirm", Width = 400, Height = 150, CanResize = false };
				var yes = new Button { Content = "Yes" }; var no = new Button { Content = "No" };
				yes.Click += (_, _) => { result = true;  dlg.Close(); };
				no.Click  += (_, _) => { result = false; dlg.Close(); };
				dlg.Content = new StackPanel { Margin = new Thickness(16), Spacing = 12,
					Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
					new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
						HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Children = { yes, no } } } };
				await dlg.ShowDialog(this);
				tcs.SetResult(result);
			});
			return tcs.Task.GetAwaiter().GetResult();
		}
		string IGrblCoreUI.ShowOpenFileDialog(string lastFile, string filter)
		{
			string r = null; var tcs = new TaskCompletionSource();
			Dispatcher.UIThread.Post(async () => { var f = await new OpenFileDialog{ Title="Open" }.ShowAsync(this); r = f?.FirstOrDefault(); tcs.SetResult(); });
			tcs.Task.GetAwaiter().GetResult(); return r;
		}
		string IGrblCoreUI.ShowSaveGCodeDialog(string ext, string filter)
		{
			string r = null; var tcs = new TaskCompletionSource();
			Dispatcher.UIThread.Post(async () => { r = await new SaveFileDialog{ Title="Save G-code", DefaultExtension=ext }.ShowAsync(this); tcs.SetResult(); });
			tcs.Task.GetAwaiter().GetResult(); return r;
		}
		string IGrblCoreUI.ShowSaveProjectDialog()
		{
			string r = null; var tcs = new TaskCompletionSource();
			Dispatcher.UIThread.Post(async () => { r = await new SaveFileDialog{ Title="Save Project", DefaultExtension="lps" }.ShowAsync(this); tcs.SetResult(); });
			tcs.Task.GetAwaiter().GetResult(); return r;
		}
		void IGrblCoreUI.ShowRasterImport(GrblCore core, string filename, bool append) { }
		void IGrblCoreUI.ShowVectorImport(GrblCore core, string filename, bool append)
		{
			// Phase 6 adds the full SVG mode dialog. For now: use headless SVG import
			// by calling LoadImportedSVG with ColorFilter.All (int value 0).
			Dispatcher.UIThread.Post(() =>
			{
				// Avoid ColorFilter ambiguity (Core + Imaging both define it): use dynamic dispatch
				var method = core.LoadedFile.GetType().GetMethod("LoadImportedSVG");
				var filterType = method?.GetParameters()[3].ParameterType;
				var filterAll  = filterType != null ? System.Enum.ToObject(filterType, 0) : null;
				if (method != null && filterAll != null)
					method.Invoke(core.LoadedFile, new object[]{ filename, append, core, filterAll });
			});
		}
		int    IGrblCoreUI.ShowRunFromPosition(int total, bool homing, out bool homingOut) { homingOut = false; return -1; }
		int    IGrblCoreUI.ShowResumeJob(int ex, int sent, int tgt, object issue, bool homingEn, bool homing, out bool homingOut, bool setwco, out bool setwcoOut, object wco) { homingOut = false; setwcoOut = false; return -1; }
		string IGrblCoreUI.ShowLaserSelector() => null;
		bool   IGrblCoreUI.ShowSafetyCountdown() => true;
	}
}