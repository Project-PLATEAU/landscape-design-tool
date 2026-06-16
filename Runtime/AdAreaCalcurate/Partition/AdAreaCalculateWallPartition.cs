// NOTE: 分割計算の Up / OBB フィルタ挙動切替シンボル
//  #define LDT3_LEGACY_WALL_PARTITION_ORIENTATION を有効化するとレガシー挙動:
//   - SetTarget: OBB Up の向き判定を行わず全件採用
//   - Calculate: 最初の OBB Up を基準 Up として使用
//  未定義時 (新方式):
//   - SetTarget: Up が Vector3.up と 0.9 以上平行なもののみ採用
//   - Calculate: Up を常に Vector3.up に固定
//  他ファイル (AdAreaCalculateWallPartitionVisualizer.cs) と同じシンボルで挙動を統一できます。
// #define LDT3_LEGACY_WALL_PARTITION_ORIENTATION

using System;
using System.Collections.Generic;
using System.Linq;
using Landscape2.Runtime.AdAreaCalcurateSub;
using UnityEngine.Assertions;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Landscape2.Runtime
{

    /// <summary>
    /// 壁面分割面積算出サービス
    /// </summary>
    public class AdAreaCalculateWallPartition : IAdAreaCalculateWallPartition
    {
        private List<Wall> wallDataList = new();
        private List<OrientedBounds> wallBoundsList = new();
        private readonly List<float> splitHeights = new();
        private readonly float[] adAreas = new float[4];


        public ReadOnlyArray<float> SplitHeights => splitHeights.ToArray();
        public ReadOnlyArray<OrientedBounds> WallBoundsList => wallBoundsList.ToArray();


        public event Action<PartitionData> Computed;


        /// <summary>
        /// コンストラクタ。初期状態として分割高さ 0 を 1 件含む。
        /// </summary>
        public AdAreaCalculateWallPartition()
        {
            splitHeights.Add(0);
        }

        /// <summary>
        /// 分割計算対象となる壁面集合(<see cref="Wall"/>)とその OBB(<see cref="OrientedBounds"/>) を設定する。
        /// <para>配列長は一致している必要がある。</para>
        /// </summary>
        /// <param name="walls">壁面データ配列 (選択中の壁)。</param>
        /// <param name="bounds">各壁面に対応する OBB。Forward が外向きである前提。</param>
        public void SetTarget(ReadOnlyArray<Wall> walls, ReadOnlyArray<OrientedBounds> bounds)
        {
#if LDT3_LEGACY_WALL_PARTITION_ORIENTATION
            // Legacy: フィルタせずそのままコピー
            wallDataList = walls.ToList();
            wallBoundsList = bounds.ToList();
#else
            // New: Up がワールド Up と概ね平行なもののみ採用
            wallDataList.Clear();
            wallBoundsList.Clear();
            int count = Math.Min(walls.Count, bounds.Count);
            for (int i = 0; i < count; i++)
            {
                var wallData = walls[i];
                var wallBounds = bounds[i];
                if (wallData == null) continue;
                if (Vector3.Dot(wallBounds.Up.normalized, Vector3.up) > 0.9f)
                {
                    wallDataList.Add(wallData);
                    wallBoundsList.Add(wallBounds);
                }
            }
#endif
            Calculate();
        }

        /// <summary>
        /// 指定インデックスの分割面高さを更新し再計算する。
        /// </summary>
        /// <param name="index">0～2 (最大 3 面)。</param>
        /// <param name="height">0 以上の高さ値 (建物ローカル Up 基準)。</param>
        public void SetHeight(int index, float height)
        {
            Assert.IsTrue(index >= 0 && index <= 2);

            if (splitHeights == null) return;
            if (index >= splitHeights.Count)
            {
                Debug.LogError("指定インデックスが不正です");
                return;
            }
            splitHeights[index] = height;

            Calculate();
        }

        public void AddSplitHeight()
        {
            if (splitHeights.Count >= 3)
            {
                Debug.LogError("分割数は最大3つまでです");
                return;
            }

            var last = splitHeights.LastOrDefault();
            splitHeights.Add(last);

            Calculate();
        }

        public void DeleteSplitHeight(int index)
        {
            Assert.IsTrue(index >= 1 && index <= 2);

            if (splitHeights == null) return;
            splitHeights.RemoveAt(index);

            Calculate();
        }

        public void SetAdArea(int index, float area)
        {
            Assert.IsTrue(index >= 0 && index <= 3);

            if (adAreas == null) return;
            var clampedAdArea = Mathf.Clamp(area, 0f, 1000f * 1000f);
            adAreas[index] = clampedAdArea;

            Calculate();
        }

        public void Reset()
        {
            wallDataList.Clear();
            wallBoundsList.Clear();
            splitHeights.Clear();
            splitHeights.Add(0);
            Array.Clear(adAreas, 0, adAreas.Length);

            Calculate();
        }

        /// <summary>
        /// 現在の splitHeights / 壁集合に基づき帯(バンド)面積を再計算し <see cref="Computed"/> を発火する。
        /// </summary>
        /// <remarks>
        /// バンド数 = 分割面数 + 1。最下バンドは (-∞, h0]、最上バンドは (hN-1, +∞) という概念的区間。
        /// 無限端は全壁に対して求めたグローバル基準面(= 全壁 OBB の最下端) と最上端で暗黙クランプされる。
        /// 分割高さ splitHeights は「全壁集合の最下端を 0 とするグローバル高さスカラー」。
        /// 幅は寄与した各壁 bounds.Size.x の総和。
        /// </remarks>
        private void Calculate()
        {
            // ガード: 対象壁が無い場合は空通知
            if (wallDataList.Count == 0 || wallBoundsList.Count != wallDataList.Count)
            {
                Computed?.Invoke(new PartitionData(SplitHeights, Array.Empty<WallSegment>()));
                return;
            }

            // ソート済み分割高さ（重複保持）
            var sortedHeights = new List<float>(splitHeights);
            sortedHeights.Sort();
            int k = sortedHeights.Count; // 分割面数 (最大3)
            int bandCount = k + 1;       // バンド数 (最大4)

            var areaSums = new float[bandCount];
            var widthSums = new float[bandCount];

            const float EPS_AREA = 1e-6f;
#if LDT3_LEGACY_WALL_PARTITION_ORIENTATION
            // Legacy: 最初の OBB の Up を基準 Up とみなす
            Vector3 referenceUp = wallBoundsList[0].Up.normalized;
#else
            // New: 強制的にグローバル Up (Vector3.up)
            Vector3 referenceUp = Vector3.up;
#endif
            float baseline = float.PositiveInfinity; // ワールド空間での最低 y(投影距離)
            float globalTop = float.NegativeInfinity;

            for (int w = 0; w < wallBoundsList.Count; w++)
            {
                var b = wallBoundsList[w];
                Vector3 up = b.Up.normalized;
                // Up が逆転している場合は揃える
                if (Vector3.Dot(up, referenceUp) < 0.99f) up = referenceUp; // 簡易統一 (TODO: 必要なら警告)
                float halfH = Mathf.Max(0f, b.Size.y) * 0.5f;
                Vector3 bottom = b.center - up * halfH;
                Vector3 top = b.center + up * halfH;
                float bottomProj = Vector3.Dot(bottom, referenceUp);
                float topProj = Vector3.Dot(top, referenceUp);
                if (bottomProj < baseline) baseline = bottomProj;
                if (topProj > globalTop) globalTop = topProj;
            }

            if (float.IsInfinity(baseline) || float.IsInfinity(globalTop))
            {
                Computed?.Invoke(new PartitionData(SplitHeights, Array.Empty<WallSegment>()));
                return;
            }
            float globalHeightSpan = Mathf.Max(0f, globalTop - baseline);

            // 各壁面処理
            for (int w = 0; w < wallDataList.Count; w++)
            {
                var wall = wallDataList[w];
                var bounds = wallBoundsList[w];
                if (wall == null) continue;

                // バンド面積（この壁）: グローバル基準面/baseline を使用するよう後続で CalculateBandsArea を改修予定
                var perWallAreas = CalculateBandsArea(wall, bounds, new ReadOnlyArray<float>(sortedHeights.ToArray()), referenceUp, baseline);
                if (perWallAreas == null || perWallAreas.Count != bandCount) continue;

                for (int b = 0; b < bandCount; b++)
                {
                    float a = perWallAreas[b];
                    if (a > 0f) areaSums[b] += a;
                    if (a > EPS_AREA)
                    {
                        // 幅寄与 (指示: 合計 = 各壁 bounds.Size.x の和)
                        widthSums[b] += bounds.Size.x;
                    }
                }
            }
            // セグメント生成 (グローバル 0 = baseline)
            var segmentsList = new List<WallSegment>(bandCount);
            for (int b = 0; b < bandCount; b++)
            {
                float lower, upper;
                if (k == 0)
                {
                    lower = 0f; upper = globalHeightSpan;
                }
                else if (b == 0)
                {
                    lower = 0f; upper = sortedHeights[0];
                }
                else if (b == bandCount - 1)
                {
                    lower = sortedHeights[k - 1]; upper = globalHeightSpan;
                }
                else
                {
                    lower = sortedHeights[b - 1]; upper = sortedHeights[b];
                }
                float height = Mathf.Max(0f, upper - lower);
                float area = areaSums[b];
                float width = widthSums[b];
                segmentsList.Add(new WallSegment(lower, upper, height, width, area));
            }

            // Fromで降順ソート
            segmentsList.Sort((a, b) =>
            {
                var fromCmp = b.From.CompareTo(a.From);
                if (fromCmp != 0) return fromCmp;
                return b.Height.CompareTo(a.Height);
            });

            // adAreasはソート後のsegmentListに対応している
            for (int i = 0; i < segmentsList.Count; i++)
            {
                var seg = segmentsList[i];
                float adArea = (i < adAreas.Length) ? adAreas[i] : 0f;
                seg.SetAdArea(adArea);
                segmentsList[i] = seg;
            }

            Computed?.Invoke(new PartitionData(SplitHeights, new ReadOnlyArray<WallSegment>(segmentsList.ToArray())));
        }

        /// <summary>
        /// 単一 <see cref="Wall"/> の全三角形を対象に、グローバル基準面 (全壁最下端) からの分割高さに基づくバンド別面積を算出する。
        /// </summary>
        /// <param name="wall">対象壁。</param>
        /// <param name="bounds">壁 OBB (Forward が面法線)。Up はグローバル up とほぼ整合している前提。</param>
        /// <param name="splitHeights">ユーザ指定の高さ群 (グローバル基準 0 からの距離)。未ソート/重複可。</param>
        /// <param name="globalUp">全壁で共通とみなす高さ方向。</param>
        /// <param name="globalBaselineProj">グローバル基準面上の射影スカラー (dot(v, globalUp) の最小値)。</param>
        /// <returns>バンド面積リスト (長さ = splitHeights.Count + 1)。重複高さによる 0 高さバンドは 0 面積。</returns>
        private static List<float> CalculateBandsArea(
            Wall wall,
            OrientedBounds bounds,
            ReadOnlyArray<float> splitHeights,
            in Vector3 globalUp,
            float globalBaselineProj)
        {
            if (wall == null) throw new ArgumentNullException(nameof(wall));
            var mesh = wall.BuildingGmlID;
            if (mesh == null) return new List<float>();

            var tris = wall.WallTris;
            int triCount = tris?.Count ?? 0;
            if (triCount == 0) return new List<float>();

            // ソートコピー (重複保持)
            var sorted = new List<float>(splitHeights.ToArray());
            sorted.Sort();
            int k = sorted.Count; // 分割面数
            int bandCount = k + 1; // バンド数
            var bandAreas = new float[bandCount];

            Vector3 up = globalUp.normalized; // グローバル Up を使用
            // グローバル基準面からの高さスパンは個々の壁で異なる。頂点ごとに投影差分を取る。
            const float EPS_H = 1e-5f;
            Vector3 forwardN = bounds.Forward.normalized;

            // 各三角形処理
            var vertices = wall.Vertices;
            for (int ti = 0; ti < triCount; ti++)
            {
                var tri = tris[ti];
                if (tri == null || tri.Length < 3) continue;
                Vector3 v0 = vertices[tri[0]];
                Vector3 v1 = vertices[tri[1]];
                Vector3 v2 = vertices[tri[2]];

                float h0 = HeightFromBaseline(v0, up, globalBaselineProj);
                float h1 = HeightFromBaseline(v1, up, globalBaselineProj);
                float h2 = HeightFromBaseline(v2, up, globalBaselineProj);
                float triMin = Mathf.Min(h0, Mathf.Min(h1, h2));
                float triMax = Mathf.Max(h0, Mathf.Max(h1, h2));

                for (int b = 0; b < bandCount; b++)
                {
                    float lower, upper;
                    if (k == 0)
                    {
                        lower = float.NegativeInfinity; upper = float.PositiveInfinity;
                    }
                    else if (b == 0)
                    {
                        lower = float.NegativeInfinity; upper = sorted[0];
                    }
                    else if (b == bandCount - 1)
                    {
                        lower = sorted[k - 1]; upper = float.PositiveInfinity;
                    }
                    else
                    {
                        lower = sorted[b - 1]; upper = sorted[b];
                    }

                    // 重複高さ (ゼロ帯)
                    if (upper - lower <= EPS_H && !float.IsInfinity(lower) && !float.IsInfinity(upper))
                    {
                        continue; // 面積 0
                    }

                    // 早期除外
                    if (triMax <= lower) continue;
                    if (triMin > upper) continue;

                    // クリップ
                    var poly = new List<Vector3>(3) { v0, v1, v2 };
                    if (!float.IsNegativeInfinity(lower)) poly = ClipLower(poly, lower, up, globalBaselineProj, EPS_H);
                    if (poly.Count < 3) continue;
                    if (!float.IsPositiveInfinity(upper)) poly = ClipUpper(poly, upper, up, globalBaselineProj, EPS_H);
                    if (poly.Count < 3) continue;

                    float area = PolygonArea(poly, forwardN);
                    if (area > 0f) bandAreas[b] += area;
                }
            }

            return bandAreas.ToList();
        }

        // ================= Helper Static Methods ================= //

        /// <summary>頂点のグローバル基準面 (baseline) からの高さを算出。</summary>
        private static float HeightFromBaseline(in Vector3 v, in Vector3 up, float baselineProj)
        {
            float proj = Vector3.Dot(v, up);
            float h = proj - baselineProj;
            if (h > -1e-3f && h < 0f) h = 0f; // 誤差吸収
            return h;
        }

        /// <summary>下側高さクリップ (y &gt;= lower) Sutherland–Hodgman 風。</summary>
        private static List<Vector3> ClipLower(List<Vector3> poly, float lower, in Vector3 up, float baselineProj, float epsH)
        {
            if (poly.Count == 0) return poly;
            var res = new List<Vector3>(poly.Count + 2);
            Vector3 prev = poly[poly.Count - 1];
            float hPrev = HeightFromBaseline(prev, up, baselineProj);
            bool prevIn = hPrev >= lower - epsH;
            foreach (var curr in poly)
            {
                float hCurr = HeightFromBaseline(curr, up, baselineProj);
                bool currIn = hCurr >= lower - epsH;
                if (prevIn && currIn)
                {
                    res.Add(curr);
                }
                else if (prevIn && !currIn)
                {
                    if (Mathf.Abs(hCurr - hPrev) > 1e-8f)
                    {
                        float t = (lower - hPrev) / (hCurr - hPrev);
                        res.Add(prev + (curr - prev) * t);
                    }
                }
                else if (!prevIn && currIn)
                {
                    if (Mathf.Abs(hCurr - hPrev) > 1e-8f)
                    {
                        float t = (lower - hPrev) / (hCurr - hPrev);
                        res.Add(prev + (curr - prev) * t);
                    }
                    res.Add(curr);
                }
                prev = curr; hPrev = hCurr; prevIn = currIn;
            }
            return res;
        }

        /// <summary>上側高さクリップ (y &lt;= upper) Sutherland–Hodgman 風。</summary>
        private static List<Vector3> ClipUpper(List<Vector3> poly, float upper, in Vector3 up, float baselineProj, float epsH)
        {
            if (poly.Count == 0) return poly;
            var res = new List<Vector3>(poly.Count + 2);
            Vector3 prev = poly[poly.Count - 1];
            float hPrev = HeightFromBaseline(prev, up, baselineProj);
            bool prevIn = hPrev <= upper + epsH;
            foreach (var curr in poly)
            {
                float hCurr = HeightFromBaseline(curr, up, baselineProj);
                bool currIn = hCurr <= upper + epsH;
                if (prevIn && currIn)
                {
                    res.Add(curr);
                }
                else if (prevIn && !currIn)
                {
                    if (Mathf.Abs(hCurr - hPrev) > 1e-8f)
                    {
                        float t = (upper - hPrev) / (hCurr - hPrev);
                        res.Add(prev + (curr - prev) * t);
                    }
                }
                else if (!prevIn && currIn)
                {
                    if (Mathf.Abs(hCurr - hPrev) > 1e-8f)
                    {
                        float t = (upper - hPrev) / (hCurr - hPrev);
                        res.Add(prev + (curr - prev) * t);
                    }
                    res.Add(curr);
                }
                prev = curr; hPrev = hCurr; prevIn = currIn;
            }
            return res;
        }

        /// <summary>多角形面積 (fan triangulation + 指定法線投影) を返す。</summary>
        private static float PolygonArea(List<Vector3> poly, in Vector3 forwardNormal)
        {
            int m = poly.Count;
            if (m < 3) return 0f;
            Vector3 origin = poly[0];
            double sum = 0.0;
            for (int i = 1; i < m - 1; i++)
            {
                Vector3 a = poly[i] - origin;
                Vector3 b = poly[i + 1] - origin;
                Vector3 cross = Vector3.Cross(a, b);
                sum += Vector3.Dot(cross, forwardNormal);
            }
            return Mathf.Abs((float)(0.5 * sum));
        }
    }
}