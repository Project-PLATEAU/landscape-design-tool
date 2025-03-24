using UnityEngine;
using System.Threading.Tasks;
using Landscape2.Runtime;
using PLATEAU.CityInfo;
using PLATEAU.CityAdjust.MaterialAdjust.ExecutorV2;
using PLATEAU.CityAdjust.MaterialAdjust.Executor;
using System;
using PLATEAU.Util;
using PLATEAU.CityAdjust.MaterialAdjust;
using PLATEAU.Util.Async;
using PlateauToolkit.Rendering;
using System.Linq;

namespace Landscape2.Editor
{
    /// <summary>
    /// 初期設定機能
    /// UIは<see cref="InitialSettingsWindow"/>が担当
    /// </summary>
    public class InitialSettings
    {
        // PLATEAUCityObjectGroupを持つオブジェクトの配列
        private Material[] buildingMats;

        private PLATEAUCityObjectGroup[] plateauCityObjectGroups;
        private PLATEAUInstancedCityModel cityModel;
        private BIMImportMaterialReference bimImportMaterialReference;
        private IMAConfig maConfig;
        private UniqueParentTransformList targetTransforms;
        private GameObject environment;
        Material[] defaultMaterials = new Material[2];

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
            cityModel = GameObject.FindObjectOfType<PLATEAUInstancedCityModel>();
            if (cityModel != null)
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
            plateauCityObjectGroups = GameObject.FindObjectsOfType<PLATEAUCityObjectGroup>();
            buildingMats = new Material[plateauCityObjectGroups.Length];

            if (plateauCityObjectGroups.Length > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsBIMImportMaterialReferenceExists()
        {
            bimImportMaterialReference = GameObject.FindObjectsByType<BIMImportMaterialReference>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            return bimImportMaterialReference != null;
        }

        // SubComponentsを生成する
        public void CreateSubComponents()
        {
            var subComponentsObj = new GameObject("SubComponents");
            subComponentsObj.AddComponent<LandscapeSubComponents>();
        }

        // Environmentの生成が可能かを確認
        public bool IsCreateEnvironmentPossible()
        {
            // ResourcesからEnvironmentプレハブを読み込み生成する
            environment = Resources.Load("Environments") as GameObject;
            return environment != null;
        }
        // Environmentを生成する
        public void CreateEnvironment()
        {
            GameObject environmentObj; // Environmentプレハブを格納するGameObject
            var environmentController = GameObject.FindObjectOfType<EnvironmentController>();

            // EnvironmentControllerがSceneに存在する場合は
            if (environmentController != null)
            {
                GameObject.DestroyImmediate(environmentController.gameObject);
            }
            if (environment != null)
            {
                environmentObj = GameObject.Instantiate(environment);
                if (environmentObj == null)
                {
                    Debug.LogError("Environmentの生成に失敗しました。");
                }
                environmentObj.name = environment.name;
            }
        }

        // BimImport用material参照gameobjectを置く
        public void CreateBIMImportMaterialSetting()
        {

            var res = Resources.Load<BIMImportMaterialReference>("BimImportMaterialReference");
            var obj = GameObject.Instantiate(res);
            obj.name = nameof(BIMImportMaterialReference);
        }

        // MainCameraを生成する
        public void CreateMainCamera()
        {
            var mainCamera = Camera.main;
            // SceneにMainCameraが存在しない場合生成
            if (mainCamera == null)
            {
                var mainCameraObj = new GameObject("MainCamera");
                mainCameraObj.tag = "MainCamera";
                mainCamera = mainCameraObj.AddComponent<Camera>();
            }
            // カメラの設定
            mainCamera.farClipPlane = 3000f;
        }

        // マテリアル分けを実行
        public async Task ExecMaterialAdjust()
        {
            int id = 0;
            // PLATEAUCityObjectGroupを持つGameObjectを取得
            // cityModelの子オブジェクト全てを取得
            var cityModelObjs = cityModel.GetComponentsInChildren<PLATEAUCityObjectGroup>();

            foreach (var model in cityModelObjs)
            {
                // 建築物のオブジェクトのマテリアルを取得
                if (model.name.Contains("bldg_"))
                {
                    // マテリアル分け前の都市モデルのマテリアルの最後の要素を取得
                    var mats = model.gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                    buildingMats[id] = mats[mats.Length - 1];
                    id++;
                }
            }

            // マテリアル分けの設定
            MaterialAdjustSettings();

            try
            {
                // ここで実行
                await ExecMaterialAdjustAsync(maConfig, targetTransforms).ContinueWithErrorCatch();
            }
            catch (Exception e)
            {
                Debug.LogError("マテリアル分けに失敗しました。\n" + e);
            }

            id = 0;

            // マテリアル分け後にはがれたマテリアルを再度設定
            // マテリアル分け後に都市モデルのオブジェクトの参照が消えるため，再度取得する
            cityModel = GameObject.FindObjectOfType<PLATEAUInstancedCityModel>();
            cityModelObjs = cityModel.GetComponentsInChildren<PLATEAUCityObjectGroup>();
            foreach (var model in cityModelObjs)
            {
                if (model.gameObject.name.Contains("bldg_"))
                {
                    var mats = model.gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = buildingMats[id];
                    }
                    model.gameObject.GetComponent<MeshRenderer>().sharedMaterials = mats;
                    id++;
                }
            }
        }

