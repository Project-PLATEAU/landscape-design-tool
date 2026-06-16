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
using UnityEngine.Rendering;

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
            NormalizeBldgMeshesBeforeMaterialAdjust(cityModel != null ? cityModel.transform : null);

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

            var editingTilesPrefabRoot = tileManager.gameObject.transform.GetChildren().FirstOrDefault(m => m.name == "EditingTiles");

            if (editingTilesPrefabRoot == null)
            {
                Debug.LogError("ExecMaterialAdjustForTiles: EditingTiles（Prefab 展開用）が見つかりません。TilePrefabsToScene の後に PLATEAUTileManager 直下に EditingTiles があるか確認してください。");
                return;
            }

            NormalizeBldgMeshesBeforeMaterialAdjust(editingTilesPrefabRoot);

            var cityModelObjs = GetPLATEAUCityObjectGroups(editingTilesPrefabRoot);

            var bldgMaterialSaveCount = cityModelObjs.Count(CanSaveBuildingMaterialForTileMa);
            buildingMats = new Material[bldgMaterialSaveCount];

            var id = 0;
            foreach (var model in cityModelObjs)
            {
                if (!CanSaveBuildingMaterialForTileMa(model))
                {
                    continue;
                }

                var meshRenderer = model.gameObject.GetComponent<MeshRenderer>();
                var mats = meshRenderer.sharedMaterials;
                buildingMats[id] = mats[mats.Length - 1];
                id++;
            }

            // マテリアル分けの設定
            MaterialAdjustSettings(new UniqueParentTransformList(editingTilesPrefabRoot));

            try
            {
                // ここで実行
                await ExecMaterialAdjustAsync(maConfig, targetTransforms, false).ContinueWithErrorCatch();
            }
            catch (Exception e)
            {
                Debug.LogError("マテリアル分けに失敗しました。\n" + e);
            }

            var materialAdjustResultRoot = tileManager.gameObject.transform.GetChildren().LastOrDefault(m => m.name == "EditingTiles");

            // 参照が消えるので再取得
            tileManager = GameObject.FindFirstObjectByType<PLATEAUTileManager>();
            if (tileManager == null)
            {
                Debug.LogError("ExecMaterialAdjustForTiles: マテリアル分け後に PLATEAUTileManager が見つかりません。処理を中断します。");
                return;
            }

            cityModel = tileManager.GetComponentInChildren<PLATEAUInstancedCityModel>();

            // マテリアル分け結果を Project 上のタイル Prefab（ソース）へ書き戻す
            SyncMeshesAndMaterialsIfDualEditingTilesTrees(materialAdjustResultRoot, editingTilesPrefabRoot);

            // マテリアル分け後にはがれたマテリアルを再度設定
            cityModelObjs = GetPLATEAUCityObjectGroups(editingTilesPrefabRoot);
            id = 0;
            foreach (var model in cityModelObjs)
            {
                if (!CanSaveBuildingMaterialForTileMa(model))
                {
                    continue;
                }

                if (id >= buildingMats.Length)
                {
                    Debug.LogError("ExecMaterialAdjustForTiles: マテリアル再適用のインデックスが範囲外です（Hierarchy 変化で退避件数と不一致の可能性）。");
                    break;
                }

                var meshRenderer = model.gameObject.GetComponent<MeshRenderer>();
                var mats = meshRenderer.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    mats[i] = buildingMats[id];
                }

                meshRenderer.sharedMaterials = mats;
                id++;
            }

            // メッシュは反映したため、マテリアル分け結果のシーン上のツリーはもう不要なので削除
            if (materialAdjustResultRoot != null && materialAdjustResultRoot != editingTilesPrefabRoot)
            {
                GameObject.DestroyImmediate(materialAdjustResultRoot.gameObject);
            }

            // Prefab 側に埋め込み直す
            EditingTilePrefabEmbedding.EmbedSceneTilesIntoPrefabAssets(editingTilesPrefabRoot);

            // Rebuild 内の ApplyEditingTilesToPrefabs が「シーン→プレハブ」を上書きするため、
            // 埋め込み直後にシーン側に残ったオーバーライドでディスク上の埋め込みが潰れないよう、アセットを正に同期する。
            RevertEditingTilePrefabSceneInstancesToSavedAssets(editingTilesPrefabRoot);

            // タイルを再構築
            await new TileRebuilder().Rebuild(tileManager).ContinueWithErrorCatch();

            // 参照が消えるので再取得
            tileManager = GameObject.FindFirstObjectByType<PLATEAUTileManager>();

            cityModel = tileManager.GetComponentInChildren<PLATEAUInstancedCityModel>();
        }

        // 動的タイル機能が有効かを確認
        public bool IsTileManagerExists()
        {
            var tileManager = GameObject.FindFirstObjectByType<PLATEAUTileManager>();

            return tileManager != null;
        }

        // 編集用タイル内のPLATEAUCityObjectGroupをすべて取得
        private List<PLATEAUCityObjectGroup> GetPLATEAUCityObjectGroups(Transform editingTilesRoot)
        {
            return editingTilesRoot.GetComponentsInChildren<PLATEAUCityObjectGroup>().ToList();
        }

        // 動的タイル用マテリアル分けツリーのマテリアル退避・復元での同一判定。
        private static bool CanSaveBuildingMaterialForTileMa(PLATEAUCityObjectGroup model)
        {
            if (model == null || model.gameObject == null || !model.gameObject.name.Contains("bldg_"))
            {
                return false;
            }

            var meshRenderer = model.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                return false;
            }

            var mats = meshRenderer.sharedMaterials;
            return mats != null && mats.Length > 0;
        }

        // 埋め込み保存後、シーン上のタイル Prefab インスタンスのオーバーライドを破棄しプレハブアセットを正とする。
        // タイル再構築の際に古いシーン状態で上書きして埋め込みサブアセット参照が失われるのを防ぐ。
        private static void RevertEditingTilePrefabSceneInstancesToSavedAssets(Transform editingTilesRoot)
        {
            if (editingTilesRoot == null)
            {
                return;
            }

            var instanceRoots = new HashSet<GameObject>();
            foreach (var editingTile in editingTilesRoot.GetComponentsInChildren<PLATEAUEditingTile>(true))
            {
                if (editingTile == null)
                {
                    continue;
                }

                var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(editingTile.gameObject);
                if (instanceRoot == null || !PrefabUtility.IsPartOfPrefabInstance(instanceRoot))
                {
                    continue;
                }

                if (!instanceRoots.Add(instanceRoot))
                {
                    continue;
                }

                try
                {
                    PrefabUtility.RevertPrefabInstance(instanceRoot, InteractionMode.AutomatedAction);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"埋め込み後の Prefab 同期(Revert)に失敗しました: {instanceRoot.name}\n{e.Message}");
                }
            }
        }

        // マテリアル分け結果ツリーからメッシュ・マテリアル参照を Prefab 側インスタンスへ複写する。
        private static void SyncMeshesAndMaterialsIfDualEditingTilesTrees(Transform maResultRoot, Transform prefabInstanceRoot)
        {
            if (maResultRoot == null || prefabInstanceRoot == null || maResultRoot == prefabInstanceRoot)
            {
                return;
            }

            if (maResultRoot.childCount != prefabInstanceRoot.childCount)
            {
                Debug.LogWarning(
                    $"SyncMeshesAndMaterialsIfDualEditingTilesTrees: 子数が一致しないためスキップします " +
                    $"({maResultRoot.name} {maResultRoot.childCount} vs {prefabInstanceRoot.name} {prefabInstanceRoot.childCount})");
                return;
            }

            for (int i = 0; i < maResultRoot.childCount; i++)
            {
                SyncMeshesAndMaterialsRecursiveBetweenTrees(maResultRoot.GetChild(i), prefabInstanceRoot.GetChild(i));
            }
        }

        private static void SyncMeshesAndMaterialsRecursiveBetweenTrees(Transform src, Transform dst)
        {
            if (src == null || dst == null)
            {
                return;
            }

            var srcMf = src.GetComponent<MeshFilter>();
            var dstMf = dst.GetComponent<MeshFilter>();
            if (srcMf != null && dstMf != null && srcMf.sharedMesh != null)
            {
                dstMf.sharedMesh = srcMf.sharedMesh;
            }

            var srcSmr = src.GetComponent<SkinnedMeshRenderer>();
            var dstSmr = dst.GetComponent<SkinnedMeshRenderer>();
            if (srcSmr != null && dstSmr != null && srcSmr.sharedMesh != null)
            {
                dstSmr.sharedMesh = srcSmr.sharedMesh;
            }

            var srcMr = src.GetComponent<MeshRenderer>();
            var dstMr = dst.GetComponent<MeshRenderer>();
            if (srcMr != null && dstMr != null && srcMr.sharedMaterials != null && srcMr.sharedMaterials.Length > 0)
            {
                dstMr.sharedMaterials = (Material[])srcMr.sharedMaterials.Clone();
            }

            if (srcSmr != null && dstSmr != null && srcSmr.sharedMaterials != null && srcSmr.sharedMaterials.Length > 0)
            {
                dstSmr.sharedMaterials = (Material[])srcSmr.sharedMaterials.Clone();
            }

            if (src.childCount != dst.childCount)
            {
                return;
            }

            for (int i = 0; i < src.childCount; i++)
            {
                SyncMeshesAndMaterialsRecursiveBetweenTrees(src.GetChild(i), dst.GetChild(i));
            }
        }

        // マテリアル分け実行前に、建築物のメッシュを1サブメッシュにまとめ、マテリアルも1枚に揃える
        private static void NormalizeBldgMeshesBeforeMaterialAdjust(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var groups = root.GetComponentsInChildren<PLATEAUCityObjectGroup>(true);
            foreach (var cog in groups)
            {
                if (cog == null || !cog.name.Contains("bldg_"))
                {
                    continue;
                }

                var go = cog.gameObject;
                var mf = go.GetComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();
                if (mf == null || mr == null)
                {
                    continue;
                }

                var srcMesh = mf.sharedMesh;
                if (srcMesh == null)
                {
                    continue;
                }

                var mats = mr.sharedMaterials;
                if (mats == null || mats.Length == 0)
                {
                    continue;
                }

                Material keepMat = ResolveKeepMaterialForBldgNormalize(mats);
                if (keepMat == null)
                {
                    continue;
                }

                Material atlasTemplateMat = ResolvePlateauTriplanerTemplateForAtlas(mats) ?? keepMat;

                int subCount = srcMesh.subMeshCount;
                bool multiSub = subCount > 1;
                bool multiMat = mats.Length > 1;
                if (!multiSub && !multiMat)
                {
                    continue;
                }

                if (!multiSub)
                {
                    mr.sharedMaterials = new[] { keepMat };
                    continue;
                }

                Mesh combined;
                Material outMat;

                if (TryBuildAtlasedSingleMaterialMesh(srcMesh, mats, atlasTemplateMat, srcMesh.name + "_PreMAAtlased", out combined, out outMat))
                {
                    mf.sharedMesh = combined;
                    mr.sharedMaterials = new[] { outMat };
                }
                else
                {
                    var combine = new CombineInstance[subCount];
                    for (int i = 0; i < subCount; i++)
                    {
                        combine[i].mesh = srcMesh;
                        combine[i].subMeshIndex = i;
                        combine[i].transform = Matrix4x4.identity;
                    }

                    combined = new Mesh
                    {
                        name = srcMesh.name + "_PreMACombined",
                        indexFormat = IndexFormat.UInt32
                    };
                    combined.CombineMeshes(combine, mergeSubMeshes: true, useMatrices: true);
                    combined.RecalculateBounds();

                    mf.sharedMesh = combined;
                    mr.sharedMaterials = new[] { keepMat };
                }

                var mc = go.GetComponent<MeshCollider>();
                if (mc != null)
                {
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }

        // メインアルベド候補のプロパティ名（サブメッシュからテクスチャ取得・重複カウント用）
        private static readonly string[] MainAlbedoTexturePropertyNames =
        {
            "_Side_MainTexture", "_Top_MainTexture",
            "_BaseColorMap", "_BaseMap", "_MainTex", "_Diffuse"
        };

        // シェーダ名に Plateau トライプラナー系が含まれるか（マテリアル名は見ない）
        private static bool ShaderNameIndicatesPlateauTriplaner(Shader shader)
        {
            if (shader == null)
            {
                return false;
            }

            string sn = shader.name;
            return sn.IndexOf("PlateauTriplaner", StringComparison.OrdinalIgnoreCase) >= 0
                || sn.IndexOf("PlateauTriplanar", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPlateauTriplanerShaderMaterial(Material m)
        {
            return m != null && ShaderNameIndicatesPlateauTriplaner(m.shader);
        }

        // PLATEAU SDK / 同梱フォルダの「既定建物」マテリアルか。AssetDatabase.GetAssetPath のみで判定（マテリアル名・シェーダ名は使わない）
        // 同一 DualTextures シェーダでも、プロジェクト側のマテリアルを代表に選ぶためのタイブレーク
        private static bool IsBundledPlateauDefaultMaterialAsset(Material m)
        {
            if (m == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(m);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (path.IndexOf("PlateauSdkDefaultMaterials", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (path.IndexOf("PlateauDefaultBuilding_Wall.mat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("PlateauDefaultBuilding_Roof.mat", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        // サブメッシュ index に対応するマテリアル（Unity と同様。インデックス不足時は最後のスロット）
        private static Material GetMaterialForSubmesh(int submeshIndex, Material[] mats)
        {
            if (mats == null || mats.Length == 0)
            {
                return null;
            }

            int last = mats.Length - 1;
            if (submeshIndex < 0)
            {
                return mats[last];
            }

            return submeshIndex < mats.Length ? mats[submeshIndex] : mats[last];
        }

        // 代表マテリアル選択用。配列順に依存せず、DualTextures 優先 → 同梱既定アセットでない方 → シェーダ名 → マテリアル名 → InstanceID で安定ソート
        private static int CompareMaterialsForKeepPriority(Material a, Material b)
        {
            if (ReferenceEquals(a, b))
            {
                return 0;
            }

            if (a == null)
            {
                return 1;
            }

            if (b == null)
            {
                return -1;
            }

            bool dualA = a.shader != null &&
                a.shader.name.IndexOf("DualTextures", StringComparison.OrdinalIgnoreCase) >= 0;
            bool dualB = b.shader != null &&
                b.shader.name.IndexOf("DualTextures", StringComparison.OrdinalIgnoreCase) >= 0;
            if (dualA != dualB)
            {
                return dualB ? 1 : -1;
            }

            bool bundleA = IsBundledPlateauDefaultMaterialAsset(a);
            bool bundleB = IsBundledPlateauDefaultMaterialAsset(b);
            if (bundleA != bundleB)
            {
                return bundleA ? 1 : -1;
            }

            int cmp = string.Compare(a.shader != null ? a.shader.name : string.Empty,
                b.shader != null ? b.shader.name : string.Empty, StringComparison.Ordinal);
            if (cmp != 0)
            {
                return cmp;
            }

            cmp = string.Compare(a.name, b.name, StringComparison.Ordinal);
            if (cmp != 0)
            {
                return cmp;
            }

            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        }

        private static Material PickMaterialStable(IReadOnlyList<Material> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var list = new List<Material>();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null)
                {
                    list.Add(candidates[i]);
                }
            }

            if (list.Count == 0)
            {
                return null;
            }

            list.Sort(CompareMaterialsForKeepPriority);
            return list[0];
        }

        // 正規化後の代表マテリアル。スロット順に依存しない（候補を集めて安定ソート）
        // PlateauTriplaner シェーダのものを優先候補とし、その中で DualTextures・非同梱既定アセットを優先
        private static Material ResolveKeepMaterialForBldgNormalize(Material[] mats)
        {
            if (mats == null || mats.Length == 0)
            {
                return null;
            }

            var plateau = new List<Material>();
            var withAlbedo = new List<Material>();
            var any = new List<Material>();
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null)
                {
                    continue;
                }

                any.Add(m);
                if (IsPlateauTriplanerShaderMaterial(m))
                {
                    plateau.Add(m);
                }

                if (TryGetMainAlbedoTextureOrScan(m, out _))
                {
                    withAlbedo.Add(m);
                }
            }

            Material pick = PickMaterialStable(plateau);
            if (pick != null)
            {
                return pick;
            }

            pick = PickMaterialStable(withAlbedo);
            if (pick != null)
            {
                return pick;
            }

            return PickMaterialStable(any);
        }

        // アトラス用の CopyProperties 元。PlateauTriplaner シェーダのマテリアルのみから選ぶ（配列順に依存しない安定ソート）
        private static Material ResolvePlateauTriplanerTemplateForAtlas(Material[] mats)
        {
            if (mats == null || mats.Length == 0)
            {
                return null;
            }

            var plateau = new List<Material>();
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m != null && IsPlateauTriplanerShaderMaterial(m))
                {
                    plateau.Add(m);
                }
            }

            return PickMaterialStable(plateau);
        }

        // アトラスに載せるアルベド。PlateauTriplaner シェーダのマテリアルはトライプラナー用のためアトラス元に含めない
        private static bool TryGetAtlasSourceAlbedo(Material m, out Texture2D tex)
        {
            tex = null;
            if (m == null || IsPlateauTriplanerShaderMaterial(m))
            {
                return false;
            }

            return TryGetMainAlbedoTextureOrScan(m, out tex) && tex != null;
        }

        // アトラスは _Side_MainTexture_Atlas のみに割り当てる。_Side_MainTexture / _Top_MainTexture は CopyPropertiesFromMaterial のトライプラナー用テクスチャのまま（アトラスを入れると世界投影で全体がにじむ）。
        private const string AtlasSideTexturePropertyName = "_Side_MainTexture_Atlas";

        private const string AtlasBlendShaderGraphGuid = "b4e8f2a1c3d947658190ab12cd34ef02";

        // レガシー／非アトラスシェーダ向けフォールバック。_Top_MainTexture は含めない
        private static readonly string[] AtlasAssignAlbedoTexturePropertyNamesFallback =
        {
            "_Side_MainTexture",
            "_BaseColorMap", "_BaseMap", "_MainTex", "_Diffuse"
        };

        // プロパティがテクスチャスロットかを判定
        private static bool IsTextureShaderProperty(Material mat, string propertyName)
        {
            if (mat == null || string.IsNullOrEmpty(propertyName) || mat.shader == null || !mat.HasProperty(propertyName))
            {
                return false;
            }

            var shader = mat.shader;
            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (shader.GetPropertyName(i) != propertyName)
                {
                    continue;
                }

                return IsShaderPropertyTypeTexture(shader.GetPropertyType(i));
            }

            return false;
        }

        // シェーダープロパティ型がテクスチャ系かを判定
        private static bool IsShaderPropertyTypeTexture(ShaderPropertyType propertyType)
        {
            string n = propertyType.ToString();
            if (n == "Texture" || n == "TexEnv" || n == "Cubemap")
            {
                return true;
            }

            return n.IndexOf("Texture", StringComparison.Ordinal) >= 0;
        }

        private static bool TryGetMainAlbedoTexture(Material mat, out Texture2D tex)
        {
            tex = null;
            if (mat == null)
            {
                return false;
            }

            foreach (var prop in MainAlbedoTexturePropertyNames)
            {
                if (!IsTextureShaderProperty(mat, prop))
                {
                    continue;
                }

                if (mat.GetTexture(prop) is Texture2D t2d && t2d != null)
                {
                    tex = t2d;
                    return true;
                }
            }

            return false;
        }

        private static bool PropertyNameLikelyAlbedoMap(string propName)
        {
            if (string.IsNullOrEmpty(propName))
            {
                return false;
            }

            if (propName.IndexOf("BaseColor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (propName.IndexOf("MainTex", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (propName.IndexOf("Diffuse", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return propName.IndexOf("Albedo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool PropertyNameLikelyNotAlbedoMap(string propName)
        {
            if (string.IsNullOrEmpty(propName))
            {
                return true;
            }

            if (propName.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (propName.IndexOf("Mask", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (propName.IndexOf("Metallic", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (propName.IndexOf("Height", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (propName.IndexOf("Occlusion", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (propName.IndexOf("Emissive", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return propName.IndexOf("Specular", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 固定プロパティ名で取れないとき（PLATEAUX3DMaterialShader 等）、シェーダー宣言のテクスチャを走査してアルベド候補を探す
        private static bool TryScanMaterialTexturesForAlbedo(Material mat, out Texture2D tex)
        {
            tex = null;
            if (mat == null || mat.shader == null)
            {
                return false;
            }

            Shader sh = mat.shader;
            int n = sh.GetPropertyCount();
            Texture2D fallback = null;
            for (int i = 0; i < n; i++)
            {
                if (!IsShaderPropertyTypeTexture(sh.GetPropertyType(i)))
                {
                    continue;
                }

                string pname = sh.GetPropertyName(i);
                if (!mat.HasProperty(pname))
                {
                    continue;
                }

                if (mat.GetTexture(pname) is not Texture2D t2d || t2d == null)
                {
                    continue;
                }

                if (PropertyNameLikelyNotAlbedoMap(pname))
                {
                    continue;
                }

                if (PropertyNameLikelyAlbedoMap(pname))
                {
                    tex = t2d;
                    return true;
                }

                fallback ??= t2d;
            }

            tex = fallback;
            return tex != null;
        }

        private static bool TryGetMainAlbedoTextureOrScan(Material mat, out Texture2D tex)
        {
            if (TryGetMainAlbedoTexture(mat, out tex))
            {
                return true;
            }

            return TryScanMaterialTexturesForAlbedo(mat, out tex);
        }

        private static Texture2D CreateWhitePackTexture()
        {
            const int dim = 32;
            var t = new Texture2D(dim, dim, TextureFormat.RGBA32, false, false);
            var px = new Color[dim * dim];
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = Color.white;
            }

            t.SetPixels(px);
            t.Apply(false, false);
            return t;
        }

        // PackTextures 向けに RGBA32 の読み取り可能コピーを作る（非Readable／圧縮テクスチャ対策）
        private static Texture2D CreatePackableTextureCopy(Texture2D src)
        {
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, false);
            copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            copy.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        // アトラス上で「テクスチャ無し（白パディング）」領域とパック外を α=0 にし、
        // それ以外は α≥1 にしてシェーダ側で UV ベース色とトライプラナー色を Lerp できるようにする
        private static void ApplyAtlasAlphaMaskForTriplanar(Texture2D atlas, Rect[] rects, int whiteRectIndex)
        {
            if (atlas == null || rects == null || rects.Length == 0)
            {
                return;
            }

            int w = atlas.width;
            int h = atlas.height;
            Color[] px = atlas.GetPixels();

            // 各ピクセルの状態を rect 単位で塗る (O(W*H + Σrect面積))。
            // 元コードと同じ「最初の rect が勝つ」セマンティクスを保つため rect を末尾→先頭で走査し、
            // 元コードのピクセル中心点判定 u=(x+0.5)/w, xMin<=u<=xMax と完全一致するピクセル範囲に変換している。
            const byte StateOutside = 0;
            const byte StateWhite = 1;
            const byte StateNonWhite = 2;

            var state = new byte[w * h];

            for (int r = rects.Length - 1; r >= 0; r--)
            {
                Rect rc = rects[r];
                int xMin = Mathf.Max(0, Mathf.CeilToInt(rc.xMin * w - 0.5f));
                int xMax = Mathf.Min(w - 1, Mathf.FloorToInt(rc.xMax * w - 0.5f));
                int yMin = Mathf.Max(0, Mathf.CeilToInt(rc.yMin * h - 0.5f));
                int yMax = Mathf.Min(h - 1, Mathf.FloorToInt(rc.yMax * h - 0.5f));
                if (xMin > xMax || yMin > yMax)
                {
                    continue;
                }

                byte s = (whiteRectIndex >= 0 && r == whiteRectIndex) ? StateWhite : StateNonWhite;
                for (int y = yMin; y <= yMax; y++)
                {
                    int row = y * w;
                    for (int x = xMin; x <= xMax; x++)
                    {
                        state[row + x] = s;
                    }
                }
            }

            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                byte s = state[i];
                if (s == StateOutside || s == StateWhite)
                {
                    c.a = 0f;
                }
                else if (c.a < 1f)
                {
                    c.a = 1f;
                }
                px[i] = c;
            }

            atlas.SetPixels(px);
            // ミップを更新すると島同士のアルファが平均化され、トライプラナー用 α=0 が壊れる
            atlas.Apply(false, false);
        }

        private static void ConfigureRuntimeAtlasTexture(Texture2D atlas)
        {
            if (atlas == null)
            {
                return;
            }

            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.filterMode = FilterMode.Bilinear;
            atlas.anisoLevel = 0;
        }

        // Shader Graph (.shadergraph) は Resources.Load&lt;Shader&gt; では取得できない
        // グラフの m_Path「PlateauTriplanerShader」とファイル名から Shader.Find 名を試す
        private static Shader TryResolvePlateauTriplanarAtlasBlendShader()
        {
            string[] shaderNames =
            {
                "PlateauTriplanerShader/PlateauTriplanarDualTexturesAtlasBlend",
                "Shader Graphs/PlateauTriplanarDualTexturesAtlasBlend",
            };

            foreach (string name in shaderNames)
            {
                Shader s = Shader.Find(name);
                if (s != null)
                {
                    return s;
                }
            }

            Shader fromResources = Resources.Load<Shader>("Shaders/PlateauTriplanarDualTexturesAtlasBlend");
            if (fromResources != null)
            {
                return fromResources;
            }

            string graphPath = AssetDatabase.GUIDToAssetPath(AtlasBlendShaderGraphGuid);
            if (string.IsNullOrEmpty(graphPath))
            {
                return null;
            }

            foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(graphPath))
            {
                if (obj is Shader sub && sub.name.IndexOf("PlateauTriplanar", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return sub;
                }
            }

            return null;
        }

        // アトラス化したメッシュ用シェーダ。テンプレート名に「DualTextures」が無い単純な PlateauTriplaner でも
        // アトラス用グラフを試す（既定建物マテは DualTextures 以外の名前のことが多い）
        private static Shader ResolveShaderForAtlasedPlateauMaterial(Shader templateShader)
        {
            if (templateShader == null)
            {
                return null;
            }

            if (!ShaderNameIndicatesPlateauTriplaner(templateShader))
            {
                return templateShader;
            }

            Shader atlasShader = TryResolvePlateauTriplanarAtlasBlendShader();

            if (atlasShader == null)
            {
                Debug.LogWarning(
                    "[InitialSettings] アトラス用シェーダ PlateauTriplanarDualTexturesAtlasBlend を解決できません。" +
                    "テンプレートシェーダのままです。Shader.Find(PlateauTriplanerShader/PlateauTriplanarDualTexturesAtlasBlend) を確認してください。");
            }

            return atlasShader != null ? atlasShader : templateShader;
        }

        // シェーダが実際に持つ Texture プロパティ名のみ使う
        private static string FindAtlasSideTexturePropertyOnShader(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }

            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (shader.GetPropertyName(i) == AtlasSideTexturePropertyName)
                {
                    return AtlasSideTexturePropertyName;
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (!IsShaderPropertyTypeTexture(shader.GetPropertyType(i)))
                {
                    continue;
                }

                string pn = shader.GetPropertyName(i);
                if (pn.IndexOf("Atlas", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (pn.IndexOf("Top", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                return pn;
            }

            return null;
        }

        private static void LogShaderTexturePropertyDiagnostics(Shader shader, string context)
        {
            if (shader == null)
            {
                return;
            }

            int count = shader.GetPropertyCount();
            var sb = new System.Text.StringBuilder(256);
            sb.Append("[InitialSettings] ").Append(context).Append(" シェーダプロパティ数=").Append(count).Append(" | ");
            int max = Mathf.Min(count, 48);
            for (int i = 0; i < max; i++)
            {
                sb.Append(shader.GetPropertyName(i)).Append(':').Append(shader.GetPropertyType(i)).Append("; ");
            }

            if (count > max)
            {
                sb.Append("...");
            }

            Debug.LogWarning(sb.ToString());
        }

        private static void AssignAtlasToMaterial(Material mat, Texture2D atlas)
        {
            ConfigureRuntimeAtlasTexture(atlas);
            Shader sh = mat != null ? mat.shader : null;
            bool atlasBlendByName = sh != null &&
                sh.name.IndexOf("AtlasBlend", StringComparison.OrdinalIgnoreCase) >= 0;

            if (atlasBlendByName)
            {
                string graphPath = AssetDatabase.GUIDToAssetPath(AtlasBlendShaderGraphGuid);
                if (!string.IsNullOrEmpty(graphPath))
                {
                    AssetDatabase.ImportAsset(graphPath, ImportAssetOptions.ForceUpdate);
                    sh = mat.shader;
                }
            }

            string atlasSlot = FindAtlasSideTexturePropertyOnShader(sh);
            if (!string.IsNullOrEmpty(atlasSlot) && mat.HasProperty(atlasSlot))
            {
                mat.SetTexture(atlasSlot, atlas);
                return;
            }

            if (atlasBlendByName)
            {
                LogShaderTexturePropertyDiagnostics(sh, "アトラス用 Texture が見つかりません");
                Debug.LogWarning(
                    "[InitialSettings] アトラス合成シェーダにアトラス用 Texture プロパティが見つかりません。" +
                    "上記ログのプロパティ一覧を確認し、PlateauTriplanarDualTexturesAtlasBlend.shadergraph を保存して再コンパイルしてください。");
                return;
            }

            foreach (var prop in AtlasAssignAlbedoTexturePropertyNamesFallback)
            {
                if (!IsTextureShaderProperty(mat, prop))
                {
                    continue;
                }

                mat.SetTexture(prop, atlas);
            }
        }

        // サブメッシュごとに異なるメインテクスチャをアトラスにまとめ、単一サブメッシュ・単一マテリアルにする
        private static bool TryBuildAtlasedSingleMaterialMesh(Mesh srcMesh, Material[] mats, Material templateMat, string meshName, out Mesh outMesh, out Material outMaterial)
        {
            outMesh = null;
            outMaterial = null;

            int subCount = srcMesh.subMeshCount;
            if (subCount < 2)
            {
                return false;
            }

            var packCopies = new List<Texture2D>();
            var toDestroy = new List<Texture2D>();
            var origToPackIndex = new Dictionary<Texture2D, int>();
            bool anyNullTex = false;

            for (int sm = 0; sm < subCount; sm++)
            {
                var m = GetMaterialForSubmesh(sm, mats);
                if (!TryGetAtlasSourceAlbedo(m, out _))
                {
                    anyNullTex = true;
                }
            }

            int whiteIndex = -1;
            if (anyNullTex)
            {
                var white = CreateWhitePackTexture();
                whiteIndex = packCopies.Count;
                packCopies.Add(white);
                toDestroy.Add(white);
            }

            for (int sm = 0; sm < subCount; sm++)
            {
                var m = GetMaterialForSubmesh(sm, mats);
                if (!TryGetAtlasSourceAlbedo(m, out var origTex))
                {
                    continue;
                }

                if (origToPackIndex.ContainsKey(origTex))
                {
                    continue;
                }

                Texture2D copy;
                try
                {
                    copy = CreatePackableTextureCopy(origTex);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[InitialSettings] アトラス用テクスチャコピーに失敗しました: {origTex.name}\n{e.Message}");
                    foreach (var d in toDestroy)
                    {
                        UnityEngine.Object.DestroyImmediate(d);
                    }

                    return false;
                }

                int idx = packCopies.Count;
                packCopies.Add(copy);
                toDestroy.Add(copy);
                origToPackIndex[origTex] = idx;
            }

            if (origToPackIndex.Count + (anyNullTex ? 1 : 0) < 2)
            {
                foreach (var d in toDestroy)
                {
                    UnityEngine.Object.DestroyImmediate(d);
                }

                return false;
            }

            var atlas = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false, linear: false)
            {
                // エディタ作業中の一時テクスチャ。シーン保存に紛れ込まないようにし、
                // 埋め込みフロー(EmbedSceneTilesIntoPrefabAssets)で正式アセットに切替後に GC される。
                hideFlags = HideFlags.HideAndDontSave
            };
            Rect[] rects;
            try
            {
                // パディングを大きめにしてアトラス境界のバイリニアにじみを抑える
                rects = atlas.PackTextures(packCopies.ToArray(), 12, 4096, false);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[InitialSettings] PackTextures に失敗しました。\n{e.Message}");
                foreach (var d in toDestroy)
                {
                    UnityEngine.Object.DestroyImmediate(d);
                }

                UnityEngine.Object.DestroyImmediate(atlas);
                return false;
            }

            foreach (var d in toDestroy)
            {
                UnityEngine.Object.DestroyImmediate(d);
            }

            if (rects == null || rects.Length != packCopies.Count)
            {
                UnityEngine.Object.DestroyImmediate(atlas);
                return false;
            }

            ApplyAtlasAlphaMaskForTriplanar(atlas, rects, whiteIndex);
            ConfigureRuntimeAtlasTexture(atlas);

            int PackIndexForSubmesh(int sm)
            {
                var m = GetMaterialForSubmesh(sm, mats);
                if (!TryGetAtlasSourceAlbedo(m, out var origTex))
                {
                    return whiteIndex;
                }

                return origToPackIndex.TryGetValue(origTex, out var idx) ? idx : whiteIndex;
            }

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var uv4 = new List<Vector4>();
            var colors = new List<Color>();
            var indices = new List<int>();

            var srcUv4Buf = new List<Vector4>();
            srcMesh.GetUVs(3, srcUv4Buf);
            bool useUv4 = srcUv4Buf.Count == srcMesh.vertexCount;

            var srcColors = new List<Color>();
            srcMesh.GetColors(srcColors);
            bool useColors = srcColors.Count == srcMesh.vertexCount;

            var vSrc = srcMesh.vertices;
            var nSrc = srcMesh.normals;
            var uvSrc = srcMesh.uv;

            int aw = atlas.width;
            int ah = atlas.height;
            int vWrite = 0;
            for (int sm = 0; sm < subCount; sm++)
            {
                int pIdx = PackIndexForSubmesh(sm);
                if (pIdx < 0 || pIdx >= rects.Length)
                {
                    UnityEngine.Object.DestroyImmediate(atlas);
                    return false;
                }

                Rect r = rects[pIdx];
                // 矩形端のテクセルにサンプルが乗らないよう、UV を内側へ数ピクセル相当インセット
                float du = Mathf.Min(8f / aw, r.width * 0.48f);
                float dv = Mathf.Min(8f / ah, r.height * 0.48f);
                float uMin = r.xMin + du;
                float uMax = r.xMax - du;
                float vMin = r.yMin + dv;
                float vMax = r.yMax - dv;
                bool triplanarPaddingSlot = whiteIndex >= 0 && pIdx == whiteIndex;
                var subIdx = srcMesh.GetIndices(sm);
                for (int t = 0; t < subIdx.Length; t++)
                {
                    int vi = subIdx[t];
                    if (vi < 0 || vi >= vSrc.Length)
                    {
                        UnityEngine.Object.DestroyImmediate(atlas);
                        return false;
                    }

                    verts.Add(vSrc[vi]);
                    norms.Add(nSrc != null && nSrc.Length > vi ? nSrc[vi] : Vector3.up);
                    Vector2 ouv = uvSrc != null && uvSrc.Length > vi ? uvSrc[vi] : Vector2.zero;
                    Vector2 uvOut;
                    if (triplanarPaddingSlot)
                    {
                        // 面内で UV が動くと白島の外側（写真島）へバイリニア染み出し、壁がストライプ化する
                        uvOut = new Vector2((uMin + uMax) * 0.5f, (vMin + vMax) * 0.5f);
                    }
                    else
                    {
                        // Clamp01 だと UV>1 の壁面がアトラス辺に張り付きストライプになる。タイルは Repeat で正規化。
                        float u01 = Mathf.Repeat(ouv.x, 1f);
                        float v01 = Mathf.Repeat(ouv.y, 1f);
                        uvOut = new Vector2(Mathf.Lerp(uMin, uMax, u01), Mathf.Lerp(vMin, vMax, v01));
                    }

                    uvs.Add(uvOut);
                    uv4.Add(useUv4 ? srcUv4Buf[vi] : Vector4.zero);
                    colors.Add(useColors ? srcColors[vi] : Color.white);
                    indices.Add(vWrite++);
                }
            }

            outMesh = new Mesh
            {
                name = meshName,
                indexFormat = IndexFormat.UInt32
            };
            outMesh.SetVertices(verts);
            outMesh.SetNormals(norms);
            outMesh.SetUVs(0, uvs);
            if (useUv4)
            {
                outMesh.SetUVs(3, uv4);
            }

            if (useColors)
            {
                outMesh.SetColors(colors);
            }

            outMesh.SetTriangles(indices, 0);
            outMesh.RecalculateBounds();
            outMesh.RecalculateTangents();

            Shader shaderForAtlas = ResolveShaderForAtlasedPlateauMaterial(templateMat.shader);
            outMaterial = new Material(shaderForAtlas)
            {
                // 一時マテリアル。埋め込みフローで正式アセット化されるまでの間だけ保持する。
                hideFlags = HideFlags.HideAndDontSave
            };
            outMaterial.CopyPropertiesFromMaterial(templateMat);
            AssignAtlasToMaterial(outMaterial, atlas);

            return true;
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