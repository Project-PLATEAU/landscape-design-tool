using PLATEAU.CityGML;
using PLATEAU.CityInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 高さ可視化機能
    /// UIは<see cref="VisualizeHeightUI"/>が担当
    /// </summary>
    public class VisualizeHeight : ISubComponent
    {
        /// <summary>
        /// カメラ視野内の建物リストを取得
        /// </summary>
        public List<PLATEAUCityObjectGroup> GetBuildingListInCameraView(Camera camera, HashSet<string> seenGmlIds, float maxDistance)
        {
            if (camera == null)
            {
                return new List<PLATEAUCityObjectGroup>();
            }

            var cameraPos = camera.transform.position;
            Collider[] nearbyColliders = Physics.OverlapSphere(cameraPos, maxDistance);
            var buildingListInView = new List<PLATEAUCityObjectGroup>();

            // カメラの視錐台（フラスタム）の6つの平面を計算
            var cameraPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            
            foreach (var collider in nearbyColliders)
            {
                var cityModelObj = collider.GetComponent<PLATEAUCityObjectGroup>();
                if (cityModelObj == null) continue;
                
                // GML IDで重複排除
                string gmlId = GetBuildingGmlId(cityModelObj);
                if (string.IsNullOrEmpty(gmlId) || seenGmlIds.Contains(gmlId))
                {
                    continue;
                }

                // 高さデータがあるか確認
                string height = GetBuildingHeight(cityModelObj);
                if (string.IsNullOrEmpty(height))
                {
                    continue;
                }

                if (collider.bounds.size == Vector3.zero)
                {
                    continue;
                }

                // フラスタムカリングで拡張された境界線を使用してテストをより寛容にする
                // 境界線の不一致による偽陰性を防ぐため、すべての辺を1m拡張する
                Bounds expandedBounds = collider.bounds;
                expandedBounds.Expand(Vector3.one * 1f);

                if (!GeometryUtility.TestPlanesAABB(cameraPlanes, expandedBounds))
                {
                    continue;
                }

                buildingListInView.Add(cityModelObj);
                seenGmlIds.Add(gmlId);
            }

            // 可視領域の建物リストを返す
            return buildingListInView;
        }

        // 建物の高さを返す
        public string GetBuildingHeight(PLATEAUCityObjectGroup building)
        {
            foreach (var buildingObj in building.GetAllCityObjects())
            {
                if (buildingObj.AttributesMap.TryGetValue("bldg:measuredheight", out var height))
                {
                    // 建物の高さを取得
                    return height.StringValue;
                }
            }

            return null;
        }

        /// <summary>
        /// 建物のGML IDを取得
        /// </summary>
        public string GetBuildingGmlId(PLATEAUCityObjectGroup building)
        {
            foreach (var cityObj in building.GetAllCityObjects())
            {
                if (!string.IsNullOrEmpty(cityObj.GmlID))
                {
                    return cityObj.GmlID;
                }
            }
            return null;
        }

        public void Start()
        {
        }
        public void Update(float deltaTime)
        {
        }
        public void OnEnable()
        {
        }
        public void OnDisable()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