        // マテリアル分けの設定
        private void MaterialAdjustSettings()
        {
            // Sceneに存在する都市モデルのTransformのリストを取得
            targetTransforms = new UniqueParentTransformList(cityModel.gameObject.transform);

            // リスト内のマテリアル分け可能な都市モデルを取得
            var searchArg = new SearchArg(targetTransforms);

            // マテリアル分け可能な種類を検索
            var searcher = new TypeSearcher(searchArg);

            // 検索結果を階層構造のノードに格納
            CityObjectTypeHierarchy.Node[] node = searcher.Search();
            // マテリアル分け設定値を取得
            maConfig = new MAMaterialConfig<CityObjectTypeHierarchy.Node>(node);

            // 都市モデルの壁面と屋根面のデフォルトマテリアルを取得
            defaultMaterials[0] = Resources.Load("PlateauDefaultBuilding_Wall") as Material;
            defaultMaterials[1] = Resources.Load("PlateauDefaultBuilding_Roof") as Material;

            int id = 0;
            // 壁面と屋根面のマテリアル分けを有効にする
            for (int i = 0; i < maConfig.Length; i++)
            {
                if (maConfig.GetKeyNameAt(i) == "建築物 (Building)/壁面 (WallSurface)" ||
                    maConfig.GetKeyNameAt(i) == "建築物 (Building)/屋根面 (RoofSurface)")
                {
                    maConfig.GetMaterialChangeConfAt(i).ChangeMaterial = true;
                    // 分割後に割り当てるマテリアルを設定
                    maConfig.GetMaterialChangeConfAt(i).Material = defaultMaterials[id];
                    id++;
                }
            }
        }

        private async Task<UniqueParentTransformList> ExecMaterialAdjustAsync(IMAConfig MAConfig, UniqueParentTransformList targetTransforms)
        {
            var conf = new MAExecutorConf(MAConfig, targetTransforms, true, true);

            // マテリアル分け
            return await ExecMaterialAdjustAsyncInner(conf);
        }

        private async Task<UniqueParentTransformList> ExecMaterialAdjustAsyncInner(MAExecutorConf conf)
        {
            await Task.Delay(100);
            await Task.Yield();

            IMAExecutorV2 maExecutor = new MAExecutorV2ByType();

            var result = await maExecutor.ExecAsync(conf);

            // Sceneに存在する都市モデルのTransformのリストを取得
            return result;
        }
    }
}