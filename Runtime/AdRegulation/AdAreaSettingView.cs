using PlateauToolkit.Sandbox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    public interface IAdAreaSettingView
    {
        // 有効状態切り替え
        event Action<bool> OnEnableChanged;

        // 広告物が選択されたときのイベント
        event Action<GameObject> OnAdObjectSelected;

        IFormContainerView FormContainerView { get; }

        // エリアの表示状態が切り替えられたときのイベント
        event Action<bool> OnDisplayStatusChanged;
    }

    public class AdAreaSettingView : IAdAreaSettingView, ISubComponent
    {
        readonly (string, string) msg = new("規制範囲を表示したい広告物を設置してください", "");

        // Advertisement_AFrameSign_01
        // Advertisement_WallAdPanel_01
        // Advertisement_Vertical_04
        // Advertisement_RooftopAdBoard_01
        // Advertisement_Vertical_05
        // Advertisement_DigitalSignage_01
        // Advertisement_AdBoard_01
        // Advertisement_AdTower_01
        // Advertisement_RooftopAdTower_01
        // Advertisement_SidewalkSign_01
        // Advertisement_Billboard_01
        // Advertisement_WallSignboard
        // Advertisement_Billboard

        private readonly string[] advertisingPlacementRestrictionTypes = new string[]
        {
            "Advertisement_AFrameSign",
            "Advertisement_DigitalSignage",
            "Advertisement_AdBoard",
            "Advertisement_AdTower",
            "Advertisement_SidewalkSign",
            "Advertisement_Billboard",
            //"Advertisement_WallSignboard",  // 仮
        };

        private VisualElement root;
        private IAdRegulationMouseOperationView adRegulationMouseOperationView;
        private IFormContainerView formContainerView;
        private IDialog_TutorialInfoView dialog_TutorialInfoView;

        private Toggle displayToggle;

        // 広告物が選択されたときのイベント IAdAreaSettingView
        public event Action<GameObject> OnAdObjectSelected;
        IFormContainerView IAdAreaSettingView.FormContainerView => formContainerView;

        private bool IsEnable
        {
            get; set;
        } = false;

        public AdAreaSettingView(VisualElement root, IDialog_TutorialInfoView dialog_TutorialInfoView, IAdRegulationMouseOperationView mouseOperationView)
        {
            this.root = root;
            var formContainer = root.Q<VisualElement>("WideContainer");
            this.formContainerView = new FormContainerView(formContainer, defaultV:5, unit:0.1f, max:500.0f);
            this.formContainerView.Reset(); // フォームコンテナのリセット

            this.dialog_TutorialInfoView = dialog_TutorialInfoView;
            this.adRegulationMouseOperationView = mouseOperationView;

            this.displayToggle = root.Q<Toggle>("DisplayToggle");

            this.displayToggle.RegisterValueChangedCallback((e) => { onDisplayStatusChanged?.Invoke(e.newValue); });

        }

        private Action<bool> onDisplayStatusChanged;
        event Action<bool> IAdAreaSettingView.OnDisplayStatusChanged
        {
            add
            {
                onDisplayStatusChanged += value;
            }

            remove
            {
                onDisplayStatusChanged -= value;
            }
        }

        // 有効状態切り替え
        private event Action<bool> onEnableChanged;
        event Action<bool> IAdAreaSettingView.OnEnableChanged 
        {
            add
            {
                onEnableChanged += value;
            }
            remove
            {
                onEnableChanged -= value;
            }
        }
        

        public void OnEnable(bool isEnable)
        {
            if (isEnable == IsEnable)
                return;
            UICalback.OnEnableToggleChanged(this, isEnable);
        }

        private void SelectAdObject(GameObject adObj)
        {
            if (IsEnable == false)
                return; // 機能が無効化されている場合は何もしない

            // カメラ移動が有効の時はスキップ
            if (!CameraMoveByUserInput.IsMouseActive)
                return;

            if (Common.AdvertisementObjectUtil.IsTargetAdObjWithNameFilter(adObj, advertisingPlacementRestrictionTypes.ToList()))
            {
                Debug.Log("Advertising Object selected.");
                OnAdObjectSelected?.Invoke(adObj); // イベントを発火
                return;
            }

            return;
        }

        void ISubComponent.LateUpdate(float deltaTime)
        {

        }

        void ISubComponent.OnEnable()
        {

        }

        void ISubComponent.OnDisable()
        {

        }

        void ISubComponent.Start()
        {

        }

        void ISubComponent.Update(float deltaTime)
        {

        }

        private static class UICalback
        {
            public static void OnEnableToggleChanged(AdAreaSettingView self, bool isEnabled)
            {
                // EnableToggleの値が変更されたときの処理

                // 設定パネル表示/非表示の切り替え
                self.root.style.display = isEnabled ? DisplayStyle.Flex : DisplayStyle.None;

                if (isEnabled)
                {
                    self.dialog_TutorialInfoView.CurrentOwner = self;
                    self.dialog_TutorialInfoView.Set(self.msg.Item1, self.msg.Item2);
                    //// カメラの移動を無効化
                    //CameraMoveByUserInput.CameraScriptOwner = self;
                    //CameraMoveByUserInput.IsCameraMoveActive = false;

                    if (self.adRegulationMouseOperationView != null)
                        self.adRegulationMouseOperationView.OnAdObjectSelected += self.SelectAdObject;
                }
                else
                {
                    //// カメラの移動を有効化
                    //if (CameraMoveByUserInput.CameraScriptOwner == self)
                    //{
                    //    CameraMoveByUserInput.IsCameraMoveActive = true;
                    //    CameraMoveByUserInput.CameraScriptOwner = null;
                    //}

                    if (self.dialog_TutorialInfoView.CurrentOwner == self)
                    {
                        self.dialog_TutorialInfoView.CurrentOwner = null;
                        self.dialog_TutorialInfoView.Set("","");
                    }


                    // 選択を外す
                    self.OnAdObjectSelected?.Invoke(null); // イベントを発火
                    if (self.adRegulationMouseOperationView != null)
                        self.adRegulationMouseOperationView.OnAdObjectSelected -= self.SelectAdObject;
                }

                self.IsEnable = isEnabled;
                self.onEnableChanged?.Invoke(isEnabled);

            }

        }

    }
}
