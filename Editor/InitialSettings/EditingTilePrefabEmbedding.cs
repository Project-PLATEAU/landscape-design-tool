using PLATEAU.DynamicTile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Landscape2.Editor
{
    // シーン上に展開された PLATEAUEditingTile の Mesh / Material / Texture を、
    // 対応する Prefab アセット内のサブアセットとして埋め込みます。
    public static class EditingTilePrefabEmbedding
    {
        // ツリーの状態を Prefab 側に埋め込み直す
        public static void EmbedSceneTilesIntoPrefabAssets(Transform editingTilesRoot)
        {
            if (editingTilesRoot == null)
            {
                Debug.LogWarning("EditingTilePrefabEmbedding: editingTilesRoot is null.");
                return;
            }

            var editingTiles = editingTilesRoot.GetComponentsInChildren<PLATEAUEditingTile>(true);
            if (editingTiles == null || editingTiles.Length == 0)
            {
                Debug.LogWarning($"EditingTilePrefabEmbedding: PLATEAUEditingTile が見つかりません: {editingTilesRoot.name}");
                return;
            }

            var processedPrefabPaths = new HashSet<string>();
            var processedInstanceIds = new HashSet<int>();

            foreach (var editingTile in editingTiles)
            {
                if (editingTile == null) continue;

                var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(editingTile.gameObject);
                if (instanceRoot == null)
                {
                    continue;
                }

                if (!PrefabUtility.IsPartOfPrefabInstance(instanceRoot))
                {
                    continue;
                }

                // 同じ Prefab instance root を重複処理しない
                if (!processedInstanceIds.Add(instanceRoot.GetInstanceID()))
                {
                    continue;
                }

                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    Debug.LogWarning($"EditingTilePrefabEmbedding: Prefab パスを取得できません: {instanceRoot.name}");
                    continue;
                }

                // 1 Prefab につき 1 回だけ処理する
                if (!processedPrefabPaths.Add(prefabPath))
                {
                    continue;
                }

                EmbedSceneAssetsIntoPrefabAsset(instanceRoot.transform, prefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 1つの Prefab instance root の Mesh / Material / Texture を Prefab アセットへ埋め込みます。
        private static void EmbedSceneAssetsIntoPrefabAsset(Transform sceneInstanceRoot, string prefabPath)
        {
            if (sceneInstanceRoot == null)
            {
                Debug.LogWarning("EditingTilePrefabEmbedding: sceneInstanceRoot is null");
                return;
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogWarning("EditingTilePrefabEmbedding: prefabPath is empty");
                return;
            }

            var prefabAsset = AssetDatabase.LoadMainAssetAtPath(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"EditingTilePrefabEmbedding: Prefab アセットが取得できません: {prefabPath}");
                return;
            }

            var prefabContentsRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabContentsRoot == null)
            {
                Debug.LogError($"EditingTilePrefabEmbedding: Prefab content could not be loaded: {prefabPath}");
                return;
            }

            try
            {
                EmbedMeshesDirectly(sceneInstanceRoot, prefabContentsRoot, prefabPath);
                EmbedMaterialsAndTexturesDirectly(sceneInstanceRoot, prefabContentsRoot, prefabPath);

                EditorUtility.SetDirty(prefabContentsRoot);
                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabContentsRoot, prefabPath);
                if (savedPrefab == null)
                {
                    Debug.LogError($"EditingTilePrefabEmbedding: Prefab could not be saved: {prefabPath}");
                    return;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
        }
        private static void EmbedMeshesDirectly(Transform sceneInstanceRoot, GameObject prefabContentsRoot, string prefabPath)
        {
            var srcMFCompMap = new Dictionary<string, MeshFilter>();
            var srcMFCompByLoosePath = new Dictionary<string, MeshFilter>();
            var srcSMRCompMap = new Dictionary<string, SkinnedMeshRenderer>();
            var srcSMRCompByLoosePath = new Dictionary<string, SkinnedMeshRenderer>();

            var srcMFMap = new Dictionary<string, Mesh>();
            var srcMFByLoosePath = new Dictionary<string, Mesh>();
            var srcSMRMap = new Dictionary<string, Mesh>();
            var srcSMRByLoosePath = new Dictionary<string, Mesh>();

            foreach (var mf in sceneInstanceRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                var stable = GetStableHierarchyPath(mf.transform, sceneInstanceRoot);
                var loose = GetNameOnlyHierarchyPath(mf.transform, sceneInstanceRoot);

                if (mf.sharedMesh != null)
                {
                    srcMFMap[stable] = mf.sharedMesh;
                    srcMFByLoosePath[loose] = mf.sharedMesh;
                    srcMFCompMap[stable] = mf;
                    srcMFCompByLoosePath[loose] = mf;
                }
            }

            foreach (var smr in sceneInstanceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var stable = GetStableHierarchyPath(smr.transform, sceneInstanceRoot);
                var loose = GetNameOnlyHierarchyPath(smr.transform, sceneInstanceRoot);

                if (smr.sharedMesh != null)
                {
                    srcSMRMap[stable] = smr.sharedMesh;
                    srcSMRByLoosePath[loose] = smr.sharedMesh;
                    srcSMRCompMap[stable] = smr;
                    srcSMRCompByLoosePath[loose] = smr;
                }
            }

            foreach (var dstMf in prefabContentsRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                var stable = GetStableHierarchyPath(dstMf.transform, prefabContentsRoot.transform);
                var loose = GetNameOnlyHierarchyPath(dstMf.transform, prefabContentsRoot.transform);

                if (!TryGetByStableThenLoose(srcMFMap, srcMFByLoosePath, stable, loose, out var srcMesh))
                    continue;

                var embedded = CreateEmbeddedMesh(srcMesh, prefabPath, stable, dstMf.sharedMesh);
                if (embedded == null)
                    continue;

                dstMf.sharedMesh = embedded;
                if (TryGetByStableThenLoose(srcMFCompMap, srcMFCompByLoosePath, stable, loose, out var sceneMf) && sceneMf != null)
                {
                    sceneMf.sharedMesh = embedded;
                    EditorUtility.SetDirty(sceneMf);
                }

                EditorUtility.SetDirty(dstMf);
            }

            foreach (var dstSmr in prefabContentsRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var stable = GetStableHierarchyPath(dstSmr.transform, prefabContentsRoot.transform);
                var loose = GetNameOnlyHierarchyPath(dstSmr.transform, prefabContentsRoot.transform);

                if (!TryGetByStableThenLoose(srcSMRMap, srcSMRByLoosePath, stable, loose, out var srcMesh))
                    continue;

                var embedded = CreateEmbeddedMesh(srcMesh, prefabPath, stable, dstSmr.sharedMesh);
                if (embedded == null)
                    continue;

                dstSmr.sharedMesh = embedded;
                if (TryGetByStableThenLoose(srcSMRCompMap, srcSMRCompByLoosePath, stable, loose, out var sceneSmr) && sceneSmr != null)
                {
                    sceneSmr.sharedMesh = embedded;
                    EditorUtility.SetDirty(sceneSmr);
                }
                EditorUtility.SetDirty(dstSmr);
            }
        }

        private static void EmbedMaterialsAndTexturesDirectly(Transform sceneInstanceRoot, GameObject prefabContentsRoot, string prefabPath)
        {
            var srcMRCompMap = new Dictionary<string, MeshRenderer>();
            var srcMRCompByLoosePath = new Dictionary<string, MeshRenderer>();
            var srcSMRCompMap = new Dictionary<string, SkinnedMeshRenderer>();
            var srcSMRCompByLoosePath = new Dictionary<string, SkinnedMeshRenderer>();

            var materialCache = new Dictionary<int, Material>();
            var textureCache = new Dictionary<int, Texture>();

            var srcMRMap = new Dictionary<string, Material[]>();
            var srcMRByLoosePath = new Dictionary<string, Material[]>();
            var srcSMRMap = new Dictionary<string, Material[]>();
            var srcSMRByLoosePath = new Dictionary<string, Material[]>();

            foreach (var mr in sceneInstanceRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                var stable = GetStableHierarchyPath(mr.transform, sceneInstanceRoot);
                var loose = GetNameOnlyHierarchyPath(mr.transform, sceneInstanceRoot);

                srcMRMap[stable] = mr.sharedMaterials;
                srcMRByLoosePath[loose] = mr.sharedMaterials;

                srcMRCompMap[stable] = mr;
                srcMRCompByLoosePath[loose] = mr;
            }

            foreach (var smr in sceneInstanceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var stable = GetStableHierarchyPath(smr.transform, sceneInstanceRoot);
                var loose = GetNameOnlyHierarchyPath(smr.transform, sceneInstanceRoot);

                srcSMRMap[stable] = smr.sharedMaterials;
                srcSMRByLoosePath[loose] = smr.sharedMaterials;

                srcSMRCompMap[stable] = smr;
                srcSMRCompByLoosePath[loose] = smr;
            }

            foreach (var dstMr in prefabContentsRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                var stable = GetStableHierarchyPath(dstMr.transform, prefabContentsRoot.transform);
                var loose = GetNameOnlyHierarchyPath(dstMr.transform, prefabContentsRoot.transform);

                if (!TryGetByStableThenLoose(srcMRMap, srcMRByLoosePath, stable, loose, out var srcMaterials))
                    continue;

                var embeddedMaterials = CreateEmbeddedMaterials(srcMaterials, prefabPath, stable, materialCache, textureCache);

                dstMr.sharedMaterials = embeddedMaterials;
                EditorUtility.SetDirty(dstMr);

                if (TryGetByStableThenLoose(srcMRCompMap, srcMRCompByLoosePath, stable, loose, out var sceneMr) && sceneMr != null)
                {
                    sceneMr.sharedMaterials = embeddedMaterials;
                    EditorUtility.SetDirty(sceneMr);
                }

                EditorUtility.SetDirty(dstMr);
            }

            foreach (var dstSmr in prefabContentsRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var stable = GetStableHierarchyPath(dstSmr.transform, prefabContentsRoot.transform);
                var loose = GetNameOnlyHierarchyPath(dstSmr.transform, prefabContentsRoot.transform);

                if (!TryGetByStableThenLoose(srcSMRMap, srcSMRByLoosePath, stable, loose, out var srcMaterials))
                    continue;

                var embeddedMaterials = CreateEmbeddedMaterials(srcMaterials, prefabPath, stable, materialCache, textureCache);

                dstSmr.sharedMaterials = embeddedMaterials;
                EditorUtility.SetDirty(dstSmr);

                if (TryGetByStableThenLoose(srcSMRCompMap, srcSMRCompByLoosePath, stable, loose, out var sceneSmr) && sceneSmr != null)
                {
                    sceneSmr.sharedMaterials = embeddedMaterials;
                    EditorUtility.SetDirty(sceneSmr);
                }

                EditorUtility.SetDirty(dstSmr);
            }
        }
        private static Material[] CreateEmbeddedMaterials(Material[] sourceMaterials, string prefabPath, string rendererPath, Dictionary<int, Material> materialCache, Dictionary<int, Texture> textureCache)
        {
            if (sourceMaterials == null)
                return Array.Empty<Material>();

            var result = new Material[sourceMaterials.Length];

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                var srcMat = sourceMaterials[i];
                if (srcMat == null)
                    continue;

                var key = srcMat.GetInstanceID();

                if (!materialCache.TryGetValue(key, out var embeddedMat))
                {
                    embeddedMat = SaveOrUpdateMaterialAsset(srcMat, prefabPath, rendererPath, i);
                    materialCache[key] = embeddedMat;
                }

                foreach (var texProp in embeddedMat.GetTexturePropertyNames())
                {
                    var srcTex = srcMat.GetTexture(texProp);
                    if (srcTex == null)
                        continue;

                    var embeddedTex = CreateEmbeddedTexture(
                        srcTex,
                        prefabPath,
                        rendererPath,
                        texProp,
                        textureCache);

                    if (embeddedTex != null)
                        embeddedMat.SetTexture(texProp, embeddedTex);
                }

                EditorUtility.SetDirty(embeddedMat);
                result[i] = embeddedMat;
            }

            return result;
        }
        private static Texture CreateEmbeddedTexture(Texture source, string prefabPath, string rendererPath, string propertyName, Dictionary<int, Texture> textureCache)
        {
            if (source == null)
                return null;

            var key = source.GetInstanceID();

            if (textureCache.TryGetValue(key, out var cached))
                return cached;

            Texture embedded = null;

            if (source is Texture2D texture2D)
            {
                embedded = DuplicateTexture2D(texture2D);
            }
            else if (source is Cubemap cubemap)
            {
                embedded = DuplicateCubemap(cubemap);
            }

            if (embedded == null)
                return source;

            embedded = SaveOrUpdateTextureAsset(embedded, source, prefabPath, rendererPath, propertyName);

            textureCache[key] = embedded;
            return embedded;
        }

        private static Material SaveOrUpdateMaterialAsset(Material source, string prefabPath, string rendererPath, int materialIndex)
        {
            if (source == null)
                return null;

            var folder = EnsureEmbeddedAssetFolder(prefabPath, "Materials");
            var assetPath = CombineAssetPath(
                folder,
                $"{SanitizeAssetName(rendererPath)}_mat{materialIndex}_{SanitizeAssetName(source.name)}.mat");

            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                existing.shader = source.shader;
                existing.CopyPropertiesFromMaterial(source);
                existing.renderQueue = source.renderQueue;
                existing.enableInstancing = source.enableInstancing;
                existing.doubleSidedGI = source.doubleSidedGI;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new Material(source)
            {
                name = source.name
            };
            AssetDatabase.CreateAsset(material, assetPath);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture SaveOrUpdateTextureAsset(Texture embedded, Texture source, string prefabPath, string rendererPath, string propertyName)
        {
            if (embedded == null || source == null)
                return source;

            var folder = EnsureEmbeddedAssetFolder(prefabPath, "Textures");
            var assetPath = CombineAssetPath(
                folder,
                $"{SanitizeAssetName(rendererPath)}_{SanitizeAssetName(propertyName)}_{SanitizeAssetName(source.name)}.asset");

            embedded.name = source.name;

            var existing = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
            if (existing != null && existing.GetType() == embedded.GetType())
            {
                EditorUtility.CopySerialized(embedded, existing);
                Object.DestroyImmediate(embedded);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(embedded, assetPath);
            EditorUtility.SetDirty(embedded);
            return embedded;
        }

        private static string EnsureEmbeddedAssetFolder(string prefabPath, string childFolderName)
        {
            var prefabFolder = NormalizeAssetPath(Path.GetDirectoryName(prefabPath));
            var rootFolderName = $"{Path.GetFileNameWithoutExtension(prefabPath)}_EmbeddedAssets";
            var rootFolder = CombineAssetPath(prefabFolder, rootFolderName);

            if (!AssetDatabase.IsValidFolder(rootFolder))
            {
                AssetDatabase.CreateFolder(prefabFolder, rootFolderName);
            }

            var childFolder = CombineAssetPath(rootFolder, childFolderName);
            if (!AssetDatabase.IsValidFolder(childFolder))
            {
                AssetDatabase.CreateFolder(rootFolder, childFolderName);
            }

            return childFolder;
        }

        private static bool IsAssetInFolder(string assetPath, string folder)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(folder))
                return false;

            var normalizedPath = NormalizeAssetPath(assetPath);
            var normalizedFolder = NormalizeAssetPath(folder).TrimEnd('/');
            return normalizedPath.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string CombineAssetPath(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
                return NormalizeAssetPath(right);

            if (string.IsNullOrEmpty(right))
                return NormalizeAssetPath(left);

            return $"{NormalizeAssetPath(left).TrimEnd('/')}/{NormalizeAssetPath(right).TrimStart('/')}";
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }

        private static string SanitizeAssetName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Root";

            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Select(c => invalid.Contains(c) || c == '/' || c == '\\' || c == '#' || c == ':' ? '_' : c).ToArray();
            var sanitized = new string(chars).Trim('_');
            if (string.IsNullOrEmpty(sanitized))
                return "Asset";

            return sanitized.Length <= 120 ? sanitized : sanitized.Substring(0, 120);
        }

        private static string GetNameOnlyHierarchyPath(Transform target, Transform root)
        {
            if (target == null || root == null)
                return string.Empty;

            var names = new Stack<string>();
            var current = target;

            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static bool TryGetByStableThenLoose<T>(
            Dictionary<string, T> stableMap,
            Dictionary<string, T> looseMap,
            string stable,
            string loose,
            out T value)
        {
            if (stableMap != null && stableMap.TryGetValue(stable, out value))
                return true;

            if (looseMap != null && looseMap.TryGetValue(loose, out value))
                return true;

            value = default;
            return false;
        }

        private static Cubemap DuplicateCubemap(Cubemap source)
        {
            if (source == null)
                return null;

            var readableCopy = new Cubemap(
                source.width,
                source.format,
                source.mipmapCount > 1
            )
            {
                name = source.name,
                wrapMode = source.wrapMode,
                filterMode = source.filterMode,
                anisoLevel = source.anisoLevel
            };

            for (int face = 0; face < 6; face++)
            {
                var cubemapFace = (CubemapFace)face;

                try
                {
                    var pixels = source.GetPixels(cubemapFace);
                    readableCopy.SetPixels(pixels, cubemapFace);
                }
                catch
                {
                    Object.DestroyImmediate(readableCopy);
                    return null;
                }
            }

            readableCopy.Apply();
            return readableCopy;
        }

        private static Mesh CreateEmbeddedMesh(Mesh source, string prefabPath, string meshPath, Mesh currentMesh)
        {
            if (source == null)
                return null;

            // 既に Prefab 内サブアセットなら、それを更新して再利用
            var meshFolder = EnsureEmbeddedAssetFolder(prefabPath, "Meshes");

            if (currentMesh != null &&
                IsAssetInFolder(AssetDatabase.GetAssetPath(currentMesh), meshFolder))
            {
                CopyMeshDataInto(source, currentMesh);
                EditorUtility.SetDirty(currentMesh);
                return currentMesh;
            }

            var assetPath = CombineAssetPath(
                meshFolder,
                $"{SanitizeAssetName(meshPath)}_{SanitizeAssetName(source.name)}.asset");

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                CopyMeshDataInto(source, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var embedded = DuplicateMeshForPrefabEmbedding(source);
            if (embedded == null)
                return null;

            embedded.name = source.name;

            AssetDatabase.CreateAsset(embedded, assetPath);
            EditorUtility.SetDirty(embedded);

            return embedded;
        }

        private static Mesh DuplicateMeshForPrefabEmbedding(Mesh source)
        {
            if (source == null) return null;

            var mesh = new Mesh();
            CopyMeshDataInto(source, mesh);
            return mesh;
        }

        private static void CopyMeshDataInto(Mesh source, Mesh destination)
        {
            if (source == null || destination == null) return;

            destination.Clear();
            destination.indexFormat = source.indexFormat;
            destination.vertices = source.vertices;
            destination.normals = source.normals;
            destination.tangents = source.tangents;
            destination.uv = source.uv;
            destination.uv2 = source.uv2;
            destination.uv3 = source.uv3;
            destination.uv4 = source.uv4;
            destination.uv5 = source.uv5;
            destination.uv6 = source.uv6;
            destination.uv7 = source.uv7;
            destination.uv8 = source.uv8;
            destination.colors = source.colors;
            destination.colors32 = source.colors32;
            destination.bindposes = source.bindposes;
            destination.boneWeights = source.boneWeights;

            destination.subMeshCount = source.subMeshCount;
            for (var i = 0; i < source.subMeshCount; i++)
            {
                destination.SetTriangles(source.GetTriangles(i), i, true);
            }

            var vertexCount = source.vertexCount;
            if (vertexCount > 0 && source.blendShapeCount > 0)
            {
                var deltaVertices = new Vector3[vertexCount];
                var deltaNormals = new Vector3[vertexCount];
                var deltaTangents = new Vector3[vertexCount];

                for (var i = 0; i < source.blendShapeCount; i++)
                {
                    var shapeName = source.GetBlendShapeName(i);
                    var frameCount = source.GetBlendShapeFrameCount(i);
                    for (var frame = 0; frame < frameCount; frame++)
                    {
                        var weight = source.GetBlendShapeFrameWeight(i, frame);
                        source.GetBlendShapeFrameVertices(i, frame, deltaVertices, deltaNormals, deltaTangents);
                        destination.AddBlendShapeFrame(shapeName, weight, deltaVertices, deltaNormals, deltaTangents);
                    }
                }
            }

            destination.bounds = source.bounds;
            destination.RecalculateBounds();
        }

        private static Texture2D DuplicateTexture2D(Texture2D source)
        {
            if (source == null) return null;

            try
            {
                return Object.Instantiate(source);
            }
            catch
            {
                return DuplicateTexture2DViaBlit(source);
            }
        }

        private static Texture2D DuplicateTexture2DViaBlit(Texture2D source)
        {
            if (source == null || source.width <= 0 || source.height <= 0)
            {
                return null;
            }

            var renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;

                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply(false, false);
                return copy;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static string GetStableHierarchyPath(Transform target, Transform root)
        {
            if (target == null || root == null) return string.Empty;
            if (target == root) return string.Empty;

            var stack = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                stack.Push($"{current.name}#{current.GetSiblingIndex()}");
                current = current.parent;
            }

            return string.Join("/", stack);
        }
    }
}
