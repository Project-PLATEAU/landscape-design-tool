using Landscape2.Runtime.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 建物の高さを管理するクラス
    /// </summary>
    public class AreaPlanningBuildingHeight
    {
        private List<Transform> areaBuildingList = new();

        private const float heightViewOffset = 0.3f; // 高さ表示のオフセット値
        private float limitHeight = -1;
        
        public AreaPlanningBuildingHeight(Transform area)
        {
            if (area.TryGetComponent<AreaPlanningCollisionHandler>(out var handler))
            {
                handler.onEnter.AddListener((other) => AddAreaBuilding(other.transform));
            }
        }

        private void AddAreaBuilding(Transform building)
        {
            if (!areaBuildingList.Contains(building))
            {
                areaBuildingList.Add(building);
            }

            if (limitHeight > 0)
            {
                Apply(building);
            }
        }
        
        public void SetHeight(float height)
        {
            limitHeight = height;
            if (limitHeight <= 0)
            {
                return;
            }

            // nullの参照をリストから削除
            areaBuildingList.RemoveAll(building => building == null);
            foreach (var areaBuilding in areaBuildingList)
            {
                Apply(areaBuilding);
            }
        }

        private void Apply(Transform building)
        {
            var mesh = building.GetComponent<MeshCollider>().sharedMesh;
            var bounds = mesh.bounds;
            
            // 地面の高さ取得
            if (!TryGetGroundPosition(bounds.center, out var groundPosition))
            {
                Debug.LogWarning($"{building.name}の地面が見つかりませんでした");
                return;
            }

            // 制限の高さを超えている場合は制限の高さに合わせる
            if (bounds.max.y - groundPosition.y > limitHeight)
            {
                // 一度元の高さに設定　特定のフローで連続で適用される時があり地面にめり込んでしまうのを防ぐため
                ResetBuildingHeight(building);

                float buildingHeight = bounds.max.y - groundPosition.y - limitHeight;
                buildingHeight += heightViewOffset; // 見た目、エリアと被らないように少し高く設定
                var buildingPosition = building.transform.position;
                var position = new Vector3(buildingPosition.x, buildingPosition.y - buildingHeight, buildingPosition.z);
                
                // 建物編集用のコンポーネントを取得
                var editingComponent = BuildingTRSEditingComponent.TryGetOrCreate(building.gameObject);

                // 高さを設定
                editingComponent.SetPosition(position);
            }
            else
            {
                // 元の高さに設定
                ResetBuildingHeight(building);
            }
        }

        /// <summary>
        /// 建物の高さの状態をリセットする
        /// </summary>
        public void ResetBuildingHeight()
        {
            foreach (var building in areaBuildingList)
            {
                if (building == null)
                {
                    continue;
                }
                ResetBuildingHeight(building);
            }

            limitHeight = -1;
        }

        /// <summary>
        /// 建物高さをリセット
        /// </summary>
        private void ResetBuildingHeight(Transform building)
        {
            // 建物編集用のコンポーネントを取得
            var editingComponent = BuildingTRSEditingComponent.TryGetOrCreate(building.gameObject);

            // 高さを設定
            var position = new Vector3(building.transform.position.x, 0, building.transform.position.z);
            editingComponent.SetPosition(position);
        }

        private bool TryGetGroundPosition(Vector3 position, out Vector3 result)
        {
            result = Vector3.zero;
            
            var origin = position + Vector3.up * 10000f;
            var ray = new Ray(origin, Vector3.down);
            var hits = Physics.RaycastAll(ray, float.PositiveInfinity);
            
            foreach (var raycastHit in hits)
            {
                if (raycastHit.transform == null)
                {
                    continue;
                }
                
                if (CityObjectUtil.IsGround(raycastHit.transform.gameObject))
                {
                    result = raycastHit.point;
                    return true;
                }
            }
            
            return false;
        }
    }
}