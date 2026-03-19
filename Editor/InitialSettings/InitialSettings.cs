using CesiumForUnity;
using Landscape2.Runtime;
using PLATEAU.CityAdjust.MaterialAdjust;
using PLATEAU.CityAdjust.MaterialAdjust.Executor;
using PLATEAU.CityAdjust.MaterialAdjust.ExecutorV2;
using PLATEAU.CityInfo;
using PLATEAU.DynamicTile;
using PLATEAU.Editor.DynamicTile;
using PLATEAU.Native;
using PLATEAU.Util;
using PLATEAU.Util.Async;
using PlateauToolkit.Rendering;
using PlateauToolkit.Sandbox.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

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
            var landscapeSubComponents = GameObject.FindFirstObjectByType<LandscapeSubComponents>();
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
            cityModel = GameObject.FindFirstObjectByType<PLATEAUInstancedCityModel>();
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
            plateauCityObjectGroups = GameObject.FindObjectsByType<PLATEAUCityObjectGroup>(FindObjectsSortMode.None);
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
            var environmentController = GameObject.FindFirstObjectByType<EnvironmentController>();

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
                mainCameraObj.AddComponent<AudioListener>();
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
            cityModel = GameObject.FindFirstObjectByType<PLATEAUInstancedCityModel>();
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

        // マテリアル分けを実行（動的タイル対応）
        public async Task ExecMaterialAdjustForTiles()
        {
            var tileManager = GameObject.FindFirstObjectByType<PLATEAUTileManager>();

            if (tileManager == null)
            {
                Debug.LogError("PLATEAUTileManagerが存在しません。");

                return;
            }

            cityModel = tileManager.GetComponentInChildren<PLATEAUInstancedCityModel>();

            // マテリアル分け前にタイルの元となったプレハブをすべてシーンに展開

            using (var dummyCancelTokenSource = new CancellationTokenSource())
            {
                var dummyCancelToken = dummyCancelTokenSource.Token;
                await new TileRebuilder().TilePrefabsToScene(tileManager, dummyCancelToken).ContinueWithErrorCatch();
            }
            // マテリアル退避用の配列を初期化
            buildingMats = new Material[GetPLATEAUCityObjectGroups(GetEditingTilesRoot(true)).Count];

            int id = 0;

            var cityModelObjs = GetPLATEAUCityObjectGroups(GetEditingTilesRoot(true));

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
            MaterialAdjustSettings(new UniqueParentTransformList(GetEditingTilesRoot()));

            try
            {
                // ここで実行
                await ExecMaterialAdjustAsync(maConfig, targetTransforms, false).ContinueWithErrorCatch();
            }
            catch (Exception e)
            {
                Debug.LogError("マテリアル分けに失敗しました。\n" + e);
            }

            id = 0;

            // 参照が消えるので再取得
            tileManager = GameObject.FindFirstObjectByType<PLATEAUTileManager>();

            cityModel = tileManager.GetComponentInChildren<PLATEAUInstancedCityModel>();

            // マテリアル分け後にはがれたマテリアルを再度設定
            cityModelObjs = GetPLATEAUCityObjectGroups(GetEditingTilesRoot(true));
            foreach (var model in cityModelObjs.Where(m => m.gameObject.name.Contains("bldg_")))
            {
                var mats = model.gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = buildingMats[id];
                }
                model.gameObject.GetComponent<MeshRenderer>().sharedMaterials = mats;
                id++;
            }

            // Reflect meshes as embedded in the Prefab parent
            ApplyMeshesAsEmbeddedToPrefabParent(GetEditingTilesRoot(false), GetEditingTilesRoot(true));

            // タイルを再構築
            await new TileRebuilder().Rebuild(tileManager).ContinueWithErrorCatch();

            // 参照が消えるので再取得
            tileManager = GameObject.FindFirstObjectByType<PLATEAUTileManager>();

            cityModel = tileManager.GetComponentInChildren<PLATEAUInstancedCityModel>();

            // シーンに展開されているプレハブなどがあれば後始末する

            var children = tileManager.transform.GetChildren();

            foreach (var child in children)
            {
                if (child == null) continue;

                if (child.name == "EditingTiles")
                {
                    child.gameObject.hideFlags = HideFlags.None;

                    Undo.DestroyObjectImmediate(child.gameObject);

                }
            }
        }

        // 動的タイル機能が有効かを確認
        public bool IsTileManagerExists()
        {
            var tileManager = GameObject.FindFirstObjectByType<PLATEAUTileManager>();

            return tileManager != null;
        }

        // 編集用タイルのルートオブジェクトを取得
        // isPrefabがtrueの場合はPrefab用，falseの場合はマテリアル分割結果用を取得
        private Transform GetEditingTilesRoot(bool isPrefab = false)
        {
            var tileManager = GameObject.FindFirstObjectByType<PLATEAUTileManager>();
            if (tileManager == null)
            {
                Debug.LogWarning("PLATEAUTileManager not found in the scene. Cannot get EditingTiles root.");
                return null;
            }

            return isPrefab ? tileManager.gameObject.transform.GetChildren().FirstOrDefault(m => m.name == "EditingTiles") : tileManager.gameObject.transform.GetChildren().LastOrDefault(m => m.name == "EditingTiles");
        }

        // 編集用タイル内のPLATEAUCityObjectGroupをすべて取得
        private List<PLATEAUCityObjectGroup> GetPLATEAUCityObjectGroups(Transform editingTilesRoot)
        {
            return editingTilesRoot.GetComponentsInChildren<PLATEAUCityObjectGroup>().ToList();
        }

        // マテリアル分割結果のmeshをPrefab内に埋め込みとして反映
        private static void ApplyMeshesAsEmbeddedToPrefabParent(Transform srcParent, Transform dstParent)
        {
            if (srcParent.childCount != dstParent.childCount)
            {
                Debug.LogWarning(
                    $"ApplyMeshesAsEmbeddedToPrefabParent skipped: child count mismatch between srcParent ('{srcParent.name}', {srcParent.childCount}) and dstParent ('{dstParent.name}', {dstParent.childCount}).");
                return;
            }

            for (int i = 0; i < srcParent.childCount; i++)
            {
                ApplyMeshesAsEmbeddedToPrefab(srcParent.GetChild(i), PrefabUtility.GetCorrespondingObjectFromSource(dstParent.GetChild(i).gameObject));
            }
        }

        // Scene 上の srcParent 以下の Mesh を、
        // Project 上の Prefab(dstPrefabAsset) の同階層に反映する
        // 反映時に、Prefab 内に Mesh サブアセットを作成して差し替える
        private static void ApplyMeshesAsEmbeddedToPrefab(Transform srcParent, GameObject dstPrefabAsset)
        {
            if (srcParent == null)
            {
                Debug.LogError("srcParent が null です");
                return;
            }

            if (dstPrefabAsset == null)
            {
                Debug.LogError("dstPrefabAsset が null です");
                return;
            }

            var prefabPath = AssetDatabase.GetAssetPath(dstPrefabAsset);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("dstPrefabAsset は Project 上の Prefab ではありません");
                return;
            }

            // Prefab 内容を一時ロード
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"Prefab をロードできません: {prefabPath}");
                return;
            }

            try
            {
                // src 側の Mesh を相対パスで辞書化
                var srcMFs = srcParent.GetComponentsInChildren<MeshFilter>(true);
                var srcSMRs = srcParent.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                var srcMFMap = new Dictionary<string, Mesh>(); // path -> Mesh
                var srcSMRMap = new Dictionary<string, Mesh>(); // path -> Mesh

                foreach (var mf in srcMFs)
                {
                    if (mf.sharedMesh == null) continue;
                    var path = GetRelativePath(mf.transform, srcParent);
                    srcMFMap[path] = mf.sharedMesh;
                }

                foreach (var smr in srcSMRs)
                {
                    if (smr.sharedMesh == null) continue;
                    var path = GetRelativePath(smr.transform, srcParent);
                    srcSMRMap[path] = smr.sharedMesh;
                }

                int updatedMF = 0;
                int updatedSMR = 0;

                // Prefab 側 MeshFilter
                var dstMFs = prefabRoot.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in dstMFs)
                {
                    var path = GetRelativePath(mf.transform, prefabRoot.transform);
                    if (!srcMFMap.TryGetValue(path, out var srcMesh)) continue;

                    // Scene 側 Mesh から Prefab 内サブアセット Mesh を作成
                    var newMesh = CreateEmbeddedMesh(srcMesh, prefabPath, path, mf.sharedMesh);
                    if (newMesh == null) continue;

                    mf.sharedMesh = newMesh;
                    EditorUtility.SetDirty(mf);
                    updatedMF++;
                }

                // Prefab 側 SkinnedMeshRenderer
                var dstSMRs = prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in dstSMRs)
                {
                    var path = GetRelativePath(smr.transform, prefabRoot.transform);
                    if (!srcSMRMap.TryGetValue(path, out var srcMesh)) continue;

                    var newMesh = CreateEmbeddedMesh(srcMesh, prefabPath, path, smr.sharedMesh);
                    if (newMesh == null) continue;

                    smr.sharedMesh = newMesh;
                    EditorUtility.SetDirty(smr);
                    updatedSMR++;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"Prefab 内に埋め込み Mesh を作成して更新しました: {prefabPath}\n" +
                    $"MeshFilter: {updatedMF} 個, SkinnedMeshRenderer: {updatedSMR} 個"
                );
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        // target から root への相対パスを取得
        private static string GetRelativePath(Transform target, Transform root)
        {
            if (target == root) return "";

            var stack = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                stack.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", stack);
        }

        // Scene 側 Mesh からコピーを作り、Prefab 内サブアセットとして追加して返す
        // 既存 Mesh（FBX 由来）を壊さずに差し替える
        private static Mesh CreateEmbeddedMesh(Mesh srcMesh, string prefabPath, string relativePath, Mesh oldDstMesh)
        {
            if (srcMesh == null) return null;

            // NOTE: 毎回新規に埋め込むが、Prefab 内に同名の Mesh がある場合は差し替えしたほうがよいかもしれない
            var prefabObject = AssetDatabase.LoadMainAssetAtPath(prefabPath);
            if (prefabObject == null)
            {
                Debug.LogError($"Prefab アセットが取得できません: {prefabPath}");
                return null;
            }

            var newMesh = GameObject.Instantiate(srcMesh);

            newMesh.name = (oldDstMesh != null ? oldDstMesh.name : srcMesh.name) + "_Embedded";

            AssetDatabase.AddObjectToAsset(newMesh, prefabObject);
            EditorUtility.SetDirty(newMesh);

            Debug.Log($"Prefab 内に Mesh サブアセットを追加しました: {prefabPath} :: {newMesh.name}");

            return newMesh;
        }

        // マテリアル分けの設定
        private void MaterialAdjustSettings(UniqueParentTransformList uniqueParentTransformList = null)
        {
            // Sceneに存在する都市モデルのTransformのリストを取得
            targetTransforms = uniqueParentTransformList != null ? uniqueParentTransformList : new UniqueParentTransformList(cityModel.gameObject.transform);

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

        private async Task<UniqueParentTransformList> ExecMaterialAdjustAsync(IMAConfig MAConfig, UniqueParentTransformList targetTransforms, bool destroy = true)
        {
            var conf = new MAExecutorConf(MAConfig, targetTransforms, destroy, true);

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

        // Cesiumの地形モデルを設定
        public void SetupCesiumTerrain()
        {
            if (cityModel != null)
            {
                // 既存のCesiumGeoreferenceを探して削除
                var existingGeoRef = GameObject.FindFirstObjectByType<CesiumGeoreference>();
                if (existingGeoRef != null)
                {
                    GameObject.DestroyImmediate(existingGeoRef.gameObject);
                }

                // 既存のCesium3DTilesetを探して削除
                var existingTileset = GameObject.FindFirstObjectByType<Cesium3DTileset>();
                if (existingTileset != null)
                {
                    GameObject.DestroyImmediate(existingTileset.gameObject);
                }

                // Georeferenceを作成
                GameObject geoRefGo = new GameObject("CesiumGeoreference");
                CesiumGeoreference geoRef = geoRefGo.AddComponent<CesiumGeoreference>();

                // CityModelの緯度経度を設定
                var coordinate = cityModel.GeoReference.Unproject(new PlateauVector3d(0, 0, 0));
                geoRef.latitude = coordinate.Latitude;
                geoRef.longitude = coordinate.Longitude;
                geoRef.height = coordinate.Height;

                // 3DTilesetを作成
                GameObject tilesetGO = new GameObject("Cesium3DTileset");
                Cesium3DTileset tileset = tilesetGO.AddComponent<Cesium3DTileset>();

                // タイルセットの設定
                tileset.tilesetSource = CesiumDataSource.FromCesiumIon;

                // Georeferenceの子にする
                tilesetGO.transform.SetParent(geoRefGo.transform, false);
                tilesetGO.transform.localPosition = Vector3.zero;
            }
        }

        // PLATEAU SDK for Toolkitのサンプルアセットの準備
        public void PreparePlateauSamples()
        {
            if (PlateauSandboxAssetUtility.GetSample(out Sample sample))
            {
                if (!sample.isImported)
                {
                    sample.Import();
                }
            }
        }
    }
}