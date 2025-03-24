using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using TriangleNet.Geometry;
using JetBrains.Annotations;
using PlateauToolkit.Maps;


namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// Shapefileの読み込みと描画を行うクラス
    /// </summary>
    public sealed class ShapefileRenderManager : IDisposable
    {
        List<IShape> m_ListOfShapes = new List<IShape>();
        List<GameObject> m_ListOfGISObjects = new List<GameObject>();
        List<List<List<Vector3>>> m_pointDataLists = new List<List<List<Vector3>>>();
        public Action m_OnRender;

        CesiumGeoreference m_GeoRef;
        readonly string m_FolderPath;
        string m_DbfFilePath;
        [CanBeNull] DbfReader m_DbfReader;
        bool m_DbfIsAvailable;
        SupportedEncoding m_SupportedStringEncoding;
        int m_RenderMode;
        int m_ShapeType;
        GameObject m_PositionMarkerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        Material m_Clockwise;

        string m_CurrentRenderingObject;
        float m_RenderHeight;
        bool m_MergeMeshes;
        bool m_LoopLineRenderer;

        GameObject m_PointDataPrefab;

        public ShapefileRenderManager(string folderPath, int renderMode, float renderHeight, bool mergeMeshes, bool loopLineRenderer, SupportedEncoding supportedEncoding, GameObject pointDataPrefab = null)
        {
            m_GeoRef = GameObject.FindObjectOfType<CesiumGeoreference>();
            m_FolderPath = folderPath;
            m_RenderMode = renderMode;
            m_RenderHeight = renderHeight;
            m_MergeMeshes = mergeMeshes;
            m_LoopLineRenderer = loopLineRenderer;
            m_PointDataPrefab = pointDataPrefab;
            m_SupportedStringEncoding = supportedEncoding;
            m_DbfReader = null;
        }

        public bool Read(float lineWidth, out List<GameObject> listOfGISObjects, out List<List<List<Vector3>>> pointDataLists)
        {
            listOfGISObjects = null;
            pointDataLists = null;

            string[] filePaths = Directory.GetFiles(m_FolderPath, "*.shp");

            if (lineWidth == 0f)
            {
                lineWidth = 10f;
            }

            if (filePaths.Length == 0)
            {
                Debug.LogError("No shapefiles found in the folder");
                return false;
            }

            foreach (string filePath in filePaths)
            {
                if (!ReadShapes(filePath)) return false;

                string dbfFileName = Path.GetFileNameWithoutExtension(filePath);
                m_CurrentRenderingObject = dbfFileName;
                if (File.Exists(m_FolderPath + "/" + dbfFileName + ".dbf"))
                {
                    m_DbfFilePath = m_FolderPath + "/" + dbfFileName + ".dbf";
                    m_DbfIsAvailable = true;
                    m_DbfReader = new DbfReader(m_DbfFilePath, m_SupportedStringEncoding);
                    m_DbfReader.ReadHeader();
                }
                else
                {
                    m_DbfFilePath = "";
                    m_DbfIsAvailable = false;
                }
                DrawShapes(m_CurrentRenderingObject, lineWidth);
            }

            listOfGISObjects = m_ListOfGISObjects;
            pointDataLists = m_pointDataLists;
            return true;
        }

        public bool ReadShapes(string filePath)
        {
            if (m_GeoRef == null)
            {
                Debug.LogError("No CesiumGeoreference found in the scene");
                return false;
            }

            m_Clockwise = Resources.Load<Material>(PlateauToolkitMapsConstants.k_ClockwiseMaterialHdrp);

            if (m_Clockwise == null)
            {
                Debug.LogError("Failed to load materials");
                return false;
            }

            using (ShapefileReader reader = new ShapefileReader(filePath))
            {
                m_ListOfShapes = reader.ReadShapes();
                m_ShapeType = reader.ShapeConstants;
            }
            m_PositionMarkerSphere.transform.SetParent(m_GeoRef.transform);
            m_PositionMarkerSphere.AddComponent<CesiumGlobeAnchor>();
            return true;
        }

        public void DrawShapes(string currentRenderingObjectName, float lineWidth)
        {
            if (m_GeoRef == null)
            {
                return;
            }

            if (m_ListOfShapes.Count > 0)
            {
                int index = 0;

                GameObject rootShpObject = new GameObject(currentRenderingObjectName + "_SHP");
                rootShpObject.transform.parent = m_GeoRef.transform;
                CesiumGlobeAnchor anchor = rootShpObject.AddComponent<CesiumGlobeAnchor>();
                rootShpObject.AddComponent<MeshFilter>();
                rootShpObject.AddComponent<MeshRenderer>();

                GameObject shpLineRendererObject = Resources.Load<GameObject>(PlateauToolkitMapsConstants.k_ShapeParentHdrpPrefab);

                GameObject mesh = Resources.Load<GameObject>(PlateauToolkitMapsConstants.k_MeshObjectPrefab);

                foreach (IShape shape in m_ListOfShapes)
                {
                    DbfRecord record = new DbfRecord();

                    if (m_DbfIsAvailable)
                    {
                        record = m_DbfReader.ReadNextRecord();
                    }
                    if (shpLineRendererObject != null)
                    {
                        switch (m_ShapeType)
                        {
                            case 3:
                            case 5:
                                DrawPolygonOrPolyline(shape, index, lineWidth, shpLineRendererObject, rootShpObject, m_DbfIsAvailable, m_DbfReader, record, mesh);
                                break;
                            case 1:
                                DrawPoint(shape, rootShpObject, m_DbfIsAvailable, m_DbfReader, record);
                                break;
                        }

                        index++;
                    }
                    else
                    {
                        Debug.LogError("Failed to load shpLineRendererObject");
                    }
                }
                if (m_RenderMode == 0)
                {
                    double3 pos = anchor.longitudeLatitudeHeight;
                    pos.z = m_RenderHeight;
                    anchor.longitudeLatitudeHeight = pos;

                    if (rootShpObject.GetComponent<MeshFilter>() != null && rootShpObject.GetComponent<MeshRenderer>() != null && m_MergeMeshes)
                    {
                        rootShpObject.AddComponent<MeshCombiner>().CombineMeshes();
                        GameObject mergedMeshes = new GameObject(rootShpObject.name + "_merged");
                        mergedMeshes.AddComponent<MeshFilter>().mesh = rootShpObject.GetComponent<MeshCombiner>().CombinedMesh;
                        mergedMeshes.AddComponent<MeshRenderer>().material = m_Clockwise;
                        mergedMeshes.transform.parent = m_GeoRef.transform;
                        mergedMeshes.AddComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = anchor.longitudeLatitudeHeight;
                        GameObject.DestroyImmediate(rootShpObject);
                    }
                }
            }
            if (m_DbfReader != null)
            {
                m_DbfReader.Dispose();
            }
        }

        void DrawPoint(IShape shape, GameObject parentObject, bool dbfRead, DbfReader dbfReader, DbfRecord record)
        {
            GameObject markerObjectDefault = Resources.Load<GameObject>(PlateauToolkitMapsConstants.k_PointMarkerHdrpPrefab);

            foreach (Vector3 point in shape.Points)
            {
                double3 coordinates = new(point.x, point.z, m_RenderHeight);
                m_PositionMarkerSphere.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = coordinates;
                Vector3 pointPos = m_PositionMarkerSphere.transform.position;
                GameObject marker = m_PointDataPrefab == null ? GameObject.Instantiate(markerObjectDefault) : GameObject.Instantiate(m_PointDataPrefab);
                marker.name = "point_data";
                marker.transform.parent = parentObject.transform;
                marker.AddComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = coordinates;
                if (!string.IsNullOrEmpty(m_DbfFilePath) && dbfRead && dbfReader.GetRecordLength() == m_ListOfShapes.Count)
                {
                    AttachMetadata(marker, record);
                }
            }
        }

        void DrawPolygonOrPolyline(IShape shape, int index, float lineWidth, GameObject shapeParent, GameObject parentObject, bool dbfRead, DbfReader dbfReader, DbfRecord record, GameObject mesh)
        {
            List<List<Vector3>> partPointsWorldList = new List<List<Vector3>>();

            for (int i = 0; i < shape.Parts.Count - 1; i++)
            {
                int start = shape.Parts[i];
                int end = shape.Parts[i + 1];

                // 頂点を取得
                List<Vector3> partPoints = shape.Points.GetRange(start, end - start);
                List<Vector3> partPointsWorld = new List<Vector3>();

                foreach (Vector3 point in partPoints)
                {
                    double3 coordinates = new(point.x, point.z, m_RenderHeight);
                    m_PositionMarkerSphere.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = coordinates;
                    Vector3 pointPos = m_PositionMarkerSphere.transform.position;
                    partPointsWorld.Add(pointPos);
                }

                if (m_RenderMode == 1)
                {
                    GameObject shpParentInstance = GameObject.Instantiate(shapeParent);

                    shpParentInstance.transform.position = Vector3.zero;
                    shpParentInstance.name = "shpParent_" + index;

                    shpParentInstance.transform.parent = parentObject.transform;
                    LineRenderer lineRenderer = shpParentInstance.GetComponent<LineRenderer>();
                    lineRenderer.positionCount = partPointsWorld.Count;
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.startWidth = lineWidth;
                    lineRenderer.endWidth = lineWidth;
                    lineRenderer.SetPositions(partPointsWorld.ToArray());
                    if (m_LoopLineRenderer)
                    {
                        lineRenderer.loop = true;
                    }
                    else
                    {
                        lineRenderer.loop = false;
                    }
                    if (!string.IsNullOrEmpty(m_DbfFilePath) && dbfRead && dbfReader.GetRecordLength() == m_ListOfShapes.Count)
                    {
                        AttachMetadata(shpParentInstance, record);
                    }

                    m_ListOfGISObjects.Add(shpParentInstance);
                }
                else if (m_RenderMode == 0)
                {
                    partPointsWorldList.Add(partPointsWorld);
                }
                else
                {
                    Debug.LogError("Failed to instantiate shpLineRendererObject");
                }
            }

            if (m_RenderMode == 0)
            {
                GameObject meshObject = GameObject.Instantiate(mesh);

                meshObject.transform.position = Vector3.zero;
                meshObject.transform.parent = parentObject.transform;
                if (!string.IsNullOrEmpty(m_DbfFilePath) && dbfRead && dbfReader.GetRecordLength() == m_ListOfShapes.Count)
                {
                    AttachMetadata(meshObject, record);
                }

                // テッセレーション処理を行ったメッシュを生成
                TessellatedMeshCreator tessellatedMeshCreator = new TessellatedMeshCreator();
                MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
                tessellatedMeshCreator.CreateTessellatedMesh(partPointsWorldList, meshFilter, 30, 40);

                m_pointDataLists.Add(partPointsWorldList);
                m_ListOfGISObjects.Add(meshObject);
            }
        }

        public void CreateMesh(bool isHole, List<Vector3> points, MeshFilter meshFilter, MeshRenderer meshRenderer)
        {
            var vertices = new List<Vertex>();

            for (int k = 0; k < points.Count; k++)
            {
                vertices.Add(new TriangleNet.Geometry.Vertex(points[k].x, points[k].z));
            }

            Polygon polygon = new Polygon();
            polygon.Add(new Contour(vertices), isHole);


            TriangleNet.Meshing.IMesh mesh = polygon.Triangulate();

            List<int> triangles = new List<int>();
            List<Vector3> unityVertices = new List<Vector3>();

            foreach (TriangleNet.Topology.Triangle triangle in mesh.Triangles)
            {
                unityVertices.Add(new Vector3((float)triangle.GetVertex(0).X, 0, (float)triangle.GetVertex(0).Y)); // Assume Y is up
                unityVertices.Add(new Vector3((float)triangle.GetVertex(1).X, 0, (float)triangle.GetVertex(1).Y));
                unityVertices.Add(new Vector3((float)triangle.GetVertex(2).X, 0, (float)triangle.GetVertex(2).Y));

                // triangleの頂点をリストに追加
                triangles.Add(unityVertices.Count - 1);
                triangles.Add(unityVertices.Count - 2);
                triangles.Add(unityVertices.Count - 3);
            }

            // メッシュを生成
            Mesh unityMesh = new Mesh();
            unityMesh.vertices = unityVertices.ToArray();
            unityMesh.triangles = triangles.ToArray();

            unityMesh.RecalculateNormals();
            meshFilter.sharedMesh = unityMesh;

            meshRenderer.sharedMaterial = m_Clockwise;
        }

        public void AttachMetadata(GameObject gisObj, DbfRecord record)
        {
            DbfComponent dbfComponent = gisObj.AddComponent<DbfComponent>();
            foreach (var attr in record.FieldNames)
            {
                dbfComponent.PropertyNames.Add($"{attr}");
            }
            foreach (var attr in record.Fields)
            {
                dbfComponent.Properties.Add($"{attr}");
            }
        }

        public void Dispose()
        {
            if (m_PositionMarkerSphere != null)
            {
                UnityEngine.Object.DestroyImmediate(m_PositionMarkerSphere);
                m_PositionMarkerSphere = null; // Disposeの重複呼び出しを防ぐ
            }
        }
    }
}
