using JapaneseNumeral;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

namespace NormalizeJapaneseAddresses.Lib
{
    public class PrefectureList : Dictionary<string, List<string>> { }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Jsonをデシリアライズするため、Jsonのデータと同じようにプロパティ名は小文字から始める。</remarks>

    [System.Serializable]
    public class SingleTown
    {
        public string? town { get; set; }
        public string? originalTown { get; set; }
        public string? koaza { get; set; }
        public string? gaiku { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }
    }

    [System.Serializable]
    public class TownList : List<SingleTown> { }


    public static class CacheRegexes
    {
        private static Dictionary<string, List<(SingleTown, string)>> cachedTownRegexes = new();
        private static Dictionary<string, string>? cachedPrefecturePatterns;
        private static Dictionary<string, Dictionary<string, string>> cachedCityPatterns = new();
        private static PrefectureList? cachedPrefectures;
        private static Dictionary<string, TownList> cachedTowns = new();
        private static Dictionary<string, string>? cachedSameNamedPrefectureCityRegexPatterns;

        public static PrefectureList GetPrefectures(List<AddressData> addressDatas)
        {
            if (cachedPrefectures is not null)
            {
                return cachedPrefectures;
            }

            if (addressDatas.Count > 0)
            {
                for(int i=0;i< addressDatas.Count; i++)
                {
                    var addressData = addressDatas[i];
                    // 県名と都市名を取得する
                    var pref = addressData.prefecture;
                    var city = addressData.city;

                    if (!cachedPrefectures?.ContainsKey(pref) ?? true)
                    {
                        cachedPrefectures ??= new PrefectureList();
                        cachedPrefectures[pref] = new List<string>();
                    }
                    if (!cachedPrefectures[pref].Any(c => c == city))
                    {
                        cachedPrefectures[pref].Add(city);
                    }
                }
            }
            return cachedPrefectures;
        }

        public static PrefectureList CachePrefectures(PrefectureList data)
        {
            cachedPrefectures = data;
            return cachedPrefectures;
        }

        public static Dictionary<string, string> GetPrefectureRegexPatterns(List<string> prefs)
        {
            if (cachedPrefecturePatterns is not null && cachedPrefecturePatterns.Any())
            {
                return cachedPrefecturePatterns;
            }
            cachedPrefecturePatterns = prefs.ToDictionary(
                pref => pref,
                pref =>
                {
                    var r = new Regex("(都|道|府|県)$");
                    var _pref = r.Replace(pref, "", 1);
                    return $"^{_pref}(都|道|府|県)?";
                }
                );
            return cachedPrefecturePatterns;
        }

        public static Dictionary<string, string> GetCityRegexPatterns(string pref, List<string> cities)
        {
            if (cachedCityPatterns.ContainsKey(pref)) return cachedCityPatterns[pref];

            // 少ない文字数の地名に対してミスマッチしないように文字の長さ順にソート
            cities.Sort((a, b) => b.Length - a.Length);

            Dictionary<string, string> patterns = cities.ToDictionary(city => city, city =>
            {
                string pattern = $"^{Dicts.ToRegexPattern(city)}";
                if (Regex.IsMatch(city, "(町|村)$"))
                {
                    var r = new Regex("(.+?)郡");
                    var temp = r.Replace(Dicts.ToRegexPattern(city), "($1郡)?", 1);
                    pattern = $"^{temp}"; // 郡が省略されてるかも
                }
                return pattern;
            });

            cachedCityPatterns[pref] = patterns;
            return patterns;
        }

        public static async Task<TownList?> GetTowns(string pref, string city, List<AddressData> datas)
        {
            if (cachedTowns.ContainsKey($"{pref}-{city}"))
            {
                return cachedTowns[$"{pref}-{city}"];
            }

            for (int i=0;i< datas.Count; i++)
            {
                var data = datas[i];
                var _pref = data.prefecture;
                var _city = data.city;
                if (_pref == pref && _city == city)
                {
                    TownList towns = new();
                    towns.Capacity = data.towns.Count;
                    foreach (var line in data.towns)
                    {
                        towns.Add(new SingleTown
                        {
                            town = line.ooaza,
                            koaza = line.koaza,
                            gaiku = "",
                            lat = line.gaikus.Count > 0 ? line.gaikus[0].lat : null,
                            lng = line.gaikus.Count > 0 ? line.gaikus[0].lon : null
                        });
                    }

                    cachedTowns[$"{pref}-{city}"] = towns;
                    return towns;
                }
            }

            return null;
        }

