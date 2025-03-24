using Landscape2.Runtime.UiCommon;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画の頂点を作成するクラス
    /// </summary>
    public class AreaPlanningRegister
    {
        private LandscapePlanLoadManager landscapePlanLoadManager;
        private DisplayPinLine displayPinLine;
        private bool isClosed = false;
        private List<Vector3> vertices = new List<Vector3>();

        public AreaPlanningRegister(DisplayPinLine displayPinLine)
        {
            this.displayPinLine = displayPinLine;
            landscapePlanLoadManager = new LandscapePlanLoadManager();
        }

        /// <summary>
        /// 区画の頂点が交差しているかを判定するメソッド
        /// </summary>
        public bool IsIntersected()
        {
            if (displayPinLine.IsIntersectedByLine())
            {
                // LogWarningを表示
                Debug.LogWarning("頂点が交差しています。");
                return true;
            }
            return false;
        }

        /// <summary>
        /// クリック時にPinとLine生成を行うメソッド
        /// </summary>
        public void AddVertexIfClicked()
        {
            if (!isClosed)
            { 
                RaycastHit[] hits;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                hits = Physics.RaycastAll(ray, Mathf.Infinity);
                if (hits == null || hits.Length == 0)
                    return;

                if (vertices.Count > 2)
                {
                    // 最初に生成したPinの場合は区画を閉じる
                    if (displayPinLine.IsClickFirstPin(hits))
                    {
                        var startVec = vertices[vertices.Count - 1] + new Vector3(0, 5.0f, 0);
                        displayPinLine.DrawLine(startVec, vertices[0] + new Vector3(0, 5.0f, 0), vertices.Count - 1);
                        isClosed = true;
                        return;
                    }
                }

                for (int i = 0; i < hits.Length; i++)
                {                  
                    if (hits[i].collider.gameObject.name.Contains("dem_"))
                    {
                        vertices.Add(hits[i].point);
                        var newVec = hits[i].point + new Vector3(0, 5.0f, 0);
                        // Pinを生成
                        displayPinLine.CreatePin(newVec, vertices.Count - 1);
                        // Lineを生成
                        if (vertices.Count > 1)
                        {
                            var startVec = vertices[vertices.Count - 2] + new Vector3(0, 5.0f, 0);
                            displayPinLine.DrawLine(startVec, newVec, vertices.Count - 2);

                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 景観区画データを作成するメソッド
        /// </summary>
        public void CreateAreaData(string name,float height,float wallMaxHeight,Color color)
        {
            int id = AreasDataComponent.GetPropertyCount();
            List<List<Vector3>> listOfVertices = new List<List<Vector3>>();
            // 頂点データが反時計回りの場合は反転
            if (!IsClockwise())
            {
                vertices.Reverse();
            }
            listOfVertices.Add(new List<Vector3>(vertices));

            // 新規景観区画データを作成
            PlanAreaSaveData newSaveData = new PlanAreaSaveData(
                id,
                name,
                height,
                10.0f,
                color,
                wallMaxHeight,
                listOfVertices
                );
            List<PlanAreaSaveData>listOfSaveData = new List<PlanAreaSaveData>
            {
                newSaveData
            };
            // 作成した景観区画データ基に景観データをロード
            var loadedProperties = landscapePlanLoadManager.LoadFromSaveData(listOfSaveData);
            foreach (var loadedProperty in loadedProperties)
            {
                // プロジェクトへ保存
                ProjectSaveDataManager.Add(ProjectSaveDataType.LandscapePlan, loadedProperty.ID.ToString());
            }
        }

        /// <summary>
        /// 区画が閉じられたかどうかを判定するメソッド
        /// </summary>
        public bool IsClosed()
        {
            return isClosed;
        }

        /// <summary>
        /// 区画の頂点の編集をクリアする処理
        /// </summary>
        public void ClearVertexEdit()
        {
            vertices.Clear();
            displayPinLine.ClearPins();
            displayPinLine.ClearLines();
            isClosed = false;
        }

        /// <summary>
        /// 頂点が時計回りかどうかを判定するメソッド
        /// </summary>
        private bool IsClockwise()
        {
            if (vertices.Count < 3)
            {
                Debug.LogWarning("頂点数が3未満です。");
            }
            float sum = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vec1 = vertices[i];
                Vector3 vec2 = vertices[(i + 1) % vertices.Count];
                sum += (vec2.x - vec1.x) * (vec2.z + vec1.z);
            }
            return sum > 0;
        }
    }
}
