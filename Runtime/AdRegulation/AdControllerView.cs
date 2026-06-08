using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    public interface IAdControllerView
    {
        // タブが変更された
        public event System.Action<bool> OnLTabChanged;
        public event System.Action<bool> OnRTabChanged;
        public event System.Action<bool> OnCTabChanged;
    }

    public class AdControllerView : IAdControllerView
    {
        private VisualElement root;

        private VisualElement TabMenu { get; set; }
        private RadioButton LTab { get => TabMenu.Q<RadioButton>("Tab_AdTest_l"); }
        private RadioButton RTab { get => TabMenu.Q<RadioButton>("Tab_AdTest_r"); }
        private RadioButton CTab { get => TabMenu.Q<RadioButton>("Tab_AdTest_c"); }

        // タブが変更された IAdControllerView
        public event System.Action<bool> OnLTabChanged;
        public event System.Action<bool> OnRTabChanged;
        public event System.Action<bool> OnCTabChanged;

        RoadAdSettingView roadAdSetting;
        BuildingAdSettingView buildingAdSetting;
        AdAreaSettingView adAreaSettingView;
        AdAssetListUI adAssetListUI;


        public AdControllerView(VisualElement root,
            RoadAdSettingView roadAdSetting,
            BuildingAdSettingView buildingAdSetting,
            AdAreaSettingView adAreaSettingView,
            AdAssetListUI adAssetListUI)

        {
            this.root = root;

            TabMenu = root.Q<GroupBox>("TabMenuGroup");

            LTab.RegisterValueChangedCallback((v) => OnTabChanged(0, v.newValue));
            RTab.RegisterValueChangedCallback((v) => OnTabChanged(1, v.newValue));
            CTab.RegisterValueChangedCallback((v) => OnTabChanged(2, v.newValue));


            this.roadAdSetting = roadAdSetting;
            this.buildingAdSetting = buildingAdSetting;
            this.adAreaSettingView = adAreaSettingView;
            this.adAssetListUI = adAssetListUI;

            OnLTabChanged += this.roadAdSetting.OnEnable;
            OnCTabChanged += this.buildingAdSetting.OnEnable;
            OnRTabChanged += this.adAreaSettingView.OnEnable;

        }

        // タブイベント遅延実行用　アクティブ処理を保持して必ず終了処理後に行う
        private System.Action<bool> tabChangedDelay = null;
        private bool isExecutedTabDisableEv = false;   // タブが無効化されたかどうか

        /// <summary>
        /// タブの状態変更時の処理をフローが分かりやすいように整理
        /// </summary>
        /// <param name="id">左から0</param>
        /// <param name="v">true: 有効化, false: 無効化</param>
        private void OnTabChanged(int id, bool v)
        {
            var tabChanged = new List<System.Action<bool>>()
            {
                OnLTabChanged,
                OnRTabChanged,
                OnCTabChanged
            };

            if (v == true)
            {
                // タブが有効化された場合
                if (isExecutedTabDisableEv)
                {
                    // 既に無効化処理が実行済みなら、即座に有効化処理を実行
                    tabChanged[id]?.Invoke(true);
                    isExecutedTabDisableEv = false;
                }
                else
                {
                    // 無効化処理がまだなら、有効化処理を遅延実行用に保持
                    tabChangedDelay = tabChanged[id];
                }
                return;
            }

            // タブが無効化された場合
            tabChanged[id]?.Invoke(false);
            isExecutedTabDisableEv = true;

            // 遅延実行用の有効化処理があれば実行
            if (tabChangedDelay != null)
            {
                tabChangedDelay?.Invoke(true);
                tabChangedDelay = null;
                isExecutedTabDisableEv = false;
            }
        }

        public void CallbackOnDisplay(bool isDisplay)
        {
            UICallBack.OnDisplay(this, isDisplay);
        }

        private static class UICallBack
        {
            public static void OnDisplay(AdControllerView self, bool isDisplay)
            {
                //Debug.Log("AdRegulationView Display : " + isDisplay.ToString());

                // 広告規制機能の表示されたらアセットリストを表示する処理を実行する。　タブ変更イベントの処理で上書きできるように処理を前に置く
                if (isDisplay)
                {
                    self.adAssetListUI.Show();
                }
                else
                {
                    self.adAssetListUI.HideAndCancel();
                }

                // 選択されているタブに　交通規制機能の表示状態に合わせてタブ変更イベントを実行
                if (self.LTab.value == true)
                {
                    self.OnLTabChanged?.Invoke(isDisplay);
                }
                else if (self.CTab.value == true)
                {
                    self.OnCTabChanged?.Invoke(isDisplay);
                }
                else if (self.RTab.value == true)
                {
                    self.OnRTabChanged?.Invoke(isDisplay);
                }
                else
                {
                    Debug.Assert(false, "RadioButtonを想定しているのでどれかはtrueでなければならない");
                }

            }

        }
    }
}
