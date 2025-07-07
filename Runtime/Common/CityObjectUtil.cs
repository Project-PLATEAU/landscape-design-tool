using PLATEAU.CityGML;
using PLATEAU.CityInfo;
using System.Linq;
using UnityEngine;

namespace Landscape2.Runtime.Common
{
    /// <summary>
    /// PLATEAUのCityObjectを扱うためのユーティリティクラス
    /// </summary>
    public static class CityObjectUtil
    {
        public static bool IsBuilding(GameObject cityObject)
        {
            if (cityObject.TryGetComponent<PLATEAUCityObjectGroup>(out var cityObjectGroup))
            {
                // 建物かどうかの判定
                if (cityObjectGroup.CityObjects.rootCityObjects.Any(o => o.CityObjectType == CityObjectType.COT_Building))
                {
                    return true;
                }
            }
            return false;
        }
        
        public static bool IsGround(GameObject cityObject)
        {
            if (cityObject.TryGetComponent<PLATEAUCityObjectGroup>(out var cityObjectGroup))
            {
                // 地面かどうかの判定
                if (cityObjectGroup.CityObjects.rootCityObjects.Any(o => o.CityObjectType == CityObjectType.COT_TINRelief))
                {
                    return true;
                }
            }

            // ToDo 次の命名規則になることを共通する箇所から取得出来るようにしたい。もしくは他の方法で"TERRAIN_dem"を識別できるようにしたい
            // ConvertedTerrainData PlaceToSceneRecursive()内でこうなるように名前が付けられる
            var terrainDemPrefix = "TERRAIN_dem";
            if (cityObject.name.Contains(terrainDemPrefix))  //if the ground object is found
            {
                return true;
            }

            return false;
        }
        
        public static string GetGmlID(GameObject targetObj)
        {
            if (targetObj.TryGetComponent<PLATEAUCityObjectGroup>(out var cityObjectGroup))
            {
                foreach (var cityObject in cityObjectGroup.GetAllCityObjects())
                {
                    return cityObject.GmlID;
                }
            }
            return "";
        }
    }
}