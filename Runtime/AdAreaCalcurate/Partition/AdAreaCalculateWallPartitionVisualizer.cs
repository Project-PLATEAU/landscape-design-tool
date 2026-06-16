// NOTE: 分割面の回転方式切替
// 下記シンボルを定義するとレガシー挙動(OBB の回転をそのまま使用)になります。未定義時は新方式(Up=Vector3.up 固定 + ヨーのみ)です。
//   レガシー: 各 OBB の回転 (ピッチ/ロール含む) を使用し、Up がズレていれば globalUp に統一
//   新方式  : Up を常に Vector3.up に固定し、OBB 回転からヨー成分のみを抽出
// #define LDT3_LEGACY_WALL_PARTITION_ORIENTATION

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering.HighDefinition;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 建物底面(= bounds の下端) からユーザ指定高さに存在する "水平分割面" を即時に Quad で可視化するビジュアライザ。
    /// 以前の帯(高さ区間)表示から、離散面(スライス)表示へ仕様変更。
    /// heights 内の高さ値ごとに 1 枚の水平 Quad を生成する。0 (底面) / H (頂部) は自動追加しないため、必要なら呼び出し側で明示的に含めること。
    /// 入力高さはクランプせず、そのまま利用 (ソート & 重複除去のみ)。
    /// </summary>
    public interface IAdAreaCalculateWallPartitionVisualizer : IDisposable
    {
        /// <summary>
        /// ユーザ入力高さリスト (グローバル基準 0 = 全壁最下端) を元に、全壁 OBB ごとに各高さで水平 Quad を表示する。
        /// 入力順と重複を保持し、NaN/Infinity のみ除外。0/H は自動追加しない。
        /// </summary>
        /// <param name="heights">グローバル基準 0 (全 OBB 最下端) からの高さ値群。</param>
        /// <param name="boundsList">対象壁面の OBB 群。</param>
        void ShowHeights(ReadOnlyArray<float> heights, ReadOnlyArray<OrientedBounds> boundsList);

        /// <summary>全表示を削除。</summary>
        void Clear();
    }

    /// <summary>
    /// Quad を単純生成して「分割面 (水平)」を視覚化する実装。
    /// - 各ユーザ指定高さ h をソート & 重複除去 (0 / H は自動追加しない, クランプしない)
    /// - 面 1 枚 = 高さ h における水平スライス
    /// - サイズ: (bounds.Size.x, bounds.Size.z) を XY 平面に持つ Quad
    /// - 位置: bounds の底面位置 + up * h
    /// - 回転: bounds.rotation * (Quaternion.Euler(90,0,0)) で水平化 (Unity の Quad は +Z を法線とするため)
    /// </summary>
    public sealed class AdAreaCalculateWallPartitionVisualizer : IAdAreaCalculateWallPartitionVisualizer
    {
        /// <summary>
        /// 高さインデックスに対応する表示色テーブル。インデックスが範囲外の場合は modulo で循環。
        /// 先頭から: Yellow, Cyan, Magenta, Red, Green, Blue, Gray, White.
        /// </summary>

        private Material templateMaterial; // 内部生成/管理
        private GameObject root; // 生成親
        private readonly List<GameObject> cache = new(); // 再利用用 Quad (1 面 = 1 要素)

        private const string RootName = "WallPartitionBandsRoot";


        public AdAreaCalculateWallPartitionVisualizer() => CreateInternalMaterial();

        /// <summary>
        /// グローバル基準 0 (全 OBB 最下端) からの高さ値リストを元に、各壁面 OBB ごとに全高さで水平 Quad を生成/再利用して表示。入力順をそのまま使用し重複高さも表示。
        /// </summary>
        public void ShowHeights(ReadOnlyArray<float> heights, ReadOnlyArray<OrientedBounds> boundsList)
        {
            EnsureRoot();
            if (boundsList.Count == 0)
            {
                DeactivateAll();
                return;
            }

            var planeHeights = NormalizePlaneHeights(heights);
            if (planeHeights.Count == 0)
            {
                DeactivateAll();
                return;
            }

#if LDT3_LEGACY_WALL_PARTITION_ORIENTATION
            // Legacy: 最初の OBB Up をグローバル Up とみなす
            Vector3 globalUp = boundsList[0].Up.normalized;
#else
            Vector3 globalUp = Vector3.up; // New: 強制的にワールド Up
#endif
            float baselineProj = float.PositiveInfinity;
            float margin = 5.0f;
            for (int i = 0; i < boundsList.Count; i++)
            {
                var b = boundsList[i];
                Vector3 up = b.Up.normalized;
                if (Vector3.Dot(up, globalUp) < 0.99f)
                {
                    // 異なる Up は現状簡易にグローバル Up に統一 (TODO: 必要なら警告ログ)
                    up = globalUp;
                }
                float halfH = Mathf.Max(0f, b.Size.y) * 0.5f;
                Vector3 bottom = b.center - up * halfH;
                float proj = Vector3.Dot(bottom, globalUp);
                if (proj < baselineProj) baselineProj = proj;
            }
            if (float.IsInfinity(baselineProj))
            {
                DeactivateAll();
                return;
            }

            int activeIndex = 0;
            for (int bi = 0; bi < boundsList.Count; bi++)
            {
                var b = boundsList[bi];
                if (b.extents == Vector3.zero || b.Size.y <= 0f || (Mathf.Abs(b.Size.x) <= 1e-6f && Mathf.Abs(b.Size.z) <= 1e-6f))
                {
                    continue; // 退化 OBB はスキップ
                }
                Quaternion rot = b.rotation;
#if LDT3_LEGACY_WALL_PARTITION_ORIENTATION
                // Legacy: OBB の Up を利用 (ズレがあれば強制統一)
                Vector3 up = rot * Vector3.up;
                if (Vector3.Dot(up, globalUp) < 0.99f) up = globalUp;
#else
                // New: Up は常に Vector3.up
                Vector3 up = globalUp;
#endif

                float totalH = b.Size.y;
                Vector3 bottom = b.center - up * (totalH * 0.5f);
                float bottomProj = Vector3.Dot(bottom, globalUp);
                float sizeX = b.Size.x + margin;
                float sizeZ = b.Size.z + margin;

                for (int hi = 0; hi < planeHeights.Count; hi++)
                {
                    float h = planeHeights[hi]; // グローバル基準からの高さ
                    // 位置 = bottom + up * ( (baseline差分補正後の高さ) )
                    float offset = h - (bottomProj - baselineProj); // bottom が 0 のとき h を使う
                    Vector3 center = bottom + up * offset;

                    var go = GetOrCreate(activeIndex);
                    go.name = $"Wall{bi}_Plane_h={h:0.###}";
                    go.transform.SetParent(root.transform, worldPositionStays: false);
                    go.transform.position = center;
                    // 回転設定: 既存処理 (OBB の回転そのまま + Quad を水平化) と
                    // Up をグローバル Y に揃えヨー成分のみ残す処理を切替可能にする。
                    //   既存: rot * (90deg X)  -> OBB 傾きをそのまま反映
                    //   新規: (yawOnly * 90deg X) -> ピッチ/ロール除去し水平面基準のヨーのみ残す
                    // 条件コンパイルで切り替え (#if false を true に変更で既存ロジック有効)
#if LDT3_LEGACY_WALL_PARTITION_ORIENTATION
                    // Legacy: OBB の回転 (ピッチ/ロール含む) をそのまま利用し Quad を水平化
                    go.transform.rotation = rot * Quaternion.Euler(90f, 0f, 0f);
#else
                    // New: グローバル Up に揃えヨーのみ抽出
                    go.transform.rotation = BuildPlaneRotationYawOnly(rot, globalUp);
#endif
                    go.transform.localScale = new Vector3(sizeX, sizeZ, 1f);
                    go.SetActive(true);

                    var renderer = go.GetComponent<MeshRenderer>();
                    if (templateMaterial != null && renderer.sharedMaterial != templateMaterial)
                    {
                        renderer.sharedMaterial = templateMaterial;
                    }
                    ApplyColor(renderer, hi);
                    activeIndex++;
                }
            }

            for (int i = activeIndex; i < cache.Count; i++)
            {
                if (cache[i] != null) cache[i].SetActive(false);
            }
        }

        public void Clear()
        {
            if (root == null) return;
            DeactivateAll();
        }

        public void Dispose()
        {
            Clear();
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root); // エディタ/PlayMode 双方許容 (既存コードと整合見るなら Destroy)
                root = null;
            }
            ReleaseInternalMaterial();
        }

        // --- Internal helpers -------------------------------------------------
        private void EnsureRoot()
        {
            if (root != null) return;
            root = new GameObject(RootName);
        }

        private void DeactivateAll()
        {
            for (int i = 0; i < cache.Count; i++)
            {
                if (cache[i] != null) cache[i].SetActive(false);
            }
        }

        private GameObject GetOrCreate(int index)
        {
            while (cache.Count <= index)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cache.Add(go);
            }
            return cache[index];
        }

        /// <summary>
        /// 平面用高さ正規化: 入力をそのままソート & 重複除去する。0 / H は自動追加しない。
        /// 返りは面を置く高さ (単点)。必要な場合は呼び出し側で 0 や H を含めること。
        /// </summary>
        private static List<float> NormalizePlaneHeights(IReadOnlyList<float> src)
        {
            // 変更: ソートと重複除去を行わず、入力順を保持し NaN/Infinity のみ除外。
            var list = new List<float>();
            if (src == null) return list;
            for (int i = 0; i < src.Count; i++)
            {
                float v = src[i];
                if (float.IsNaN(v) || float.IsInfinity(v)) continue;
                list.Add(v);
            }
            return list;
        }

        private void ApplyColor(MeshRenderer renderer, int index)
        {
            if (renderer == null) return;
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            var c = AdAreaPartitionColors.Get(index);
            c.a = 0.5f;
            if (templateMaterial != null && templateMaterial.HasProperty("_UnlitColor"))
            {
                mpb.SetColor("_UnlitColor", c);
            }
            renderer.SetPropertyBlock(mpb);
        }

        private void CreateInternalMaterial()
        {
            if (templateMaterial != null) return;
            Shader shader = Shader.Find("HDRP/Unlit");
            Assert.IsNotNull(shader, "Expected HDRP/Unlit shader to be available. Ensure HDRP package is installed.");

            templateMaterial = new Material(shader) { name = "WallPartitionBands_Mat (Generated)" };

            // 透過設定 (HDRP Unlit)
            if (templateMaterial.HasProperty("_SurfaceType")) templateMaterial.SetInt("_SurfaceType", 1); // 0=Opaque,1=Transparent
            if (templateMaterial.HasProperty("_BlendMode")) templateMaterial.SetInt("_BlendMode", 0); // Alpha
            if (templateMaterial.HasProperty("_SrcBlend")) templateMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (templateMaterial.HasProperty("_DstBlend")) templateMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (templateMaterial.HasProperty("_ZWrite")) templateMaterial.SetInt("_ZWrite", 0);

            // 両面描画
            if (templateMaterial.HasProperty("_CullMode")) templateMaterial.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            if (templateMaterial.HasProperty("_DoubleSidedEnable")) templateMaterial.SetInt("_DoubleSidedEnable", 1);

            HDMaterial.ValidateMaterial(templateMaterial);
        }

        private void ReleaseInternalMaterial()
        {
            if (templateMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(templateMaterial);
                templateMaterial = null;
            }
        }

        /// <summary>
        /// OBB 回転 <paramref name="obbRotation"/> からピッチ/ロールを除去し、グローバル Up (<paramref name="globalUp"/>) を Up に固定したヨー回転を構築し、
        /// Quad (+Z 法線) を水平にするため X+90° を適用した最終回転を返す。
        /// <para>Forward がほぼ Up/Down で水平投影が消える場合は Right ベクトルでフォールバックし、それでも失敗時は Vector3.forward を使用。</para>
        /// </summary>
        /// <param name="obbRotation">元の OBB ワールド回転。</param>
        /// <param name="globalUp">固定 Up (通常 Vector3.up)。</param>
        /// <returns>水平化済み最終回転。</returns>
        private static Quaternion BuildPlaneRotationYawOnly(in Quaternion obbRotation, in Vector3 globalUp)
        {
            Vector3 horizFwd = obbRotation * Vector3.forward;
            horizFwd = Vector3.ProjectOnPlane(horizFwd, globalUp);
            if (horizFwd.sqrMagnitude < 1e-6f)
            {
                // Forward がほぼ鉛直 -> Right 軸を利用
                horizFwd = obbRotation * Vector3.right;
                horizFwd = Vector3.ProjectOnPlane(horizFwd, globalUp);
                if (horizFwd.sqrMagnitude < 1e-6f)
                {
                    // それでもダメならデフォルト
                    horizFwd = Vector3.forward;
                }
            }
            horizFwd.Normalize();
            var yawOnly = Quaternion.LookRotation(horizFwd, globalUp);
            return yawOnly * Quaternion.Euler(90f, 0f, 0f); // Quad(+Z) を水平へ
        }

        // TODO: (future) 0 と H を省略できるオプション / 半透明度グラデーション / 面ラベル (TextMeshPro) / まとめて 1 メッシュ化 (DrawCall 削減)
        // IDEA: CombineInstance を用いて 1 フレーム後に静的結合し、編集中は個別表示などのモード切替。

    }
}

