using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Data;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ToguzKumalakProcessor
{
    public partial class CsvViewerWindow : Window
    {
        private readonly string _highlightName;

        public CsvViewerWindow() : this("") { }

        public CsvViewerWindow(string highlightName)
        {
            InitializeComponent();
            _highlightName = highlightName;
            LoadCsvSheets();
        }

        private void InitializeComponent() => Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

        private void LoadCsvSheets()
        {
            var baseDir = Directory.GetCurrentDirectory();
            var foldersToLoad = new List<string>();
            string errorMessage = "";

            if (!string.IsNullOrWhiteSpace(_highlightName)) {
                string path = Path.Combine(baseDir, _highlightName + "_output");
                if (Directory.Exists(path)) foldersToLoad.Add(path);
                else errorMessage = $"Kein Ordner für '{_highlightName}' gefunden.";
            } else {
                string path = Path.Combine(baseDir, "output");
                if (Directory.Exists(path)) foldersToLoad.Add(path);
                else errorMessage = "Kein 'output'-Ordner gefunden.";
            }

            if (foldersToLoad.Count == 0) {
                var closeBtn = new Button { Content = "Schließen", HorizontalAlignment = HorizontalAlignment.Center };
                closeBtn.Click += (s, e) => this.Close(); // FIX: += statt =
                this.Content = new StackPanel { 
                    VerticalAlignment = VerticalAlignment.Center, Spacing = 10,
                    Children = { new TextBlock { Text = errorMessage, HorizontalAlignment = HorizontalAlignment.Center }, closeBtn }
                };
                return;
            }

            var tabControl = this.FindControl<TabControl>("CsvTabs");
            var tabItems = new List<TabItem>();

            foreach (var folder in foldersToLoad) {
                foreach (var file in Directory.GetFiles(folder, "*.csv").OrderBy(f => f)) {
                    var lines = File.ReadAllLines(file);
                    if (lines.Length < 1) continue;
                    var header = lines[0].Split(',').ToList();
                    var data = lines.Skip(1).Select(l => l.Split(',').ToList()).ToList();

                    var grid = new DataGrid { ItemsSource = data, AutoGenerateColumns = false, GridLinesVisibility = DataGridGridLinesVisibility.All };
                    for (int i = 0; i < header.Count; i++) {
                        int idx = i;
                        if (idx % 3 == 2) { grid.Columns.Add(new DataGridTextColumn { Header = "", Width = new DataGridLength(15) }); continue; }
                        
                        bool isS1 = (idx % 3 == 0);
                        bool hi = !string.IsNullOrEmpty(_highlightName) && header[idx].Equals(_highlightName, StringComparison.OrdinalIgnoreCase);
                        
                        grid.Columns.Add(new DataGridTextColumn {
                            Header = new Border {
                                Background = isS1 ? Brushes.Black : Brushes.LightGray, Padding = new Thickness(8,4),
                                Child = new TextBlock { 
                                    Text = header[idx], Foreground = isS1 ? Brushes.White : Brushes.Black,
                                    FontWeight = hi ? FontWeight.Bold : FontWeight.Normal, FontStyle = hi ? FontStyle.Italic : FontStyle.Normal
                                }
                            },
                            Binding = new Binding($"[{idx}]"), Width = new DataGridLength(110)
                        });
                    }
                    tabItems.Add(new TabItem { Header = Path.GetFileName(file), Content = new ScrollViewer { Content = grid, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible } });
                }
            }
            if (tabControl != null) tabControl.ItemsSource = tabItems;
        }
    }
}