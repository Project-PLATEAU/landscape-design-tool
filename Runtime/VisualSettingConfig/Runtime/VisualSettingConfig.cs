using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class VisualSettingConfig : ISubComponent
    {
        const string VisualSettingConfigButtonName = "VisualSettingButton";

        VisualSettingConfigUI ui;

        public VisualSettingConfig(VisualElement globalNavi, VisualSettingConfigUI visualSettingConfigUI)
        {
            var visualSettingButton = globalNavi.Q<Button>(VisualSettingConfigButtonName);
            ui = visualSettingConfigUI;
            var settingPanel = globalNavi.Q<VisualElement>("SettingPanel");

            visualSettingButton.clicked += () =>
            {
                if (settingPanel.style.display == DisplayStyle.Flex)
                {
                    settingPanel.style.display = DisplayStyle.None;
                }
                ui.Show(true);
            };
        }

        public void LateUpdate(float deltaTime)
        {
        }

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
        }

        public void Start()
        {

        }

        public void Update(float deltaTime)
        {
        }
    }
}
