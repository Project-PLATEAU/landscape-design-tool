using PlateauToolkit.Sandbox.Runtime;
using System;
using UnityEditor;
using UnityEngine;
using PlateauSandboxBuilding = PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime.PlateauSandboxBuilding;

namespace Landscape2.Runtime
{
    public interface IArrangementSandboxAssetSaveData<T> where T : Component
    {
        public void Save(T target);
        public void Apply(T target);
    }

    /// <summary>
    /// プロジェクト保存時に配置されている情報
    /// </summary>
    [Serializable]
    public class ArrangementSaveData
    {
        public TransformData transformData;
        
        public AdvertisementSaveData advertisementData = new();
        public ArrangementBuildingSaveData buildingSaveData = new();
        
        public void Save(Transform target)
        {
            transformData = new TransformData(target);
            
            // 広告データ
            if (target.TryGetComponent<PlateauSandboxAdvertisement>(out var outTarget))
            {
                advertisementData.Save(outTarget);
            }
            
            // 建物データ
            if (target.TryGetComponent<PlateauSandboxBuilding>(out var buildingTarget))
            {
                buildingSaveData.Save(buildingTarget);
            }
        }

        /// <summary>
        /// ゲームオブジェクトにデータを反映
        /// </summary>
        /// <param name="target"></param>
        public void Apply(GameObject target)
        {
            target.transform.localScale = transformData.scale;
            target.name = transformData.name;
            
            // 広告データ
            if (target.TryGetComponent<PlateauSandboxAdvertisement>(out var outTarget))
            {
                advertisementData.Apply(outTarget);
            }
            
            // 建物データ
            if (target.TryGetComponent<PlateauSandboxBuilding>(out var buildingTarget))
            {
                buildingSaveData.Apply(buildingTarget);
            }
        }
    }
    
    /// <summary>
    /// Transform情報
    /// </summary>
    [Serializable]
    public struct TransformData
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        // Transformを引数に取るコンストラクタ
        public TransformData(Transform transform)
        {
            name = transform.name;
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.localScale;
        }
    }
}