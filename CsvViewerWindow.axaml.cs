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
            string folder = !string.IsNullOrWhiteSpace(_highlightName) 
                ? Path.Combine(baseDir, _highlightName + "_output") 
                : Path.Combine(baseDir, "output");

            if (!Directory.Exists(folder)) return;

            var tabControl = this.FindControl<TabControl>("CsvTabs");
            var tabItems = new List<TabItem>();

            foreach (var file in Directory.GetFiles(folder, "*.csv").OrderBy(f => f)) {
                var lines = File.ReadAllLines(file);
                if (lines.Length < 2) continue;
                
                var header = lines[0].Split(',').ToList();
                var data = lines.Skip(1).Select(l => l.Split(',').ToList()).ToList();

                var grid = new DataGrid { 
                    ItemsSource = data, 
                    AutoGenerateColumns = false, 
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    CanUserSortColumns = false // Sortierung für das gesamte Grid deaktivieren
                };

                for (int i = 0; i < header.Count; i++) {
                    int idx = i;
                    if (idx % 3 == 2) { 
                        grid.Columns.Add(new DataGridTextColumn { Header = "", Width = new DataGridLength(15), CanUserSort = false }); 
                        continue; 
                    }
                    
                    bool isWhite = (idx % 3 == 0);
                    string name = header[idx];
                    bool hi = !string.IsNullOrEmpty(_highlightName) && name.Equals(_highlightName, StringComparison.OrdinalIgnoreCase);

                    grid.Columns.Add(new DataGridTextColumn {
                        Header = new Border {
                            Background = isWhite ? new SolidColorBrush(Color.Parse("#2C3E50")) : new SolidColorBrush(Color.Parse("#BDC3C7")),
                            Padding = new Thickness(8, 4),
                            Child = new TextBlock { 
                                Text = name, 
                                Foreground = isWhite ? Brushes.White : Brushes.Black, 
                                FontWeight = hi ? FontWeight.Bold : FontWeight.Normal 
                            }
                        },
                        Binding = new Binding($"[{idx}]"),
                        Width = new DataGridLength(130),
                        CanUserSort = false // Sortierung für diese Spalte explizit deaktivieren
                    });
                }
                
                // Styling für die Datumszeile
                grid.LoadingRow += (s, e) => {
                    if (e.Row.GetIndex() == 0) { 
                        e.Row.Background = new SolidColorBrush(Color.Parse("#D6EAF8")); // Helles Blau-Grau
                        e.Row.Foreground = new SolidColorBrush(Color.Parse("#1A5276")); // Schönes dunkles Blau
                        e.Row.FontWeight = FontWeight.Bold;
                    }
                };

                tabItems.Add(new TabItem { 
                    Header = Path.GetFileName(file), 
                    Content = new ScrollViewer { 
                        Content = grid, 
                        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible 
                    } 
                });
            }
            if (tabControl != null) tabControl.ItemsSource = tabItems;
        }
    }
}