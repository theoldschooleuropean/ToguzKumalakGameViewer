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

        public CsvViewerWindow()
        {
            InitializeComponent();
            LoadCsvSheets();
        }

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
            string folder = !string.IsNullOrWhiteSpace(_highlightName) ? Path.Combine(baseDir, _highlightName + "_output") : Path.Combine(baseDir, "output");
            if (!Directory.Exists(folder)) return;

            var tabControl = this.FindControl<TabControl>("CsvTabs");
            var tabItems = new List<TabItem>();

            foreach (var file in Directory.GetFiles(folder, "*.csv").OrderBy(f => f)) {
                var lines = File.ReadAllLines(file);
                if (lines.Length < 2) continue;
                
                var headerParts = lines[0].Split(',').ToList();
                var data = lines.Skip(1).Select(l => l.Split(',').ToList()).ToList();

                var grid = new DataGrid { ItemsSource = data, AutoGenerateColumns = false, GridLinesVisibility = DataGridGridLinesVisibility.All, CanUserSortColumns = false };

                for (int i = 0; i < headerParts.Count; i++) {
                    int idx = i;
                    if (idx % 3 == 2) { 
                        grid.Columns.Add(new DataGridTextColumn { Header = "", Width = new DataGridLength(15), CanUserSort = false }); 
                        continue; 
                    }
                    
                    bool isWhite = (idx % 3 == 0);
                    string[] cellParts = headerParts[idx].Split('|');
                    string name = cellParts[0];
                    string result = cellParts.Length > 1 ? cellParts[1] : "";

                    // Farblogik (Dezente Töne)
                    IBrush headerBrush = isWhite ? new SolidColorBrush(Color.Parse("#455A64")) : new SolidColorBrush(Color.Parse("#B0BEC5"));
                    
                    if (result == "1-0") headerBrush = isWhite ? new SolidColorBrush(Color.Parse("#2E7D32")) : new SolidColorBrush(Color.Parse("#C62828")); // Weiß Sieg (Grün) : Schwarz Niederlage (Rot)
                    else if (result == "0-1") headerBrush = isWhite ? new SolidColorBrush(Color.Parse("#C62828")) : new SolidColorBrush(Color.Parse("#2E7D32")); // Weiß Niederlage : Schwarz Sieg
                    else if (result == "1/2-1/2") headerBrush = new SolidColorBrush(Color.Parse("#78909C")); // Remis (Blau-Grau)

                    bool hi = !string.IsNullOrEmpty(_highlightName) && name.Equals(_highlightName, StringComparison.OrdinalIgnoreCase);

                    grid.Columns.Add(new DataGridTextColumn {
                        Header = new Border {
                            Background = headerBrush,
                            Padding = new Thickness(8, 4),
                            Child = new TextBlock { Text = name, Foreground = Brushes.White, FontWeight = hi ? FontWeight.Bold : FontWeight.Normal }
                        },
                        Binding = new Binding($"[{idx}]"),
                        Width = new DataGridLength(130),
                        CanUserSort = false
                    });
                }
                
                grid.LoadingRow += (s, e) => {
                    if (e.Row.Index == 0) { 
                        e.Row.Background = new SolidColorBrush(Color.Parse("#D6EAF8")); 
                        e.Row.Foreground = new SolidColorBrush(Color.Parse("#1A5276")); 
                        e.Row.FontWeight = FontWeight.Bold;
                    }
                };

                tabItems.Add(new TabItem { Header = Path.GetFileName(file), Content = new ScrollViewer { Content = grid, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible } });
            }
            if (tabControl != null) tabControl.ItemsSource = tabItems;
        }
    }
}