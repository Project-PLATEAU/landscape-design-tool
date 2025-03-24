using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 配置ビルの機能を提供するクラス
    /// </summary>
    public class ArrangementBuildingEditor
    {
        private PlateauSandboxBuilding target;

        public void SetTarget(PlateauSandboxBuilding selectBuilding)
        {
            target = selectBuilding;
        }

        public float GetWidth()
        {
            return target.buildingWidth;
        }

        public void SetWidth(float width)
        {
            target.buildingWidth = width;
        }
        
        public float GetHeight()
        {
            return target.buildingHeight;
        }
        
        public void SetHeight(float height)
        {
            target.buildingHeight = height;
        }
        
        public float GetDepth()
        {
            return target.buildingDepth;
        }
        
        public void SetDepth(float depth)
        {
            target.buildingDepth = depth;
        }
        
        public void ApplyBuildingMesh()
        {
            // Supported LOD: LOD0
            foreach (int lodNum in new List<int> {0})
            {
                target.GenerateMesh(lodNum, target.buildingWidth, target.buildingDepth);
            }
        }
        
        public bool CanSlideHeight()
        {
            // 特定のタイプのみ高さを変更可能
            return target.buildingType is
                BuildingType.k_Hotel or
                BuildingType.k_Apartment or
                BuildingType.k_OfficeBuilding or
                BuildingType.k_CommercialBuilding;
        }
        
        public (float min, float high) GetMinAndMaxHeight()
        {
            var minHeight = 5f;
            var maxHeight = 100f;
            
            // ホテルのみ最小高さが異なる
            if (target.buildingType == BuildingType.k_Hotel)
            {
                minHeight = 8f;
            }
            return (minHeight, maxHeight);
        }
        
        public (float min, float high) GetMinAndMaxWidth()
        {
            return (3f, 50f);
        }
        
        public (float min, float high) GetMinAndMaxDepth()
        {
            return (3f, 50f);
        }
    }
}