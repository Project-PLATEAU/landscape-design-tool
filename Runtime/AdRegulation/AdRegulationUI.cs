using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    public static class AdRegulationBuilder
    {
        public static List<ISubComponent> Build(VisualElement root, LandscapeCamera landscapeCamera, SaveSystem saveSystem, DynamicTile.INotifyUpdated dynamicTileUpdater)
        {



            // それぞれのVisualElementを取得
            var adReguRoot = root.Q("MainContainer");

            var assetListTitleRoot = root.Q("Title_Left");

            var adControllerRoot = adReguRoot.Q("Panel_AdController");
            var roadAdSettingRoot = adControllerRoot.Q("RoadAdSetting");
            var buildingAdSettingRoot = adControllerRoot.Q("BuildingAdSetting");

            var adAreaSettingRoot = adControllerRoot.Q("AdAreaSetting");
            var adSizeSettingRoot = adControllerRoot.Q("AdSizeSettingPanel");

            var dialogTutorialInfoRoot = adReguRoot.Q("Dialog");
            var roadEdgePanelRoot = adReguRoot.Q("Panel_RoadEdgePanel");

            var adAssetListRoot = adReguRoot.Q("Panel_AssetList");



            // UI系クラスを作成

            var adAssetListUI = new AdAssetListUI(adAssetListRoot, assetListTitleRoot, new AdAssetList());

            // view
            var mouseOperationView = new AdRegulationMouseOperationView(root);
            var adAreaCalcurateWallUIView = new AdAreaCalcurateWallUIView(buildingAdSettingRoot, roadEdgePanelRoot.parent);
            var adAreaCalcurateBuildingUIView = new AdAreaCalcurateBuildingUIView(buildingAdSettingRoot);
            var roadAdSettingView = new RoadAdSettingView(roadAdSettingRoot, mouseOperationView, adAssetListUI, dynamicTileUpdater);
            var adSizeSettingView = new AdSizeSettingView(adSizeSettingRoot);
            var dialogTutorialInfoV = new Dialog_TutorialInfoView(dialogTutorialInfoRoot);
            var buildingAdSettingView = new BuildingAdSettingView(buildingAdSettingRoot, dialogTutorialInfoV);
            var adAreaSettingView = new AdAreaSettingView(adAreaSettingRoot, dialogTutorialInfoV, mouseOperationView);
            var adControllerV = new AdControllerView(adControllerRoot, roadAdSettingView, buildingAdSettingView, adAreaSettingView, adAssetListUI);
            var roadEdgePanelV = new Panel_RoadEdgePanelView(roadEdgePanelRoot);
            var partitionView = new AdAreaCalculateWallPartitionUIView(buildingAdSettingRoot);
            var partitionBandVisualizer = new AdAreaCalculateWallPartitionVisualizer();

            // model
            var adSizeSettingM = new AdSizeSettingModule();
            var partition = new AdAreaCalculateWallPartition();
            var adAreaCalculate = new AdAreaCalcurate(landscapeCamera, adControllerRoot, partition, dynamicTileUpdater);
            var displayInstallationRestrictionZonesModule = new DisplayInstallationRestrictionZones.DisplayInstallationRestrictionZonesModule();
            var roadAdSettingM = new AdvertisingPlacementRestrictions.AdvertisingPlacementRestrictionsModule(saveSystem);

            // presenter
            var adAreaCalcurateUI = new AdAreaCalcurateUI(adAssetListUI, buildingAdSettingView, adAreaCalcurateBuildingUIView, adAreaCalcurateWallUIView, adAreaCalculate, partitionView);
            var roadAdSetttngUI = new RoadAdSettingUI(roadAdSettingView, dialogTutorialInfoV, displayInstallationRestrictionZonesModule);
            var adSizeSettingUI = new AdSizeSettingUI(adSizeSettingView, adSizeSettingM, adAreaSettingView);
            var adAreaSettingUI = new AdAreaSettingUI(adAreaSettingView, adAssetListUI, roadAdSettingM);
            var partitionUI = new AdAreaCalculateWallPartitionUI(adAreaCalculate, partition, partitionView, partitionBandVisualizer);

            var adRegulation = new AdRegulationUI(
                new AdRegulationView(root, adReguRoot, adControllerV),
                roadAdSetttngUI,
                adAreaSettingUI,
                adSizeSettingUI,
                mouseOperationView,
                partitionUI
                );


            return new List<ISubComponent>() { adRegulation, adAreaCalculate, adAssetListUI };

        }
    }

    //AdRegulation：広告規制全体の画面
    //Dialog_TutorialInfo：画面中央ツールチップ
    //Panel_Search：検索パネル
    //Panel_AdController：設置規制編集パネル（右側のパネル）
    //→道路規制で表示するエレメント名：RoadAdSetting
    //→建物規制で表示するエレメント名：BuildingAdSetting
    //→→建物規制/1壁面タブで表示するエレメント：WallInfoPanel
    //→→建物規制/総面積タブで表示するエレメント：BuildingInfoPanel

    //道路の広告制限幅を設定するパネル：Panel_RoadEdgePanel
    //左側アセットリスト：Panel_AdvertiseAssetList


    public class AdRegulationUI : ISubComponent
    {
        AdRegulationView view;

        AdAreaSettingUI adAreaSettingUI;
        AdSizeSettingUI adSizeSettingUI;
        IAdRegulationMouseOperationView mouseOperationView;


        List<ISubComponent> subComponents = new List<ISubComponent>();

        public AdRegulationUI(
            AdRegulationView view,
            RoadAdSettingUI roadAdSettingUI,
            AdAreaSettingUI adAreaSettingUI,
            AdSizeSettingUI adSizeSettingUI,
            IAdRegulationMouseOperationView mouseOperationView,
            AdAreaCalculateWallPartitionUI partitionUI)
        {
            this.view = view;

            this.adAreaSettingUI = adAreaSettingUI;
            this.adSizeSettingUI = adSizeSettingUI;
            this.mouseOperationView = mouseOperationView;

            // SubComponentがあるものはリストに追加
            subComponents = new List<ISubComponent>
            {
                roadAdSettingUI as ISubComponent,
                adAreaSettingUI as ISubComponent,
                adSizeSettingUI as ISubComponent,
                mouseOperationView as ISubComponent,
                partitionUI as ISubComponent,
            };
            subComponents.RemoveAll(x => x == null);
        }

        void ISubComponent.LateUpdate(float deltaTime)
        {
            subComponents.ForEach(x => x.LateUpdate(deltaTime));
        }

        void ISubComponent.OnDisable()
        {
            subComponents.ForEach(x => x.OnDisable());
        }

        void ISubComponent.OnEnable()
        {
            subComponents.ForEach(x => x.OnEnable());
        }

        void ISubComponent.Start()
        {
            subComponents.ForEach(x => x.Start());
        }

        void ISubComponent.Update(float deltaTime)
        {
            subComponents.ForEach(x => x.Update(deltaTime));
        }
    }
}
