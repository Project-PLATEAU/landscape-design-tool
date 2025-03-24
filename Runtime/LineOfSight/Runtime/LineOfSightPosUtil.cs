using PLATEAU.CityInfo;
using PLATEAU.Native;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Landscape2.Runtime
{
    public class LineOfSightPosUtil
    {
        private const float K_GroundCheckLength = 10000.0f;

        private PLATEAUInstancedCityModel instancedCityModel;

        public LineOfSightPosUtil()
        {
            instancedCityModel = GameObject.FindFirstObjectByType<PLATEAUInstancedCityModel>(FindObjectsInactive.Include);
        }

        public Vector3 LatLonToVector3(double latitude, double longitude)
        {
            var geoCoordinate = new GeoCoordinate(latitude, longitude, 0f);
            PlateauVector3d plateauPosition = instancedCityModel.GeoReference.Project(geoCoordinate);
            var unityPosition = new Vector3((float)plateauPosition.X, (float)plateauPosition.Y, (float)plateauPosition.Z);

            // heightは地面高さ+uiのheightがy座標になる
            if (TryGetColliderHeight(unityPosition, out var colliderHeight))
            {
                unityPosition.y = colliderHeight;
            }

            return unityPosition;
        }

        public (double, double) Vector3ToLatLon(Vector3 pos)
        {
            var latlon = instancedCityModel.GeoReference.Unproject(new PlateauVector3d(pos.x, pos.y, pos.z));
            return new(latlon.Latitude, latlon.Longitude);
        }

        /// <summary>
        /// PlateauSandboxPrefabPlacementから拾ってきました。
        /// </summary>
        /// <param name="position"></param>
        /// <param name="colliderHeight"></param>
        /// <returns></returns>
        private bool TryGetColliderHeight(Vector3 position, out float colliderHeight)
        {
            var rayStartPosition = new Vector3(position.x, K_GroundCheckLength, position.z);
            float rayDistance = K_GroundCheckLength * 2;

            // Send a ray downward to get the height of the collider.
            var ray = new Ray(rayStartPosition, Vector3.down);

            var hitPointHeights = new List<float>();
            RaycastHit[] results = Physics.RaycastAll(ray, rayDistance);
            foreach (RaycastHit rayCastHit in results)
            {
                if (rayCastHit.transform.TryGetComponent(out PLATEAUCityObjectGroup cityObjectGroup))
                {
                    if (cityObjectGroup.CityObjects.rootCityObjects.Any(o => o.CityObjectType == PLATEAU.CityGML.CityObjectType.COT_Building))
                    {
                        // 建物であればスキップ
                        continue;
                    }

                    // その他のオブジェクトは配置可能
                    hitPointHeights.Add(rayCastHit.point.y);
                }
            }

            if (0 < hitPointHeights.Count)
            {
                // 一番上にヒットしたコライダーの高さを取得
                colliderHeight = hitPointHeights.Max();
                return true;
            }

            // Not found.
            colliderHeight = 0.0f;
            return false;
        }



    }
}
