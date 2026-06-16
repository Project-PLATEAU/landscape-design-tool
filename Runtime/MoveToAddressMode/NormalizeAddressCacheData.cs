using UnityEngine;
using System.Collections.Generic;

namespace Landscape2.Runtime.MoveToAddressMode
{
    public class NormalizeAddressCacheData : ScriptableObject
    {
        public List<CachedTownRegex> cachedTownRegexes;
        public List<StringDictionary> cachedPrefecturePatterns;
        public List<CachedCityPattern> cachedCityPatterns;
        public List<CachedPrefecturePattern> cachedPrefectures;
        public List<CachedTowns> cachedTowns;
        public List<StringDictionary> cachedSameNamedPrefectureCityRegexPatterns;
    }

    [System.Serializable]
    public class CachedTownRegex
    {
        public string key;
        public List<SingleTownList> value;
    }

    [System.Serializable]
    public class  SingleTownList
    {
        public SingleTownDmy town;
        public string str;
    }

    [System.Serializable]
    public class StringDictionary
    {
        public string key;
        public string value;
    }

    [System.Serializable]
    public class CachedCityPattern
    {
        public string key;
        public List<StringDictionary> value;
    }

    [System.Serializable]
    public class CachedPrefecturePattern
    {
        public string key;
        public List<string> value;
    }

    [System.Serializable]
    public class CachedTowns
    {
        public string key;
        public List<SingleTownDmy> value;
    }

    [System.Serializable]
    public class SingleTownDmy
    {
        public string town;
        public string originalTown;
        public string koaza;
        public string gaiku;
        public string lat;
        public string lng;
    }
}
