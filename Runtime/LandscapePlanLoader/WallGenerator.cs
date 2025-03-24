using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 区画メッシュの外周に壁を生成するクラス
    /// </summary>
    public sealed class WallGenerator
    {
        /// <summary>
        /// メッシュ外周に壁を生成するメソッド
        /// </summary>
        /// <param name="originalMesh">天面となる区画メッシュ</param>
        /// <param name="wallHeight">生成する壁オブジェクトの高さ</param>
        /// <param name="wallDirection">生成方向（上下）</param>
        /// <returns>生成した壁のオブジェクト</returns>
        public GameObject[] GenerateWall(Mesh originalMesh, float wallHeight, Vector3 wallDirection, Material wallMaterial)
        {
            Vector3[] vertices = originalMesh.vertices;
            int[] triangles = originalMesh.triangles;

            Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();

            // 天面メッシュの全Triangleのエッジの出現回数をカウント
            for (int i = 0; i < triangles.Length; i += 3)
            {
                CountEdge(edgeCount, vertices, triangles[i], triangles[i + 1]);
                CountEdge(edgeCount, vertices, triangles[i + 1], triangles[i + 2]);
                CountEdge(edgeCount, vertices, triangles[i + 2], triangles[i]);
            }


            List<Edge> outlineEdges = new List<Edge>();

            // 外周を形成するエッジを抽出
            foreach (var item in edgeCount)
            {
                // エッジの出現回数が1のものを外周エッジとして判定
                if (item.Value == 1)
                {
                    outlineEdges.Add(item.Key);
                }
            }

            // 隣接する外周エッジを探索してソート
            List<List<Vector3>> sortedOutlineVerticesList = new List<List<Vector3>>();
            while (outlineEdges.Count != 0)
            {
                // 初期エッジをリストに追加
                List<Vector3> sortedOutlineVertices = new List<Vector3>
                {
                    outlineEdges[0].vertex1,
                    outlineEdges[0].vertex2
                };
                outlineEdges.RemoveAt(0);


                // 隣接エッジの探索
                for (int i = 0; outlineEdges.Count != 0; i++)
                {
                    Vector3 preEndPoint = sortedOutlineVertices[i * 2 + 1];
                    bool isFound = false;

                    for (int j = 0; j < outlineEdges.Count; j++)
                    {
                        if (outlineEdges[j].vertex1 == preEndPoint)
                        {
                            sortedOutlineVertices.Add(outlineEdges[j].vertex1);
                            sortedOutlineVertices.Add(outlineEdges[j].vertex2);
                            outlineEdges.RemoveAt(j);
                            isFound = true;
                            break;
                        }
                        else if (outlineEdges[j].vertex2 == preEndPoint)
                        {
                            sortedOutlineVertices.Add(outlineEdges[j].vertex2);
                            sortedOutlineVertices.Add(outlineEdges[j].vertex1);
                            outlineEdges.RemoveAt(j);
                            isFound = true;
                            break;
                        }
                        isFound = false;
                    }

                    if (!isFound) break;
                }
                sortedOutlineVerticesList.Add(sortedOutlineVertices);
            }


            GameObject[] wallObjects = new GameObject[sortedOutlineVerticesList.Count];
            for (int k = 0; k < sortedOutlineVerticesList.Count; k++)
            {
                List<Vector3> wallVertices = new List<Vector3>();
                List<int> wallTriangles = new List<int>();

                // 壁の頂点とTriangle、UVを生成
                Vector2[] uv = new Vector2[sortedOutlineVerticesList[k].Count * 2];
                float uvXPitch = 1.0f / sortedOutlineVerticesList[k].Count;

                for (int i = 0; i * 2 < sortedOutlineVerticesList[k].Count; i++)
                {
                    Vector3 v1 = sortedOutlineVerticesList[k][i * 2];
                    Vector3 v2 = sortedOutlineVerticesList[k][i * 2 + 1];

                    wallVertices.Add(v1);
                    wallVertices.Add(v2);
                    wallVertices.Add(v1 + wallDirection.normalized * wallHeight);
                    wallVertices.Add(v2 + wallDirection.normalized * wallHeight);

                    uv[i * 4] = new Vector2(i * 2 * uvXPitch, 1);
                    uv[i * 4 + 1] = new Vector2((i * 2 + 1) * uvXPitch, 1);
                    uv[i * 4 + 2] = new Vector2(i * 2 * uvXPitch, 0);
                    uv[i * 4 + 3] = new Vector2((i * 2 + 1) * uvXPitch, 0);

                    int count = wallVertices.Count;
                    wallTriangles.Add(count - 4);
                    wallTriangles.Add(count - 3);
                    wallTriangles.Add(count - 2);
                    wallTriangles.Add(count - 3);
                    wallTriangles.Add(count - 1);
                    wallTriangles.Add(count - 2);
                }

                Mesh wallMesh = new Mesh();
                wallMesh.vertices = wallVertices.ToArray();
                wallMesh.triangles = wallTriangles.ToArray();
                wallMesh.uv = uv;

                GameObject wallObject = new GameObject("Wall");
                MeshFilter meshFilter = wallObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = wallObject.AddComponent<MeshRenderer>();
                meshFilter.mesh = wallMesh;
                meshRenderer.material = wallMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                wallObjects[k] = wallObject;
            }

            return wallObjects;
        }

        /// <summary>
        /// エッジの重複数をカウントするメソッド
        /// </summary>
        /// <param name="edgeCount">重複数を記録するディクショナリ</param>
        /// <param name="vertices">頂点座標配列</param>
        /// <param name="v1">対象エッジの両端の頂点のindex</param>
        /// <param name="v2">対象エッジの両端の頂点のindex</param>
        void CountEdge(Dictionary<Edge, int> edgeCount, Vector3[] vertices, int v1, int v2)
        {
            Edge edge = new Edge(vertices[v1], vertices[v2]);
            if (edgeCount.ContainsKey(edge))
            {
                edgeCount[edge]++;
            }
            else
            {
                edgeCount[edge] = 1;
            }
        }

        struct Edge
        {
            public Vector3 vertex1;
            public Vector3 vertex2;

            public Edge(Vector3 vertex1, Vector3 vertex2)
            {
                this.vertex1 = vertex1;
                this.vertex2 = vertex2;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Edge))
                    return false;

                Edge other = (Edge)obj;
                return (vertex1 == other.vertex1 && vertex2 == other.vertex2) ||
                       (vertex1 == other.vertex2 && vertex2 == other.vertex1);
            }

            public override int GetHashCode()
            {
                return vertex1.GetHashCode() ^ vertex2.GetHashCode();
            }
        }
    }
}

