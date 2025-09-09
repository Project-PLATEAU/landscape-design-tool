using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Landscape2.Runtime.UiCommon;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画の頂点を編集するクラス
    /// </summary>
    public class AreaPlanningEdit
    {
        private readonly AreaEditManager areaEditManager;
        private readonly DisplayPinLine displayPinLine;
        private readonly LandscapePlanMeshModifier landscapePlanMeshModifier;
        private readonly TessellatedMeshCreator tessellatedMeshCreator;
        private readonly WallGenerator wallGenerator;
        private List<Vector3> vertices = new List<Vector3>();
        private bool isVertexEditing = false;
        private GameObject editingPin;
        private GameObject editingLine;
        private Vector3 lastPinPosition;


        public AreaPlanningEdit(AreaEditManager areaEditManager,DisplayPinLine displayPinLine)
        {
            this.areaEditManager = areaEditManager;
            this.displayPinLine = displayPinLine;
            landscapePlanMeshModifier = new LandscapePlanMeshModifier();
            tessellatedMeshCreator = new TessellatedMeshCreator();
            wallGenerator = new WallGenerator();
        }

        /// <summary>
        /// 頂点に変更があったかを返すメソッド
        /// </summary>
        public bool IsVertexEdited()
        {
            return isVertexEditing;
        }

        /// <summary>
        /// 変更内容を確定・保持するメソッド
        /// </summary>
        public void ConfirmEditData()
        {
            // 頂点の編集を適用
            EditVertexIfClicked();
            // メッシュを生成
            CreateMeshByVertice();
        }

        /// <summary>
        /// 編集した頂点データを基にメッシュを生成するメソッド
        /// </summary>
        private void CreateMeshByVertice()
        {
            if (!isVertexEditing)
            {
                Debug.LogWarning("Vertex is not edited");
                return;
            }
            List<List<Vector3>> listOfVertices = new List<List<Vector3>>
            {
                new List<Vector3>(vertices)
            };

            // メッシュを再生成
            LandscapePlanLoadManager landscapePlanLoadManager = new LandscapePlanLoadManager();
            landscapePlanLoadManager.ReloadMeshes(listOfVertices, areaEditManager.GetEditingAreaIndex());
        }

        /// <summary>
        /// 区画の頂点座標リストを取得し，PinとLineを表示するメソッド
        /// </summary>
        public void CreatePinline()
        {
            int count = areaEditManager.GetPointCount();
            if (count < 3)
            {
                Debug.LogWarning("The number of vertices is less than 3");
                return;
            };
            Vector3 startVec = Vector3.zero;
            Vector3 endVec = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Vector3 vertex = areaEditManager.GetPointData(i);
                if (vertex == null)
                {
                    Debug.LogWarning("Vertex is null");
                    return;
                }

                vertices.Add(vertex);

                // Lineを描画
                if (i == 0)
                {
                    endVec = landscapePlanMeshModifier.SearchGroundPoint(vertex) + new Vector3(0, 5.0f, 0);
                }
                else
                {
                    startVec = endVec;
                    endVec = landscapePlanMeshModifier.SearchGroundPoint(vertex) + new Vector3(0, 5.0f, 0);
                    displayPinLine.DrawLine(startVec, endVec, vertices.Count - 2);
                }
                displayPinLine.CreatePin(endVec, vertices.Count - 1);
            }
            // 区画を閉じる
            startVec = landscapePlanMeshModifier.SearchGroundPoint(vertices[0]) + new Vector3(0, 5.0f, 0);
            displayPinLine.DrawLine(endVec, startVec, vertices.Count - 1);
        }

        /// <summary>
        /// シーンビュー上からピンを選択するメソッド
        /// </summary>
        /// <returns></returns>
        public bool SelectPinOnScreen()
        {
            RaycastHit[] hits;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, Mathf.Infinity);
            if (hits == null || hits.Length == 0)
                return false;
            // Pinをクリックした場合
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.gameObject.name.Contains("Pin"))
                {
                    editingPin = hits[i].collider.gameObject;
                    lastPinPosition = editingPin.transform.position;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Pinをクリックしたかどうかを判定するメソッド
        /// </summary>
        public bool IsClickedPin()
        {
            return editingPin != null;
        }

        /// <summary>
        /// シーンビュー上からラインを選択するメソッド
        /// </summary>
        public bool SelectLineOnScreen()
        {
            RaycastHit[] hits;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, Mathf.Infinity);
            if (hits == null || hits.Length == 0)
                return false;
            // Lineをクリックした場合
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.gameObject.name.Contains("Line"))
                {
                    editingLine = hits[i].collider.gameObject;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///Lineをクリックしたかどうかを判定するメソッド
        /// </summary>
        public bool IsClickedLine()
        {
            return editingLine != null;
        }

        /// <summary>
        ///Lineの中点に新しく頂点を追加するメソッド
        /// </summary>
        public void AddVertexToLine()
        {
            int lineIndex = displayPinLine.FindLineIndex(editingLine);
            if (lineIndex == -1)
            { 
                Debug.LogWarning("Line is not found");
                return;
            };

            //// y座標は前後の頂点の中点の座標にする           
            Vector3 previousVec = vertices[lineIndex];
        
            Vector3 nextVec;
            if (lineIndex == vertices.Count - 1)
            {
                nextVec = vertices[0];
            }
            else
            { 
                nextVec = vertices[lineIndex + 1];           
            }
            Vector3 newVec = (previousVec + nextVec) / 2;
            vertices.Insert(lineIndex + 1, newVec);

            // PinとLineを挿入
            Vector3 newPinVec = landscapePlanMeshModifier.SearchGroundPoint(newVec) + new Vector3(0, 5.0f, 0);
            displayPinLine.InsertPinLine(newPinVec, lineIndex);
        }

        /// <summary>
        /// Pinをドラッグしたときの処理
        /// </summary>
        public void OnDragPin()
        {
            if (editingPin == null)return;

            Vector3 pinPos = Camera.main.WorldToScreenPoint(editingPin.transform.position);
            Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, pinPos.z);
            // 地面の高さを取得
            Vector3 groundPos = landscapePlanMeshModifier.SearchGroundPoint(editingPin.transform.position);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            editingPin.transform.position = new Vector3(worldPos.x, groundPos.y, worldPos.z) + new Vector3(0, 5.0f, 0);
            // Lineの頂点を移動させる
            displayPinLine.MoveLineVertex(editingPin, editingPin.transform.position);
            // 頂点データを更新
            int index = displayPinLine.FindPinIndex(editingPin);
            if (index == -1) return;
            vertices[index] = new Vector3(editingPin.transform.position.x, groundPos.y, editingPin.transform.position.z);
            
            isVertexEditing = true;
        }

        /// <summary>
        /// 頂点，Pin，Lineを元の位置に戻す処理
        /// </summary>
        public void ResetVertexPosition()
        {
            if (editingPin == null) return;

            editingPin.transform.position = lastPinPosition;
            vertices[displayPinLine.FindPinIndex(editingPin)] = lastPinPosition;
            displayPinLine.MoveLineVertex(editingPin, lastPinPosition);
        }

        /// <summary>
        /// 編集しているPinとLineを解除する処理
        /// </summary>
        public void ReleaseEditingPin()
        {
            editingPin = null;
            editingLine = null;
        }

        /// <summary>
        /// 頂点が交差しているかを判定
        /// </summary>
        public bool IsIntersected()
        {
            if (editingPin == null)
            {
                return false;
            }
            return displayPinLine.IsIntersectedByPin(editingPin);
        }

        /// <summary>
        /// 頂点を削除するメソッド
        /// </summary>
        public void DeleteVertex()
        {
            if (editingPin == null)
            {
                Debug.LogWarning("Pin is not selected");
                return;
            }
            const int NumRequiredVertices = AreaPlanningModuleRegulation.NumRequiredPins;
            bool hasRemovableVertices = vertices.Count < NumRequiredVertices + 1;
            if (hasRemovableVertices)
            {
                Debug.LogWarning("The number of vertices is less than " + (NumRequiredVertices + 1).ToString());
                return;
            }

            int index = displayPinLine.FindPinIndex(editingPin);
            if (index == -1)
            { 
                Debug.LogWarning("Pin is not found");
                return;
            };

            // PinとLineを削除
            displayPinLine.RemovePinLine(index);
            vertices.RemoveAt(index);
            isVertexEditing = true;
        }

        /// <summary>
        /// 区画の頂点の編集を適用させる処理
        /// </summary>
        public void EditVertexIfClicked()
        {
            if (!IsClockwise())
            {
                vertices.Reverse();
            }
            areaEditManager.EditPointData(new List<Vector3>(vertices));
        }

        /// <summary>
        /// 区画の頂点の編集をクリアする処理
        /// </summary>
        public void ClearVertexEdit()
        {
            vertices.Clear();
            displayPinLine.ClearPins();
            displayPinLine.ClearLines();
            isVertexEditing = false;
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
