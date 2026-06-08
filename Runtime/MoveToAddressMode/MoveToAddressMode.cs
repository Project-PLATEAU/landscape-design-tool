using PLATEAU.Native;
using UnityEngine;
using NormalizeJapaneseAddresses;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cinemachine;
using NormalizeJapaneseAddresses.Lib;

namespace Landscape2.Runtime.MoveToAddressMode
{
    public class MoveToAddressMode
    {
        private const string AddressDataPath = "AddressDatas";
        private const string CacheDataPath = "AddressCache";

        private CinemachineVirtualCamera mainCamera;
        private CinemachineVirtualCamera walkerCamera;
        private List<AddressData> addressDataFiles = new List<AddressData>();
        private bool isAddressDataLoaded = false;
        private bool isFindingAddress = false;

        public enum AddressMoveStatus
        {
            None,
            Success,
            Failed,
            NoGround,
            NoData,
        }

        public MoveToAddressMode(CinemachineVirtualCamera mainCamera, CinemachineVirtualCamera walkerCamera) {
            this.mainCamera = mainCamera;
            this.walkerCamera = walkerCamera;
            isAddressDataLoaded = false;
            isFindingAddress = false;
            if (AddressDataPath != "")
            {
                LoadDatas();
            }
        }

        public async Task<AddressMoveStatus> MoveToAddress(string address, LandscapeCamera landscape)
        {
            bool walkerMode = landscape != null && landscape.cameraState == LandscapeCameraState.Walker;
            if(walkerMode && walkerCamera == null)
            {
                Debug.LogWarning("歩行者カメラが存在しません。");
                return AddressMoveStatus.None;
            }
            if (mainCamera == null)
            {
                Debug.LogWarning("MainCameraが存在しません。");
                return AddressMoveStatus.None;
            }

            var oldPos = mainCamera.transform.position;
            (var success, var newPos, var status) = await GetAddressVec3(oldPos, address);
            if (success)
            {
                if (walkerMode)
                {
                    landscape.SetWalkerPos(newPos);
                    walkerCamera.PreviousStateIsValid = false;
                }

                newPos.y = oldPos.y;
                mainCamera.transform.position = newPos;
                mainCamera.PreviousStateIsValid = false;
            }

            return status; // エラーダイアログを表示するかどうか
        }

        // (移動可能の成否、取得した位置、ダイアログの表示判定)
        public async Task<(bool, Vector3, AddressMoveStatus)> GetAddressVec3(Vector3 oldPos, string address)
        {
            bool success;
            float lat, lon;
            (success, lat, lon) = await Task.Run(() => GetLatLon(address));
            if(!success)
            {
                Debug.LogWarning("住所から緯度経度の取得に失敗しました。");
                return (success, oldPos, addressDataFiles.Count == 0 ? AddressMoveStatus.NoData : AddressMoveStatus.Failed);
            }
            GeoCoordinate geoCoordinate = new GeoCoordinate(lat, lon, 0);
            PlateauVector3d plateauVector3 = CityModelHandler.CityModel.GeoReference.Project(geoCoordinate);

            Vector3 chkPos = new Vector3((float)plateauVector3.X, 300f, (float)plateauVector3.Z);
            if (Physics.Raycast(chkPos, Vector3.down, out RaycastHit hitInfo, 300f)) 
            {
                Vector3 pos = new Vector3((float)plateauVector3.X, hitInfo.point.y + 1.0f, (float)plateauVector3.Z);
                return (success, pos, AddressMoveStatus.Success);
            }

            Debug.LogWarning("移動先に地形が存在しません。");
            return (false, oldPos, AddressMoveStatus.NoGround);
        }

        // (緯度経度取得の成否、緯度、経度)
        private async Task<(bool, float, float)> GetLatLon(string address)
        {
            while (!isAddressDataLoaded)
            {
                await Task.Yield();
            }

            if(addressDataFiles.Count == 0)
            {
                Debug.LogWarning("住所データが存在しません。");
                return (false, 0, 0);
            }

            if (isFindingAddress)
            {
                return (false, 0, 0);
            }

            isFindingAddress = true;

            // 住所から緯度経度を取得
            var option = new NormalizerOption() { level = 4 };
            NormalizeResult res = await NormalizeJapaneseAddresses.NormalizeJapaneseAddresses.Normalize(address, addressDataFiles, option);

            Debug.Log($"Normalized Address: {res.pref} / {res.city} / {res.town} / {res.gaiku} |{res.addr}| Level: {res.level}\nlat:{res.lat}, Lon:{res.lng}");

            isFindingAddress = false;
            bool success = res.level > 0 && res.lat != null && res.lng != null;
            return (success, (float)(res.lat ?? 0), (float)(res.lng ?? 0));
        }

        public async Task<List<string>> GetSuggestList(string address)
        {
            List<string> suggestList = new List<string>();
            if (addressDataFiles.Count == 0)
            {
                Debug.LogWarning("住所データが存在しません。");
                return suggestList;
            }
            if (isFindingAddress)
            {
                return suggestList;
            }

            // 住所から候補を取得
            suggestList = await NormalizeJapaneseAddresses.NormalizeJapaneseAddresses.GetSuggestList(address, addressDataFiles);
            return suggestList;
        }

