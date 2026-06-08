using PlateauToolkit.Sandbox.Runtime;
using UnityEngine;

namespace Landscape2.Runtime.AdRegulation
{
    public class AdSizeSettingUI : ISubComponent
    {
        // オブジェクト選択を取得
        IAdAreaSettingView adAreaSettingView;

        //
        IAdSizeSettingView view;
        IAdSizeSettingModule model;

        public AdSizeSettingUI(IAdSizeSettingView view, IAdSizeSettingModule model, IAdAreaSettingView adAreaSettingView)
        {
            this.view = view;
            this.model = model;

            this.adAreaSettingView = adAreaSettingView;
        }

        void ISubComponent.LateUpdate(float deltaTime)
        {
        }

        void ISubComponent.OnDisable()
        {
        }

        void ISubComponent.OnEnable()
        {
            // adAreaSettingViewがnullじゃないならイベントを登録する

            if (adAreaSettingView!= null)
            {
                adAreaSettingView.OnAdObjectSelected += v =>
                {
                    if (v == null)
                    {
                        model.SetScalableAd(null);
                        view.Hide();
                        return;
                    }

                    if (v.TryGetComponent<PlateauSandboxAdvertisementScaled>(out var scaled))
                    {
                        model.SetScalableAd(scaled);
                        view.Show();
                    }
                    else
                    {
                        model.SetScalableAd(null);
                        view.Hide();
                    }
                };

            }

            // View単位がcmなので変換mに変換する
            var meterScale = view.MeterScale;
            view.WideContainer.OnDistanceChanged += v =>
            {
                model.Width = v * meterScale;
            };
            view.HeightContainer.OnDistanceChanged += v =>
            {
                model.Height = v * meterScale;
            };
            view.DepthContainer.OnDistanceChanged += v =>
            {
                model.Depth = v * meterScale;
            };
            view.AdHeightContainer.OnDistanceChanged += v =>
            {
                model.AdHeight = v * meterScale;
            };
            view.AdLengthContainer.OnDistanceChanged += v =>
            {
                model.AdLength = v * meterScale;
            };

            model.OnValueChanged += () =>
            {
                SetToViewWithoutNotify(meterScale);
            };
            model.OnFaildValueChange += () =>
            {
                // 変更に失敗した場合は、現在の値を再設定する
                SetToViewWithoutNotify(meterScale);
            };

            // 初期値を設定
            SetToViewWithoutNotify(meterScale);
        }

        void ISubComponent.Start()
        {
        }

        void ISubComponent.Update(float deltaTime)
        {
        }

        private void SetToViewWithoutNotify(float meterScale)
        {
            view.SetWithoutNotify(
                model.Width / meterScale, model.Height / meterScale,
                model.Depth / meterScale, model.AdHeight / meterScale);
        }

    }
}
