using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using NormalizeJapaneseAddresses;
using Landscape2.Runtime.MoveToAddressMode;
using System.Linq;
//using System.Threading.Tasks;
using NormalizeJapaneseAddresses.Lib;

namespace Landscape2.Editor
{

    public class AddressDataConvert : EditorWindow
    {
        static List<string> path = new List<string>();
        static bool isConvert = false;
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 scrollPosition2 = Vector2.zero;
        private Vector2 scrollPosition3 = Vector2.zero;
        private Vector2 scrollPosition4 = Vector2.zero;

        [MenuItem("PLATEAU/Landscape/Import address data")]
        static void Open()
        {
            EditorWindow.GetWindow<AddressDataConvert>("Import address data");
        }

        void OnGUI()
        {
            //--- ウィンドウ表示設定
            CSVImport();
            minSize = new Vector2(300, 400);
        }

        void CSVImport()
        {
            EditorGUILayout.Space();
            Event evt = Event.current;
            GUILayout.Box("Drag & Drop CSV Files Here", GUILayout.Width(150.0f), GUILayout.Height(150.0f));
            Rect dropArea = GUILayoutUtility.GetLastRect();
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var draggedObject in DragAndDrop.paths)
                        {
                            if (Path.GetExtension(draggedObject).ToLower() == ".csv")
                            {
                                // 重複登録防止
                                if (path.Contains(draggedObject))
                                {
                                    Debug.Log("Already Added: " + draggedObject);
                                }
                                else
                                {
                                    path.Add(draggedObject);
                                }
                            }
                            else
                            {
                                Debug.Log("Not a CSV file: " + draggedObject);
                            }
                        }
                    }
                    Event.current.Use();
                    break;
            }

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            int pathCount = (path != null) ? path.Count : 0;
            if (pathCount > 0)
            {
                //--- インポートボタン
                if (GUILayout.Button("Import address data", GUILayout.Width(200.0f), GUILayout.Height(20.0f)))
                {
                    if (!isConvert)
                    {
                        isConvert = true;
                        ConvertAddress(path);
                        SetCacheDatas();
                        path.Clear();
                        pathCount = 0;
                        isConvert = false;
                    }
                }
                //--- インポートボタン
                if (GUILayout.Button("Import address data and link to Current Scene\n(" + sceneName + ")", GUILayout.Width(300.0f), GUILayout.Height(40.0f)))
                {
                    if (!isConvert)
                    {
                        isConvert = true;
                        ConvertAddress(path);
                        SetCacheDatas();
                        CopyToSceneFolder();
                        path.Clear();
                        pathCount = 0;
                        isConvert = false;
                    }
                }
            }

            string resourcePath = Application.dataPath + "/SampleDatas/Resources/AddressDatas";
            string resourceCachePath = Application.dataPath + "/SampleDatas/Resources/AddressCache";
            if (Directory.Exists(resourcePath) || Directory.Exists(resourceCachePath))
            {
                var files = Directory.GetFiles(resourcePath, "*.asset");
                var files2 = Directory.GetFiles(resourceCachePath, "*.asset");
                if (files.Length > 0 || files2.Length > 0)
                {
                    //--- 住所データ削除ボタン
                    if (GUILayout.Button("Delete Imported AddressDatas", GUILayout.Width(200.0f), GUILayout.Height(20.0f)))
                    {
                        if (!isConvert)
                        {
                            isConvert = true;
                            DeleteAddressResources();
                            isConvert = false;
                        }
                    }
                }
            }

            string sceneDataPath = Application.dataPath + "/SampleDatas/SceneDatas/";
            var sceneDir = sceneDataPath + sceneName;
            if (Directory.Exists(sceneDataPath) && Directory.GetDirectories(sceneDataPath).Length > 0)
            {
                //--- シーンに紐づけたデータ削除ボタン
                if (GUILayout.Button("Reset All link to Scenes", GUILayout.Width(200.0f), GUILayout.Height(20.0f)))
                {
                    if (!isConvert)
                    {
                        var dirs = Directory.GetDirectories(sceneDataPath);
                        foreach(var dir in dirs)
                        {
                            Directory.Delete(dir, true);
                            File.Delete(dir + ".meta");
                        }
                        AssetDatabase.Refresh();
                    }
                }

                // 現在のシーンとの紐づけデータ削除ボタン
                if (Directory.Exists(sceneDir))
                {
                    if (GUILayout.Button("Reset link to Current Scene\n(" + sceneName + ")", GUILayout.Width(200.0f), GUILayout.Height(40.0f)))
                    {
                        if (!isConvert)
                        {
                            Directory.Delete(sceneDir, true);
                            File.Delete(sceneDir + ".meta");
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            if (pathCount > 0)
            {
                // インポートファイルリセットボタン
                if (GUILayout.Button("Remove All", GUILayout.Width(100.0f), GUILayout.Height(20.0f)))
                {
                    if (!isConvert)
                    {
                        path.Clear();
                        EditorGUILayout.Space();
                        return;
                    }
                }

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                for (int i = 0; i < pathCount; i++)
                {
                    // ファイル名だけ表示
                    string fileName = Path.GetFileName(path[i]);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(fileName, GUILayout.Width(150.0f), GUILayout.Height(20.0f));

                        // 削除ボタン
                        if (GUILayout.Button("Remove", GUILayout.Width(60.0f), GUILayout.Height(20.0f)))
                        {
                            path.RemoveAt(i);
                            i--;
                            pathCount--;
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();

            // Import済みファイルを表示
            if (Directory.Exists(resourcePath))
            {
                var files = Directory.GetFiles(resourcePath, "*.asset");
                if (files.Length > 0)
                {
                    GUILayout.Label("Imported Files");
                    scrollPosition2 = EditorGUILayout.BeginScrollView(scrollPosition2);
                    for (int i = 0; i < files.Length; i++)
                    {
                        GUILayout.Label("　" + Path.GetFileName(files[i]));
                    }
                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.Space();

            // 現在のシーンとリンク済みのファイルを表示
            if (Directory.Exists(sceneDir) && Directory.Exists(sceneDir + "/AddressDatas") && Directory.GetFiles(sceneDir + "/AddressDatas").Length > 0)
            {
                var files = Directory.GetFiles(sceneDir + "/AddressDatas", "*.asset");
                if(files.Length > 0)
                {
                    GUILayout.Label("Files linked to Current Scene (" + sceneName + ")");
                    scrollPosition3 = EditorGUILayout.BeginScrollView(scrollPosition3);
                    for (int i = 0; i < files.Length; i++)
                    {
                        GUILayout.Label("　" + Path.GetFileName(files[i]));
                    }
                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.Space();

            if (Directory.Exists(sceneDataPath) && Directory.GetDirectories(sceneDataPath).Length > 0)
            {
                // 紐づけたデータがあるシーン名を表示
                var dirs = Directory.GetDirectories(sceneDataPath);
                if (dirs.Length > 0)
                {
                    GUILayout.Label("Scenes with linked data");
                    scrollPosition4 = EditorGUILayout.BeginScrollView(scrollPosition4);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        GUILayout.Label("　" + Path.GetFileName(dirs[i]));
                    }
                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.Space();
        }

        private async void ConvertAddress(List<string> path)
        {
            //await System.Threading.Tasks.Task.Run(() => ConvertAddressAsync(path));
            ConvertAddressAsync(path);
        }

        private async void ConvertAddressAsync(List<string> path)
        {
            // フォルダ内のcsvファイルを全部読み込んでscriptableobjectに変換
            // 0:都道府県名、1:市区町村名、2:大字・町丁目名、3:小字名、4:街区、8:緯度、9:経度、11：代表フラグ(0なら無視)
            List<string> files = path;
            foreach (string file in files)
            {
                if(!File.Exists(file))
                {
                    Debug.LogWarning("File not found: " + file);
                    continue;
                }

                Debug.Log("Convert File \"" + file + "\"");
                var lines = File.ReadAllLines(file, System.Text.Encoding.GetEncoding("Shift_JIS"));
                AddressData addressData = new AddressData();
                List<TownData> townDatas = new List<TownData>();
                bool citySet = false;
                int lineCnt = 0;
                int cnt = 0;
                foreach (var line in lines)
                {
                    lineCnt++;
                    cnt++;
                    if (lineCnt == 1)
                    {
                        continue; // ヘッダー行をスキップ
                    }
                    var columns = line.Split(',');
                    if (columns.Length < 12) continue;

                    for (int i = 0; i < columns.Length; i++)
                    {
                        columns[i] = columns[i].Trim().Replace("\"", "");
                    }

                    int daihyoFlag = int.Parse(columns[11]);
                    if (daihyoFlag != 1) continue; // 代表フラグが1のものだけ

                    if (!citySet)
                    {
                        addressData.prefecture = columns[0];
                        addressData.city = columns[1];
                        citySet = true;
                    }
                    string ooaza = columns[2];
                    string koaza = columns[3];
                    string gaiku = columns[4];
                    var lat = double.Parse(columns[8]);
                    var lon = double.Parse(columns[9]);

                    if (townDatas.Count > 0 &&
                    (!string.IsNullOrEmpty(ooaza) && townDatas.Any(t => t.ooaza == ooaza) && (string.IsNullOrEmpty(koaza) || townDatas.Any(t => t.ooaza == ooaza && t.koaza == koaza)))
                    || (string.IsNullOrEmpty(ooaza) && !string.IsNullOrEmpty(koaza) && townDatas.Any(t => t.koaza == koaza)))
                    {
                        // 既存のTownDataに小字を追加
                        for (int i = 0; i < townDatas.Count; i++)
                        {
                            if ((!string.IsNullOrEmpty(ooaza) && townDatas[i].ooaza == ooaza && (string.IsNullOrEmpty(koaza) || townDatas[i].koaza == koaza))
                                || (string.IsNullOrEmpty(ooaza) && !string.IsNullOrEmpty(koaza) && townDatas[i].koaza == koaza))
                            {
                                if (!string.IsNullOrEmpty(gaiku) && !townDatas[i].gaikus.Any(g => g.gaiku == gaiku))
                                {
                                    GaikuData gaikuData = new GaikuData
                                    {
                                        gaiku = gaiku,
                                        lat = lat,
                                        lon = lon
                                    };
                                    townDatas[i].gaikus.Add(gaikuData);
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        TownData townData = new TownData
                        {
                            ooaza = ooaza,
                            koaza = koaza,
                            gaikus = new List<GaikuData>()
                        };

                        if (!string.IsNullOrEmpty(gaiku))
                        {
                            GaikuData gaikuData = new GaikuData
                            {
                                gaiku = gaiku,
                                lat = lat,
                                lon = lon
                            };
                            townData.gaikus.Add(gaikuData);
                        }
                        townDatas.Add(townData);
                    }

                    if(cnt >= 1000)
                    {
                        cnt = 0;
                        //await Task.Yield(); // 1000行ごとに一時停止してUIスレッドに制御を戻す
                    }
                }
                addressData.towns = townDatas;

                if (addressData.towns.Count == 0)
                {
                    Debug.LogWarning("No valid address data found in file: " + file);
                    continue;
                }

                // ScriptableObjectを作成
                var asset = ScriptableObject.CreateInstance<AddressDataBase>();
                asset.data = addressData;

                // 保存先フォルダを確認
                string folderPath = "Assets/SampleDatas/Resources/AddressDatas";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                // ファイル名を設定
                string fileName = Path.GetFileNameWithoutExtension(file);
                string assetPath = Path.Combine(folderPath, fileName + ".asset");
                // Assetとして保存
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created Asset at \"" + assetPath + "\"");
            }
        }

        private async void SetCacheDatas()
        {
            //await System.Threading.Tasks.Task.Run(() => SetCacheDatasAsync());
            SetCacheDatasAsync();
            isConvert = false;
        }

        private async void SetCacheDatasAsync()
        {
            string path = Application.dataPath + "/SampleDatas/Resources/AddressDatas";
            string[] files = Directory.GetFiles(path, "*.asset");
            List<AddressData> addressDataBases = new List<AddressData>();
            foreach (string file in files)
            {
                var asset = AssetDatabase.LoadAssetAtPath<AddressDataBase>(file.Replace(Application.dataPath, "Assets"));
                if (asset != null)
                {
                    addressDataBases.Add(asset.data);
                }
            }

            var cache = ScriptableObject.CreateInstance<NormalizeAddressCacheData>();
            // 一行目のデータを使ってキャッシュを作成
            if (addressDataBases.Count > 0)
            {
                for(int i=0; i < addressDataBases.Count; i++)
                {
                    var data = addressDataBases[i];
                    if(data.towns.Count == 0)
                    {
                        Debug.LogWarning("No towns in AddressData: " + i);
                        continue;
                    }
                    string address = data.prefecture + data.city + data.towns[0].ooaza + data.towns[0].koaza + ((data.towns[0].gaikus.Count > 0) ? data.towns[0].gaikus[0].gaiku : "");
                    await NormalizeJapaneseAddresses.NormalizeJapaneseAddresses.Normalize(address, addressDataBases);
                }

                // キャッシュを保存
                cache.cachedTownRegexes = new List<CachedTownRegex>();
                cache.cachedPrefecturePatterns = new List<StringDictionary>();
                cache.cachedCityPatterns = new List<CachedCityPattern>();
                cache.cachedPrefectures = new List<CachedPrefecturePattern>();
                cache.cachedTowns = new List<CachedTowns>();
                cache.cachedSameNamedPrefectureCityRegexPatterns = new List<StringDictionary>();

                var townRegexes = CacheRegexes.GetCacheTownRegexes();
                if (townRegexes != null && townRegexes.Count > 0)
                {
                    for (int i = 0; i < townRegexes.Count; i++)
                    {
                        List<(SingleTown, string)> townVals = new List<(SingleTown, string)>();
                        foreach (var val in townRegexes.ElementAt(i).Value)
                        {
                            townVals.Add((val.Item1, val.Item2));
                        }

                        List<SingleTownList> singleTownLists = new List<SingleTownList>();
                        for (int j = 0; j < townVals.Count; j++)
                        {
                            SingleTownList singleTownList = new SingleTownList();
                            SingleTownDmy town = new SingleTownDmy
                            {
                                town = townVals[j].Item1.town,
                                originalTown = townVals[j].Item1.originalTown,
                                koaza = townVals[j].Item1.koaza,
                                gaiku = townVals[j].Item1.gaiku,
                                lat = (townVals[j].Item1.lat != null) ? townVals[j].Item1.lat.ToString() : null,
                                lng = (townVals[j].Item1.lng != null) ? townVals[j].Item1.lng.ToString() : null
                            };

                            singleTownList.town = town;
                            singleTownList.str = townVals[j].Item2;
                            singleTownLists.Add(singleTownList);
                        }

                        CachedTownRegex cacheTown = new CachedTownRegex
                        {
                            key = townRegexes.ElementAt(i).Key,
                            value = singleTownLists
                        };
                        cache.cachedTownRegexes.Add(cacheTown);
                    }
                }
                var cachedTowns = CacheRegexes.GetCacheTowns();
                if (cachedTowns != null && cachedTowns.Count > 0)
                {
                    foreach (var kv in cachedTowns)
                    {
                        List<SingleTownDmy> singleTownDmys = new List<SingleTownDmy>();
                        foreach (var val in kv.Value)
                        {
                            SingleTownDmy town = new SingleTownDmy
                            {
                                town = (val.town != null) ? val.town : "",
                                originalTown = (val.originalTown != null) ? val.originalTown : "",
                                koaza = (val.koaza != null) ? val.koaza : "",
                                gaiku = (val.gaiku != null) ? val.gaiku : "",
                                lat = (val.lat != null) ? val.lat.ToString() : "",
                                lng = (val.lng != null) ? val.lng.ToString() : ""
                            };
                            singleTownDmys.Add(town);
                        }
                        CachedTowns cachedTown = new CachedTowns
                        {
                            key = kv.Key,
                            value = singleTownDmys
                        };
                        cache.cachedTowns.Add(cachedTown);
                    }
                }

                cache.cachedPrefecturePatterns = CacheRegexes.GetCachePrefecturePatterns().Select(kv => new StringDictionary { key = kv.Key, value = kv.Value }).ToList();
                cache.cachedCityPatterns = CacheRegexes.GetCacheCityPatterns().Select(kv => new CachedCityPattern { key = kv.Key, value = kv.Value.Select(vk => new StringDictionary { key = vk.Key, value = vk.Value }).ToList() }).ToList();
                cache.cachedPrefectures = CacheRegexes.GetCachePrefectures().Select(kv => new CachedPrefecturePattern { key = kv.Key, value = kv.Value }).ToList();
                cache.cachedSameNamedPrefectureCityRegexPatterns = CacheRegexes.GetCacheSameNamedPrefectureCityRegexPatterns().Select(kv => new StringDictionary { key = kv.Key, value = kv.Value }).ToList();

                string folderPath = "Assets/SampleDatas/Resources/AddressCache";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                string assetPath = Path.Combine(folderPath, "NormalizeAddressCacheData.asset");
                AssetDatabase.CreateAsset(cache, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created Cache Asset at \"" + assetPath + "\"");
            }
            else
            {
                Debug.LogWarning("No AddressDataBase assets found in path: " + path);
            }
        }

        private void DeleteAddressResources()
        {
            string path = Application.dataPath + "/SampleDatas/Resources/AddressDatas";
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.asset");
                foreach (var file in files)
                {
                    File.Delete(file);
                    File.Delete(file + ".meta");
                }
                AssetDatabase.Refresh();
                Debug.Log("Deleted all AddressDataBase assets in path: " + path);
            }
            else
            {
                Debug.LogWarning("Directory not found: " + path);
            }

            string cachePath = Application.dataPath + "/SampleDatas/Resources/AddressCache/NormalizeAddressCacheData.asset";
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
                File.Delete(cachePath + ".meta");
                AssetDatabase.Refresh();
                Debug.Log("Deleted NormalizeAddressCacheData asset at: " + cachePath);
            }
        }

        // Assets\SampleDatas\AddressDatasに現在のシーン名のフォルダを作り、AddressDatasとAddressCacheをコピーする
        private void CopyToSceneFolder()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("No active scene found.");
                return;
            }
            string sourcePath = Application.dataPath + "/SampleDatas/Resources/AddressDatas";
            string cacheSourcePath = Application.dataPath + "/SampleDatas/Resources/AddressCache";
            string destDir = Application.dataPath + "/SampleDatas/SceneDatas/" + sceneName;
            if (!Directory.Exists(sourcePath) && !Directory.Exists(cacheSourcePath))
            {
                return;
            }
            if(!Directory.Exists(Application.dataPath + "/SampleDatas/SceneDatas"))
            {
                Directory.CreateDirectory(Application.dataPath + "/SampleDatas/SceneDatas");
            }
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string destPath = Path.Combine(destDir, "AddressDatas");
            string cacheDestPath = Path.Combine(destDir, "AddressCache");
            if (Directory.Exists(sourcePath))
            {
                if (Directory.Exists(destPath))
                {
                    Directory.Delete(destPath, true);
                }
                Directory.CreateDirectory(destPath);
                foreach (var newPath in Directory.GetFiles(sourcePath))
                {
                    string fileName = Path.GetFileName(newPath);
                    File.Copy(newPath, destPath + "/" + fileName, true);
                }
            }
            if (Directory.Exists(cacheSourcePath))
            {
                if (Directory.Exists(cacheDestPath))
                {
                    Directory.Delete(cacheDestPath, true);
                }
                Directory.CreateDirectory(cacheDestPath);
                foreach (var newPath in Directory.GetFiles(cacheSourcePath))
                {
                    string fileName = Path.GetFileName(newPath);
                    File.Copy(newPath, cacheDestPath + "/" + fileName, true);
                }
            }

            if(Directory.GetFiles(sourcePath).Length > 0 || Directory.GetFiles(cacheSourcePath).Length > 0)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
