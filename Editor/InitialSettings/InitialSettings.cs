﻿using UnityEngine;
using System.Threading.Tasks;
using Landscape2.Runtime;
using PLATEAU.CityInfo;
using PLATEAU.GranularityConvert;
using PLATEAU.PolygonMesh;

namespace Landscape2.Editor
{
    /// <summary>
    /// 初期設定機能
    /// UIは<see cref="InitialSettingsWindow"/>が担当
    /// </summary>
    public class InitialSettings
    {
        // PLATEAUCityObjectGroupを持つGameObjectの配列
        private GameObject[] cityModelObjs;

        // SubComponentsが存在しない，つまり初期設定が未実行かを確認
        public bool IsSubComponentsNotExists()
        {
            var landscapeSubComponents = GameObject.FindObjectOfType<LandscapeSubComponents>();
            if (landscapeSubComponents != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

       　// 都市モデルがインポートされているかを確認
        public bool IsImportCityModelExists()
        {
            var plateauInstancedCityModel = GameObject.FindObjectOfType<PLATEAUInstancedCityModel>();
            if (plateauInstancedCityModel != null)
            {
                return true;
            }
            else
            { 
                return false;   
            }
        }

        // 都市モデルがSceneに存在するかを確認
        public bool IsCityObjectGroupExists()
        {
            var plateauCityObjectGroups = GameObject.FindObjectsOfType<PLATEAUCityObjectGroup>();
            cityModelObjs = new GameObject[plateauCityObjectGroups.Length];
            int id = 0;

            if (plateauCityObjectGroups.Length > 0)
            {
                // PLATEAUCityObjectGroupを持つGameObjectを取得
                foreach (var cityModel in plateauCityObjectGroups)
                {
                    cityModelObjs[id] = cityModel.gameObject;
                    id++;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        // SubComponentsを生成する
        public void CreateSubComponents()
        {
            var subComponentsObj = new GameObject("SubComponents");
            subComponentsObj.AddComponent<LandscapeSubComponents>();
        }

        // 初期設定を実行※仕様変更のため今は呼び出さない
        public async Task ExecuteInitialSettings()
        {
            // マテリアル分割の下準備として、都市オブジェクトを最小地物単位に分解
            var granularityConverter = new CityGranularityConverter();
            var granularityConvertConf = new GranularityConvertOptionUnity(
                new GranularityConvertOption(MeshGranularity.PerAtomicFeatureObject, 1),
                cityModelObjs, true
            );
            var result = await granularityConverter.ConvertAsync(granularityConvertConf);
            if (!result.IsSucceed)
            {
                Debug.LogError("ゲームオブジェクトの分解に失敗しました。");
                return;
            }
        }
    }
}