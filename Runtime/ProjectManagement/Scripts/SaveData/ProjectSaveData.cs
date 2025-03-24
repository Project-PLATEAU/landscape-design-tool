using System.Collections.Generic;
using System.Linq;

namespace Landscape2.Runtime.SaveData
{
    public abstract class ProjectSaveData
    {
        // データ
        protected Dictionary<string, List<string>> data = new();
        
        public void Add(string projectID, string id)
        {
            if (data.TryGetValue(projectID, out var datas))
            {
                if (!datas.Contains(id))
                {
                    datas.Add(id);
                    data[projectID] = datas;
                }
            }
            else
            {
                data.Add(projectID, new List<string> { id });
            }
        }

        public void Delete(string id)
        {
            foreach (var key in data.Keys.ToList())
            {
                if (data[key].Contains(id))
                {
                    data[key].Remove(id);
                    if (data[key].Count == 0)
                    {
                        data.Remove(key);
                    }
                    break;
                }
            }
        }
        
        public string GetProjectID(string id)
        {
            foreach (var keyValuePair in data)
            {
                if (keyValuePair.Value.Contains(id))
                {
                    return keyValuePair.Key;
                }
            }
            return "";
        }

        public bool TryCheckData(string projectID, string id)
        {
            if (data.TryGetValue(projectID, out var datas))
            {
                return datas.Contains(id);
            }
            return false;
        }
    }
}