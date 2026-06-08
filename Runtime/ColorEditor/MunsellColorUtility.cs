using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// Utility helpers for working with Munsell color table data.
    /// 提供: フラット化キャッシュ構築 / 最近傍色検索 / HueスライダーIndex計算 / Hex直接参照。
    /// </summary>
    public static class MunsellColorUtility
    {
        /// <summary>Hue value steps inside each hue series.</summary>
        public static readonly string[] HueValueSteps = { "2.5", "5", "7.5", "10" };
        /// <summary>Hue name series order.</summary>
        public static readonly string[] HueNames = { "R", "YR", "Y", "GY", "G", "BG", "B", "PB", "P", "RP" };

        private static readonly object _lock = new();
        private static List<MunsellEntry> s_entries; // lazy cache
        private static Dictionary<string, MunsellEntry> s_hexLookup; // hex(no '#')->entry

        /// <summary>
        /// Returns immutable cached list of all Munsell entries (lazy built on first access).
        /// </summary>
        public static IReadOnlyList<MunsellEntry> Entries
        {
            get
            {
                if (s_entries != null) return s_entries;
                lock (_lock)
                {
                    if (s_entries == null)
                        BuildEntriesInternal();
                }
                return s_entries!;
            }
        }

        /// <summary>
        /// Try get exact entry by 6-digit hex (with or without leading '#').
        /// </summary>
        public static bool TryGetExact(string hex, out MunsellEntry entry)
        {
            if (string.IsNullOrEmpty(hex))
            {
                entry = default;
                return false;
            }
            hex = NormalizeHex(hex);
            _ = Entries; // ensure built
            return s_hexLookup!.TryGetValue(hex, out entry);
        }

        /// <summary>
        /// Find nearest MunsellEntry by squared RGB distance in sRGB space.
        /// Returns null if no entries exist.
        /// NOTE: For small dataset O(N) linear scan is sufficient.
        /// </summary>
        public static MunsellEntry? FindNearest(Color target)
        {
            var list = Entries;
            if (list.Count == 0) return null;
            float tr = target.r, tg = target.g, tb = target.b;
            float best = float.MaxValue;
            MunsellEntry bestEntry = default;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                var c = e.Color;
                float dr = tr - c.r;
                float dg = tg - c.g;
                float db = tb - c.b;
                float d = dr * dr + dg * dg + db * db;
                if (d < best)
                {
                    best = d;
                    bestEntry = e;
                    if (best == 0f) break; // exact match
                }
            }
            return bestEntry;
        }

        /// <summary>
        /// Compute hue slider index (0-39) from entry.
        /// (HueNameIndex * 4 + HueValueIndex)
        /// </summary>
        public static int ComputeHueSliderIndex(in MunsellEntry entry)
        {
            int hueNameIndex = Array.IndexOf(HueNames, entry.HueName);
            int hueValueIndex = Array.IndexOf(HueValueSteps, entry.HueValue);
            if (hueNameIndex < 0 || hueValueIndex < 0) return 0; // fallback
            return hueNameIndex * 4 + hueValueIndex;
        }

        private static void BuildEntriesInternal()
        {
            var data = new MunsellData();
            s_entries = new List<MunsellEntry>(600);
            s_hexLookup = new Dictionary<string, MunsellEntry>(600);

            // Table order matches HueNames order.
            var hueSeriesTables = new List<List<List<string>>[]>
            {
                new[]{ data.codemap25R, data.codemap5R, data.codemap75R, data.codemap10R },
                new[]{ data.codemap25YR, data.codemap5YR, data.codemap75YR, data.codemap10YR },
                new[]{ data.codemap25Y, data.codemap5Y, data.codemap75Y, data.codemap10Y },
                new[]{ data.codemap25GY, data.codemap5GY, data.codemap75GY, data.codemap10GY },
                new[]{ data.codemap25G, data.codemap5G, data.codemap75G, data.codemap10G },
                new[]{ data.codemap25BG, data.codemap5BG, data.codemap75BG, data.codemap10BG },
                new[]{ data.codemap25B, data.codemap5B, data.codemap75B, data.codemap10B },
                new[]{ data.codemap25PB, data.codemap5PB, data.codemap75PB, data.codemap10PB },
                new[]{ data.codemap25P, data.codemap5P, data.codemap75P, data.codemap10P },
                new[]{ data.codemap25RP, data.codemap5RP, data.codemap75RP, data.codemap10RP },
            };

            for (int hueNameIdx = 0; hueNameIdx < hueSeriesTables.Count; hueNameIdx++)
            {
                var hueName = HueNames[hueNameIdx];
                var hueValueArray = hueSeriesTables[hueNameIdx]; // 4 groups correspond to HueValueSteps
                for (int hvIdx = 0; hvIdx < hueValueArray.Length; hvIdx++)
                {
                    string hueValue = HueValueSteps[hvIdx];
                    var valueRows = hueValueArray[hvIdx]; // rows: Value descending 9..1
                    int value = 9;
                    foreach (var row in valueRows)
                    {
                        int chroma = 0;
                        foreach (var hex in row)
                        {
                            chroma += 2;
                            if (!ColorUtility.TryParseHtmlString("#" + hex, out var c))
                                continue; // skip invalid
                            var entry = new MunsellEntry(hueValue, hueName, value, chroma, hex, c);
                            s_entries.Add(entry);
                            s_hexLookup![hex] = entry;
                        }
                        value--;
                    }
                }
            }

            // 無彩色
            int valueN = 9;
            foreach (var row in data.codemapN)
            {
                foreach (var hex in row)
                {
                    if (!ColorUtility.TryParseHtmlString("#" + hex, out var c))
                        continue; // skip invalid
                    var entry = new MunsellEntry("", "N", valueN, 0, hex, c);
                    s_entries.Add(entry);
                    s_hexLookup![hex] = entry;
                }
                valueN--;
            }
        }

        private static string NormalizeHex(string hex)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            return hex.ToUpperInvariant();
        }
    }

    /// <summary>
    /// Immutable Munsell color entry.
    /// </summary>
    public readonly struct MunsellEntry
    {
        public string HueValue { get; }
        public string HueName { get; }
        public int Value { get; }
        public int Chroma { get; }
        public string Hex { get; }
        public Color Color { get; }

        public MunsellEntry(string hueValue, string hueName, int value, int chroma, string hex, Color color)
        {
            HueValue = hueValue;
            HueName = hueName;
            Value = value;
            Chroma = chroma;
            Hex = hex;
            Color = color;
        }

        public override string ToString() => $"{HueValue}{HueName} {Value}/{Chroma} #{Hex}";
    }
}