        /// <summary>
        /// 十六町 のように漢数字と町が連結しているか
        /// </summary>
        /// <param name="targetTownName"></param>
        /// <returns></returns>
        public static bool IsKanjiNumberFollowedByCho(string targetTownName)
        {
            var xCho = Regex.Matches(targetTownName, ".町");
            if (xCho.Count == 0)
            {
                return false;
            }
            var kanjiNumbers = JapaneseNumeral.JapaneseNumeral.FindKanjiNumbers(xCho[0].Value);
            return kanjiNumbers.Count > 0;
        }

        public static async Task<List<(SingleTown, string)>> GetTownRegexPatterns(string pref, string city, List<AddressData> datas)
        {
            if (cachedTownRegexes.ContainsKey($"{pref}-{city}"))
            {
                return cachedTownRegexes[$"{pref}-{city}"];
            }

            var preTowns = await GetTowns(pref, city, datas);
            var townSet = new HashSet<string>(preTowns.Select(town => town.town));
            var towns = new List<SingleTown>();

            var isKyoto = Regex.IsMatch(city, @"^京都市");

            // 町丁目に「○○町」が含まれるケースへの対応
            // 通常は「○○町」のうち「町」の省略を許容し同義語として扱うが、まれに自治体内に「○○町」と「○○」が共存しているケースがある。
            // この場合は町の省略は許容せず、入力された住所は書き分けられているものとして正規化を行う。
            // 更に、「愛知県名古屋市瑞穂区十六町1丁目」漢数字を含むケースだと丁目や番地・号の正規化が不可能になる。このようなケースも除外。
            foreach (var town in preTowns)
            {
                towns.Add(town);
                var originalTown = town.town;
                if (originalTown.IndexOf("町") == -1) continue;
                var townAbbr = Regex.Replace(originalTown, @"(?!^町)町", "");
                if (!isKyoto && // 京都は通り名削除の処理があるため、意図しないマッチになるケースがある。これを除く
                    !townSet.Contains(townAbbr) &&
                    !townSet.Contains($"大字{townAbbr}") && // 大字は省略されるため、大字〇〇と〇〇町がコンフリクトする。このケースを除外
                    !IsKanjiNumberFollowedByCho(originalTown))
                {
                    // エイリアスとして町なしのパターンを登録
                    towns.Add(new SingleTown
                    {//例：東京都江戸川区西小松川12-345　→　originalTown 西小松川町, town 西小松川
                        koaza = town.koaza,
                        gaiku = town.gaiku,
                        lat = town.lat,
                        lng = town.lng,
                        originalTown = originalTown,
                        town = townAbbr
                    });
                }
            }
            // 少ない文字数の地名に対してミスマッチしないように文字の長さ順にソート
            //オリジナルのTypeScriptのコードでは、townsをSort(a,b)しても、同じtown.lengthなら元の順序が保持されるようだ（安定ソート）
            //そのため、C#では、OrderByを利用する。https://stackoverflow.com/a/12402519/9924249
            var comparer = new TownsComparer();
            towns = towns.OrderBy(x => x, comparer).ToList();
            // 次の方法は、安定ソートにならない。ListにSortをしても安定ソートにならないため。
            //towns.Sort((a, b) =>
            //  {
            //      var aLen = a.town.Length;
            //      var bLen = b.town.Length;
            //      // 大字で始まる場合、優先度を低く設定する。
            //      // 大字XX と XXYY が存在するケースもあるので、 XXYY を先にマッチしたい
            //      if (a.town.StartsWith("大字")) aLen -= 2;
            //      if (b.town.StartsWith("大字")) bLen -= 2;
            //      return bLen - aLen;
            //  });

            //https://stackoverflow.com/questions/31326451/replacing-regex-matches-using-lambda-expression

            List<(SingleTown, string)> patterns = new();
            foreach (var town in towns)
            {
                // 横棒を含む場合（流通センター、など）に対応
                var output1 = Regex.Replace(town.town, "[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]");
                var output2 = Regex.Replace(output1, "大?字", "(大?字)?");
                // 以下住所マスターの町丁目に含まれる数字を正規表現に変換する
                var output4 = Regex.Replace(output2, "([壱一二三四五六七八九十]+)(丁目?|番(町|丁)|条|軒|線|(の|ノ)町|地割|号)", MatchEvaluator);

                patterns.Add((town, Dicts.ToRegexPattern(output4)));
            }

            // X丁目の丁目なしの数字だけ許容するため、最後に数字だけ追加していく
            foreach (SingleTown town in towns)
            {
                Match chomeMatch = Regex.Match(town.town, "([^一二三四五六七八九十]+)([一二三四五六七八九十]+)(丁目?)");
                if (!chomeMatch.Success)
                {
                    continue;
                }
                string chomeNamePart = chomeMatch.Groups[1].Value;
                string chomeNum = chomeMatch.Groups[2].Value;
                string pattern = Dicts.ToRegexPattern($"^{chomeNamePart}({chomeNum}|{Utils.Kan2Num(chomeNum)})");
                patterns.Add((town, pattern));
            }

            cachedTownRegexes[$"{pref}-{city}"] = patterns;

            return patterns;
        }

