using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 都市オブジェクトのテクスチャ状態を保持するデータモデル
    /// Stores texture state for a city object
    /// </summary>
    public class TextureSwitchBuilding
    {
        private readonly MeshRenderer meshRenderer;
        private readonly List<Texture2D> originalTextures = new List<Texture2D>();
        private readonly int buildingInstanceID;
        private Material[] cachedMaterials;
        private float defaultLOD1TopTiling = 0.2f;  // インポート直後のLOD1のマテリアルの設定値　建物テクスチャを元に戻す際に利用する
        private float defaultLOD1SideTiling = 0.056f;  // インポート直後のLOD1のマテリアルの設定値　建物テクスチャを元に戻す際に利用する
        [Serializable]
        public class DirectionalTextures
        {
            public Texture2D Left;
            public Texture2D Right;
            public Texture2D Top;
            public Texture2D Front;
            public Texture2D Back;
        }
        public class DirectionalTextureBinding
        {
            public string PropertyName;

            public Action<DirectionalTextures, Texture2D> Setter;

            public Func<DirectionalTextures, Texture2D> Getter;

            public DirectionalTextureBinding(string propertyName, Action<DirectionalTextures, Texture2D> setter, Func<DirectionalTextures, Texture2D> getter)
            {
                PropertyName = propertyName;
                Setter = setter;
                Getter = getter;
            }
        }
        static class DirectionalTextureMap
        {
            public static readonly List<DirectionalTextureBinding> Bindings = new()
            {
                new DirectionalTextureBinding("_LeftTexture", (t, v) => t.Left = v, t => t.Left),
                new DirectionalTextureBinding("_RightTexture", (t, v) => t.Right = v, t => t.Right),
                new DirectionalTextureBinding("_TopTexture", (t, v) => t.Top = v, t => t.Top),
                new DirectionalTextureBinding("_FrontTexture", (t, v) => t.Front = v, t => t.Front),
                new DirectionalTextureBinding("_BackTexture", (t, v) => t.Back = v, t => t.Back),
            };
        }

        private readonly Dictionary<int, DirectionalTextures> originalDirectionalTextures = new();

        /// <summary>
        /// 現在のテクスチャ表示状態（true = 非表示、false = 表示）
        /// </summary>
        public bool IsTextureHidden { get; private set; }

        public TextureSwitchBuilding(MeshRenderer meshRenderer, bool isTextureHidden = false)
        {
            this.meshRenderer = meshRenderer;
            IsTextureHidden = isTextureHidden;
            buildingInstanceID = meshRenderer.gameObject.GetInstanceID();

            // 各テクスチャのコピーを取得
            for (int i = 0; i < meshRenderer.materials.Length; i++)
            {
                var material = meshRenderer.materials[i];

                // legacy 単一テクスチャ（既存互換）
                Texture2D mainLike = null;

                if (material.HasProperty("_MainTex"))
                {
                    mainLike = material.GetTexture("_MainTex") as Texture2D;

                    originalTextures.Add(mainLike);
                }
                // ShaderGraph 複数テクスチャ（新設、あれば）
                else
                {
                    var dir = CopyDirectionalTextures(material);

                    if (dir != null)
                    {
                        originalDirectionalTextures[i] = dir;
                    }
                }
            }
        }

        private DirectionalTextures CopyDirectionalTextures(Material material)
        {
            DirectionalTextures result = null;

            foreach (var binding in DirectionalTextureMap.Bindings)
            {
                if (!material.HasProperty(binding.PropertyName)) continue;

                var tex = material.GetTexture(binding.PropertyName) as Texture2D;

                if (tex == null) continue;

                result ??= new DirectionalTextures();

                binding.Setter(result, tex);
            }

            return result;
        }

        private void ClearDirectionalTextures(Material material)
        {
            foreach (var binding in DirectionalTextureMap.Bindings)
            {
                if (material.HasProperty(binding.PropertyName))
                {
                    material.SetTexture(binding.PropertyName, null);
                }
            }
        }

        private void RestoreDirectionalTextures(Material material, DirectionalTextures dir)
        {
            foreach (var binding in DirectionalTextureMap.Bindings)
            {
                if (!material.HasProperty(binding.PropertyName)) continue;

                var tex = binding.Getter(dir);

                material.SetTexture(binding.PropertyName, tex);
            }
        }

        /// <summary>
        /// 同じインスタンスの建物かどうか
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSameBuilding(GameObject other)
        {
            return buildingInstanceID == other.GetInstanceID();
        }

        /// <summary>
        /// 参照切れのオブジェクト
        /// </summary>
        /// <returns></returns>
        public bool IsNullObject()
        {
            return meshRenderer == null;
        }

        /// <summary>
        /// テクスチャの表示状態を設定して適用する
        /// </summary>
        /// <param name="isVisible">true = 表示、false = 非表示</param>
        public void SetTextureVisibility(bool isVisible)
        {
            IsTextureHidden = !isVisible;
            ApplyTextureState();
        }

        /// <summary>
        /// テクスチャの状態をMeshRendererに適用する
        /// </summary>
        private void ApplyTextureState()
        {
            if (meshRenderer == null) return;

            // マテリアルインスタンスを取得（初回呼び出し時に自動作成される）
            if (cachedMaterials == null)
            {
                cachedMaterials = meshRenderer.materials;
            }

            for (int i = 0; i < cachedMaterials.Length; i++)
            {
                var material = cachedMaterials[i];

                // ShaderGraph (新設)
                if (originalDirectionalTextures.TryGetValue(i, out var dir))
                {
                    if (IsTextureHidden)
                    {
                        ClearDirectionalTextures(material);
                    }
                    else
                    {
                        RestoreDirectionalTextures(material, dir);
                    }
                }
                // legacy（既存互換）
                else
                {
                    if (originalTextures.Count > i)
                    {
                        material.mainTexture = IsTextureHidden ? null : originalTextures[i];
                    }

                    // LOD1のShader設定
                    float topTiling = defaultLOD1TopTiling;
                    float sideTiling = defaultLOD1SideTiling;

                    if (IsTextureHidden)
                    {
                        topTiling = 0f;
                        sideTiling = 0f;
                    }

                    // LOD1のShader設定
                    material.SetFloat("_Top_Titling", topTiling);   // TitlingのスペルはShader側の定義に合わせているため誤字のまま
                    material.SetFloat("_Side_Titling", sideTiling); // TitlingのスペルはShader側の定義に合わせているため誤字のまま

                }
            }

            // 変更したマテリアル配列をレンダラーに適用
            meshRenderer.materials = cachedMaterials;
        }

    }
}