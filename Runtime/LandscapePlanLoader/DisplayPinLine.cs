using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画作成・編集画面の頂点編集におけるPin＆Lineクラス
    /// </summary>
    public class DisplayPinLine : MonoBehaviour
    {
        private GameObject pinPrefab;
        private GameObject linePrefab;
        private List<GameObject> pinList = new List<GameObject>();
        private List<GameObject> lineList = new List<GameObject>();

        private bool isClosed = false;

        private float scaleStep = 1f;           // スクロールごとのスケール変化量
        private float widthStep = 0.2f;         // スクロールごとのスケール変化量
        private float minScale = 4f;            // 最小スケール
        private float minWidth = 0.8f;          // 最小幅
        private float maxScale = 30f;           // 最大スケール
        private float maxWidth = 6f;            // 最大幅
        private float currentScale = 10f;       // 現在のスケール（初期値）
        private float currentWidth = 2f;        // 現在のLine幅（初期値）
        private float scaleValue = 10f;         // スケール値
        private float widthValue = 2f;          // 幅値

        private float lineColliderHeight = 2f;  // 交差判定のLineのコライダーの高さ
        private float lineColliderWidth = 2f;   // 交差判定のLineのコライダーの幅

        public void Awake()
        {
            pinPrefab = Resources.Load("PlanAreaPin") as GameObject;
            linePrefab = Resources.Load("PlanAreaLine") as GameObject;
            scaleValue = currentScale;
            widthValue = currentWidth;
        }

        /// <summary>
        /// Pinを生成するメソッド
        /// </summary>
        public void CreatePin(Vector3 vec,int index)
        {
            // クリックした位置にPinを生成
            if (pinPrefab == null)
            {
                Debug.LogWarning("Pin用のオブジェクトが見つかりません。");
                return;
            }
            var pinObj = GameObject.Instantiate(pinPrefab);
            // Pinの初期設定
            pinObj.transform.parent = this.transform;
            pinObj.name = "Pin" + index;
            pinObj.transform.localScale = new Vector3(
                currentScale,
                currentScale,
                currentScale
            );
            pinObj.transform.position = vec;
            pinList.Insert(index,pinObj);
        }

        /// <summary>
        /// Lineを生成するメソッド
        /// </summary>
        public void DrawLine(Vector3 startVec,Vector3 endVec,int index) 
        {
            if (linePrefab == null)
            {
                Debug.LogWarning("Line用のオブジェクトが見つかりません。");
                return;
            }
            var lineObj = GameObject.Instantiate(linePrefab);
            // Lineの初期設定
            lineObj.transform.parent = this.transform;
            lineObj.name = "Line" + index;
            var lineRenderer = lineObj.GetComponent<LineRenderer>();
            lineRenderer.startWidth = currentWidth;
            lineRenderer.endWidth = currentWidth;

            // 線を引く
            UpdateLinePositions(lineObj,startVec,endVec);
            lineList.Insert(index, lineObj);    
        }

        /// <summary>
        /// PinとLineを挿入するメソッド
        /// </summary>
        public void InsertPinLine(Vector3 newVec,int index)
        {
            // 元のLineを削除
            GameObject.Destroy(lineList[index]);
            lineList.RemoveAt(index);

            // 新しいPinとLineを挿入
            CreatePin(newVec, index + 1);
            DrawLine(pinList[index].transform.position, newVec, index);
            if (index == lineList.Count - 1)
            {
                DrawLine(newVec, pinList[0].transform.position, index + 1);
            }
            else
            {
                DrawLine(newVec, pinList[index + 2].transform.position, index + 1);
            }

        }

        /// <summary>
        /// PinとLineを削除するメソッド
        /// </summary>
        public void RemovePinLine(int index)
        {
            // Pinを削除
            GameObject.Destroy(pinList[index]);
            pinList.RemoveAt(index);

            // Lineの終点を更新
            GameObject.Destroy(lineList[index]);
            lineList.RemoveAt(index);
            if (index == 0)
            {
                GameObject.Destroy(lineList[lineList.Count - 1]);
                lineList.RemoveAt(lineList.Count - 1);
                DrawLine(pinList[pinList.Count - 1].transform.position, pinList[0].transform.position, index);
            }
            else if(index == pinList.Count)
            {
                GameObject.Destroy(lineList[index - 1]);
                lineList.RemoveAt(index - 1);
                DrawLine(pinList[index - 1].transform.position, pinList[0].transform.position, index - 1);
            }
            else
            {
                GameObject.Destroy(lineList[index - 1]);
                lineList.RemoveAt(index - 1);
                DrawLine(pinList[index - 1].transform.position, pinList[index].transform.position, index - 1);
            }
        }

        /// <summary>
        /// Lineの頂点を動かすメソッド
        /// </summary>
        public void MoveLineVertex(GameObject editingPin, Vector3 newVec)
        {
            int index = FindPinIndex(editingPin);
            if(index == -1) return;

            if (index == lineList.Count - 1)
            {
                UpdateLinePositions(lineList[index], newVec, pinList[0].transform.position);
            }
            else 
            {
                UpdateLinePositions(lineList[index], newVec, pinList[index + 1].transform.position);
            }

            // 対象の頂点が終点となるLineの頂点を編集
            if (index == 0)
            {
                UpdateLinePositions(lineList[lineList.Count - 1], pinList[lineList.Count - 1].transform.position, newVec);
            }
            else
            {
                UpdateLinePositions(lineList[index - 1], pinList[index - 1].transform.position, newVec);
            }
        }

        /// <summary>
        /// Lineオブジェクトの頂点を更新し、線とコライダーを設定するメソッド
        /// </summary>
        private void UpdateLinePositions(GameObject lineObj, Vector3 newStartVec, Vector3 newEndVec)
        {
            Vector3 lineVec = newEndVec - newStartVec;
            float dist = lineVec.magnitude; // 新しい線の長さ
            Vector3 lineX = new Vector3(dist, 0f, 0f); // X軸方向に伸びているベクトル

            // LineRendererの更新
            LineRenderer lineRenderer = lineObj.GetComponent<LineRenderer>();

            if (lineRenderer != null)
            {
                lineRenderer.useWorldSpace = false;
                lineRenderer.positionCount = 2;
                Vector3[] linePositions = new Vector3[] { Vector3.zero, lineX };
                lineRenderer.SetPositions(linePositions);

                // BoxColliderの更新
                BoxCollider col = lineObj.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = lineObj.AddComponent<BoxCollider>();
                }

                // コライダーのサイズを更新
                col.size = new Vector3(dist, currentWidth, currentWidth);
                // コライダーを線の中心に配置
                col.center = new Vector3(dist / 2f, 0f, 0f);

                // 線を新しい方向と位置に回転・移動させる
                lineObj.transform.rotation = Quaternion.FromToRotation(lineX, lineVec);
                lineObj.transform.position = newStartVec;
            }
        }

        /// <summary>
        /// クリックの対象が最初のPinかどうかを判定するメソッド
        /// </summary>
        public bool IsClickFirstPin(RaycastHit[] hits)
        {
            if (pinList == null) return false;
            return Array.Exists(hits, h => h.collider.gameObject == pinList[0]);
        }

        /// <summary>
        /// エリアが閉じられたかどうかを返すメソッド
        /// </summary>
        public bool IsClosed()
        {
            return isClosed;
        }

        /// <summary>
        /// Pinを削除するメソッド
        /// </summary>
        public void ClearPins()
        {
            foreach (var pin in pinList)
            {
                GameObject.Destroy(pin);
            }
            pinList.Clear();
        }

        /// <summary>
        /// Lineを削除するメソッド
        /// </summary>
        public void ClearLines()
        {
            foreach (var line in lineList)
            {
                GameObject.Destroy(line);
            }
            lineList.Clear();
        }

        /// <summary>
        /// Pinのインデックスを検索するメソッド
        /// </summary>
        public int FindPinIndex(GameObject pin)
        {
            int index = pinList.IndexOf(pin);
            if (index == -1)
            {
                Debug.LogWarning("Pinが見つかりません。");
            }
            return index;
        }

        /// <summary>
        /// Lineのインデックスを検索するメソッド
        /// </summary>
        public int FindLineIndex(GameObject line)
        {
            int index = lineList.IndexOf(line);
            if (index == -1)
            {
                Debug.LogWarning("Lineが見つかりません。");
            }
            return index;
        }

        /// <summary>
        /// PinとLineのTransformをカメラの距離に応じて変更するメソッド
        /// </summary>
        public void ZoomPinLine(float scroll)
        {
            // スクロール量に応じてLineの幅を増減
            if (scroll < 0)
            {
                widthValue += widthStep;
                scaleValue += scaleStep;

            }
            else if (scroll > 0)
            {
                widthValue -= widthStep;
                scaleValue -= scaleStep;
            }

            // 幅とスケールが最小値・最大値を超えないように制限
            currentWidth = widthValue <= maxWidth ? widthValue : maxWidth;
            currentWidth = widthValue >= minWidth ? currentWidth : minWidth;
            currentScale = scaleValue <= maxScale ? scaleValue : maxScale;
            currentScale = scaleValue >= minScale ? currentScale : minScale;

            // Lineのスケールを更新
            foreach (var lineObject in lineList)
            {
                LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
                // LineRendererの幅を調整
                lineRenderer.startWidth = currentWidth;
                lineRenderer.endWidth = currentWidth;
            }
            // Pinのスケールを更新
            foreach (var pinObject in pinList)
            {
                pinObject.transform.localScale = new Vector3(
                    currentScale,
                    currentScale,
                    currentScale
                );
            }
        }

        /// <summary>
        /// 作成したLineによってLineオブジェクトが他のLineオブジェクトと交差しているかどうかを判定するメソッド
        /// </summary>
        public bool IsIntersectedByLine()
        {
            // 交差判定を行うLineのリストを作成
            List<BoxCollider> lineColliderList = new List<BoxCollider>();
            for (int i = 0; i < lineList.Count; i++)
            {
                // Lineが極端に短い場合は交差判定から除外
                if (lineList[i].GetComponent<LineRenderer>().GetPosition(1).x < lineColliderWidth)
                {
                    continue;
                }
                lineColliderList.Add(lineList[i].GetComponent<BoxCollider>());
            }

            for (int i = 0; i < lineColliderList.Count; i++)
            {
                for (int j = i + 2; j < lineColliderList.Count; j++)
                {
                    // 最後のLineと最初のLineは隣接しているため交差判定を行わない
                    if (i == 0 && j == lineColliderList.Count - 1) continue;

                    if (AreLinesOverlappingAccurate(lineColliderList[i], lineColliderList[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 動かしたPinによってLineオブジェクトが他のLineオブジェクトと交差したかどうかを判定するメソッド
        /// </summary>
        public bool IsIntersectedByPin(GameObject pin)
        {
            int index = FindPinIndex(pin);
            if (index == -1) return false;

            int lineID1 = index;
            int lineID2 = index == 0 ? lineList.Count - 1 : index - 1;
            var lineCol1 = lineList[lineID1].GetComponent<BoxCollider>();
            var lineCol2 = lineList[lineID2].GetComponent<BoxCollider>();


            // 交差判定を行うLineのリストを作成
            List<BoxCollider> lineColliderList = new List<BoxCollider>();
            for (int i = 0; i < lineList.Count; i++)
            {
                // Lineが極端に短い場合は交差判定から除外
                if (lineList[i].GetComponent<LineRenderer>().GetPosition(1).x < lineColliderWidth)
                {
                    continue;
                }
                lineColliderList.Add(lineList[i].GetComponent<BoxCollider>());
            }

            int lineColID1 = lineColliderList.IndexOf(lineCol1);
            if (lineColID1 != -1)
            {
                for (int i = 0; i < lineColliderList.Count; i++)
                {
                    // 自身と隣接するLineは交差判定を行わない
                    if (i == lineColID1) continue;
                    if (i == lineColID1 + 1) continue;
                    if (i == lineColID1 - 1) continue;
                    if (i == 0 && lineColID1 == lineColliderList.Count - 1 ) continue;
                    if (i == lineColliderList.Count - 1 && lineColID1 == 0 ) continue;

                    if (AreLinesOverlappingAccurate(lineColliderList[i], lineColliderList[lineColID1]))
                    {
                        return true;
                    }
                }
            }

            int lineColID2 = lineColliderList.IndexOf(lineCol2);
            if (lineColID2 != -1)
            {
                for (int i = 0; i < lineColliderList.Count; i++)
                {
                    // 自身と隣接するLineは交差判定を行わない
                    if (i == lineColID2) continue;
                    if (i == lineColID2 + 1) continue;
                    if (i == lineColID2 - 1) continue;
                    if (i == 0 && lineColID2 == lineColliderList.Count - 1) continue;
                    if (i == lineColliderList.Count - 1 && lineColID2 == 0) continue;

                    if (AreLinesOverlappingAccurate(lineColliderList[i], lineColliderList[lineColID2]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Lineオブジェクト同士の重なりを判定するメソッド
        /// </summary>
        private bool AreLinesOverlappingAccurate(BoxCollider col1, BoxCollider col2)
        {
            col1.size = new Vector3(col1.size.x, lineColliderHeight, lineColliderWidth);
            col2.size = new Vector3(col2.size.x, lineColliderHeight, lineColliderWidth);


            // コライダーのトランスフォーム情報を使用して交差を判定
            bool isOverlap = Physics.ComputePenetration(
                col1, col1.transform.position, col1.transform.rotation,
                col2, col2.transform.position, col2.transform.rotation,
                out _, out _
            );

            // コライダーのサイズを元に戻す
            col1.size = new Vector3(col1.size.x, currentWidth, currentWidth);
            col2.size = new Vector3(col2.size.x, currentWidth, currentWidth);

            return isOverlap;
        }

        public void Update()
        {
            // マウスのホイールがスクロールされたらPinとLineのサイズを変更
            if (Input.mouseScrollDelta.y != 0)
            {
                ZoomPinLine(Input.mouseScrollDelta.y);
            }
        }
    }
}
