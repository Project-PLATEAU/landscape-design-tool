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

        private bool isApplied;
        private float limitHeight = -1;
        
        public AreaPlanningBuildingHeight(Transform area)
        {
            if (area.TryGetComponent<AreaPlanningCollisionHandler>(out var handler))
            {
                handler.OnEnter.AddListener((other) => AddAreaBuilding(other.transform));
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
                float buildingHeight = bounds.max.y - groundPosition.y - limitHeight;
                buildingHeight += heightViewOffset; // 見た目、エリアと被らないように少し高く設定
                var buildingPosition = building.transform.position;
                var position = new Vector3(buildingPosition.x, buildingPosition.y - buildingHeight, buildingPosition.z);
                
                // 建物編集用のコンポーネントを取得
                var editingComponent = BuildingTRSEditingComponent.TryGetOrCreate(building.gameObject);
                
                // 高さを設定
                editingComponent.SetPosition(position);
            }
        }

        public void Reset()
        {
            var height = 0f;
            foreach (var building in areaBuildingList)
            {
                // 建物編集用のコンポーネントを取得
                var editingComponent = BuildingTRSEditingComponent.TryGetOrCreate(building.gameObject);
                        
                // 高さを設定
                var position = new Vector3(building.transform.position.x, height, building.transform.position.z);
                editingComponent.SetPosition(position);
            }

            limitHeight = -1;
            areaBuildingList.Clear();
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