#nullable enable

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ToguzKumalakProcessor
{
    public partial class MainWindow : Window
    {
        private List<string> _inputFilePaths = new List<string>();
        private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.conf");

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            var selectBtn = this.FindControl<Button>("SelectInputFileButton");
            var processBtn = this.FindControl<Button>("ProcessButton");
            var viewBtn = this.FindControl<Button>("ViewCsvButton");

            if (selectBtn != null) selectBtn.Click += SelectFiles_Click;
            if (processBtn != null) processBtn.Click += Process_Click;
            if (viewBtn != null) viewBtn.Click += (s, e) => 
            {
                var isChecked = this.FindControl<CheckBox>("FilterNameCheckbox")?.IsChecked ?? false;
                var pName = this.FindControl<TextBox>("PlayerNameTextBox")?.Text ?? "";
                new CsvViewerWindow(isChecked ? pName : "").Show();
            };
        }

        private void LoadSettings()
        {
            if (!File.Exists(_settingsPath)) return;
            try {
                var lines = File.ReadAllLines(_settingsPath);
                if (lines.Length >= 2) {
                    var cb = this.FindControl<CheckBox>("FilterNameCheckbox");
                    var tb = this.FindControl<TextBox>("PlayerNameTextBox");
                    if (cb != null) cb.IsChecked = lines[0] == "True";
                    if (tb != null) tb.Text = lines[1];
                }
            } catch { }
        }

        private void SaveSettings()
        {
            var isChecked = this.FindControl<CheckBox>("FilterNameCheckbox")?.IsChecked ?? false;
            var pName = this.FindControl<TextBox>("PlayerNameTextBox")?.Text ?? "";
            try { File.WriteAllLines(_settingsPath, new[] { isChecked.ToString(), pName }); } catch { }
        }

        private async void SelectFiles_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions { Title = "TXT wählen", AllowMultiple = true });
            
            if (files.Any()) {
                _inputFilePaths = files.Select(f => f.Path.LocalPath).ToList();
                var label = this.FindControl<TextBlock>("InputFileLabel");
                if (label != null) label.Text = $"{_inputFilePaths.Count} Dateien gewählt.";
                var procBtn = this.FindControl<Button>("ProcessButton");
                if (procBtn != null) procBtn.IsEnabled = true;
            }
        }

        private void Process_Click(object? sender, RoutedEventArgs e)
        {
            SaveSettings();
            var isChecked = this.FindControl<CheckBox>("FilterNameCheckbox")?.IsChecked ?? false;
            var pName = this.FindControl<TextBox>("PlayerNameTextBox")?.Text?.Trim() ?? "";
            var statusLabel = this.FindControl<TextBlock>("StatusLabel");

            try {
                int total = 0, added = 0;
                foreach (var path in _inputFilePaths) {
                    var games = GameParser.ParseGames(path, isChecked ? pName : "");
                    total += games.Count;
                    added += GameParser.WriteGamesToCsv(games, isChecked ? pName : "");
                    if (File.Exists(path)) File.Delete(path);
                }

                if (statusLabel != null) {
                    statusLabel.Text = (total == 0) ? "Nichts gefunden." : (added == 0) ? "Alles Duplikate." : $"Erfolg! {added} neue Spiele.";
                    statusLabel.Foreground = (added > 0) ? Brushes.Green : Brushes.Orange;
                }
                
                _inputFilePaths.Clear();
                var procBtn = this.FindControl<Button>("ProcessButton");
                if (procBtn != null) procBtn.IsEnabled = false;
            } catch (Exception ex) { if (statusLabel != null) statusLabel.Text = "Fehler: " + ex.Message; }
        }
    }
}