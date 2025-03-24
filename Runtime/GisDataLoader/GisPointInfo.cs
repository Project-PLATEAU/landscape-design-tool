using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// GISのポイント情報を管理するクラス
    /// </summary>
    [Serializable]
    public class GisPointInfo
    {
        [SerializeField]
        public int ID { get; private set; }
        
        // 属性のID
        [SerializeField]
        public string AttributeID { get; private set; }

        // 施設名
        [SerializeField]
        public string FacilityName { get; private set; }
        
        // 登録されたピンの表示名
        [SerializeField]
        public string DisplayName { get; private set; }
        
        // 施設の位置
        [SerializeField]
        public Vector3 FacilityPosition { get; private set; }
        
        // 表示状態かどうか
        [SerializeField]
        public bool IsShow { get; private set; }
        public void SetShow(bool isShow) => IsShow = isShow;
        
        // ポイントの色
        [SerializeField]
        public Color Color { get; private set; }

        public GisPointInfo(
            int index,
            string attributeID,
            string facilityName,
            string displayName,
            Vector3 facilityPosition,
            Color color,
            bool isShow)
        {
            ID = index;
            AttributeID = attributeID;
            FacilityName = facilityName;
            DisplayName = displayName;
            FacilityPosition = facilityPosition;
            Color = color;
            IsShow = isShow;
        }
    }
}