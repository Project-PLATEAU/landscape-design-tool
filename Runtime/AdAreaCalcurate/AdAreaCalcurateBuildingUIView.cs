using Landscape2.Runtime.AdRegulation;
using System;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public interface IAdAreaCalcurateBuildingUIView
    {
        public float WallArea { get; set; }

        /// <summary>
        /// 広告面積変更時イベント
        /// </summary>
        public event System.Action<float> OnAdAreaValueChanged;

        void Show(bool isShow);

        bool IsShow { get; }
    }

    public class AdAreaCalcurateBuildingUIView : IAdAreaCalcurateBuildingUIView
    {

        const string basePanelName = "BuildingInfoPanel";

        const string areaLabelPanelName = "InffoArea";

        const string adAreaInputPanelName = "AdAreaContainer";

        const string adAreaPercentageLabelName = "PercentValue";


        public event Action<float> OnAdAreaValueChanged;

        readonly VisualElement panel;

        float wallArea = 0f;

        float adArea = 0f;

        float adPercentage = 0f;

        // 建物総面積field
        Label areaLabel;

        Label adPercentageLabel;

        // 広告面積入力field
        FormContainerView adAreaFormContainer;

        public float WallArea
        {
            get => wallArea;
            set
            {
                wallArea = value;
                areaLabel.text = $"{wallArea:F2}";

                var percentage = CalculateAdAreaPercentage(wallArea, adArea);
                adPercentage = percentage;
                adPercentageLabel.text = $"{adPercentage:F2}";
            }
        }

        public bool IsShow => panel.style.display == DisplayStyle.Flex;

        public AdAreaCalcurateBuildingUIView(VisualElement root)
        {
            UnityEngine.Debug.Log($"BuildingUIView root: {root.name}");
            panel = root.Q<VisualElement>(basePanelName);
            areaLabel = panel.Q<VisualElement>(areaLabelPanelName).Q<Label>("Area");

            var adAreaPanel = panel.Q<VisualElement>(adAreaInputPanelName);
            adAreaFormContainer = new FormContainerView(adAreaPanel, defaultV:0.0f, unit:0.01f, max:99999, min:0, format:"F2");

            adPercentageLabel = panel.Q<Label>(adAreaPercentageLabelName);

            adAreaFormContainer.OnDistanceChanged += (v) =>
            {
                adArea = v;

                var percentage = CalculateAdAreaPercentage(wallArea, adArea);
                adPercentage = percentage;
                adPercentageLabel.text = $"{adPercentage:F2}";

                OnAdAreaValueChanged?.Invoke(v);
                //AdArea = v;
            };

            WallArea = 0f;
            adPercentage = 0f;
        }

        float CalculateAdAreaPercentage(float wallArea, float adArea)
        {
            if (wallArea <= 0f)
            {
                return 0f;
            }

            return (adArea / wallArea) * 100f;
        }


        public void Show(bool isShow)
        {
            UnityEngine.Debug.Log($"BuildingUIView show :{isShow}");
            panel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
