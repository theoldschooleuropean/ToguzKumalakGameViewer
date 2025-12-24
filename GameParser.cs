using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ToguzKumalakProcessor
{
    public class Game
    {
        public string White { get; set; } = "";
        public string Black { get; set; } = "";
        public string Date { get; set; } = "";
        public string Time { get; set; } = "";
        public List<string> Sp1 { get; } = new();
        public List<string> Sp2 { get; } = new();
    }

    public static class GameParser
    {
        public static List<Game> ParseGames(string filePath, string filterName)
        {
            if (!File.Exists(filePath)) return new List<Game>();
            var lines = File.ReadAllLines(filePath);
            var games = new List<Game>();
            Game? current = null;

            foreach (var line in lines.Select(l => l.Trim()))
            {
                if (line.StartsWith("[White \"")) { current = new Game { White = line.Split('\"')[1] }; games.Add(current); }
                else if (line.StartsWith("[Black \"") && current != null) current.Black = line.Split('\"')[1];
                else if (line.StartsWith("[Date \"") && current != null) current.Date = line.Split('\"')[1];
                else if (line.StartsWith("[Time \"") && current != null) current.Time = line.Split('\"')[1];
                else if (current != null && (line.StartsWith("1.") || (line.Length > 0 && char.IsDigit(line[0])))) ParseMoves(line, current);
            }

            return games.Where(g => g.Sp1.Count > 0 && (string.IsNullOrEmpty(filterName) || 
                g.White.Equals(filterName, StringComparison.OrdinalIgnoreCase) || 
                g.Black.Equals(filterName, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(g => g.Date).ThenByDescending(g => g.Time).ToList();
        }

        public static int WriteGamesToCsv(List<Game> games, string filterName)
        {
            string targetFolder = string.IsNullOrEmpty(filterName) ? "output" : filterName + "_output";
            Directory.CreateDirectory(targetFolder);
            int newlyAddedCount = 0;

            foreach (var g in games)
            {
                string fileName = Regex.Replace(g.Sp1.Count > 0 ? g.Sp1[0] : "Unbekannt", @"[<>:""/\\|?*]", "_") + ".csv";
                string path = Path.Combine(targetFolder, fileName);
                var table = File.Exists(path) ? File.ReadAllLines(path).Select(l => l.Split(',').ToList()).ToList() : new List<List<string>>();

                if (IsDuplicate(table, g)) continue;
                newlyAddedCount++;

                if (table.Count == 0) table.Add(new List<string>());
                table[0].AddRange(new[] { g.White, g.Black, "" });

                if (table.Count < 2) table.Add(Enumerable.Repeat("", table[0].Count - 3).ToList());
                table[1].AddRange(new[] { g.Date, g.Time, "" });

                int maxRows = Math.Max(g.Sp1.Count, g.Sp2.Count) + 2; 
                while (table.Count < maxRows) table.Add(Enumerable.Repeat("", table[0].Count - 3).ToList());

                for (int i = 2; i < table.Count; i++) {
                    int mIdx = i - 2;
                    table[i].AddRange(new[] { mIdx < g.Sp1.Count ? g.Sp1[mIdx] : "", mIdx < g.Sp2.Count ? g.Sp2[mIdx] : "", "" });
                }
                File.WriteAllLines(path, table.Select(r => string.Join(",", r)));
            }
            return newlyAddedCount;
        }

        private static void ParseMoves(string line, Game game)
        {
            string cleanLine = Regex.Replace(line, @"\{.*?\}", "");
            var tokens = cleanLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (Regex.IsMatch(token, @"^\d+\.$")) continue;
                if (token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*") {
                    if (game.Sp2.Count < game.Sp1.Count && game.Sp1.Count > 0) game.Sp1[game.Sp1.Count - 1] += $" ({token})";
                    else if (game.Sp2.Count > 0) game.Sp2[game.Sp2.Count - 1] += $" ({token})";
                    continue;
                }
                if (game.Sp1.Count == game.Sp2.Count) game.Sp1.Add(token); else game.Sp2.Add(token);
            }
        }

        private static bool IsDuplicate(List<List<string>> table, Game g)
        {
            if (table.Count < 2) return false;
            for (int col = 0; col < table[0].Count; col += 3) {
                if (col + 1 < table[1].Count && table[1][col] == g.Date && table[1][col+1] == g.Time) return true;
            }
            return false;
        }
    }
}