        public static async Task<List<(SingleTown, string)>> GetKoazaRegexPatterns(string pref, string city, string town, List<AddressData> datas)
        {
            var preTowns = await GetTowns(pref, city, datas);
            if (preTowns is null) return new List<(SingleTown, string)>();

            var koazas = preTowns.Where(t => t.town == town && !string.IsNullOrEmpty(t.koaza)).ToList();
            if( koazas.Count == 0) 
                return new List<(SingleTown, string)>();

            // 少ない文字数の地名に対してミスマッチしないように文字の長さ順にソート
            koazas.Sort((a, b) => b.koaza!.Length - a.koaza!.Length);

            List<(SingleTown, string)> patterns = new();
            foreach (var koaza in koazas)
            {
                var output1 = Regex.Replace(koaza.koaza, "[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]");
                var output4 = Regex.Replace(output1, "([壱一二三四五六七八九十]+)(丁目?|番(町|丁)|条|軒|線|(の|ノ)町|地割|号)", MatchEvaluator);
                patterns.Add((koaza, Dicts.ToRegexPattern(output4)));
            }
            return patterns;
        }

        public static async Task<List<(SingleTown, string)>> GetGaikuRegexPatterns(string pref, string city, string town, string koaza, List<AddressData> datas)
        {
            List<(SingleTown, string)> patterns = new();
            for(int i=0;i<datas.Count; i++)
            {
                var data = datas[i];
                var _pref = data.prefecture;
                var _city = data.city;
                if (_pref == pref && _city == city)
                {
                    var gaikus = data.towns
                        .Where(t => t.ooaza == town && t.koaza == koaza && t.gaikus.Count > 0)
                        .SelectMany(t => t.gaikus)
                        .ToList();
                    if( gaikus.Count == 0)
                        return patterns;

                    // 少ない文字数の地名に対してミスマッチしないように文字の長さ順にソート
                    gaikus.Sort((a, b) => b.gaiku!.Length - a.gaiku!.Length);
                    foreach (var gaiku in gaikus)
                    {
                        //var output1 = Regex.Replace(gaiku.gaiku, "[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]");
                        //var output4 = Regex.Replace(output1, "([壱一二三四五六七八九十]+)(丁目?|番(町|丁)|条|軒|線|(の|ノ)町|地割|号)", MatchEvaluator);
                        patterns.Add((new SingleTown
                        {
                            town = town,
                            koaza = koaza,
                            gaiku = gaiku.gaiku,
                            lat = gaiku.lat,
                            lng = gaiku.lon
                        }, gaiku.gaiku));
                    }
                    return patterns;
                }
            }

            return patterns;
        }

        public static async Task<List<string>> GetKoazas(string pref, string city, string town, List<AddressData> datas)
        {
            var preTowns = await GetTowns(pref, city, datas);
            if (preTowns is null) return new List<string>();
            var koazas = preTowns.Where(t => t.town == town && !string.IsNullOrEmpty(t.koaza)).Select(t => t.koaza!).ToList();
            return koazas;
        }

        public static List<string> GetGaikus(string pref, string city, string town, string koaza, List<AddressData> datas)
        {
            List<string> gaikus = new();
            for (int i = 0; i < datas.Count; i++)
            {
                var data = datas[i];
                var _pref = data.prefecture;
                var _city = data.city;
                if (_pref == pref && _city == city)
                {
                    gaikus = data.towns
                        .Where(t => t.ooaza == town && t.koaza == koaza && t.gaikus.Count > 0)
                        .SelectMany(t => t.gaikus)
                        .Select(g => g.gaiku!)
                        .ToList();
                    return gaikus;
                }
            }
            return gaikus;
        }

        public static string MatchEvaluator(Match m)
        {
            //int index = m.Index; // 発見した文字列の開始位置
            string value = m.Value; // 発見した文字列
            var patterns3 = new List<string>();
            var r = new Regex("(丁目?|番(町|丁)|条|軒|線|(の|ノ)町|地割|号)"); //globalではないので、1回のみ置換
            patterns3.Add(r.Replace(value, "", 1));
            // 漢数字
            if (Regex.IsMatch(value, "^壱"))
            {
                patterns3.Add("一");
                patterns3.Add("1");
                patterns3.Add("１");
            }
            else
            {
                string num1 = Regex.Replace(value, "([一二三四五六七八九十]+)", match =>
                (
                   Utils.Kan2Num(match.Value.ToString())
                ));
                var r2 = new Regex("(丁目?|番(町|丁)|条|軒|線|(の|ノ)町|地割|号)");
                var num2 = r2.Replace(num1, "", 1);
                patterns3.Add(num2); // 半角アラビア数字
            }
            string _pattern = $"({string.Join("|", patterns3)})((丁|町)目?|番(町|丁)|条|軒|線|の町?|地割|号|[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━])";
            return _pattern;
        }

