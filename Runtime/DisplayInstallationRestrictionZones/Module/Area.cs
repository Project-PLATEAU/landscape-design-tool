using PLATEAU.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.DisplayInstallationRestrictionZones
{
    /// <summary>
    /// 
    /// </summary>
    public class Area
    {
        // アウトラインを形成する頂点群
        public List<Vector3> Outline { get; private set; }
        // エリアを所持するゲームオブジェクト
        public GameObject AreaObj { get; private set; }
        
        public List<Vector3> OutlineEx { get; private set; }

        public Area(GameObject obj, List<Vector3> outline, List<Vector3> ex)
        {
            this.AreaObj = obj;
            this.Outline = outline;
            this.OutlineEx = ex;
        }

        public List<Vector2> Project()
        {
            var result = new List<Vector2>(Outline.Count);
            foreach (var vertex in Outline)
            {
                result.Add(vertex.Xz());
            }
            return result;
        }

        public List<Vector2> ProjectEx()
        {
            var result = new List<Vector2>(OutlineEx.Count);
            foreach (var vertex in OutlineEx)
            {
                result.Add(vertex.Xz());
            }
            return result;
        }


        public static void Release(ref Area area)
        {
            if (area == null) return;
            if (area.AreaObj != null)
            {
                GameObject.Destroy(area.AreaObj);
            }
            //area.Outline.Clear();
            area = null;
        }
        public static void Release(ref List<Area> areas)
        {
            if (areas == null) return;
            var nArea = areas.Count;
            for (var i = 0; i < nArea; i++)
            {
                var area = areas[i];
                if (area == null) continue;
                Area.Release(ref area);
                areas[i] = area;
            }
        }

    }
}
