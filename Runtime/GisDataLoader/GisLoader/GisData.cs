using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.GisDataLoader
{
    public class GisData
    {
        // GISデータの属性情報
        public List<KeyValuePair<string, string>> Attributes = new();
        
        // GISデータのUnity座標系のポイントリスト
        public List<Vector3> WorldPoints = new();
    }
}