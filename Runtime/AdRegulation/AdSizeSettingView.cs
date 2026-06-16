using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    public interface IAdSizeSettingView
    {
        // サイズを設定する 支柱の高さは height-adHeightで求まるため引数にしない
        public void SetWithoutNotify(float width, float height, float depth, float adHeight);

        // 単位 1.0:1m 0.01:1cm
        public float MeterScale { get; }

        // 幅 cm
        public IFormContainerView WideContainer { get; }

        // 高さ cm
        public IFormContainerView HeightContainer { get; }

        // 奥行 cm
        public IFormContainerView DepthContainer { get; }

        // 高さ 広告 cm  billboard
        public IFormContainerView AdHeightContainer { get; }

        // 高さ　支柱 cm  pole
        public IFormContainerView AdLengthContainer { get; }

        public void Show();
        public void Hide();
    }

    public class AdSizeSettingView : IAdSizeSettingView
    {
        VisualElement root;

        public AdSizeSettingView(VisualElement root)
        {
            this.root = root;
            var wide = root.Q<VisualElement>("WideContainer");
            var height = root.Q<VisualElement>("HeightContainer");
            var depth = root.Q<VisualElement>("DepthContainer");
            var adHeight = root.Q<VisualElement>("AdHeightContainer");
            var adLength = root.Q<VisualElement>("AdLengthContainer");

            wideContainer = new FormContainerView(wide, defaultV: 1, max: 1000, unit: 0.01f, format: "F2");
            heightContainer = new FormContainerView(height, defaultV: 1, max: 1000, unit: 0.01f, format: "F2");
            depthContainer = new FormContainerView(depth, defaultV: 1, max: 1000, unit: 0.01f, format: "F2");
            adHeightContainer = new FormContainerView(adHeight, defaultV: 1, max: 1000, unit: 0.01f, format: "F2");
            adLengthContainer = new FormContainerView(adLength, defaultV: 1, max: 1000, unit: 0.01f, format: "F2");

        }

        IFormContainerView wideContainer;
        IFormContainerView heightContainer;
        IFormContainerView depthContainer;
        IFormContainerView adHeightContainer;
        IFormContainerView adLengthContainer;

        void IAdSizeSettingView.SetWithoutNotify(float width, float height, float depth, float adHeight)
        {
            wideContainer.SetWithoutNotify(width);
            heightContainer.SetWithoutNotify(height);
            depthContainer.SetWithoutNotify(depth);
            adHeightContainer.SetWithoutNotify(adHeight);
            adLengthContainer.SetWithoutNotify(height - adHeight);
        }

        void IAdSizeSettingView.Show()
        {
            root.style.display = DisplayStyle.Flex;
        }

        void IAdSizeSettingView.Hide()
        {
            root.style.display = DisplayStyle.None;
        }

        float IAdSizeSettingView.MeterScale => 1f;

        IFormContainerView IAdSizeSettingView.WideContainer => wideContainer;

        IFormContainerView IAdSizeSettingView.HeightContainer => heightContainer;

        IFormContainerView IAdSizeSettingView.DepthContainer => depthContainer;

        IFormContainerView IAdSizeSettingView.AdHeightContainer => adHeightContainer;

        IFormContainerView IAdSizeSettingView.AdLengthContainer => adLengthContainer;

    }
}
