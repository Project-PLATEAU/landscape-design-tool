using PlateauToolkit.Sandbox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Landscape2.Runtime.Common
{
    /// <summary>
    /// メッシュ関連のユーティリティクラス
    /// </summary>
    internal static class MeshUtil
    {
        /// <summary>
        /// 指定したMeshFilterのBoundsのAABBコーナー8点をワールド座標で返す
        /// </summary>
        public static List<Vector3> GetWorldCorners(Bounds bounds, Transform transform)
        {
            var corners = new List<Vector3>(8);
            var center = bounds.center;
            var extents = bounds.extents;
            var tf = transform;

            // ビット演算で8通りの組み合わせを生成
            for (int i = 0; i < 8; i++)
            {
                float x = ((i & 1) == 0) ? -extents.x : extents.x;
                float y = ((i & 2) == 0) ? -extents.y : extents.y;
                float z = ((i & 4) == 0) ? -extents.z : extents.z;

                Vector3 localCorner = center + new Vector3(x, y, z);
                Vector3 worldCorner = tf.TransformPoint(localCorner);
                corners.Add(worldCorner);
            }

            return corners;
        }



        // Vector2 版：射影法（Ray Casting）で判定
        public static bool IsPointInPolygon(Vector2 point, List<Vector2> outline)
        {
            if (outline == null || outline.Count < 3)
                return false;

            bool inside = false;
            int count = outline.Count;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Vector2 vi = outline[i];
                Vector2 vj = outline[j];

                // Y軸方向のエッジ交差チェック
                bool intersects = (vi.y > point.y) != (vj.y > point.y)
                                    && point.x < (vj.x - vi.x) * (point.y - vi.y) / (vj.y - vi.y) + vi.x;
                if (intersects)
                    inside = !inside;
            }

            return inside;
        }

        // Vector3 版：X/Y 成分のみ取り出して判定
        public static bool IsPointInPolygon(Vector3 point, List<Vector3> outline)
        {
            var outline2D = outline.Select(v => new Vector2(v.x, v.y)).ToList();
            return IsPointInPolygon(new Vector2(point.x, point.y), outline2D);
        }

        public interface IEdge
        {
            Vector3 Start { get; }
            Vector3 End { get; }
            void Translate(Vector3 vec);
        }

        /// <summary>
        /// エッジを表すシンプルなクラス
        /// </summary>
        public class Edge : IEdge
        {
            public Vector3 Start { get; private set; }
            public Vector3 End { get; private set; }

            /// <summary>
            /// エッジを平行移動する
            /// </summary>
            /// <param name="vec"></param>
            public void Translate(Vector3 vec)
            {
                Start += vec;
                End += vec;
            }

            public Edge(Vector3 start, Vector3 end)
            {
                Start = start;
                End = end;
            }
        }

        public class EdgeWithAttribute<_Attribute> : Edge
        {
            public _Attribute Attribute { get; set; }
            public EdgeWithAttribute(Vector3 start, Vector3 end, _Attribute attribute) : base(start, end)
            {
                Attribute = attribute;
            }

            public EdgeWithAttribute(Edge edge, _Attribute attribute) : base(edge.Start, edge.End)
            {
                Attribute = attribute;
            }

        }


        /// <summary>
        /// エッジに関するユーティリティクラス
        /// </summary>
        public static class EdgeUtility
        {
            /// <summary>
            /// 2頂点 A, B の辺に対し、参照平面法線 planeNormal をもとに法線ベクトルを計算する。
            /// </summary>
            /// <param name="A">始点</param>
            /// <param name="B">終点</param>
            /// <param name="planeNormal">参照する面の法線ベクトル（例: Vector3.up）</param>
            /// <returns>単位化した法線ベクトル</returns>
            public static Vector3 CalculateEdgeNormal(Vector3 A, Vector3 B, Vector3 planeNormal)
            {
                Vector3 edgeDir = (B - A).normalized;
                Vector3 normal = Vector3.Cross(edgeDir, planeNormal).normalized;
                return normal;
            }

            /// <summary>
            /// XZ平面上の辺（Y成分は無視）に限定して法線を求める簡易版。
            /// </summary>
            public static Vector3 CalculateEdgeNormalXZ(Vector3 A, Vector3 B)
            {
                var dir = (B - A);
                Vector3 flatDir = new Vector3(dir.x, 0f, dir.z).normalized;
                // XZ平面での法線：右手系に合わせて ( -z, 0, x )
                return new Vector3(-flatDir.z, 0f, flatDir.x);
            }

            /// <summary>
            /// XY平面上の辺（Z成分は無視）に限定して法線を求める簡易版。
            /// </summary>
            public static Vector3 CalculateEdgeNormalXY(Vector3 A, Vector3 B)
            {
                var dir = (B - A);
                Vector3 flatDir = new Vector3(dir.x, dir.y, 0f).normalized;
                // XY平面での法線： ( -y, x, 0 )
                return new Vector3(-flatDir.y, flatDir.x, 0f);
            }


            /// <summary>
            /// 頂点群から各辺の法線を計算してリストで返す。
            /// </summary>
            /// <param name="vertices">頂点リスト</param>
            /// <param name="closeLoop">
            /// true の場合は最後の頂点→最初の頂点間の辺も含める
            /// </param>
            /// <returns>各辺（隣接ペア）の法線リスト</returns>
            public static List<Vector3> CalculateEdgeNormalsXZ(List<Vector3> vertices, bool closeLoop = false)
            {
                var normals = new List<Vector3>();

                if (vertices == null || vertices.Count < 2)
                    return normals;

                int count = vertices.Count;
                int lastIndex = closeLoop ? count : count - 1;

                for (int i = 0; i < lastIndex; i++)
                {
                    Vector3 current = vertices[i];
                    Vector3 next = (i == count - 1) ? vertices[0] : vertices[i + 1];
                    normals.Add(CalculateEdgeNormalXZ(current, next));
                }

                return normals;
            }

            /// <summary>
            /// 頂点リストから連続エッジを生成する
            /// </summary>
            /// <param name="vertices">頂点座標のリスト</param>
            /// <param name="closed">最後の頂点→最初の頂点をつなぐか</param>
            /// <returns>Edgeのリスト</returns>
            public static List<Edge> CreateEdges(List<Vector3> vertices, bool closed = false)
            {
                var edges = new List<Edge>();

                if (vertices == null || vertices.Count < 2)
                    return edges;

                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    edges.Add(new Edge(vertices[i], vertices[i + 1]));
                }

                if (closed)
                {
                    edges.Add(new Edge(vertices[vertices.Count - 1], vertices[0]));
                }

                return edges;
            }

            public static List<EdgeWithAttribute<_Attribute>> CreateEdges<_Attribute>(List<Vector3> vertices, _Attribute defaultAttribute, bool closed = false)
            {
                var edges = new List<EdgeWithAttribute<_Attribute>>();

                if (vertices == null || vertices.Count < 2)
                    return edges;

                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    edges.Add(new EdgeWithAttribute<_Attribute>(vertices[i], vertices[i + 1], defaultAttribute));
                }

                if (closed)
                {
                    edges.Add(new EdgeWithAttribute<_Attribute>(vertices[vertices.Count - 1], vertices[0], defaultAttribute));
                }

                return edges;
            }
            /// <summary>
            /// ループ状に並んだedgesの隣接ペアの交点一覧を返す
            /// </summary>
            public static List<Vector3> GetAdjacentEdgeIntersections<_Edge>(List<_Edge> edges)
                where _Edge : IEdge
            {
                var result = new List<Vector3>();

                int count = edges.Count;
                if (count < 2) return result;

                // 元の頂点群の並び維持しつつ新しい頂点を計算する

                // そのまま順にエッジ群を走査すると 構成される頂点群はエッジ群を構成する時に使用した頂点群から一つずれるので　本来最初に計算されるべき計算を先に行う
                // (対応無しの時) ex 頂点 a,b,c  a-b, b-c, c-a  -> 交差判定 (a-b, b-c), (b-c, c-a)... 　となるので結果は b´, c´, a´の順になる

                AddNewPoint(result, edges[count - 1], edges[0]);    // 本来の頂点群の最初の要素に紐づく頂点を先に計算
                for (int i = 0; i < count - 1; i++)
                {
                    IEdge e1 = edges[i];
                    IEdge e2 = edges[i + 1];  // count - 1となっているので要素外アクセスとはならない

                    AddNewPoint(result, e1, e2);
                }

                return result;

                static void AddNewPoint(List<Vector3> result, IEdge e1, IEdge e2)
                {
                    if (XZIntersection.LineSegmentIntersectXZ(e1.Start, e1.End, e2.Start, e2.End, out Vector3 inter))
                    {
                        result.Add(inter);
                    }
                    else
                    {
                        // 交差しない場合は中間点でごまかす
                        result.Add((e1.End + e2.Start) / 2.0f);
                    }
                }
            }
        }

        public static class XZIntersection
        {
            /// <summary>
            /// XZ平面上で2本の線分(p1→p2, p3→p4)が交差するか判定し、
            /// 交差点を返します。交差しなければfalseを返し、intersectionはVector3.zeroになります。
            /// </summary>
            public static bool LineSegmentIntersectXZ(
                Vector3 p1, Vector3 p2,
                Vector3 p3, Vector3 p4,
                out Vector3 intersection)
            {
                intersection = Vector3.zero;

                // 2D投影
                Vector2 a = new Vector2(p1.x, p1.z);
                Vector2 b = new Vector2(p2.x, p2.z);
                Vector2 c = new Vector2(p3.x, p3.z);
                Vector2 d = new Vector2(p4.x, p4.z);

                Vector2 ab = b - a;
                Vector2 cd = d - c;

                // 行列式（2Dクロス積）を計算
                float det = ab.x * cd.y - ab.y * cd.x;
                if (Mathf.Abs(det) < Mathf.Epsilon)
                {
                    // det=0 → 平行または同一直線上
                    return false;
                }

                // p1からp3へのベクトル
                Vector2 ac = c - a;

                // t を求める (a + t * ab == intersection)
                float t = (ac.x * cd.y - ac.y * cd.x) / det;

                // XZ平面での交点
                Vector2 hit2D = a + ab * t;

                // Y座標も直線 p1→p2 に沿って補間しておく
                float hitY = p1.y + (p2.y - p1.y) * t;

                intersection = new Vector3(hit2D.x, hitY, hit2D.y);
                return true;
            }
        }

        /// <summary>
        /// エッジを表す構造体
        /// </summary>
        private struct EdgeIndices
        {
            public int v1;
            public int v2;
            bool isReversed;
            public Vector2Int Original
            {
                get =>
                    isReversed ? new Vector2Int(v2, v1) : new Vector2Int(v1, v2);
            }

            public EdgeIndices(int a, int b)
            {
                // 常に小さい番号をv1に合わせる
                if (a < b)
                {
                    isReversed = false;
                    v1 = a; v2 = b;
                }
                else
                {
                    isReversed = true;
                    v1 = b; v2 = a;
                }
            }

            // Dictionaryのキーに使うためにGetHashCode/Equalsをオーバーライド
            public override int GetHashCode() => v1 * 31 + v2;
            public override bool Equals(object obj)
            {
                if (!(obj is EdgeIndices)) return false;
                var e = (EdgeIndices)obj;
                return v1 == e.v1 && v2 == e.v2;
            }

            /// <summary>
            /// エッジが有効かどうかを判定する
            /// </summary>
            /// <returns></returns>
            public bool IsValid() { return v1 != v2; }
        }



        /// <summary>
        /// メッシュのアウトラインを抽出するクラス
        /// </summary>
        public static class OutlineExtractor
        {
            /// <summary>
            /// アウトラインに位置する頂点群を返す関数
            /// 
            /// 対応：
            /// アウトラインが一つである
            /// アウトラインを構成する頂点はアウトラインを構成するちょうど2本のエッジから利用されている。（一筆書きした時に一通りしかない）
            /// 
            /// 未対応例：
            /// 穴あきメッシュ（アウトラインが一つであることを前提に実装しているため。穴あきを許すとアウトラインが複数になる。）
            /// 2枚の三角形ポリゴンが一点を共有するメッシュ（三角形が頂点ABCで構成されAを別の三角形で共有している時、ループ検出処理でAの次にB,Cのどちらに進むか判断できないため）
            /// </summary>
            /// <param name="mesh"></param>
            /// <returns></returns>
            public static List<Vector3> GetOutlineVertices(UnityEngine.Mesh mesh)
            {
                var vertices = mesh.vertices;       // ローカル座標
                var indices = mesh.triangles;      // 三角形インデックス
                var edgeCount = new Dictionary<EdgeIndices, int>();

                // 1. エッジをカウント
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int i0 = indices[i];
                    int i1 = indices[i + 1];
                    int i2 = indices[i + 2];

                    var edges = new EdgeIndices[]
                    {
                        new EdgeIndices(i0, i1),
                        new EdgeIndices(i1, i2),
                        new EdgeIndices(i2, i0)
                    };

                    foreach (var e in edges)
                    {
                        if (edgeCount.ContainsKey(e))
                            edgeCount[e]++;
                        else
                            edgeCount[e] = 1;
                    }
                }

                // 2. 使用回数が1のエッジだけを取り出す
                var borderEdges = new List<EdgeIndices>();
                foreach (var kv in edgeCount)
                    if (kv.Value == 1)
                        borderEdges.Add(kv.Key);


                // 穴あきに対応する場合はborderEdgesが空になるまでループで回せばいけそう
                // エラーでポリゴンではなくエッジ単体で存在する場合があれば正常に動作しない

                // ループするインデックス群 
                var loopIndices = CreateLoopIndices(ref borderEdges);
                if (loopIndices.Count == 0)
                    return null;

                // 重複してはいけない
                if (ContainsDuplicate(loopIndices) == true)
                {
                    // 重複は発生しないはず
                    Debug.LogWarning("Outline indices contain duplicates, this may cause unexpected behavior.");
                    return null;
                }



                // 頂点配列を生成
                var outlineVerts = new List<Vector3>(loopIndices.Count);
                foreach (var idx in loopIndices)
                {
                    outlineVerts.Add(vertices[idx]);
                }
                return outlineVerts;

                bool ContainsDuplicate(List<int> list)
                {
                    var set = new HashSet<int>();
                    foreach (var x in list)
                    {
                        // すでに同じ値があれば Add() は false を返す
                        if (!set.Add(x))
                            return true;
                    }
                    return false;
                }

                static List<int> CreateLoopIndices(ref List<EdgeIndices> borderEdges)
                {
                    var loopIndices = new List<int>();

                    var startEdge = borderEdges[0]; // 最初のエッジをスタートとして使用
                    loopIndices.Add(startEdge.Original.x); // 最初の頂点を追加
                    var nextIdx = startEdge.Original.y; // 次の頂点はv2から始める
                    borderEdges.Remove(startEdge);  // 参照済みのエッジを削除　複数回使用されるエッジは含まれていないことが前提
                    while (nextIdx != loopIndices[0])
                    {
                        loopIndices.Add(nextIdx); // 次の頂点を追加
                        // Edgeはインデックスを若い順で並び替えるため両方確認が必要。
                        var nextEdges = borderEdges.FindAll(edge => { return edge.Original.x == nextIdx; });

                        // 同じインデックスを複数回使用している場合には対応していない。僅かに隙間が空いた一点のみ共有した三角形ポリゴンはこれにあたる
                        if (nextEdges.Count > 1)
                        {
                            Debug.LogWarning("Mesh must be loop. Multiple edges found for vertex: " + nextIdx);
                            loopIndices.Clear();
                            break;
                        }
                        if (nextEdges.Count == 0)
                        {
                            // loopが見つけられなかった
                            Debug.LogWarning("Mesh must be loop.");
                            loopIndices.Clear();
                            break;
                        }
                        var nextEdge = nextEdges[0];
                        nextIdx = nextEdge.Original.y; // 次の頂点を更新
                        borderEdges.Remove(nextEdge); // 参照済みのエッジを削除
                    }

                    return loopIndices;
                }

            }
        }

        /// <summary>
        /// 頂点リスト内の2点間で最も離れている距離を返す。
        /// </summary>
        public static float GetMaxDistance(List<Vector3> vertices)
        {
            if (vertices == null || vertices.Count < 2)
            {
                Debug.LogWarning("頂点リストが null か要素数が不足しています。");
                return 0f;
            }

            float maxDistSq = 0f;

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                for (int j = i + 1; j < vertices.Count; j++)
                {
                    float distSq = (vertices[i] - vertices[j]).sqrMagnitude;
                    if (distSq > maxDistSq)
                    {
                        maxDistSq = distSq;
                    }
                }
            }

            // 最終的に平方根を取って実際の距離を返す
            return Mathf.Sqrt(maxDistSq);
        }

        /// <summary>
        /// 頂点リストの隣接する2点間で最も離れている距離を返します。
        /// ループを有効にすると、最後と最初の頂点も隣接として扱います。
        /// </summary>
        /// <param name="vertices">頂点群のリスト</param>
        /// <param name="loop">true にすると最後と最初の頂点間も比較</param>
        /// <returns>最大距離</returns>
        public static float GetMaxAdjacentDistance(List<Vector3> vertices, bool loop = false)
        {
            if (vertices == null || vertices.Count < 2)
            {
                Debug.LogWarning("頂点リストが null か要素数が不足しています。");
                return 0f;
            }

            float maxDistSq = 0f;
            int count = vertices.Count;

            // 頂点[i] と 頂点[i+1] を比較
            for (int i = 0; i < count - 1; i++)
            {
                float distSq = (vertices[i] - vertices[i + 1]).sqrMagnitude;
                if (distSq > maxDistSq)
                    maxDistSq = distSq;
            }

            // ループ時は最後と最初も比較
            if (loop)
            {
                float loopDistSq = (vertices[count - 1] - vertices[0]).sqrMagnitude;
                if (loopDistSq > maxDistSq)
                    maxDistSq = loopDistSq;
            }

            return Mathf.Sqrt(maxDistSq);
        }

        /// <summary>
        /// 辺長から正三角形の面積を計算する。
        /// </summary>
        public static float GetEquilateralTriangleArea(float sideLength)
        {
            return Mathf.Sqrt(3f) / 4f * sideLength * sideLength;
        }

        /// <summary>
        /// ３点から中央頂点の法線方向を計算します。
        /// Y軸を上向きベクトルとし、隣接辺の平均方向を基に法線を求めます。
        /// </summary>
        /// <param name="prev">前の頂点</param>
        /// <param name="current">中央の頂点</param>
        /// <param name="next">次の頂点</param>
        /// <returns>中央頂点における法線方向（単位ベクトル）</returns>
        public static Vector3 CalculateVertexNormal(Vector3 prev, Vector3 current, Vector3 next)
        {
            // 隣接する２辺の方向を取得
            Vector3 dir1 = (current - prev).normalized;
            Vector3 dir2 = (next - current).normalized;

            // 両方向ベクトルを合成してタンジェント方向を算出
            Vector3 tangent = (dir1 + dir2);
            if (tangent.sqrMagnitude < 1e-6f)
            {
                // ほぼ一直線上の場合は一方の方向を採用
                tangent = dir2;
            }
            tangent.Normalize();

            // Y軸を上方向としたときの垂直ベクトル（法線）を計算
            Vector3 normal = Vector3.Cross(Vector3.up, tangent).normalized;
            return normal;
        }

        /// <summary>
        /// 頂点リストを一周ループとして扱い、すべての頂点の法線配列を返します。
        /// </summary>
        /// <param name="vertices">頂点の連続リスト</param>
        /// <returns>各頂点に対応した法線ベクトル配列</returns>
        public static Vector3[] CalculateNormalsForLoop(List<Vector3> vertices, bool isReverseNormal)
        {
            int count = vertices.Count;
            if (count < 3)
                return new Vector3[count];

            Vector3[] normals = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                int prevIndex = (i - 1 + count) % count;
                int nextIndex = (i + 1) % count;

                Vector3 prev = vertices[prevIndex];
                Vector3 current = vertices[i];
                Vector3 next = vertices[nextIndex];

                normals[i] = CalculateVertexNormal(prev, current, next);
                if (isReverseNormal)
                    normals[i] = -normals[i]; // 法線を反転する場合は符号を反転
            }

            return normals;
        }

        /// <summary>
        /// threshold 以下の距離にある頂点をまとめ、代表点として平均位置を返す。
        /// </summary>
        /// <param name="vertices">入力頂点リスト</param>
        /// <param name="threshold">結合距離の閾値</param>
        /// <returns>重複排除＆平均化後の頂点リスト</returns>
        public static List<Vector3> MergeNearbyVertices(List<Vector3> vertices, float threshold)
        {
            var merged = new List<Vector3>();
            var used = new bool[vertices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                if (used[i]) continue;

                // 新しいクラスタを作成
                Vector3 sum = vertices[i];
                int count = 1;
                used[i] = true;

                // 閾値内にある頂点をクラスタ化
                for (int j = i + 1; j < vertices.Count; j++)
                {
                    if (used[j]) continue;
                    if (Vector3.Distance(vertices[i], vertices[j]) <= threshold)
                    {
                        sum += vertices[j];
                        count++;
                        used[j] = true;
                    }
                }

                // 平均位置を代表点として登録
                merged.Add(sum / count);
            }

            return merged;
        }

        /// <summary>
        /// 隣接する頂点間の距離が threshold 以下の場合に平均化してマージします。
        /// 入力は閉ループを想定。
        /// </summary>
        public static List<Vector3> MergeCloseVertices(List<Vector3> points, float threshold)
        {
            if (points == null || points.Count <= 1)
                return points == null ? new List<Vector3>() : new List<Vector3>(points);

            float sqrThreshold = threshold * threshold;
            List<Vector3> merged = new List<Vector3>();
            merged.Add(points[0]);

            // 1. 逐次走査して隣接頂点をマージ
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 last = merged[merged.Count - 1];
                Vector3 curr = points[i];

                if ((last - curr).sqrMagnitude <= sqrThreshold)
                {
                    // 平均化してマージ
                    merged[merged.Count - 1] = (last + curr) * 0.5f;
                }
                else
                {
                    merged.Add(curr);
                }
            }

            // 2. ループの閉じ部分(最後と最初)もチェック
            if (merged.Count > 1)
            {
                Vector3 first = merged[0];
                Vector3 last = merged[merged.Count - 1];

                if ((last - first).sqrMagnitude <= sqrThreshold)
                {
                    Vector3 avg = (first + last) * 0.5f;
                    // 両端を同一位置に揃える
                    merged[0] = avg;
                    merged[merged.Count - 1] = avg;
                }
            }

            return merged;
        }

        /// <summary>
        /// アウトライン（ポリゴン）に対して、任意の点が内側にあるかを判定します。
        /// ポリゴンは凹凸どちらでもOK。XY 平面上にある前提ですが、任意の方向のポリゴンも対応可能です。
        /// </summary>
        /// <param name="outline">アウトラインを構成する頂点リスト（ワールド空間）</param>
        /// <param name="testPoint">判定したい点（ワールド空間）</param>
        /// <returns>内側なら true、外側または境界上なら false</returns>
        public static bool IsPointInsideOutline(List<Vector3> outline, Vector3 testPoint)
        {
            // 頂点数チェック
            if (outline == null || outline.Count < 3)
                return false;

            // 1) ポリゴン面の法線を算出
            Vector3 v0 = outline[1] - outline[0];
            Vector3 v1 = outline[2] - outline[0];
            Vector3 normal = Vector3.Cross(v0, v1).normalized;

            // 2) 2D に投影するためのローカル軸を作成
            Vector3 axisX = v0.normalized;
            Vector3 axisY = Vector3.Cross(normal, axisX).normalized;

            // 3) ポリゴン頂点をローカル 2D 座標に変換
            int n = outline.Count;
            Vector2[] poly2D = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 local = outline[i] - outline[0];
                poly2D[i] = new Vector2(Vector3.Dot(local, axisX), Vector3.Dot(local, axisY));
            }

            // 4) 判定点も同様に変換
            Vector3 localP = testPoint - outline[0];
            Vector2 point2D = new Vector2(Vector3.Dot(localP, axisX), Vector3.Dot(localP, axisY));

            // 5) 2D の射影ポリゴン点内判定（レイキャスト法）
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 pi = poly2D[i];
                Vector2 pj = poly2D[j];

                bool intersect = ((pi.y > point2D.y) != (pj.y > point2D.y)) &&
                                 (point2D.x < (pj.x - pi.x) * (point2D.y - pi.y) / (pj.y - pi.y) + pi.x);
                if (intersect)
                    inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// ほぼ直線上の中間頂点を削除し、折れ曲がりが大きい頂点のみを残します。
        /// </summary>
        /// <param name="vertices">
        /// ループ (閉じたポリライン) を構成する頂点リスト。
        /// </param>
        /// <param name="angleThresholdDeg">
        /// ３頂点間の折れ曲がり角がこの角度以下なら中間頂点を削除。
        /// 例：10度以下ならほぼ直線とみなす。
        /// </param>
        /// <returns>
        /// 折れ曲がりが大きい頂点のみを残した簡略化ループ。
        /// </returns>
        public static List<Vector3> MergeCollinear(
            List<Vector3> vertices,
            float angleThresholdDeg = 10f
            )
        {
            if (vertices == null || vertices.Count < 3)
                return new List<Vector3>(vertices);

            var result = new List<int>();
            float cosThreshold = Mathf.Cos(angleThresholdDeg * Mathf.Deg2Rad);
            int n = vertices.Count;

            for (int i = 0; i < n; i++)
            {
                // 前、現在、次 のインデックス（ループさせる）
                Vector3 prev = vertices[(i - 1 + n) % n];
                Vector3 curr = vertices[i];
                Vector3 next = vertices[(i + 1) % n];

                var p2c = (curr - prev);
                var c2n = (next - curr);

                // ベクトルの方向を正規化
                Vector3 dir1 = p2c.normalized;
                Vector3 dir2 = c2n.normalized;

                // 内積が cos(しきい値) 以上 → 折れ曲がり角が小さい → ほぼ直線
                // ほぼ直線上の中間頂点は削除
                if (Vector3.Dot(dir1, dir2) > cosThreshold)
                {
                    // 削除対象に追加
                    result.Add(i);
                    i++; // 次の頂点もスキップ 連続で削除対象に angleThresholdDegが蓄積されるため ex a->b->c->d で b,cが連続して削除される場合、a->dはab,bc,cdの角度が累積されてしまう。
                }
            }

            // インデックスが大きい順に参照できるようにする　別の配列の要素を後ろから削除するため
            result.Sort();  // nextに0が入る時の対策　ex 1,3,0 -> 0,1,3
            result.Reverse(); // 要素の後ろから削除したいので

            // 新しい頂点リストを用意
            var newVerts = new List<Vector3>(vertices);
            foreach (var item in result)
            {
                newVerts.RemoveAt(item);
            }
            return newVerts;
        }

        public class Vector3EpsilonComparer : IEqualityComparer<Vector3>
        {
            private readonly float eps;

            public Vector3EpsilonComparer(float epsilon = 1e-4f)
            {
                eps = epsilon;
            }

            public bool Equals(Vector3 a, Vector3 b)
            {
                return Mathf.Abs(a.x - b.x) < eps
                    && Mathf.Abs(a.y - b.y) < eps
                    && Mathf.Abs(a.z - b.z) < eps;
            }

            public int GetHashCode(Vector3 v)
            {
                // ε単位で丸めてからハッシュを生成
                int x = Mathf.RoundToInt(v.x / eps);
                int y = Mathf.RoundToInt(v.y / eps);
                int z = Mathf.RoundToInt(v.z / eps);
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + x;
                    hash = hash * 31 + y;
                    hash = hash * 31 + z;
                    return hash;
                }

            }
        }

        /// <summary>
        /// ε誤差内で一致する頂点を共通頂点として返す。
        /// </summary>
        public static List<Vector3> GetSharedVerticesApprox(
            IReadOnlyList<List<Vector3>> outlines,
            float epsilon = 1e-4f)
        {
            var comparer = new Vector3EpsilonComparer(epsilon);
            var countMap = new Dictionary<Vector3, int>(comparer);

            for (int i = 0; i < outlines.Count; i++)
            {
                foreach (var v in outlines[i].Distinct(comparer))
                {
                    if (countMap.ContainsKey(v))
                        countMap[v]++;
                    else
                        countMap[v] = 1;
                }
            }

            return countMap
                .Where(kvp => kvp.Value > 1)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        internal static class DebugDrawer
        {
            public static bool IsDebugEnabled
            {
                get;
                set;
            } = true;

            public static bool IsVretsEnabled
            {
                get;
                set;
            } = true;

            public static bool IsEdgesEnabled
            {
                get;
                set;
            } = true;

            [System.Diagnostics.Conditional("UNITY_EDITOR")]
            public static void DrawEdges<_Edge>(List<_Edge> edges, List<Vector3> edgeNormals, Color edgeColor, Color normalColor, float duration = 50.0f, float normalLength = 50.0f)
                where _Edge : IEdge
            {
                if (IsDebugEnabled == false)
                    return;

                if (IsEdgesEnabled == false)
                    return;

                for (var i = 0; i < edges.Count; i++)
                {
                    Debug.DrawLine(edges[i].Start, edges[i].End, edgeColor, duration, false);
                    var center = (edges[i].Start + edges[i].End) / 2;
                    Debug.DrawLine(center, center + edgeNormals[i] * normalLength, normalColor, duration, false);
                }
            }

            [System.Diagnostics.Conditional("UNITY_EDITOR")]
            public static void DrawVerts(List<Vector3> intersectionPoints, Color color, float duration = 50.0f, float size = 30.0f)
            {
                if (IsDebugEnabled == false)
                    return;

                if (IsVretsEnabled == false)
                    return;

                foreach (var item in intersectionPoints)
                {
                    Debug.DrawLine(item, item + Vector3.up * size, color, duration, false);
                }
            }
        }

    }
}
