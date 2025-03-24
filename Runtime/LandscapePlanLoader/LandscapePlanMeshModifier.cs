using PLATEAU.CityGML;
using PLATEAU.CityInfo;
using UnityEngine;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 区画のメッシュを地面からの高さ設定値に合わせて変形するクラス
    /// </summary>
    public sealed class LandscapePlanMeshModifier
    {
        /// <summary>
        /// 地面からの高さを目標高さ設定値に合わせて変形するメソッド
        /// 
        /// cityObjectTypeがCOT_TINRelief（地面を示すタグ）に設定されたオブジェクトが
        /// 変形させる区画メッシュの上または下に配置されている必要がある。
        /// </summary>
        /// <param name="mesh">区画のメッシュ</param>
        /// <param name="targetHeight">地面からの目標高さ</param>
        /// <param name="globalPos">メッシュを持つオブジェクトのクローバル座標</param>
        /// <returns>修正成功時はtrue、メッシュ上下に地面のオブジェクトが無い場合はfalse</returns>
        public bool TryModifyMeshToTargetHeight(Mesh mesh, float targetHeight, Vector3 globalPos)
        {
            Vector3[] vertices = mesh.vertices; //メッシュから頂点データを取得

            // Raycastを用いて、各頂点の高さを目標値に合わせて修正
            for (int verticeIndex = 0; verticeIndex < vertices.Length; verticeIndex++)
            {
                Physics.queriesHitBackfaces = true;

                // 頂点上方向に地面オブジェクトを探索
                RaycastHit[] hitsAbove = Physics.RaycastAll(vertices[verticeIndex], Vector3.up, float.PositiveInfinity);
                RaycastHit? hit = FindGroundObj(hitsAbove);

                // 地面オブジェクトが見つかった場合、高さを修正して次の頂点の処理へ移行
                if (hit != null)
                {
                    vertices[verticeIndex] = new Vector3(
                                vertices[verticeIndex].x,
                                hit.Value.point.y + targetHeight - globalPos.y,
                                vertices[verticeIndex].z);
                    continue;
                }

                // 頂点下方向に地面オブジェクトを探索
                RaycastHit[] hitsBelow = Physics.RaycastAll(vertices[verticeIndex], Vector3.down, float.PositiveInfinity);
                hit = FindGroundObj(hitsBelow);

                // 地面オブジェクトが見つかった場合、高さを修正して次の頂点の処理へ移行
                if (hit != null)
                {
                    vertices[verticeIndex] = new Vector3(
                                vertices[verticeIndex].x,
                                hit.Value.point.y + targetHeight - globalPos.y,
                                vertices[verticeIndex].z);
                    continue;
                }

                return false;  // 頂点の上下に地面オブジェクトが見つからず、修正に失敗
            }

            mesh.vertices = vertices; // 修正した頂点データをメッシュに適用
            mesh.RecalculateBounds();
            return true;    // 全ての頂点の修正に成功
        }

        /// <summary>
        /// 頂点上下方向の地面オブジェクトの座標を探索するメソッド
        /// </summary>
        public Vector3 SearchGroundPoint(Vector3 vector)
        {
            // 頂点上方向に地面オブジェクトを探索
            RaycastHit[] hitsAbove = Physics.RaycastAll(vector, Vector3.up, float.PositiveInfinity);
            RaycastHit? hit = FindGroundObj(hitsAbove);

            // 地面オブジェクトが見つかった場合
            if (hit != null)
            {
                return hit.Value.point;
            }

            // 頂点下方向に地面オブジェクトを探索
            RaycastHit[] hitsBelow = Physics.RaycastAll(vector, Vector3.down, float.PositiveInfinity);
            hit = FindGroundObj(hitsBelow);

            // 地面オブジェクトが見つかった場合
            if (hit != null)
            {
                return hit.Value.point;
            }

            return vector;  // ちょうど地面上に頂点がある場合
        }

        /// <summary>
        /// RaycastHit配列から、cityObjectTypeがCOT_TINReliefのオブジェクトを探索するメソッド
        /// </summary>
        /// <returns>オブジェクトが見つかった場合は対象オブジェクトのRaycastHit、見つからない場合はnull</returns>
        RaycastHit? FindGroundObj(RaycastHit[] hits)
        {
            foreach (var hit in hits)
            {
                PLATEAUCityObjectGroup cityObjectData = hit.transform.GetComponent<PLATEAUCityObjectGroup>();
                if (cityObjectData == null) continue;

                foreach (var rootCityObject in cityObjectData.CityObjects.rootCityObjects)
                {
                    if (rootCityObject.CityObjectType == CityObjectType.COT_TINRelief)  //if the ground object is found
                    {
                        return hit;
                    }
                }
            }
            return null;
        }
    }
}
