using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class VisualSettingConfigUI
    {
        const string SettingPanelResourceName = "Panel_AdjustColor";

        const string BrightnessSlider_Name = "PostExposureSlider";
        const string ContrastSlider_Name = "ContrastSlider";

        const string CloseButton_Name = "OKButton";

        const float Brightness_Max = 2.0f;
        const float Contrast_Max = 100.0f;

        VisualElement panelClone;

        // 0〜100で0.0〜2.0。デフォルトは0.3
        SliderInt brightnessSlider;
        // 0〜100で0〜100。defaultは40(多分)
        SliderInt contrastSlider;

        Button closeButton;

        ColorAdjustments colorAdjustments = null;

        public VisualSettingConfigUI(VisualElement uiRoot)
        {
            var panel = Resources.Load<VisualTreeAsset>(SettingPanelResourceName);
            panelClone = panel.CloneTree();
            uiRoot.Add(panelClone);

            brightnessSlider = panelClone.Q<SliderInt>(BrightnessSlider_Name);
            contrastSlider = panelClone.Q<SliderInt>(ContrastSlider_Name);

            var volume = GameObject.FindAnyObjectByType<Volume>();
            if (!volume.profile.TryGet<ColorAdjustments>(out var ca))
            {
                ca = volume.profile.Add<ColorAdjustments>();
            }
            colorAdjustments = ca;

            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.postExposure.overrideState = true;


            brightnessSlider.value = (int)(colorAdjustments.postExposure.value / Brightness_Max * 100f);
            contrastSlider.value = (int)(colorAdjustments.contrast.value / Contrast_Max * 100f);

            brightnessSlider.RegisterValueChangedCallback(evt =>
            {
                colorAdjustments.postExposure.value = (float)evt.newValue / 100f * Brightness_Max;
            });

            contrastSlider.RegisterValueChangedCallback(evt =>
            {
                colorAdjustments.contrast.value = (float)evt.newValue / 100f * Contrast_Max;
            });

            closeButton = panelClone.Q<Button>(CloseButton_Name);

            closeButton.clicked += () =>
            {
                Show(false);
            };


            panelClone.RegisterCallback<GeometryChangedEvent>(v =>
            {
                var panel = panelClone.Q<VisualElement>("Panel");
                panelClone.style.position = Position.Absolute;
                Vector3 newPos = new Vector3(
                    (Screen.width - panel.contentRect.width) / 2,
                    (Screen.height - panel.contentRect.height) / 2,
                    0f
                );
                panelClone.transform.position = newPos;
            });

            Show(false);
        }

        public void Show(bool show)
        {
            panelClone.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }



    }
}
