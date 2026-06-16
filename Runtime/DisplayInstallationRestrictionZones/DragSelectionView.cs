using Landscape2.Runtime.AdRegulation;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.DisplayInstallationRestrictionZones
{
    public interface IDragSelectionView
    {
        // 選択候補の設定
        void SetSelectable(List<MeshCollider> targets);
        void AddSelectable(MeshCollider target);
        void RemoveSelectable(MeshCollider target);

        // ドラッグ開始
        void Start();
        // ドラッグ中の更新
        void Update();
        // 選択状態をリセット
        void ResetSelectionStatus();

        // 選択が変更された時のイベント
        event System.Action<List<MeshCollider>> OnSelectionChanged;


        bool IsEnable { get; set; }
    }

    public class DragSelectionView : IDragSelectionView
    {
        // 選択範囲を表示するVisualElement
        VisualElement selectionBox;

        // マウス操作を行うためのビューモデル
        IAdRegulationMouseOperationView mouseOperationView;

        // 選択対象リスト
        private List<MeshCollider> selectable;

        // ドラッグ開始位置と選択範囲の矩形
        private Vector2 startPos;
        private Rect selectionRect;

        public event Action<List<MeshCollider>> OnSelectionChanged;

        // 有効化フラグ
        private bool isEnable = false;
        bool IDragSelectionView.IsEnable { get => isEnable; set => isEnable = value; }

        // 選択中か
        public bool IsSelecting => selectionBox?.enabledSelf == true && selectionBox?.visible == true;

        public DragSelectionView(VisualElement root, IAdRegulationMouseOperationView mouseOperationView, string selectionBoxVisualElmentPath)
        {
            // 枠組みをロード
            var uiAsset = Resources.Load<VisualTreeAsset>(selectionBoxVisualElmentPath);

            if (uiAsset == null)
            {
                Debug.LogError("Failed to load UXML asset.");
                return;
            }

            // UXMLを複製してVisualElementを取得
            selectionBox = uiAsset.CloneTree();

            // 最上位のルートに追加　親の設定により表示される範囲に影響が出るのを防ぐため
            VisualElement originalRoot = root;
            while (originalRoot.parent != null)
            {
                originalRoot = originalRoot.parent;
            }
            originalRoot.Add(selectionBox);
            ActivateVisualElement(false);   // 初期状態では非表示

            selectable = new List<MeshCollider>();
            startPos = Vector2.zero;
            selectionRect = new Rect(0, 0, 0, 0);

            this.mouseOperationView = mouseOperationView;
            if (mouseOperationView != null)
            {
                mouseOperationView.OnMouseButtonDown += OnMouseButtonDown;

                mouseOperationView.OnMouseButton += OnMouseButton;

                mouseOperationView.OnMouseButtonUp += OnMouseButtonUp;
            }
        }

        private void OnMouseButtonDown(Vector3 mousePos)
        {
            if (isEnable == false)
                return;

            startPos = mousePos;

            // 選択ボックスを表示
            ActivateVisualElement(true);
        }

        private void OnMouseButton(Vector3 mousePos)
        {
            if (IsSelecting == false)
                return;
            Vector2 currentPos = mousePos;
            UpdateSelectionBox(startPos, currentPos);

        }

        private void OnMouseButtonUp(Vector3 mousePos)
        {
            if (IsSelecting == false)
                return;
            SelectWithin();
            ActivateVisualElement(false); // 選択ボックスを非表示

        }

        /// <summary>
        /// 選択候補の設定
        /// </summary>
        /// <param name="targets"></param>
        void IDragSelectionView.SetSelectable(List<MeshCollider> targets)
        {
            selectable = targets ?? new List<MeshCollider>();
        }

        void IDragSelectionView.AddSelectable(MeshCollider target)
        {
            if (selectable == null)
                selectable = new List<MeshCollider>();
            if (target != null && selectable.Contains(target) == false)
                selectable.Add(target);
        }

        void IDragSelectionView.RemoveSelectable(MeshCollider target)
        {
            if (selectable == null)
                return;
            if (target != null && selectable.Contains(target))
                selectable.Remove(target);
        }

        void IDragSelectionView.Start()
        {
            ActivateVisualElement(false);
        }

        void IDragSelectionView.Update()
        {
            //var mousePos = Input.mousePosition;

            //// １）ドラッグ開始
            //if (Input.GetMouseButtonDown(0))
            //{

            //    if (IsPointerOverUI(mousePos))
            //        return; // UI上での操作や選択ボックスが非表示の場合は何もしない

            //}

            //// ２）ドラッグ中
            //if (Input.GetMouseButton(0))
            //{
            //}

            //// ３）ドラッグ終了
            //if (Input.GetMouseButtonUp(0))
            //{
        }

        private bool IsPointerOverUI(Vector3 mousePos)
        {
            if (selectionBox == null)
            {
                Debug.LogError("Selection box is not set.");
                return false;
            }

            var root = selectionBox?.parent;
            return Common.UIUtil.IsPointerOverUI(mousePos, root);
        }

        /// <summary>
        /// 選択状態をリセットする
        /// </summary>
        void IDragSelectionView.ResetSelectionStatus()
        {
            isEnable = false; // 有効化フラグをリセット
            startPos = Vector2.zero;
            selectionRect = new Rect(0, 0, 0, 0);
            ActivateVisualElement(false); // 選択ボックスを非表示にする

        }


        private void UpdateSelectionBox(Vector2 p1, Vector2 p2)
        {
            // 左下原点の長方形を作る
            float x = Mathf.Min(p1.x, p2.x);
            float y = Mathf.Min(p1.y, p2.y);
            float w = Mathf.Abs(p1.x - p2.x);
            float h = Mathf.Abs(p1.y - p2.y);
            selectionRect = new Rect(x, y, w, h);

            // UI表示用に上下を逆にする
            var uiY = Screen.height - y - h; // UIのY座標は上が0, 下が最大なので調整

            //// UI表示用にアンカー／サイズ設定
            if (selectionBox != null)
            {
                selectionBox.style.left = x;
                selectionBox.style.top = uiY;
                selectionBox.style.width = w;
                selectionBox.style.height = h;
            }
        }


        private void SelectWithin()
        {
            var targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("Main Camera not found.");
                return;
            }

            // ビューポート座標で矩形を正規化
            Vector2 v1 = targetCamera.ScreenToViewportPoint(selectionRect.min);
            Vector2 v2 = targetCamera.ScreenToViewportPoint(selectionRect.max);
            Rect vpRect = new Rect(
                Mathf.Min(v1.x, v2.x),
                Mathf.Min(v1.y, v2.y),
                Mathf.Abs(v1.x - v2.x),
                Mathf.Abs(v1.y - v2.y)
            );

            List<MeshCollider> hits = new List<MeshCollider>();
            foreach (var t in selectable)
            {
                var position = t.sharedMesh.bounds.center + t.transform.position;
                Vector3 vp = targetCamera.WorldToViewportPoint(position);
                if (vpRect.Contains(new Vector2(vp.x, vp.y)) && vp.z > 0)
                    hits.Add(t);
            }

            OnSelectionChanged?.Invoke(hits); // 選択が変更されたことを通知
            //Debug.Log($"選択オブジェクト数: {hits.Count}");
            //// 例：色を変える
            //foreach (var t in hits)
            //{
            //    t.GetComponent<Renderer>()?.material.SetColor("_Color", Color.red);
            //    t.transform.position += Vector3.up * 10.0f; // 少し上に移動して選択状態を視覚化
            //}
        }

        private void ActivateVisualElement(bool isActive)
        {
            if (selectionBox == null)
            {
                Debug.LogError("Selection box is not set.");
                return;
            }
            if (selectionBox?.enabledSelf == isActive)
            {
                // 既に同じ状態なら何もしない
                return;
            }

            selectionBox.SetEnabled(isActive); // VisualElementの有効化／無効化
            selectionBox.visible = isActive; // 表示状態を更新
            //selectionBox.style.visibility = isActive ? Visibility.Visible : Visibility.Hidden; // CSSのvisibilityを更新 　
        }
    }
}
