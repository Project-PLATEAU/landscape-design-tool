using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    public class BuildingAdSettingView : IBuildingAdSettingView
    {

        // readonly (string, string) msg = new("広告面積割合を算出した建物を選択して下さい", "");
        readonly (string, string) wallAreaMsg = new("広告面積割合を算出したい建物の対象壁面を選択してください", "");
        readonly (string, string) buildingAreaMsg = new("広告面積割合を算出したい建物を選択してください", "");
        readonly (string, string) divBuildingAreaMsg = new("広告面積割合を算出したい建物の対象壁面を選択してください", "");

        private VisualElement root;

        private VisualElement tabMenu;

        private IDialog_TutorialInfoView dialog_TutorialInfoView;


        private readonly RadioButton leftTab;
        private readonly RadioButton centerTab;
        private readonly RadioButton rightTab;

        bool IsEnable { get; set; } = false;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="root">Q<VisualElement>("BuildingAdSetting")のVisualElement</param>
        public BuildingAdSettingView(VisualElement root, IDialog_TutorialInfoView dialog_TutorialInfoView)
        {
            this.root = root;

            //root.style.display = DisplayStyle.Flex;
            tabMenu = root.Q<GroupBox>("TabMenuGroup");

            leftTab = root.Q<RadioButton>("Tab_AdArea_face_l");
            centerTab = root.Q<RadioButton>("Tab_AdArea_face_c");
            rightTab = root.Q<RadioButton>("Tab_AdArea_face_r");

            leftTab.RegisterValueChangedCallback(CallbackLTabChanged);
            centerTab.RegisterValueChangedCallback(CallbackCTabChanged);
            rightTab.RegisterValueChangedCallback(CallbackRTabChanged);

            this.dialog_TutorialInfoView = dialog_TutorialInfoView;

        }

        public event Action<bool> OnLTabChanged;
        public event Action<bool> OnCTabChanged;
        public event Action<bool> OnRTabChanged;
        public event Action OnViewEnable;
        public event Action OnViewDisable;


        void CallbackLTabChanged(ChangeEvent<bool> v)
        {
            if (v.newValue)
            {
                dialog_TutorialInfoView.Set(wallAreaMsg.Item1, wallAreaMsg.Item2);
            }
            OnLTabChanged?.Invoke(v.newValue);
        }

        void CallbackCTabChanged(ChangeEvent<bool> v)
        {
            if (v.newValue)
            {
                dialog_TutorialInfoView.Set(buildingAreaMsg.Item1, buildingAreaMsg.Item2);
            }
            OnCTabChanged?.Invoke(v.newValue);
        }

        void CallbackRTabChanged(ChangeEvent<bool> v)
        {
            if (v.newValue)
            {
                dialog_TutorialInfoView.Set(divBuildingAreaMsg.Item1, divBuildingAreaMsg.Item2);
            }
            OnRTabChanged?.Invoke(v.newValue);
        }

        public void OnEnable(bool isEnable)
        {
            if (isEnable == IsEnable)
                return;
            UICalback.OnEnableToggleChanged(this, isEnable);
        }

        public void OnDisplay(bool isDisplay)
        {
            if (isDisplay)
            {
                OnViewEnable?.Invoke();
            }
            else
            {
                OnViewDisable?.Invoke();
            }
        }

        private static class UICalback
        {
            public static void OnEnableToggleChanged(BuildingAdSettingView self, bool isEnabled)
            {
                // EnableToggleの値が変更されたときの処理

                // 設定パネル表示/非表示の切り替え
                var currentState = self.root.style.display;
                self.root.style.display = isEnabled ? DisplayStyle.Flex : DisplayStyle.None;

                self.OnDisplay(isEnabled);


                if (isEnabled)
                {
                    self.dialog_TutorialInfoView.CurrentOwner = self;
                    if (self.leftTab.value)
                    {
                        self.dialog_TutorialInfoView.Set(self.wallAreaMsg.Item1, self.wallAreaMsg.Item2);
                    }
                    else
                    {
                        self.dialog_TutorialInfoView.Set(self.buildingAreaMsg.Item1, self.buildingAreaMsg.Item2);
                    }
                }
                else
                {
                    if (self.dialog_TutorialInfoView.CurrentOwner == self)
                    {
                        self.dialog_TutorialInfoView.CurrentOwner = null;
                        self.dialog_TutorialInfoView.Set("", "");
                    }
                }

                self.IsEnable = isEnabled;

            }

        }
    }
}
