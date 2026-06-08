using PlateauToolkit.Sandbox.Runtime;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    /// <summary>
    /// シーンビュー上のゲームオブジェクトに対するマウス操作を行うためのインターフェース。
    /// </summary>
    public interface IAdRegulationMouseOperationView
    {

        // 広告物オブジェクトが選択された時のイベント
        event System.Action<GameObject> OnAdObjectSelected;

        // マウスボタンが押された時のイベント
        event System.Action<Vector3> OnMouseButtonDown;
        event System.Action<Vector3> OnMouseButton;
        event System.Action<Vector3> OnMouseButtonUp;

        //// サイズ可変のゲームオブジェクトが選択された時のイベント
        //event System.Action<PlateauSandboxAdvertisementScaled> OnScalableAdSelected;

    }

    public class AdRegulationMouseOperationView : IAdRegulationMouseOperationView, ISubComponent
    {
        private VisualElement root;

        public AdRegulationMouseOperationView(VisualElement root)
        {
            this.root = root;
        }

        private event Action<GameObject> OnAdObjectSelected;
        event Action<GameObject> IAdRegulationMouseOperationView.OnAdObjectSelected
        {
            add
            {
                OnAdObjectSelected += value;
            }

            remove
            {
                OnAdObjectSelected -= value;
            }
        }

        private event Action<Vector3> OnMouseButtonDown;
        event Action<Vector3> IAdRegulationMouseOperationView.OnMouseButtonDown
        {
            add
            {
                OnMouseButtonDown += value;
            }

            remove
            {
                OnMouseButtonDown -= value;
            }
        }

        private event Action<Vector3> OnMouseButton;
        event Action<Vector3> IAdRegulationMouseOperationView.OnMouseButton
        {
            add
            {
                OnMouseButton += value;
            }

            remove
            {
                OnMouseButton -= value;
            }
        }

        private event Action<Vector3> OnMouseButtonUp;
        event Action<Vector3> IAdRegulationMouseOperationView.OnMouseButtonUp
        {
            add
            {
                OnMouseButtonUp += value;
            }

            remove
            {
                OnMouseButtonUp -= value;
            }
        }

        void ISubComponent.LateUpdate(float deltaTime)
        {
        }

        void ISubComponent.OnDisable()
        {
        }

        void ISubComponent.OnEnable()
        {
        }

        void ISubComponent.Start()
        {
        }

        void ISubComponent.Update(float deltaTime)
        {
            var mousePos = Input.mousePosition;

            // 広告物の選択
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI(mousePos))
                    return; // UI上での操作や選択ボックスが非表示の場合、またはマウス操作が無効な場合は何もしない

                SelectAdObj(mousePos);

                OnMouseButtonDown?.Invoke(mousePos); // マウスボタンが押されたイベントを発火

            }

            // ２）長押し
            if (Input.GetMouseButton(0))
            {
                OnMouseButton?.Invoke(mousePos); // マウスボタンが押されている間のイベントを発火
            }

            // ３）ドラッグ終了
            if (Input.GetMouseButtonUp(0))
            {
                OnMouseButtonUp?.Invoke(mousePos); // マウスボタンが離されたイベントを発火
            }

        }

        /// <summary>
        /// 広告物を選択する
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void SelectAdObj(Vector3 mousePos)
        {
            // Raycastを使用して広告物を選択
            var ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                var hitObject = hitInfo.collider.gameObject;

                var adObjRoot = Common.AdvertisementObjectUtil.FindAdObjRoot(hitObject);
                if (adObjRoot != null)
                {
                    Debug.Log("Advertising Object selected.");
                    OnAdObjectSelected?.Invoke(adObjRoot); // イベントを発火
                    return;
                }

            }

            Debug.Log("No Advertising Object selected.");
        }

        private bool IsPointerOverUI(Vector3 mousePos)
        {
            return Common.UIUtil.IsPointerOverUI(mousePos, root);
        }

    }
}
