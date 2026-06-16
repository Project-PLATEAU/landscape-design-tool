using PLATEAU.Util;
using PLATEAU.Util.GeoGraph;
using PlateauToolkit.Sandbox;
using PlateauToolkit.Sandbox.Runtime;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace Landscape2.Runtime.DisplayInstallationRestrictionZones
{
    /// <summary>
    /// エリア内の広告オブジェクトを検出するクラス
    /// </summary>
    public class AreaChecker
    {
        private List<Area> areaList;

        private List<List<Vector2>> outlinesOnXZ;



        private List<PlateauSandboxPlaceableHandler> AdObjects { get; set; }

        public AreaChecker()
        {
            areaList = new List<Area>();
            outlinesOnXZ = new List<List<Vector2>>();
        }

        /// <summary>
        /// エリアの設定
        /// </summary>
        /// <param name="areas"></param>
        public void SetAreas(List<Area> areas)
        {
            areaList = areas;
        }

        public void Update()
        {
            //var nameFileter = new List<string>();
            AdObjects = Common.AdvertisementObjectUtil.GetPlateauSandboxAdvertisements();

            // XZ平面上に投影したアウトラインを取得
            outlinesOnXZ.Clear();
            foreach (var item in areaList)
            {
                outlinesOnXZ.Add(item.ProjectEx());
            }
        }

        public List<PlateauSandboxPlaceableHandler> Check()
        {
            var res = new List<PlateauSandboxPlaceableHandler>();
            foreach (var item in AdObjects)
            {
                // 広告オブジェクトの境界を取得
                if (Common.AdvertisementObjectUtil.GetBoundsFromPlateauSandboxAdvertisement(item, out var bounds) == false)
                {
                    continue;
                }

                // 境界線を構築する頂点をワールド座標のコーナーに変換
                var corners = Common.MeshUtil.GetWorldCorners(bounds, item.transform);
                foreach (var outlineOnXZ in outlinesOnXZ)
                {
                    foreach (var vert in outlineOnXZ)
                    {
                        var v = new Vector3(vert.x, 0, vert.y);
                        //Debug.DrawLine(v, v + Vector3.up * 50, Color.green, 30.0f, false);

                    }

                    bool isInArea = false;
                    // アウトラインの各頂点が広告オブジェクトの境界内にあるかチェック
                    foreach (var corner in corners)
                    {
                        //Debug.DrawLine(corner, corner + Vector3.up * 50, Color.red, 30.0f, false);

                        if (Common.MeshUtil.IsPointInPolygon(corner.Xz(), outlineOnXZ))
                        {
                            Debug.Log(item.name + " is in area.");
                            res.Add(item);
                            isInArea = true;
                            break; // 一つでも見つかったらループを抜ける
                        }
                    }

                    if (isInArea)
                    {
                        Common.MansellColorUtil.SetBaseColor(item.gameObject, Color.red, true);
                    }
                    else
                    {
                        Common.MansellColorUtil.SetBaseColor(item.gameObject, Color.white, true);
                    }
                }
            }

            return res;
        }



    }
}
