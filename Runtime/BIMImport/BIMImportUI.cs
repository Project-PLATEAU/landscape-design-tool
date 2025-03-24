using Landscape2.Runtime.UiCommon;
using SFB;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BIMImportUI
    {
        const string PanelResourceName = "Panel_BIMImport";
        const string ImportButtonName = "OKButton";

        const string FileLoadButtonName = "ImportButton";

        const string FileNameLabelName = "FileNameText";

        const string LatitudeInputFieldName = "Latitude"; // TextField
        const string LongitudeInputFieldName = "Longitude"; // TextField


        const string YawSliderName = "Slider_Yaw";
        const string HeightSliderName = "Slider_Height";

        TemplateContainer uiRoot;

        private TextField latitudeField;
        private TextField longitudeField;

        private SliderInt heightSliderField;

        private SliderInt yawSliderField;


        /// <summary>
        /// UIが表示されているか
        /// </summary>
        /// <returns>表示されている場合はtrue</returns>
        public bool IsShow() => uiRoot.style.display == DisplayStyle.Flex;

        public string FileNameLabel
        {
            get
            {
                return fileNameLabel.text;
            }
            set
            {
                fileNameLabel.text = value;
            }
        }

        public float LatitudeValue
        {
            get
            {
                if (float.TryParse(latitudeField.value, out var result))
                {
                    return result;
                }
                return float.NaN;
            }
            set
            {
                latitudeField.value = value.ToString();
            }
        }

        public float LongitudeValue
        {
            get
            {
                if (float.TryParse(longitudeField.value, out var result))
                {
                    return result;
                }
                return float.NaN;
            }
            set
            {
                longitudeField.value = value.ToString();
            }
        }

        public int HeightSliderValue
        {
            get => heightSliderField.value;
            set => heightSliderField.value = value;
        }

        public int YawSliderValue
        {
            get => yawSliderField.value;
            set => yawSliderField.value = value;
        }



        public System.Action<string> openFileAction;


        public System.Action<string> longitudeInputValueChanged;

        public System.Action<string> latitudeInputValueChanged;

        public System.Action<int> yawSliderValueChanged;
        public System.Action<int> heightSliderValueChanged;

        public System.Action importButtonOnClickAction;

        private Label fileNameLabel;




        public BIMImportUI(VisualElement uiRoot)
        {
            UIInitialize(uiRoot);
        }

        /// <summary>
        /// 右上の初期化
        /// 配置用UI初期化
        /// </summary>
        /// <param name="uiElement"></param>
        private void UIInitialize(VisualElement uiElement)
        {
            uiRoot = uiElement.Q<TemplateContainer>(PanelResourceName);

            // openFile
            var fileLoadButton = uiElement.Q<Button>(FileLoadButtonName);
            fileLoadButton.clicked += () =>
            {
                var path = StandaloneFileBrowser.OpenFilePanel("IFC File Path", "", "ifc", false);
                if (path.Length < 1)
                {
                    Debug.LogWarning($"filePanel selection is canceled");
                    return;
                }
                openFileAction?.Invoke(path[0]);
            };

            var fileNameLabel = uiRoot.Q<Label>(FileNameLabelName);
            this.fileNameLabel = fileNameLabel;

            // 緯度
            var lat = uiRoot.Q<TextField>(LatitudeInputFieldName);
            latitudeField = lat;
            lat.RegisterCallback<ChangeEvent<string>>(input =>
            {
                if (input.newValue != input.previousValue)
                {
                    latitudeInputValueChanged?.Invoke(input.newValue);
                }

            });

            // 経度
            var lon = uiRoot.Q<TextField>(LongitudeInputFieldName);
            longitudeField = lon;
            lon.RegisterCallback<ChangeEvent<string>>(input =>
            {
                if (input.newValue != input.previousValue)
                {
                    longitudeInputValueChanged?.Invoke(input.newValue);
                }
            });


            // Yaw回転 => Y軸回転
            var yawSlider = uiRoot.Q<SliderInt>(YawSliderName);
            yawSliderField = yawSlider;
            yawSlider.RegisterValueChangedCallback(evt => yawSliderValueChanged?.Invoke(evt.newValue));

            // 高さ => Y軸移動
            var heightSlider = uiRoot.Q<SliderInt>(HeightSliderName);
            heightSliderField = heightSlider;
            heightSlider.RegisterValueChangedCallback(evt => heightSliderValueChanged?.Invoke(evt.newValue));

            // 配置する
            var okButton = uiRoot.Q<Button>(ImportButtonName);
            okButton.clicked += () =>
            {
                importButtonOnClickAction?.Invoke();
            };
        }

        public void Show(bool show)
        {
            uiRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }


        public void Update(float deltaTime)
        {

        }
    }
}
