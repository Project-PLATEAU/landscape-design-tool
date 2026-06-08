using PlateauToolkit.Sandbox;
using PlateauToolkit.Sandbox.Runtime;
using System;
using UnityEngine;

namespace Landscape2.Runtime
{
    public class AdAreaCalculateWallUI : IAdAreaCalcurateWallUI
    {
        private float wallAreaWidth = 0f;
        private float wallAreaHeight = 0f;

        public float WallAreaWidth { get => wallAreaWidth; set => wallAreaWidth = value; }
        public float WallAreaHeight { get => wallAreaHeight; set => wallAreaHeight = value; }

        public event Action<float, float> OnChangeWallAreaSize;

        IAdAreaCalculateModel model;

        IAdAreaCalcurateWallUIView view;

        GameObject adObject;


        public AdAreaCalculateWallUI(IAdAreaCalcurateWallUIView view, AdAreaCalcurate adAreaCalcurate)
        {
            this.view = view;
            this.model = adAreaCalcurate;

            // 壁面の寸法が変更された時の処理
            adAreaCalcurate.OnWallAreaWHChanged += (w, h) =>
            {
                WallAreaWidth = w;
                WallAreaHeight = h;
                OnChangeWallAreaSize?.Invoke(w, h);
            };

            // 広告面積の変更イベントを監視
            this.view.OnAdAreaWidthChanged += (width) =>
            {
                // AdAreaCalcurateWallUIView.AdAreaContainer.Widthで広告物のサイズ変更を行っている。
                // また、何故かここを実行する時にはadObjectがnullなっている
                UpdateAdAreaRatio();
            };
            this.view.OnAdAreaHeightChanged += (height) =>
            {
                // AdAreaCalcurateWallUIView.AdAreaContainer.Heightで広告物のサイズ変更を行っている。
                // また、何故かここを実行する時にはadObjectがnullなっている

                UpdateAdAreaRatio();
            };

            // 色変更を広告物に適用する
            this.view.OnAdObjectColorChanged += (color) =>
            {
                // AdAreaCalcurateWallUIView.AdColorControlで広告物の色変更を行っている。
                // また、何故かここを実行する時にはadObjectがnullなっている
            };

            this.view.SetPresenter(this);
        }

        /// <summary>
        /// 広告面積と面積割合を更新する
        /// </summary>
        private void UpdateAdAreaRatio()
        {
            // 壁面面積を計算
            var wallArea = WallAreaWidth * WallAreaHeight;

            if (wallArea <= 0f)
            {
                return; // 壁面面積が0の場合は計算しない
            }

            // ビューから現在の広告寸法を取得する必要があるが、
            // インターフェースに取得メソッドがないため、
            // ビュー側で直接計算を行うように通知する

            // 壁面面積をビューに通知して、ビュー側で面積割合を更新させる
            OnChangeWallAreaSize?.Invoke(WallAreaWidth, WallAreaHeight);
        }


        public void SetAdObject(GameObject obj)
        {
            adObject = obj;
        }

        public void PutAdObject(GameObject obj)
        {
            if (model.TryGetSelectWallBounds(out var bounds))
            {
                if (obj.TryGetComponent<AssetPlacedDirectionComponent>(out var comp))
                {
                    comp.enabled = false;
                }

                Debug.DrawRay(bounds.center + Vector3.up, bounds.Forward * 10f, Color.blue, 120, true);

                var lookAtPos = obj.transform.position + bounds.Forward;

                obj.transform.LookAt(lookAtPos);
            }

            adObject = obj;
        }

        public void SetWallAreaBounds(OrientedBounds bounds)
        {
            Debug.Log($"SetWallAreaBounds: {adObject} / {bounds}");
            if (adObject != null)
            {
                Debug.DrawRay(bounds.center + Vector3.up, bounds.Forward * 10f, Color.blue, 120, true);

                view.SetAdObjectToBuildingWall(adObject, bounds);
            }
        }
    }
}
