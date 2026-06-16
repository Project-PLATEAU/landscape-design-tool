using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NormalizeJapaneseAddresses.Lib;

namespace NormalizeJapaneseAddresses
{
    public class TransformRequestResponse
    {
        // properties
    }

    public class TransformRequestQuery
    {
        public int level { get; set; } //  level = -1 は旧 API。 transformRequestFunction を設定しても無視する
        public string? pref { get; set; }
        public string? city { get; set; }
        public string? town { get; set; }
    }

    public delegate TransformRequestResponse TransformRequestFunction(Uri url, TransformRequestQuery query);

    /// <summary>
    /// normalize {@link Normalizer} の動作オプション。
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 住所データを URL 形式で指定。 file:// 形式で指定するとローカルファイルを参照できます。
        /// </summary>
        public string? JapaneseAddressesApi { get; set; }
        /// <summary>
        /// 町丁目のデータを何件までキャッシュするか。デフォルト 1,000
        /// C#プログラムでは、利用しない。
        /// </summary>
        public int TownCacheSize { get; set; }
    }

    public static partial class NormalizeJapaneseAddresses
    {
        /// <summary>
        /// staticのため、アプリ全体のconfigになる。
        /// </summary>
        /// <remarks>
        /// <para>TypeScriptでは、export const config: Config = currentConfigとあり、configと、currentConfigとは同じインスタンスである。</para>
        /// <para>従って、NormalizeJapaneseAddresses.configと、Configs.CurrentConfigとを同じインスタンスにした。</para>
        /// <para>NormalizeJapaneseAddresses.configを利用しているコードもあるが、Configs.CurrentConfigに統一するのが分かりやすいかもしれない。しかし、オリジナルに準拠することにした。</para>
        /// </remarks>
        public static Config config { get; } = Configs.CurrentConfig;
    }

    public class NormalizeResult
    {
        public string? pref { get; set; }
        public string? city { get; set; }
        public string? town { get; set; }
        public string? gaiku { get; set; }
        public string? addr { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }

        /// <summary>
        /// 住所文字列をどこまで判別できたかを表す正規化レベル
        // - 0 - 都道府県も判別できなかった。
        // - 1 - 都道府県まで判別できた。
        // - 2 - 市区町村まで判別できた。
        // - 3 - 町丁目まで判別できた。
        // - 7 - 住居表示住所の街区までの判別ができた。
        // - 8 - 住居表示住所の街区符号・住居番号までの判別ができた。
        /// </summary>
        public int level { get; set; }
    }

    /// <summary>
    /// normalizeTownNameで、latとlngをstringとして返すためのクラス。
    /// </summary>
    public class NormalizeResultString
    {
        public string? pref { get; set; }
        public string? city { get; set; }
        public string? town { get; set; }
        public string? koaza { get; set; }
        public string? addr { get; set; }
        public string? lat { get; set; } //string
        public string? lng { get; set; } //string
        public int level { get; set; }
    }

    // csv
    /*public class AddressData
    {
        public string prefecture;   // 都道府県名
        public string city;         // 市区町村名
        public string ooaza;        // 大字_丁目名
        public string koaza;        // 小字_通称名
        public string gaiku;        // 街区符号_地番
        //public int no;              // 座標系番号(未使用)
        //public double x;             // X座標(未使用)
        //public double y;             // Y座標(未使用)
        public double lat;           // 緯度
        public double lon;           // 経度
        //public int view;            // 住居表示フラグ(未使用)
        //public int daihyou;         // 代表フラグ(候補が複数ある場合は、これが立っているものを選ぶ)
        //public int pre;             // 更新前履歴フラグ(未使用)
        //public int post;            // 更新後履歴フラグ(未使用)
    }*/

    [System.Serializable]
    public class AddressData
    {
        public string prefecture;   // 都道府県名
        public string city;         // 市区町村名
        public List<TownData> towns; // 町丁目データの配列
    }

    [System.Serializable]
    public class TownData
    {
        public string ooaza;
        public string koaza;
        public List<GaikuData> gaikus;
    }

    [System.Serializable]
    public class GaikuData
    {
        public string gaiku;
        public double lat;
        public double lon;
    }


    /// <summary>
    /// 正規化関数の {@link normalize} のオプション
    /// </summary>
    public interface IOption
    {
        /// <summary>
        /// 正規化を行うレベルを指定します。{@link Option.level}
        /// </summary>
        /// <remarks>https://github.com/geolonia/normalize-japanese-addresses#normalizeaddress-string</remarks>
        int? level { get; set; }
    }

    /// <summary>
    /// オリジナルのTypeScriptにはないクラス。
    /// </summary>
    public class NormalizerOption : IOption
    {
        public int? level { get; set; }
    }

    public static class DefaultOption
    {
        public static int level = 3;
    }

    public static partial class NormalizeJapaneseAddresses
    {
        public static async Task<NormalizeResultString?> NormalizeTownName(string addr, string pref, string city, List<AddressData> datas)
        {
            addr = addr.Trim();
            var r = new Regex("^大字");
            addr = r.Replace(addr, "", 1);

            List<(SingleTown, string)> townPatterns = await CacheRegexes.GetTownRegexPatterns(pref, city, datas);
            List<string> regexPrefixes = new List<string> { "^" };
            if (city.StartsWith("京都市"))
            {
                // 京都は通り名削除のために後方一致を使う
                regexPrefixes.Add(".*");
            }

            if (townPatterns == null || townPatterns.Count < 1 || regexPrefixes.Count < 1)
            {
                return null;
            }

            foreach (string regexPrefix in regexPrefixes)
            {
                foreach ((SingleTown, string) tuple in townPatterns)
                {
                    SingleTown town = tuple.Item1;
                    string pattern = tuple.Item2;
                    Regex regex = new Regex($"{regexPrefix}{pattern}");
                    Match match = regex.Match(addr);
                    if (match.Success)
                    {
                        return new NormalizeResultString //返値を返すために、NormalizeResultのクラスを利用（JavaScriptにはない）
                        {
                            town = town.originalTown ?? town.town,
                            addr = addr.Substring(match.Length),
                            lat = town.lat.ToString(),
                            lng = town.lng.ToString()
                        };
                    }
                }
            }
            return null;
        }

        public static async Task<NormalizeResultString?> NormalizeKoazaName(string addr, string pref, string city, string ooaza, List<AddressData> datas)
        {
            addr = addr.Trim();

            List<(SingleTown, string)> townPatterns = await CacheRegexes.GetKoazaRegexPatterns(pref, city, ooaza, datas);
            if (townPatterns == null || townPatterns.Count < 1)
            {
                return null;
            }

            List<string> regexPrefixes = new List<string> { "^" };
            if (city.StartsWith("京都市"))
            {
                // 京都は通り名削除のために後方一致を使う
                regexPrefixes.Add(".*");
            }
            foreach (string regexPrefix in regexPrefixes)
            {
                foreach ((SingleTown, string) tuple in townPatterns)
                {
                    SingleTown town = tuple.Item1;
                    string pattern = tuple.Item2;
                    Regex regex = new Regex($"{regexPrefix}{pattern}");
                    Match match = regex.Match(addr);
                    if (match.Success)
                    {
                        return new NormalizeResultString //返値を返すために、NormalizeResultのクラスを利用（JavaScriptにはない）
                        {
                            koaza = town.koaza,
                            addr = addr.Substring(match.Length),
                            lat = town.lat.ToString(),
                            lng = town.lng.ToString()
                        };
                    }
                }
            }
            return null;
        }

        public static async Task<NormalizeResult> NormalizeGaikuName(string addr, string pref, string city, string ooaza, string koaza, List<AddressData> datas)
        {
            List<(SingleTown, string)> gaikuPatterns = await CacheRegexes.GetGaikuRegexPatterns(pref, city, ooaza, koaza, datas);
            if (gaikuPatterns == null || gaikuPatterns.Count < 1)
            {
                return null;
            }

            List<string> regexPrefixes = new List<string> { "^" };
            if (city.StartsWith("京都市"))
            {
                // 京都は通り名削除のために後方一致を使う
                regexPrefixes.Add(".*");
            }
            foreach (string regexPrefix in regexPrefixes)
            {
                foreach ((SingleTown, string) tuple in gaikuPatterns)
                {
                    SingleTown town = tuple.Item1;
                    string pattern = tuple.Item2;
                    Regex regex = new Regex($"{regexPrefix}{pattern}");
                    Match match = regex.Match(addr);
                    if (match.Success)
                    {
                        return new NormalizeResult //返値を返すために、NormalizeResultのクラスを利用（JavaScriptにはない）
                        {
                            gaiku = town.gaiku,
                            addr = addr.Substring(match.Length),
                            lat = town.lat,
                            lng = town.lng,
                            level = 8
                        };
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 住所を正規化します。
        /// </summary>
        /// <param name="address">住所文字列</param>
        /// <param name="option">正規化のオプション</param>
        /// <returns>正規化結果のオブジェクト</returns>
        /// <exception cref="Exception"></exception>
        /// <remarks>https://github.com/geolonia/normalize-japanese-addresses#normalizeaddress-string</remarks>
        public static async Task<NormalizeResult> Normalize(string address, List<AddressData> addrDatas, NormalizerOption? option = null)
        {
            if (addrDatas.Count < 1)
            {
                return new NormalizeResult()
                {
                    level = 0
                };
            }

            if (option is null)
            {
                option = new NormalizerOption();
                option.level = DefaultOption.level;
            }

            // 入力された住所に対して以下の正規化を予め行う。
            //
            // 1. `1-2-3` や `四-五-六` のようなフォーマットのハイフンを半角に統一。
            // 2. 町丁目以前にあるスペースをすべて削除。
            // 3. 最初に出てくる `1-` や `五-` のような文字列を町丁目とみなして、それ以前のスペースをすべて削除する。

            var addr = address
                .Normalize(NormalizationForm.FormC)
                .Replace("　", " "); //全角の空白を半角の空白へ
            addr = Regex.Replace(addr, " +", " ");
            addr = Regex.Replace(addr, "([０-９Ａ-Ｚａ-ｚ]+)", (match) =>
            {
                // 全角のアラビア数字は問答無用で半角にする
                return Utils.Zen2Han(match.Value);
            });
            // 数字の後または数字の前にくる横棒はハイフンに統一する
            addr = Regex.Replace(addr, "([0-9０-９一二三四五六七八九〇十百千][-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━])|([-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━])[0-9０-９一二三四五六七八九〇十]", (match) =>
            {
                return Regex.Replace(match.Value, "[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "-");
            });

            var r1 = new Regex("(.+)(丁目?|番(町|地|丁)|条|軒|線|(の|ノ)町|地割)");
            addr = r1.Replace(addr, (match) =>
            {
                return match.Value.Replace(" ", ""); // 町丁目名以前のスペースはすべて削除
            }, 1);

            var r2 = new Regex("(.+)((郡.+(町|村))|((市|巿).+(区|區)))");
            addr = r2.Replace(addr, (match) =>
            {
                return match.Value.Replace(" ", ""); // 区、郡以前のスペースはすべて削除
            }, 1);

            var r3 = new Regex(".+?[0-9一二三四五六七八九〇十百千]-"); //globalで一致されるので、replaceで、1回のみ（最初のみ）置換する。
            addr = r3.Replace(addr, (match) =>
            {
                return match.Value.Replace(" ", ""); // 1番はじめに出てくるアラビア数字以前のスペースを削除
            }, 1);
            //次は、globalで正規表現が実行されるので、一致するのが全て置換される。
            //addr = Regex.Replace(addr, ".+?[0-9一二三四五六七八九〇十百千]-", (match) =>
            //{
            //    return match.Value.Replace(" ", ""); // 1番はじめに出てくるアラビア数字以前のスペースを削除
            //});

            string pref = "";
            string city = "";
            string town = "";
            string koaza = "";
            double? lat = null;
            double? lng = null;
            int level = 0;
            NormalizeResultString? normalized = null;

            // 都道府県名の正規化

            var prefectures = CacheRegexes.GetPrefectures(addrDatas);
            var prefs = prefectures.Keys.ToList();
            var prefPatterns = CacheRegexes.GetPrefectureRegexPatterns(prefs);
            var sameNamedPrefectureCityRegexPatterns = CacheRegexes.GetSameNamedPrefectureCityRegexPatterns(prefs, prefectures);

            // 県名が省略されており、かつ市の名前がどこかの都道府県名と同じ場合(例.千葉県千葉市)、
            // あらかじめ県名を補完しておく。
            foreach (var item in sameNamedPrefectureCityRegexPatterns)
            {
                string prefectureCity = item.Key;
                string reg = item.Value;
                var match = Regex.Match(addr, reg);
                if (match.Success)
                {
                    addr = Regex.Replace(addr, reg, prefectureCity);
                    break;
                }
            }
            foreach (var item in prefPatterns)
            {
                string _pref = item.Key;
                string pattern = item.Value;
                var match = Regex.Match(addr, pattern);
                if (match.Success)
                {
                    pref = _pref;
                    addr = addr.Substring(match.Groups[0].Length);// 都道府県名以降の住所
                    (lat, lng) = CacheRegexes.GetPrefLatLon(pref, addrDatas);
                    break;
                }
            }

            if (string.IsNullOrEmpty(pref))
            {
                // 都道府県名が省略されている
                var matched = new List<dynamic>();
                foreach (var _pref in prefectures.Keys)
                {
                    var cities = prefectures[_pref];
                    var cityPatterns = CacheRegexes.GetCityRegexPatterns(_pref, cities);

                    addr = addr.Trim();
                    foreach (var item in cityPatterns)
                    {
                        string _city = item.Key;
                        string pattern = item.Value;
                        var match = Regex.Match(addr, pattern);
                        if (match.Success)
                        {
                            matched.Add(new
                            {
                                pref = _pref,
                                city = _city,
                                addr = addr.Substring(match.Groups[0].Length)
                            });
                        }
                    }
                }

                // マッチする都道府県が複数ある場合は町名まで正規化して都道府県名を判別する。（例: 東京都府中市と広島県府中市など）
                if (matched.Count == 1)
                {
                    pref = matched[0].pref;
                    (lat, lng) = CacheRegexes.GetPrefLatLon(pref, addrDatas);
                }
                else
                {
                    for (int i = 0; i < matched.Count; i++)
                    {
                        if (string.IsNullOrEmpty(matched[i].addr))
                        {
                            continue;
                        }
                        var normalized2 = await NormalizeTownName(matched[i].addr, matched[i].pref, matched[i].city, addrDatas);
                        if (normalized2 is not null)
                        {
                            pref = matched[i].pref;
                            lat = string.IsNullOrEmpty(normalized2.lat) ? null : double.Parse(normalized2.lat); //lat, lngとも、オリジナルのデータでnullがある //TODO TryParseにするか検討せよ //　float.Parse(normalized.lat); //normalized.latはstring 
                            lng = string.IsNullOrEmpty(normalized2.lng) ? null : double.Parse(normalized2.lng);//  float.Parse(normalized.lng); //normalized.lngはstring
                            if ((lat is double and not double.NaN) && (lng is double and not double.NaN))  //float.IsNaN(lat) || float.IsNaN(lng)  //https://stackoverflow.com/a/69558942/9924249
                            {
                                //latはlngは、nullでもなければ非数でもない
                            }
                            else
                            {
                                lat = null;
                                lng = null;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(pref) && option.level >= 2)
            {
                var cities = prefectures[pref];
                var cityPatterns = CacheRegexes.GetCityRegexPatterns(pref, cities);
                addr = addr.Trim();
                foreach (var item in cityPatterns)
                {
                    string _city = item.Key;
                    string pattern = item.Value;
                    var match = Regex.Match(addr, pattern);
                    if (match.Success)
                    {
                        city = _city;
                        addr = addr.Substring(match.Value.Length); // 市区町村名以降の住所

                        double? lat2 = null;
                        double? lng2 = null;
                        (lat2, lng2) = CacheRegexes.GetCityLatLon(pref, city, addrDatas);
                        if (lat2 is double and not double.NaN && lng2 is double and not double.NaN) //float.IsNaN(lat2) || float.IsNaN(lng2)
                        {
                            lat = lat2;
                            lng = lng2;
                        }
                        break;
                    }
                }
            }

            // 町丁目以降の正規化
            if (!string.IsNullOrEmpty(city) && option.level >= 3 && !string.IsNullOrEmpty(addr))
            {
                normalized = await NormalizeTownName(addr, pref, city, addrDatas);
                if (normalized is not null)
                {
                    town = normalized.town;
                    addr = normalized.addr;
                    lat = string.IsNullOrEmpty(normalized.lat) ? null : double.Parse(normalized.lat); //lat, lngとも、オリジナルのデータでnullがある //TODO TryParseにするか検討せよ //　float.Parse(normalized.lat); //normalized.latはstring 
                    lng = string.IsNullOrEmpty(normalized.lng) ? null : double.Parse(normalized.lng);//  float.Parse(normalized.lng); //normalized.lngはstring
                    if ((lat is double and not double.NaN) && (lng is double and not double.NaN))  //float.IsNaN(lat) || float.IsNaN(lng)  //https://stackoverflow.com/a/69558942/9924249
                    {
                        //latはlngは、nullでもなければ非数でもない
                    }
                    else
                    {
                        lat = null;
                        lng = null;
                    }
                }
                if (option.level > 3 && normalized is not null && town is not null && !string.IsNullOrEmpty(addr))
                {
                    // 小字も確認
                    var normalized_koaza = await NormalizeKoazaName(addr, pref, city, town, addrDatas);
                    if (normalized_koaza is not null)
                    {
                        koaza = normalized_koaza.koaza;
                        addr = normalized_koaza.addr;
                        lat = string.IsNullOrEmpty(normalized_koaza.lat) ? null : double.Parse(normalized_koaza.lat); //lat, lngとも、オリジナルのデータでnullがある //TODO TryParseにするか検討せよ //　float.Parse(normalized.lat); //normalized.latはstring 
                        lng = string.IsNullOrEmpty(normalized_koaza.lng) ? null : double.Parse(normalized_koaza.lng);//  float.Parse(normalized.lng); //normalized.lngはstring
                        if ((lat is double and not double.NaN) && (lng is double and not double.NaN))  //float.IsNaN(lat) || float.IsNaN(lng)  //https://stackoverflow.com/a/69558942/9924249
                        {
                            //latはlngは、nullでもなければ非数でもない
                        }
                        else
                        {
                            lat = null;
                            lng = null;
                        }
                    }
                }

                // townが取得できた場合にのみ、addrに対する各種の変換処理を行う。
                if (!string.IsNullOrEmpty(town) || !string.IsNullOrEmpty(koaza))
                {
                    addr = Regex.Replace(addr, @"^-", "");

                    addr = Regex.Replace(addr, @"([0-9]+)(丁目)",
                      (match) =>
                        {
                            var m = Regex.Replace(match.Value, @"([0-9]+)",
                                (num) =>
                                {
                                    return JapaneseNumeral.JapaneseNumeral.Number2kanji(long.Parse(num.Value));
                                });
                            return m;
                        });

                    addr = Regex.Replace(addr, @"(([0-9]+|[〇一二三四五六七八九十百千]+)(番地?)([0-9]+|[〇一二三四五六七八九十百千]+)号)\s*(.+)", "$1 $5");

                    addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)\s*(番地?)\s*([0-9]+|[〇一二三四五六七八九十百千]+)\s*号?", "$1-$3");

                    addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)番地?", "$1");

                    addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)の", "$1-");

                    addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", (match) =>
                          {
                              var m = Utils.Kan2Num(match.Value);
                              m = Regex.Replace(m, @"[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "-");
                              return m;
                          });

                    addr = Regex.Replace(addr, @"[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]([0-9]+|[〇一二三四五六七八九十百千]+)", (match) =>
                    {
                        var m = Utils.Kan2Num(match.Value);
                        m = Regex.Replace(m, @"[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "-");
                        return m;
                    });

                    addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)-", (match) =>
                    {
                        // `1-` のようなケース
                        return Utils.Kan2Num(match.Value);
                    });

                    addr = Regex.Replace(addr, @"-([0-9]+|[〇一二三四五六七八九十百千]+)", (match) =>
                    {
                        // `-1` のようなケース
                        return Utils.Kan2Num(match.Value);
                    });

                    addr = Regex.Replace(addr, @"-[^0-9]([0-9]+|[〇一二三四五六七八九十百千]+)", (match) =>
                    {
                        // `-あ1` のようなケース
                        return Utils.Kan2Num(Utils.Zen2Han(match.Value));
                    });

                    addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)$", (match) =>
                    {
                        // `串本町串本１２３４` のようなケース
                        return Utils.Kan2Num(match.Value);
                    });

                    addr = addr.Trim();
                }
            }

            addr = AddressUtils.PatchAddr(pref, city, town, addr);
            if (!string.IsNullOrEmpty(pref)) level++;
            if (!string.IsNullOrEmpty(city)) level++;
            if (!string.IsNullOrEmpty(town)) level++;
            if (!string.IsNullOrEmpty(koaza)) level++;
            if (option.level <= 3 || level < 3)
            {
                return new NormalizeResult()
                {
                    pref = pref,
                    city = city,
                    town = town + koaza,
                    addr = addr,
                    level = level,
                    lat = lat,
                    lng = lng
                };
            }

            NormalizeResult? NormalizeResult = null;
            if (option.level > 3 && normalized is not null && town is not null && !string.IsNullOrEmpty(addr))
            {
                NormalizeResult = await NormalizeGaikuName(addr, pref, city, town, koaza, addrDatas);
                if (NormalizeResult is not null)
                {
                    addr = NormalizeResult.addr;
                    lat = NormalizeResult.lat;
                    lng = NormalizeResult.lng;
                    level = NormalizeResult.level;
                }
            }

            var result = new NormalizeResult
            {
                pref = pref,
                city = city,
                town = town + koaza,
                gaiku = NormalizeResult?.gaiku,
                addr = addr,
                lat = lat,
                lng = lng,
                level = level
            };

            return result;
        }

        // サジェスト用 文字列リストを返す
        public static async Task<List<string>> GetSuggestList(string address, List<AddressData> addrDatas)
        {
            var list = new List<string>();
            if (addrDatas.Count < 1)
            {
                return list;
            }

            var addr = address
                .Normalize(NormalizationForm.FormC)
                .Replace("　", " "); //全角の空白を半角の空白へ
            addr = Regex.Replace(addr, " +", " ");
            addr = Regex.Replace(addr, "([０-９Ａ-Ｚａ-ｚ]+)", (match) =>
            {
                // 全角のアラビア数字は問答無用で半角にする
                return Utils.Zen2Han(match.Value);
            });
            // 数字の後または数字の前にくる横棒はハイフンに統一する
            addr = Regex.Replace(addr, "([0-9０-９一二三四五六七八九〇十百千][-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━])|([-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━])[0-9０-９一二三四五六七八九〇十]", (match) =>
            {
                return Regex.Replace(match.Value, "[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "-");
            });

            var r1 = new Regex("(.+)(丁目?|番(町|地|丁)|条|軒|線|(の|ノ)町|地割)");
            addr = r1.Replace(addr, (match) =>
            {
                return match.Value.Replace(" ", ""); // 町丁目名以前のスペースはすべて削除
            }, 1);

            var r2 = new Regex("(.+)((郡.+(町|村))|((市|巿).+(区|區)))");
            addr = r2.Replace(addr, (match) =>
            {
                return match.Value.Replace(" ", ""); // 区、郡以前のスペースはすべて削除
            }, 1);

            var r3 = new Regex(".+?[0-9一二三四五六七八九〇十百千]-"); //globalで一致されるので、replaceで、1回のみ（最初のみ）置換する。
            addr = r3.Replace(addr, (match) =>
            {
                return match.Value.Replace(" ", ""); // 1番はじめに出てくるアラビア数字以前のスペースを削除
            }, 1);
            
            // 都道府県名の正規化
            var prefectures = CacheRegexes.GetPrefectures(addrDatas);
            var prefs = prefectures.Keys.ToList();
            var prefPatterns = CacheRegexes.GetPrefectureRegexPatterns(prefs);
            var sameNamedPrefectureCityRegexPatterns = CacheRegexes.GetSameNamedPrefectureCityRegexPatterns(prefs, prefectures);

            // 県名が省略されており、かつ市の名前がどこかの都道府県名と同じ場合(例.千葉県千葉市)、
            // あらかじめ県名を補完しておく。
            foreach (var item in sameNamedPrefectureCityRegexPatterns)
            {
                string prefectureCity = item.Key;
                string reg = item.Value;
                var match = Regex.Match(addr, reg);
                if (match.Success)
                {
                    addr = Regex.Replace(addr, reg, prefectureCity);
                    break;
                }
            }

            string pref = "";
            foreach (var item in prefPatterns)
            {
                string _pref = item.Key;
                string pattern = item.Value;
                var match = Regex.Match(addr, pattern);
                if (match.Success)
                {
                    pref = _pref;
                    addr = addr.Substring(match.Groups[0].Length);// 都道府県名以降の住所
                    break;
                }
            }

            if (string.IsNullOrEmpty(pref))
            {
                // 都道府県名が省略されている
                var matched = new List<dynamic>();
                foreach (var _pref in prefectures.Keys)
                {
                    var cities_ = prefectures[_pref];
                    var cityPatterns_ = CacheRegexes.GetCityRegexPatterns(_pref, cities_);

                    addr = addr.Trim();
                    if (!string.IsNullOrEmpty(addr))
                    {
                        foreach (var item in cityPatterns_)
                        {
                            string _city = item.Key;
                            string pattern = item.Value;
                            var match = Regex.Match(addr, pattern);
                            if (match.Success)
                            {
                                matched.Add(new
                                {
                                    pref = _pref,
                                    city = _city,
                                    addr = addr.Substring(match.Groups[0].Length)
                                });
                            }
                        }
                    }
                }

                // マッチする都道府県が複数ある場合は町名まで正規化して都道府県名を判別する。（例: 東京都府中市と広島県府中市など）
                if (matched.Count == 1)
                {
                    pref = matched[0].pref;
                }
                else if (matched.Count > 1)
                {
                    for (int i = 0; i < matched.Count; i++)
                    {
                        if (string.IsNullOrEmpty(matched[i].addr))
                        {
                            continue;
                        }
                        var normalized2 = await NormalizeTownName(matched[i].addr, matched[i].pref, matched[i].city, addrDatas);
                        if (normalized2 is not null)
                        {
                            pref = matched[i].pref;
                        }
                    }
                }

                if (string.IsNullOrEmpty(pref))
                {
                    if (matched.Count > 0)
                    {
                        foreach (var m in matched)
                        {
                            var s = m.pref + m.city;
                            if (!list.Contains(s))
                            {
                                list.Add(s);
                            }
                        }
                    }
                    else
                    {
                        // 部分一致を探す
                        List<(string, int)> prefPartMatchList = new List<(string, int)>();
                        int len = addr.Length;
                        int matchMax = -1;
                        if (len > 0)
                        {
                            foreach (var p in prefectures.Keys)
                            {
                                int minLen = Math.Min(len, p.Length);
                                for (int i = minLen; i > 0; i--)
                                {
                                    if (matchMax >= 0 && i < matchMax)
                                    {
                                        // これ以上短い部分一致は意味がないのでスキップ
                                        break;
                                    }
                                    if (i >= p.Length)
                                    {
                                        continue;
                                    }
                                    string part = addr.Substring(0, i);
                                    if (p.StartsWith(part))
                                    {
                                        if (!prefPartMatchList.Contains((p, i)))
                                        {
                                            prefPartMatchList.Add((p, i));
                                            matchMax = i;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        if (list.Count < 1 && prefPartMatchList.Count > 0)
                        {
                            list = prefPartMatchList
                                .Where(p => p.Item2 == matchMax)
                                .Select(p => p.Item1)
                                .Distinct()
                                .ToList();
                        }
                        else if (list.Count < 1)
                        {
                            foreach (var _pref in prefectures.Keys)
                            {
                                var cities_ = prefectures[_pref];
                                // 市区町村名の部分一致を探す
                                List<(string, int)> cityPartMatchList = new List<(string, int)>();
                                len = addr.Length;
                                matchMax = -1;
                                if (len > 0)
                                {
                                    foreach (var c in cities_)
                                    {
                                        int minLen = Math.Min(len, c.Length);
                                        for (int i = minLen; i > 0; i--)
                                        {
                                            if (matchMax >= 0 && i < matchMax)
                                            {
                                                // これ以上短い部分一致は意味がないのでスキップ
                                                break;
                                            }
                                            if (i >= c.Length)
                                            {
                                                continue;
                                            }
                                            string part = addr.Substring(0, i);
                                            if (c.StartsWith(part))
                                            {
                                                if (!cityPartMatchList.Contains((c, i)))
                                                {
                                                    cityPartMatchList.Add((c, i));
                                                    matchMax = i;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (list.Count < 1 && cityPartMatchList.Count > 0)
                                {
                                    list = cityPartMatchList
                                        .Where(c => c.Item2 == matchMax)
                                        .Select(c => _pref + c.Item1)
                                        .Distinct()
                                        .ToList();
                                }
                            }
                        }
                    }
                    return list;
                }
            }

            string city = "";
            var cities = prefectures[pref];
            var cityPatterns = CacheRegexes.GetCityRegexPatterns(pref, cities);
            addr = addr.Trim();
            bool cityMatched = false;
            if (!string.IsNullOrEmpty(addr))
            {
                foreach (var item in cityPatterns)
                {
                    string _city = item.Key;
                    string pattern = item.Value;
                    var match = Regex.Match(addr, pattern);
                    if (match.Success)
                    {
                        cityMatched = true;
                        city = _city;
                        addr = addr.Substring(match.Value.Length); // 市区町村名以降の住所
                        break;
                    }
                }
            }
            if (!cityMatched)
            {
                if(!string.IsNullOrEmpty(addr))
                {
                    // 部分一致を探す
                    List<(string, int)> cityPartMatchList = new List<(string, int)>();
                    int len = addr.Length;
                    int matchMax = -1;
                    if (len > 0)
                    {
                        foreach (var c in cities)
                        {
                            int minLen = Math.Min(len, c.Length);
                            for (int i = minLen; i > 0; i--)
                            {
                                if (matchMax >= 0 && i < matchMax)
                                {
                                    // これ以上短い部分一致は意味がないのでスキップ
                                    break;
                                }
                                if (i >= c.Length)
                                {
                                    continue;
                                }
                                string part = addr.Substring(0, i);
                                if (c.StartsWith(part))
                                {
                                    if (!cityPartMatchList.Contains((c, i)))
                                    {
                                        cityPartMatchList.Add((c, i));
                                        matchMax = i;
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    if(list.Count < 1 && cityPartMatchList.Count > 0)
                    {
                        list = cityPartMatchList
                            .Where(c => c.Item2 == matchMax)
                            .Select(c => pref + c.Item1)
                            .Distinct()
                            .ToList();
                    }

                    if (list.Count > 0)
                    {
                        return list;
                    }
                }
                // 市区町村がマッチしなかった場合は、市区町村の候補を全て返す
                foreach (var c in cities)
                {
                    var s = pref + c;
                    if (!list.Contains(s))
                    {
                        list.Add(s);
                    }
                }
                return list;
            }

            if (string.IsNullOrEmpty(city))
            {
                return list;
            }

            // 町丁目以降の正規化
            string town = "";
            var towns = await CacheRegexes.GetTowns(pref, city, addrDatas);
            if (!string.IsNullOrEmpty(addr))
            {
                // 部分一致を探す
                List<(SingleTown, int)> townPartMatchList = new List<(SingleTown, int)>();
                List<SingleTown> townMatchList = new List<SingleTown>();
                int len = addr.Length;
                int matchMax = -1;
                if (len > 0)
                {
                    foreach (var t in towns)
                    {
                        if(t.town.StartsWith(addr))
                        {
                            if(t.town.Length == addr.Length)
                            {
                                continue;
                            }
                            if (!townMatchList.Contains(t))
                            {
                                townMatchList.Add(t);
                            }
                            continue;
                        }
                        int minLen = Math.Min(len, t.town.Length);
                        for (int i = minLen; i > 0; i--)
                        {
                            if (matchMax >= 0 && i < matchMax)
                            {
                                // これ以上短い部分一致は意味がないのでスキップ
                                break;
                            }
                            if (i >= t.town.Length)
                            {
                                continue;
                            }
                            string part = addr.Substring(0, i);
                            if (t.town.StartsWith(part))
                            {
                                if (!townPartMatchList.Contains((t, i)))
                                {
                                    townPartMatchList.Add((t, i));
                                    matchMax = i;
                                }
                                break;
                            }
                        }
                    }
                    if (townMatchList.Count > 0)
                    {
                        list.AddRange(townMatchList
                                .Select(t => pref + city + t.town + t.koaza)
                                .Distinct()
                                .ToList());
                    }
                }
                var normalized = await NormalizeTownName(addr, pref, city, addrDatas);
                if (normalized is not null)
                {
                    town = normalized.town;
                    addr = normalized.addr;
                }
                else
                {
                    if (townPartMatchList.Count > 0)
                    {
                        list.AddRange(townPartMatchList
                                .Where(t => t.Item2 == matchMax)
                                .Select(t => pref + city + t.Item1.town + t.Item1.koaza)
                                .Distinct()
                                .ToList());
                    }
                    if (list.Count > 0)
                    {
                        list.Sort((a, b) => a.Length - b.Length);
                        return list;
                    }
                }
            }

            if (string.IsNullOrEmpty(town))
            {
                if (towns.Count > 0)
                {
                    // 町丁目がマッチしなかった場合は、町丁目の候補を全て返す
                    foreach (var t in towns)
                    {
                        var s = pref + city + t.town + t.koaza;
                        if (!list.Contains(s))
                        {
                            list.Add(s);
                        }
                    }
                }
                list.Sort((a, b) => a.Length - b.Length);
                return list;
            }

            // 小字以降
            string koaza = "";
            var koazas = await CacheRegexes.GetKoazas(pref, city, town, addrDatas);
            if (koazas.Count > 0)
            {
                if (!string.IsNullOrEmpty(addr))
                {
                    // 部分一致を探す
                    List<(string, int)> koazaPartMatchList = new List<(string, int)>();
                    List<string> koazaMatchList = new List<string>();
                    int len = addr.Length;
                    int matchMax = -1;
                    if (len > 0)
                    {
                        foreach (var k in koazas)
                        {
                            if (k.StartsWith(addr))
                            {
                                if (k.Length == addr.Length)
                                {
                                    continue;
                                }
                                if (!koazaMatchList.Contains(k))
                                {
                                    koazaMatchList.Add(k);
                                }
                                continue;
                            }
                            int minLen = Math.Min(len, k.Length);
                            for (int i = minLen; i > 0; i--)
                            {
                                if (matchMax >= 0 && i < matchMax)
                                {
                                    // これ以上短い部分一致は意味がないのでスキップ
                                    break;
                                }
                                if (i >= k.Length)
                                {
                                    continue;
                                }
                                string part = addr.Substring(0, i);
                                if (k.StartsWith(part))
                                {
                                    if (!koazaPartMatchList.Contains((k, i)))
                                    {
                                        koazaPartMatchList.Add((k, i));
                                        matchMax = i;
                                    }
                                    break;
                                }
                            }
                        }
                        if (koazaMatchList.Count > 0)
                        {
                            list.AddRange(koazaMatchList
                                .Select(k => pref + city + town + k)
                                .Distinct()
                                .ToList());
                        }
                    }
                    var normalized_koaza = await NormalizeKoazaName(addr, pref, city, town, addrDatas);
                    if (normalized_koaza is not null)
                    {
                        koaza = normalized_koaza.koaza;
                        addr = normalized_koaza.addr;
                    }
                    else if (list.Count > 0)
                    {
                        if (koazaPartMatchList.Count > 0)
                        {
                            list.AddRange(koazaPartMatchList
                                .Where(k => k.Item2 == matchMax)
                                .Select(k => pref + city + town + k.Item1)
                                .Distinct()
                                .ToList());
                        }
                        list.Sort((a, b) => a.Length - b.Length);
                        return list;
                    }
                }
                else
                {
                    foreach (var k in koazas)
                    {
                        var s = pref + city + town + k;
                        if (!list.Contains(s))
                        {
                            list.Add(s);
                        }
                    }
                }
            }

            // 街区以降
            if (!string.IsNullOrEmpty(addr))
            {
                addr = Regex.Replace(addr, @"^-", "");

                addr = Regex.Replace(addr, @"([0-9]+)(丁目)",
                  (match) =>
                  {
                      var m = Regex.Replace(match.Value, @"([0-9]+)",
                              (num) =>
                            {
                                return JapaneseNumeral.JapaneseNumeral.Number2kanji(long.Parse(num.Value));
                            });
                      return m;
                  });

                addr = Regex.Replace(addr, @"(([0-9]+|[〇一二三四五六七八九十百千]+)(番地?)([0-9]+|[〇一二三四五六七八九十百千]+)号)\s*(.+)", "$1 $5");

                addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)\s*(番地?)\s*([0-9]+|[〇一二三四五六七八九十百千]+)\s*号?", "$1-$3");

                addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)番地?", "$1");

                addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)の", "$1-");

                addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", (match) =>
                {
                    var m = Utils.Kan2Num(match.Value);
                    m = Regex.Replace(m, @"[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "-");
                    return m;
                });

                addr = Regex.Replace(addr, @"[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]([0-9]+|[〇一二三四五六七八九十百千]+)", (match) =>
                {
                    var m = Utils.Kan2Num(match.Value);
                    m = Regex.Replace(m, @"[-－﹣−‐⁃‑‒–—﹘―⎯⏤ーｰ─━]", "-");
                    return m;
                });

                addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)-", (match) =>
                {
                    // `1-` のようなケース
                    return Utils.Kan2Num(match.Value);
                });

                addr = Regex.Replace(addr, @"-([0-9]+|[〇一二三四五六七八九十百千]+)", (match) =>
                {
                    // `-1` のようなケース
                    return Utils.Kan2Num(match.Value);
                });

                addr = Regex.Replace(addr, @"-[^0-9]([0-9]+|[〇一二三四五六七八九十百千]+)", (match) =>
                {
                    // `-あ1` のようなケース
                    return Utils.Kan2Num(Utils.Zen2Han(match.Value));
                });

                addr = Regex.Replace(addr, @"([0-9]+|[〇一二三四五六七八九十百千]+)$", (match) =>
                {
                    // `串本町串本１２３４` のようなケース
                    return Utils.Kan2Num(match.Value);
                });

                addr = addr.Trim();
            }

            List<string> gaikus = CacheRegexes.GetGaikus(pref, city, town, koaza, addrDatas);
            if(gaikus.Count > 0)
            {
                var gaikuList = new List<string>();
                if (!string.IsNullOrEmpty(addr))
                {
                    // 部分一致を探す
                    List<(string, int)> gaikuPartMatchList = new List<(string, int)>();
                    List<string> gaikuMatchList = new List<string>();
                    int len = addr.Length;
                    int matchMax = -1;
                    if (len > 0)
                    {
                        foreach (var g in gaikus)
                        {
                            if (g.StartsWith(addr))
                            {
                                if (!gaikuMatchList.Contains(g))
                                {
                                    gaikuMatchList.Add(g);
                                }
                                continue;
                            }
                            int minLen = Math.Min(len, g.Length);
                            for (int i = minLen; i > 0; i--)
                            {
                                if (matchMax >= 0 && i < matchMax)
                                {
                                    // これ以上短い部分一致は意味がないのでスキップ
                                    break;
                                }
                                if (i >= g.Length)
                                {
                                    // 街区名よりも長い部分一致は意味がないのでスキップ
                                    continue;
                                }
                                string part = addr.Substring(0, i);
                                if (g.StartsWith(part))
                                {
                                    if (!gaikuPartMatchList.Contains((g, i)))
                                    {
                                        gaikuPartMatchList.Add((g, i));
                                        matchMax = i;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    if(gaikuMatchList.Count > 0)
                    {
                        gaikuList = gaikuMatchList
                            .Select(g => pref + city + town + koaza + g)
                            .Distinct()
                            .ToList();
                    }
                    else if (gaikuPartMatchList.Count > 0)
                    {
                        gaikuList = gaikuPartMatchList
                            .Where(g => g.Item2 == matchMax)
                            .Select(g => pref + city + town + koaza + g.Item1)
                            .Distinct()
                            .ToList();                        
                    }
                    if(gaikuList.Count > 0)
                    {
                        gaikuList.Sort((a, b) =>
                        {
                            if (a.Length != b.Length)
                            {
                                return a.Length - b.Length;
                            }
                            else
                            {
                                // 文字数が同じ場合は数字の大小で比較する
                                var aNums = Regex.Matches(a, @"\d+").Select(m => long.Parse(m.Value)).ToList();
                                var bNums = Regex.Matches(b, @"\d+").Select(m => long.Parse(m.Value)).ToList();
                                int minCount = Math.Min(aNums.Count, bNums.Count);
                                for (int i = 0; i < minCount; i++)
                                {
                                    if (aNums[i] != bNums[i])
                                    {
                                        return (int)(aNums[i] - bNums[i]);
                                    }
                                }
                                return aNums.Count - bNums.Count;
                            }
                        });
                        list.Sort((a, b) => a.Length - b.Length);
                        list.AddRange(gaikuList);
                        return list;
                    }
                }
                foreach (var g in gaikus)
                {
                    var s = pref + city + town + koaza + g;
                    if (!gaikuList.Contains(s))
                    {
                        gaikuList.Add(s);
                    }
                }
                if(list.Count > 0 || gaikuList.Count > 0)
                {
                    gaikuList.Sort((a, b) =>
                    {
                        if (a.Length != b.Length)
                        {
                            return a.Length - b.Length;
                        }
                        else
                        {
                            // 文字数が同じ場合は数字の大小で比較する
                            var aNums = Regex.Matches(a, @"\d+").Select(m => long.Parse(m.Value)).ToList();
                            var bNums = Regex.Matches(b, @"\d+").Select(m => long.Parse(m.Value)).ToList();
                            int minCount = Math.Min(aNums.Count, bNums.Count);
                            for (int i = 0; i < minCount; i++)
                            {
                                if (aNums[i] != bNums[i])
                                {
                                    return (int)(aNums[i] - bNums[i]);
                                }
                            }
                            return aNums.Count - bNums.Count;
                        }
                    });
                    list.Sort((a, b) => a.Length - b.Length);
                    list.AddRange(gaikuList);
                    return list;
                }
            }

            if(list.Count > 1)
            {
                list.Sort((a, b) => a.Length - b.Length);
            }
            return list;
        }
    }
}
