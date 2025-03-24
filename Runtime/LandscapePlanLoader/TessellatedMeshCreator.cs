using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using iShape.Geometry.Container;
using iShape.Geometry;
using iShape.Mesh2d;
using iShape.Triangulation.Shape.Delaunay;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 頂点座標データからテッセレーションされたMeshを生成するクラス
    /// </summary>
    public sealed class TessellatedMeshCreator
    {
        /// <summary>
        /// 頂点データをPlainShapeに変換するメソッド
        /// </summary>
        /// <param name="hull">メッシュの外周の頂点座標</param>
        /// <param name="holes">メッシュの穴部分の頂点座標</param>
        public PlainShape ConvertToPlainShape(IntGeom iGeom, Allocator allocator, Vector2[] hull, Vector2[][] holes)
        {
            var iHull = iGeom.Int(hull);
            iHull = RemoveDuplicates(iHull);

            IntShape iShape;
            if (holes != null && holes.Length > 0)
            {
                var iHoles = iGeom.Int(holes);
                
                for (int i = 0; i < iHoles.Length; i++)
                {
                    iHoles[i] = RemoveDuplicates(iHoles[i]);
                }

                iShape = new IntShape(iHull, iHoles);
            }
            else
            {
                iShape = new IntShape(iHull, Array.Empty<IntVector[]>());
            }

            var pShape = new PlainShape(iShape, allocator);

            return pShape;
        }


        /// <summary>
        /// 重複した頂点を削除するメソッド
        /// </summary>
        IntVector[] RemoveDuplicates(IntVector[] vectors)
        {
            List<IntVector> uniqueList = new List<IntVector>();
            HashSet<IntVector> seen = new HashSet<IntVector>();

            foreach (IntVector vector in vectors)
            {
                if (!seen.Contains(vector))
                {
                    seen.Add(vector);
                    uniqueList.Add(vector);
                }
            }

            return uniqueList.ToArray();
        }


        /// <summary>
        /// テッセレーションとMesh生成を行うメソッド
        /// </summary>
        /// <param name="points">メッシュの頂点座標。右回りに並んでいる必要がある。</param>
        /// <param name="meshFilter">生成したメッシュをアタッチするMeshFilter</param>
        /// <param name="tessellateMaxEdge">エッジの最大長</param>
        /// <param name="tessellateMaxArea">Triangleの最大面積</param>
        public void CreateTessellatedMesh(List<List<Vector3>> points, MeshFilter meshFilter,  float tessellateMaxEdge = 30, float tessellateMaxArea = 40)
        {
            var iGeom = IntGeom.DefGeom;
            PlainShape pShape;

            // メッシュに穴がある場合
            if(points.Count > 1)
            {
                int hullVertexCount = points[0].Count;
                Vector2[] hull = new Vector2[hullVertexCount];
                for (int i = 0; i < hullVertexCount; i++)
                {
                    hull[i] = new Vector2(points[0][i].x, points[0][i].z);
                }

                Vector2[][] hole = new Vector2[points.Count - 1][];
                for (int i = 1; i < points.Count; i++)
                {
                    int holeVertexCount = points[i].Count;
                    hole[i - 1] = new Vector2[holeVertexCount];
                    for (int index = 0; index < holeVertexCount; index++)
                    {
                        hole[i - 1][index] = new Vector2(points[i][index].x, points[i][index].z);
                    }
                }

                pShape = ConvertToPlainShape(iGeom, Allocator.Temp, hull, hole);
            }
            else
            {
                int hullVertexCount = points[0].Count;
                Vector2[] hull = new Vector2[hullVertexCount];
                for (int i = 0; i < hullVertexCount; i++)
                {
                    hull[i] = new Vector2(points[0][i].x, points[0][i].z);
                }

                pShape = ConvertToPlainShape(iGeom, Allocator.Temp, hull, null);
            }

            var extraPoints = new NativeArray<IntVector>(0, Allocator.Temp);
            var delaunay = pShape.Delaunay(iGeom.Int(tessellateMaxEdge), extraPoints, Allocator.Temp);

            delaunay.Tessellate(iGeom, tessellateMaxArea);

            extraPoints.Dispose();

            var triangles = delaunay.Indices(Allocator.Temp);
            var vertices = delaunay.Vertices(Allocator.Temp, iGeom, 0);

            delaunay.Dispose();

            var subVertices = new NativeArray<float3>(3, Allocator.Temp);
            var subIndices = new NativeArray<int>(new[] { 0, 1, 2 }, Allocator.Temp);

            var colorMesh = new NativeColorMesh(triangles.Length, Allocator.Temp);


            for (int i = 0; i < triangles.Length; i += 3)
            {

                for (int j = 0; j < 3; j += 1)
                {
                    var v = vertices[triangles[i + j]];
                    subVertices[j] = new float3(v.x, v.z, v.y);
                }

                var subMesh = new StaticPrimitiveMesh(subVertices, subIndices, Allocator.Temp);
                var color = Color.white;

                colorMesh.AddAndDispose(subMesh, color);
            }

            // メッシュを生成
            Mesh mesh = new Mesh();

            subIndices.Dispose();
            subVertices.Dispose();

            vertices.Dispose();
            triangles.Dispose();
            colorMesh.FillAndDispose(mesh);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
            pShape.Dispose();
        }
    }
}
