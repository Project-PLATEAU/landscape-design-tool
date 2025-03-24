using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using PlateauToolkit.Maps;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 頂点座標データからMeshを生成するクラス
    /// </summary>
    public sealed class PointDataRenderManager
    {
        private readonly List<GameObject> listOfGISObjects = new List<GameObject>();
        private CesiumGeoreference geoRef;

        /// <summary>
        /// 頂点座標データから景観区画メッシュを生成するクラス
        /// </summary>
        /// <param name="parentObjectName"> 景観区画オブジェクトの親とするオブジェクトの名前（任意の名前） </param>
        /// <param name="pointDatas"> メッシュのworld pointのデータリスト </param>
        /// <param name="listOfGISObjects"> 生成したメッシュオブジェクトを保持するリスト </param>
        /// <returns> メッシュの生成が成功した場合はtrue、頂点座標データが空の場合はfalse </returns>
        public bool DrawShapes(string parentObjectName, List<List<List<Vector3>>> pointDatas, out List<GameObject> listOfGISObjects)
        {
            listOfGISObjects = null;
            if (pointDatas.Count > 0)
            {
                GameObject rootObject = new GameObject(parentObjectName + "_GIS");
                geoRef = GameObject.FindObjectOfType<CesiumGeoreference>();
                rootObject.transform.parent = geoRef.transform;
                CesiumGlobeAnchor anchor = rootObject.AddComponent<CesiumGlobeAnchor>();
                rootObject.AddComponent<MeshFilter>();

                var meshRenderer = rootObject.AddComponent<MeshRenderer>();
                //                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                GameObject mesh = Resources.Load<GameObject>(PlateauToolkitMapsConstants.k_MeshObjectPrefab);

                DrawPolygonOrPolyline(rootObject, mesh, pointDatas);

                double3 pos = anchor.longitudeLatitudeHeight;
                pos.z = 0;
                anchor.longitudeLatitudeHeight = pos;

                listOfGISObjects = this.listOfGISObjects;

                return true;
            }

            Debug.LogError("No point data included");
            return false;
        }

        public void DrawPolygonOrPolyline(GameObject parentObject, GameObject originMeshObj, List<List<List<Vector3>>> pointDatas)
        {
            foreach (List<List<Vector3>> partPointsWorld in pointDatas)
            {
                if (partPointsWorld[0].Count < 3)
                {
                    Debug.LogError("Point data is empty");
                    continue;
                }

                GameObject meshObject = GameObject.Instantiate(originMeshObj);

                meshObject.transform.position = Vector3.zero;
                meshObject.transform.parent = parentObject.transform;

                // テッセレーション処理を行ったメッシュを生成
                TessellatedMeshCreator tessellatedMeshCreator = new TessellatedMeshCreator();
                MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
                tessellatedMeshCreator.CreateTessellatedMesh(partPointsWorld, meshFilter, 30, 40);
                var mr = meshObject.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                listOfGISObjects.Add(meshObject);
            }
        }
    }
}
