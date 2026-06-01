using System.Linq;
// GRBL Configuration dialog. Ports GrblConfig.cs.
// Shows the GRBL $-parameters with descriptions from the CSV code tables.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LaserGRBL;

namespace LaserGRBL.Dialogs
{
	/// <summary>A single GRBL configuration row.</summary>
	public class GrblConfigRow
	{
		public string Key         { get; set; }
		public string Value       { get; set; }
		public string Description { get; set; }
	}

	public partial class GrblConfigDialog : Window
	{
		private readonly GrblCore _core;
		private readonly ObservableCollection<GrblConfigRow> _rows = new();

		public GrblConfigDialog(GrblCore core)
		{
			_core = core;
			InitializeComponent();
			Opened += OnOpened;
		}

		private void OnOpened(object sender, EventArgs e)
		{
			ConfigGrid.ItemsSource = _rows;
			PopulateFromConfiguration();
		}

		private void PopulateFromConfiguration()
		{
			_rows.Clear();
			var conf = GrblCore.Configuration;
			if (conf == null) return;

			foreach (var kv in conf)
			{
				string key  = $"${kv.Key}";
				string desc = CSVD.Settings.GetItem(kv.Key.ToString(), 0) ?? "";
				string unit = CSVD.Settings.GetItem(kv.Key.ToString(), 1) ?? "";
				if (!string.IsNullOrEmpty(unit)) desc = $"{desc} [{unit}]";

				_rows.Add(new GrblConfigRow { Key = key, Value = kv.Value, Description = desc });
			}
		}

		private void BtnWrite_Click(object sender, RoutedEventArgs e)
		{
			// Collect changed values and send to machine
			var toWrite = new List<GrblCommand>();
			foreach (var row in _rows)
			{
				if (row.Key?.StartsWith("$") == true)
					toWrite.Add(new GrblCommand($"{row.Key}={row.Value}"));
			}
			foreach (var cmd in toWrite)
				_core?.EnqueueCommand(cmd);
		}

		private async void BtnExport_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new SaveFileDialog { Title = "Export GRBL Config",
				DefaultExtension = "csv",
				Filters = new() { new() { Name = "CSV", Extensions = new[]{"csv"}.ToList() } } };
			string path = await dlg.ShowAsync(this);
			if (string.IsNullOrEmpty(path)) return;

			var lines = new System.Text.StringBuilder();
			foreach (var row in _rows)
				lines.AppendLine($"{row.Key},{row.Value},{row.Description}");
			System.IO.File.WriteAllText(path, lines.ToString());
		}

		private async void BtnImport_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog { Title = "Import GRBL Config",
				Filters = new() { new() { Name = "CSV", Extensions = new[]{"csv"}.ToList() } } };
			var files = await dlg.ShowAsync(this);
			if (files?.Length == 0) return;
			// TODO: parse CSV and populate rows (Phase 7)
		}

		private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
	}
}
