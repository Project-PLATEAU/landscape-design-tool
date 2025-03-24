using PLATEAU.CityInfo;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.BuildingEditor
{
    /// <summary>
    /// 建物編集のセーブデータ項目
    /// </summary>
    [Serializable]
    public struct BuildingSaveData
    {
        [SerializeField] private string gmlID;
        [SerializeField] private List<Color> colorData;
        [SerializeField] private List<float> smoothnessData;
        [SerializeField] private bool isDeleted;

        public string GmlID { get => gmlID; }
        public List<Color> ColorData { get => colorData; }
        public List<float> SmoothnessData { get => smoothnessData; }
        public bool IsDeleted { get => isDeleted; set => isDeleted = value; }

        public BuildingSaveData(string gmlID, List<Color> colorData, List<float> smoothnessData, bool isDeleted)
        {
            this.gmlID = gmlID;
            this.colorData = colorData;
            this.smoothnessData = smoothnessData;
            this.isDeleted = isDeleted;
        }
    }
}
