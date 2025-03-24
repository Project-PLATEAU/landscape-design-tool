using PLATEAU.CityGML;
using PLATEAU.CityInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 高さ可視化機能
    /// UIは<see cref="VisualizeHeightUI"/>が担当
    /// </summary>
    public class VisualizeHeight : ISubComponent
    {
        private List<PLATEAUCityObjectGroup> buildingList = new List<PLATEAUCityObjectGroup>();
        public VisualizeHeight()
        {
            foreach (var cityModelObj in CityModelHandler.CityModelList)
            {
                foreach (var buildingObj in cityModelObj.GetAllCityObjects())
                {
                    // 建物の高さが取得できるか確認
                    if (buildingObj.AttributesMap.TryGetValue("bldg:measuredheight", out var height))
                    {
                        // 建物オブジェクトをリストに格納
                        buildingList.Add(cityModelObj);
                    }
                }
            }
        }

        // 建物リストを返す
        public List<PLATEAUCityObjectGroup> GetBuildingList()
        {
            return buildingList;
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
