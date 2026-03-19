using Landscape2.Runtime.Common;
using PLATEAU.CityGML;
using PLATEAU.CityInfo;
using System.Collections.Generic;
using System.Linq;
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
        public bool TryModifyMeshToTargetHeight(Mesh mesh, float targetHeight, Vector3 globalPos, bool enableVirtualGround = false)
        {
            Vector3[] vertices = mesh.vertices; //メッシュから頂点データを取得

            // 地面検出に成功した頂点
            var scsIndecies = new List<int>(vertices.Length);

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
                    scsIndecies.Add(verticeIndex);
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
                    scsIndecies.Add(verticeIndex);
                    continue;
                }

            }

            if (scsIndecies.Any() == false)
            {
                return false;  // 頂点の上下に地面オブジェクトが見つからず、修正に失敗
            }

            // 失敗した頂点があるなら　成功した頂点を元に値を設定する
            var aveHeight = 0f;
            if (scsIndecies.Count != vertices.Length)
            {
                // 判定に失敗した頂点があるかつ、仮想地面が有効ではない時は失敗として扱う
                if (enableVirtualGround == false)
                {
                    return false;  // 頂点の上下に地面オブジェクトが見つからず、修正に失敗
                }

                // 平均の高さを計算
                foreach (var item in scsIndecies)
                {
                    aveHeight += vertices[item].y;
                }
                aveHeight /= scsIndecies.Count;

                // 失敗した頂点に高さを適用
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (scsIndecies.Contains(i) == false)
                    {
                        vertices[i] = new Vector3(
                                    vertices[i].x,
                                    aveHeight,
                                    vertices[i].z);
                    }
                }

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
                if (CityObjectUtil.IsGround(hit.transform.gameObject))
                    return hit;
            }

            foreach (var hit in hits)
            {
                // 名前にdemを含むオブジェクトを探索
                if (hit.transform.gameObject.name.Contains("dem_"))
                {
                    return hit;
                }
            }

            return null;
        }
    }
}
