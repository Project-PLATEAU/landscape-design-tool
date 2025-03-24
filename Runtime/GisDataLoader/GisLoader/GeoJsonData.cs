using System;
using System.Collections.Generic;

namespace Landscape2.Runtime.GisDataLoader
{
    [Serializable]
    public struct GeoJsonData
    {
        [Serializable]
        public class GeoJson
        {
            public string type;
            public List<Feature> features;
        }

        [Serializable]
        public class Feature
        {
            public string type;
            public Geometry geometry;
            public Dictionary<string, object> properties;
        }

        [Serializable]
        public class Geometry
        {
            public string type;
            /// <summary>
            /// coordinates will be used for both multipolygon and lines
            /// </summary>
            public object coordinates;
        }
    }
}