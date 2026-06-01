using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace LaserGRBL.Dialogs
{
	public partial class SafetyCountdownDialog : Window
	{
		public bool CanGo { get; private set; } = false;
		private int _countdown = 3;
		private DispatcherTimer _timer;

		public SafetyCountdownDialog()
		{
			InitializeComponent();
			Opened += OnOpened;
		}

		private void OnOpened(object sender, EventArgs e)
		{
			int countdownSec = Settings.GetObject("SafetyCountdownSeconds", 3);
			_countdown = countdownSec;
			if (_countdown <= 0) { CanGo = true; Close(); return; }

			TxtCountdown.Text = $"Starting in {_countdown}...";
			_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			_timer.Tick += (_, _) =>
			{
				_countdown--;
				if (_countdown <= 0)
				{
					_timer.Stop();
					TxtCountdown.Text = "Ready!";
					BtnGo.Content = "GO NOW";
					BtnGo.IsEnabled = true;
				}
				else
				{
					TxtCountdown.Text = $"Starting in {_countdown}...";
				}
			};
			_timer.Start();
		}

		private void BtnGo_Click    (object s, RoutedEventArgs e) { CanGo = true;  _timer?.Stop(); Close(); }
		private void BtnCancel_Click(object s, RoutedEventArgs e) { CanGo = false; _timer?.Stop(); Close(); }
	}
}