        public static Dictionary<string, string> GetSameNamedPrefectureCityRegexPatterns(List<string> prefs, Dictionary<string, List<string>> prefList)
        {
            if (cachedSameNamedPrefectureCityRegexPatterns is not null && cachedSameNamedPrefectureCityRegexPatterns.Any())
            {
                return cachedSameNamedPrefectureCityRegexPatterns;
            }

            List<string> _prefs = prefs.ConvertAll(pref =>
            {
                var r = new Regex("[都|道|府|県]$");
                return r.Replace(pref, "", 1);
            });

            cachedSameNamedPrefectureCityRegexPatterns = new();
            foreach (var pref in prefList)
            {
                foreach (var city in pref.Value)
                {
                    // 「福島県石川郡石川町」のように、市の名前が別の都道府県名から始まっているケースも考慮する。
                    for (int j = 0; j < _prefs.Count; j++)
                    {
                        if (city.IndexOf(_prefs[j]) == 0)
                        {
                            cachedSameNamedPrefectureCityRegexPatterns.Add($"{pref.Key}{city}", $"^{city}");
                        }
                    }
                }
            }

            return cachedSameNamedPrefectureCityRegexPatterns;
        }

        public static (double?, double?) GetPrefLatLon(string pref, List<AddressData> datas)
        {
            for(int i=0;i< datas.Count; i++)
            {
                var data = datas[i];
                var _pref = data.prefecture;
                if (_pref == pref)
                {
                    if (data.towns.Count < 1)
                        continue;

                    return (data.towns[0].gaikus.Count > 0 ? data.towns[0].gaikus[0].lat : null,
                            data.towns[0].gaikus.Count > 0 ? data.towns[0].gaikus[0].lon : null);
                }
            }
            return (null, null);
        }
        public static (double?, double?) GetCityLatLon(string pref, string city, List<AddressData> datas)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                var data = datas[i];
                var _pref = data.prefecture;
                var _city = data.city;
                if (_pref == pref && _city == city)
                {
                    if (data.towns.Count < 1)
                        continue;

                    return (data.towns[0].gaikus.Count > 0 ? data.towns[0].gaikus[0].lat : null,
                            data.towns[0].gaikus.Count > 0 ? data.towns[0].gaikus[0].lon : null);
                }
            }
            return (null, null);
        }

        public static void SetInitCacheDatas(Dictionary<string, List<(SingleTown, string)>> townRegexes
            , Dictionary<string, string>? prefPatterns , Dictionary<string, Dictionary<string, string>> cityPatterns
            , PrefectureList? prefs, Dictionary<string, TownList> towns, Dictionary<string, string>? prefCityRegex)
        {
            if (townRegexes != null)
            {
                cachedTownRegexes = townRegexes;
            }
            if( prefPatterns != null)
            {
                cachedPrefecturePatterns = prefPatterns;
            }
            if(cityPatterns != null)
            {
                cachedCityPatterns = cityPatterns;
            }
            if(prefs != null)
            {
                cachedPrefectures = prefs;
            }
            if (towns != null)
            {
                cachedTowns = towns;
            }
            if (prefCityRegex != null)
            {
                cachedSameNamedPrefectureCityRegexPatterns = prefCityRegex;
            }
        }

        public static Dictionary<string, List<(SingleTown, string)>> GetCacheTownRegexes()
        {
            return cachedTownRegexes;
        }

        public static Dictionary<string, string>? GetCachePrefecturePatterns()
        {
            return cachedPrefecturePatterns;
        }
        public static Dictionary<string, Dictionary<string, string>> GetCacheCityPatterns()
        {
            return cachedCityPatterns;
        }
        public static PrefectureList? GetCachePrefectures()
        {
            return cachedPrefectures;
        }
        public static Dictionary<string, TownList> GetCacheTowns()
        {
            return cachedTowns;
        }
        public static Dictionary<string, string>? GetCacheSameNamedPrefectureCityRegexPatterns()
        {
            return cachedSameNamedPrefectureCityRegexPatterns;
        }
    }


    public class TownsComparer : IComparer<SingleTown>
    {
        public int Compare(SingleTown a, SingleTown b)
        {
            var aLen = a.town.Length;
            var bLen = b.town.Length;
            if (a.town.StartsWith("大字")) aLen -= 2;
            if (b.town.StartsWith("大字")) bLen -= 2;
            if (bLen > aLen) //town名が長いのを優先
                return 1;
            else if (aLen == bLen)
                return 0;
            else
                return -1;
        }
    }
}