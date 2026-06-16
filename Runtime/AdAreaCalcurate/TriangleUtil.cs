using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{

    public static class TriangleUtil
    {
        // ========= 1) インデックス一致（同一エッジ＝2頂点一致）で連結面を取る（既出） =========
        public static List<int[]> GetConnectedSurface(int[] start, List<int[]> triangles)
        {
            if (start == null || start.Length != 3) throw new ArgumentException("start must be int[3].");
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));

            var result = new List<int[]>();
            DfsByIndex(start, triangles, result);
            return result;
        }

        private static void DfsByIndex(int[] current, List<int[]> triangles, List<int[]> result)
        {
            if (!ContainsTriangleByIndices(result, current))
            {
                result.Add(current);
            }
            else
            {
                return;
            }

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                if (tri == null || tri.Length != 3) continue;
                if (IsSameTriangleIndices(current, tri)) continue;
                if (SharesEdgeByIndices(current, tri))
                {
                    if (!ContainsTriangleByIndices(result, tri))
                    {
                        DfsByIndex(tri, triangles, result);
                    }
                }
            }
        }

        private static bool SharesEdgeByIndices(int[] a, int[] b)
        {
            return EdgeEq(a[0], a[1], b[0], b[1]) ||
                   EdgeEq(a[0], a[1], b[1], b[2]) ||
                   EdgeEq(a[0], a[1], b[2], b[0]) ||
                   EdgeEq(a[1], a[2], b[0], b[1]) ||
                   EdgeEq(a[1], a[2], b[1], b[2]) ||
                   EdgeEq(a[1], a[2], b[2], b[0]) ||
                   EdgeEq(a[2], a[0], b[0], b[1]) ||
                   EdgeEq(a[2], a[0], b[1], b[2]) ||
                   EdgeEq(a[2], a[0], b[2], b[0]);
        }

        private static bool EdgeEq(int a, int b, int c, int d)
        {
            return (a == c && b == d) || (a == d && b == c);
        }

        private static bool IsSameTriangleIndices(int[] a, int[] b)
        {
            int[] aa = new int[3] { a[0], a[1], a[2] };
            int[] bb = new int[3] { b[0], b[1], b[2] };
            Array.Sort(aa);
            Array.Sort(bb);
            return aa[0] == bb[0] && aa[1] == bb[1] && aa[2] == bb[2];
        }

        private static bool ContainsTriangleByIndices(List<int[]> list, int[] tri)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (IsSameTriangleIndices(list[i], tri)) return true;
            }
            return false;
        }

        // ========= 2) 位置の“ほぼ一致”で隣接（三角形の2頂点の座標がε以内で一致）とみなす版 =========
        // start から ε 以内で2頂点共有する三角形を再帰で辿って全取得（start を含む）
        public static List<int[]> GetConnectedSurfaceByPosition(Mesh mf, List<int[]> triangles, int[] start, float epsilon = 1e-4f)
        {
            if (mf == null) throw new ArgumentNullException(nameof(mf));
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));
            if (start == null || start.Length != 3) throw new ArgumentException("start must be int[3].");

            var verts = mf.vertices;      // ローカル座標（必要なら TransformPoint に置き換え）
                                          // // ワールド座標で判定したいなら：
                                          // var verts = ToWorldVertices(mf);

            var result = new List<int[]>();
            DfsByPosition(start, triangles, verts, epsilon, result);
            return result;
        }

        private static void DfsByPosition(int[] current, List<int[]> triangles, Vector3[] verts, float eps, List<int[]> result)
        {
            if (!ContainsTriangleByIndices(result, current)) // 訪問管理はインデックス集合でOK
            {
                result.Add(current);
            }
            else
            {
                return;
            }

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                if (tri == null || tri.Length != 3) continue;
                if (IsSameTriangleIndices(current, tri)) continue;

                // “位置”での隣接（2頂点がε以内で一致）
                if (SharesEdgeByPosition(current, tri, verts, eps))
                {
                    if (!ContainsTriangleByIndices(result, tri))
                    {
                        DfsByPosition(tri, triangles, verts, eps, result);
                    }
                }
            }
        }

        // 2頂点が ε 以内で一致していれば「エッジ共有」とみなす
        private static bool SharesEdgeByPosition(int[] a, int[] b, Vector3[] v, float eps)
        {
            int shared = CountSharedVerticesByPosition(a, b, v, eps);
            return shared >= 2;
        }

        // aとbの三角形で、座標が ε 以内で一致する頂点の“組”の個数を数える（各頂点は一度だけマッチ）
        private static int CountSharedVerticesByPosition(int[] a, int[] b, Vector3[] v, float eps)
        {
            bool[] usedB = new bool[3];
            int count = 0;

            for (int i = 0; i < 3; i++)
            {
                Vector3 pa = v[a[i]];
                for (int j = 0; j < 3; j++)
                {
                    if (usedB[j]) continue;
                    Vector3 pb = v[b[j]];
                    if (Near(pa, pb, eps))
                    {
                        usedB[j] = true;
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        private static bool Near(Vector3 a, Vector3 b, float eps)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float dz = a.z - b.z;
            return (dx * dx + dy * dy + dz * dz) <= (eps * eps);
        }

        // ワールド座標で判定したい場合の補助（使うなら上で呼び替え）
        private static Vector3[] ToWorldVertices(MeshFilter mf)
        {
            var local = mf.sharedMesh.vertices;
            var world = new Vector3[local.Length];
            var t = mf.transform;
            for (int i = 0; i < local.Length; i++)
            {
                world[i] = t.TransformPoint(local[i]);
            }
            return world;
        }


        /// <summary>
        /// List<int[]>（各要素が {i0,i1,i2} の三角形）で指定された面の
        /// 面積重み付き法線の総和を正規化して返す（ローカル空間）。
        /// 無効三角形はスキップ。総和が 0 に近ければ Vector3.zero。
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="tris"></param>
        /// <returns></returns>
        public static Vector3 ComputeNormalizedSumNormal(Mesh mesh, List<int[]> tris)
        {
            if (mesh == null || tris == null || tris.Count == 0) return Vector3.zero;
            return ComputeNormalizedSumNormal(mesh.vertices, tris);
        }

        public static Vector3 ComputeNormalizedSumNormal(Vector3[] verts, List<int[]> tris)
        {
            if (verts == null || tris == null || tris.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;

            for (int t = 0; t < tris.Count; t++)
            {
                var tri = tris[t];
                if (tri == null || tri.Length < 3) continue;

                int i0 = tri[0], i1 = tri[1], i2 = tri[2];
                // 範囲チェック（負数/範囲外は弾く）
                if ((uint)i0 >= verts.Length || (uint)i1 >= verts.Length || (uint)i2 >= verts.Length) continue;

                Vector3 v0 = verts[i0];
                Vector3 v1 = verts[i1];
                Vector3 v2 = verts[i2];

                // 面法線（面積重み付き）：(v1-v0)×(v2-v0)
                sum += Vector3.Cross(v1 - v0, v2 - v0);
            }

            float mag = sum.magnitude;
            return mag > 1e-20f ? (sum / mag) : Vector3.zero;
        }

        /// <summary>
        /// int[]（mesh.triangles 形式：i0,i1,i2,i3,i4,i5,...）を直接受け取る版。
        /// </summary>
        public static Vector3 ComputeNormalizedSumNormal(Mesh mesh, int[] triangleIndices)
        {
            if (mesh == null || triangleIndices == null || triangleIndices.Length < 3) return Vector3.zero;

            var verts = mesh.vertices;
            Vector3 sum = Vector3.zero;

            int count = triangleIndices.Length - (triangleIndices.Length % 3);
            for (int i = 0; i < count; i += 3)
            {
                int i0 = triangleIndices[i];
                int i1 = triangleIndices[i + 1];
                int i2 = triangleIndices[i + 2];

                if ((uint)i0 >= verts.Length || (uint)i1 >= verts.Length || (uint)i2 >= verts.Length) continue;

                Vector3 v0 = verts[i0];
                Vector3 v1 = verts[i1];
                Vector3 v2 = verts[i2];

                sum += Vector3.Cross(v1 - v0, v2 - v0);
            }

            float mag = sum.magnitude;
            return mag > 1e-20f ? (sum / mag) : Vector3.zero;
        }

        /// <summary>
        /// 補助：mesh.triangles（int[]）→ List<int[]> に変換したい場合用。
        /// </summary>
        public static List<int[]> ToTriangleList(int[] triangleIndices)
        {
            var list = new List<int[]>(triangleIndices?.Length / 3 ?? 0);
            if (triangleIndices == null) return list;

            int count = triangleIndices.Length - (triangleIndices.Length % 3);
            for (int i = 0; i < count; i += 3)
            {
                list.Add(new int[] { triangleIndices[i], triangleIndices[i + 1], triangleIndices[i + 2] });
            }
            return list;
        }

        /// <summary>
        /// 三角形abcとpの最近傍点を返す。
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // Check if P in vertex region outside A
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return a;

            // Check if P in vertex region outside B
            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return b;

            // Check if P in edge region of AB, if so return projection of P onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                float v = d1 / (d1 - d3);
                return a + v * ab;
            }

            // Check if P in vertex region outside C
            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return c;

            // Check if P in edge region of AC, if so return projection of P onto AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float w = d2 / (d2 - d6);
                return a + w * ac;
            }

            // Check if P in edge region of BC, if so return projection of P onto BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * (c - b);
            }

            // P inside face region. Compute projection of P onto plane
            float denom = 1f / (va + vb + vc);
            float v2 = vb * denom;
            float w2 = vc * denom;
            return a + ab * v2 + ac * w2;
        }
    }
}