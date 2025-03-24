using System.Collections.Generic;
using UnityEngine;
namespace LandscapeDesignTool
{

    public class LDTShapeFileHandler
    {
        [SerializeField] public string areaType = "";
        [SerializeField] public string type = "";
        [SerializeField] public List<Vector2> points = new List<Vector2>();
        [SerializeField] public float height;
        [SerializeField] public Color col;

        public void AddPoint(Vector2 pos)
        {
            points.Add(pos);

        }

        public void Save( int instanceID)
        {

            PlayerPrefs.SetString(instanceID.ToString()+"-areaType", areaType);
            PlayerPrefs.SetString(instanceID.ToString() + "-type", type);
            PlayerPrefs.SetFloat(instanceID.ToString() + "-height", height);
            string colstr = col.r + "," + col.g + "," + col.b + "," + col.a;
            PlayerPrefs.SetString(instanceID.ToString() + "-color", colstr);
            PlayerPrefs.SetInt(instanceID.ToString() + "-npoints", points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                string pstr = points[i].x.ToString() + "," + points[i].y.ToString();
                PlayerPrefs.SetString(instanceID.ToString()+ "-point"+i.ToString(), pstr);
            }
            
            PlayerPrefs.Save();
        }

    }
}