        private void LoadDatas()
        {
            // Resourcesフォルダ内のscriptable objectをすべて読み込む
            var loadDatas = Resources.LoadAll<AddressDataBase>(AddressDataPath);
            if (loadDatas.Length == 0)
            {
                isAddressDataLoaded = true;
                Debug.LogWarning("住所データが見つかりません。");
                return;
            }
            foreach (var data in loadDatas)
            {
                if (data != null && data.data != null && data.data.towns.Count > 0)
                {
                    addressDataFiles.Add(data.data);
                }
                else
                {
                    Debug.LogWarning("住所データが不正です。");
                }
            }

            var cacheData = Resources.Load<NormalizeAddressCacheData>(CacheDataPath + "/NormalizeAddressCacheData");
            if (cacheData != null)
            {
                // キャッシュリストを辞書に変換
                Dictionary<string, List<(SingleTown, string)>> cachedTownRegexes = new();
                Dictionary<string, string> cachedPrefecturePatterns = new();
                Dictionary<string, Dictionary<string, string>> cachedCityPatterns = new();
                PrefectureList cachedPrefectures = new PrefectureList();
                Dictionary<string, TownList> cachedTowns = new();
                Dictionary<string, string> cachedSameNamedPrefectureCityRegexPatterns = new();
                if (cacheData.cachedTownRegexes != null)
                {
                    foreach (var item in cacheData.cachedTownRegexes)
                    {
                        List<(SingleTown, string)> list = new();
                        foreach (var v in item.value)
                        {
                            SingleTown singleTown = new SingleTown
                            {
                                town = string.IsNullOrEmpty(v.town.town) ? null : v.town.town,
                                originalTown = string.IsNullOrEmpty(v.town.originalTown) ? null : v.town.originalTown,
                                koaza = string.IsNullOrEmpty(v.town.koaza) ? null : v.town.koaza,
                                gaiku = string.IsNullOrEmpty(v.town.gaiku) ? null : v.town.gaiku,
                                lat = string.IsNullOrEmpty(v.town.lat) ? null : double.Parse(v.town.lat),
                                lng = string.IsNullOrEmpty(v.town.lng) ? null : double.Parse(v.town.lng),
                            };
                            list.Add((singleTown, v.str));
                        }
                        cachedTownRegexes[item.key] = list;
                    }
                }
                if(cacheData.cachedPrefecturePatterns != null)
                {
                    foreach (var item in cacheData.cachedPrefecturePatterns)
                    {
                        cachedPrefecturePatterns[item.key] = item.value;
                    }
                }
                if(cacheData.cachedCityPatterns != null)
                {
                    foreach (var item in cacheData.cachedCityPatterns)
                    {
                        Dictionary<string, string> dict = new();
                        foreach (var v in item.value)
                        {
                            dict[v.key] = v.value;
                        }
                        cachedCityPatterns[item.key] = dict;
                    }
                }
                if(cacheData.cachedPrefectures != null)
                {
                    foreach (var item in cacheData.cachedPrefectures)
                    {
                        cachedPrefectures[item.key] = item.value;
                    }
                }
                if(cacheData.cachedTowns != null)
                {
                    foreach (var item in cacheData.cachedTowns)
                    {
                        TownList list = new();
                        foreach (var v in item.value)
                        {
                            SingleTown singleTown = new SingleTown
                            {
                                town = string.IsNullOrEmpty(v.town) ? null : v.town,
                                originalTown = string.IsNullOrEmpty(v.originalTown) ? null : v.originalTown,
                                koaza = string.IsNullOrEmpty(v.koaza) ? null : v.koaza,
                                gaiku = string.IsNullOrEmpty(v.gaiku) ? null : v.gaiku,
                                lat = string.IsNullOrEmpty(v.lat) ? null : double.Parse(v.lat),
                                lng = string.IsNullOrEmpty(v.lng) ? null : double.Parse(v.lng),
                            };
                            list.Add(singleTown);
                        }
                        cachedTowns[item.key] = list;
                    }
                }
                if(cacheData.cachedSameNamedPrefectureCityRegexPatterns != null)
                {
                    foreach (var item in cacheData.cachedSameNamedPrefectureCityRegexPatterns)
                    {
                        cachedSameNamedPrefectureCityRegexPatterns[item.key] = item.value;
                    }
                }

                CacheRegexes.SetInitCacheDatas(cachedTownRegexes, cachedPrefecturePatterns, cachedCityPatterns
                    , cachedPrefectures, cachedTowns, cachedSameNamedPrefectureCityRegexPatterns);
            }
            else
            {
                Debug.LogWarning("住所正規化キャッシュデータが見つかりません。");
            }

            isAddressDataLoaded = true;
        }

        public bool IsAddressDataLoaded()
        {
            return isAddressDataLoaded;
        }

        public bool IsFindingAddress()
        {
            return isFindingAddress;
        }
    }
}
