using Landscape2.Runtime.AdAreaCalcurateSub;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Landscape2.Runtime
{
    /// <summary>
    /// AdAreaCalculate(==model)に要求するInterface
    /// </summary>
    public interface IAdAreaCalculateModel
    {
        enum SelectMode
        {
            // 1壁面選択
            AreaWall,

            // 総壁面選択
            BuildingWall,

            // 分割壁面選択
            PartitionWall,
        }

        // 総壁面選択モード
        void SetBuidingWallSelectMode();

        // 1壁面選択モード
        void SetWallSelectMode();

        // 分割壁面選択モード
        void SetPartitionWallSelectMode();

        bool TryGetSelectWallBounds(out OrientedBounds bounds);

        void Activate();
        void Deactivate();

        event System.Action<float, float> OnWallAreaWHChanged;

        void OnAssetSelect(GameObject asset);
        void OnAssetPut(GameObject asset);

        void OnAssetSelectCancel();

        event System.Action<SelectMode, ReadOnlyArray<Wall>, ReadOnlyArray<OrientedBounds>> OnWallSelected;
    }

    /// <summary>
    /// BuildingAdSettingViewに要求するInterface
    /// </summary>
    public interface IBuildingAdSettingView
    {
        public event System.Action<bool> OnLTabChanged;
        public event System.Action<bool> OnCTabChanged;
        public event System.Action<bool> OnRTabChanged;

        /// <summary>
        /// Viewが有効になった時に呼ばれる
        /// </summary>
        public event System.Action OnViewEnable;

        /// <summary>
        /// Viewが無効になった時に呼ばれる
        /// </summary>
        public event System.Action OnViewDisable;
    }


    /// <summary>
    /// 壁面広告計算UIのPresenter
    /// </summary>
    public class AdAreaCalcurateUI
    {

        AdAreaCalcurateBuildingUI buildingUI;

        AdAreaCalculateWallUI wallUI;

        IAdAreaCalculateModel adCalculateModel;

        IAdAreaCalcurateWallUIView wallUIView;
        IAdAreaCalcurateBuildingUIView buildingUIView;
        IAdAreaCalculateWallPartitionUIView partitionUIView;

        IAdAssetListUI adAssetListUI;

        IAdAreaCalculateModel model;

        bool firstView = true;


        public AdAreaCalcurateUI(
            AdAssetListUI adAssetListUI,
            IBuildingAdSettingView buildingAdSettingView,
            IAdAreaCalcurateBuildingUIView adAreaCalculateBuidlingUIView,
            IAdAreaCalcurateWallUIView adAreaCalculateWallUIView,
            AdAreaCalcurate model,
            IAdAreaCalculateWallPartitionUIView partitionUIView
            )
        {
            this.adCalculateModel = model;
            this.wallUIView = adAreaCalculateWallUIView;
            this.buildingUIView = adAreaCalculateBuidlingUIView;
            this.partitionUIView = partitionUIView;
            this.adAssetListUI = adAssetListUI;
            this.model = model;

            adAreaCalculateWallUIView.Show(false);
            adAreaCalculateBuidlingUIView.Show(false);


            buildingAdSettingView.OnViewDisable += () =>
            {
                Debug.Log($"onviewdisable");
                ResetAssetListCallback();

                adAssetListUI.Show();
                model.Deactivate();
            };

            buildingAdSettingView.OnViewEnable += () =>
            {
                Debug.Log($"onviewenable : {adAreaCalculateWallUIView.IsShow} / {adAreaCalculateBuidlingUIView.IsShow}");
                model.Activate();

                SetAssetListCallback();

                if (firstView)
                {
                    adAreaCalculateWallUIView.Show(true);
                    firstView = false;
                }
            };

            // tab切り替え
            buildingAdSettingView.OnLTabChanged += (v) =>
            {
                // 1壁面に切り替え

                Debug.Log($"onltabchanged:{v}");

                adAreaCalculateWallUIView.Show(v);
                if (v)
                {
                    adAssetListUI.Show();

                    SetAssetListCallback();

                    adCalculateModel.SetWallSelectMode();
                }
            };

            buildingAdSettingView.OnCTabChanged += (v) =>
            {
                // 総壁面に切り替え

                Debug.Log($"onctabchanged:{v}");
                adAreaCalculateBuidlingUIView.Show(v);
                if (v)
                {
                    adAssetListUI.CancelAssetPlacement();
                    adAssetListUI.HideAndCancel();

                    ResetAssetListCallback();
                    adCalculateModel.SetBuidingWallSelectMode();
                }
            };

            buildingAdSettingView.OnRTabChanged += (v) =>
            {
                // 分割壁面に切り替え

                Debug.Log($"onrtabchanged:{v}");
                partitionUIView.Show(v);

                if (v)
                {
                    adAssetListUI.CancelAssetPlacement();
                    adAssetListUI.HideAndCancel();

                    ResetAssetListCallback();
                    adCalculateModel.SetPartitionWallSelectMode();
                }
            };

            // buildingviewと繋ぎ込む
            buildingUI = new AdAreaCalcurateBuildingUI(adAreaCalculateBuidlingUIView, model);

            // wallareaviewと繋ぎこむ
            wallUI = new AdAreaCalculateWallUI(adAreaCalculateWallUIView, model);
        }


        void OnAssetSelect(GameObject asset)
        {
            Debug.Log($"onassetselect:{asset.name}");
            model.OnAssetSelect(asset);
            wallUI.SetAdObject(asset);
        }

        void OnAssetPut(GameObject asset)
        {
            model.OnAssetPut(asset);
            var result = model.TryGetSelectWallBounds(out var bounds);
            Debug.Log($"OnAssetPut : {result} / {bounds}");

            if (!result)
            {
                return;
            }

            wallUI.PutAdObject(asset);
            wallUI.SetWallAreaBounds(bounds);
        }

        void OnAssetSelectCancel()
        {
            model.OnAssetSelectCancel();
        }

        void SetAssetListCallback()
        {
            adAssetListUI.OnPutAsset += OnAssetPut;
            adAssetListUI.OnSelectAsset += OnAssetSelect;
            adAssetListUI.OnSelectAssetCancel += OnAssetSelectCancel;
        }

        void ResetAssetListCallback()
        {
            adAssetListUI.OnSelectAsset -= OnAssetSelect;
            adAssetListUI.OnPutAsset -= OnAssetPut;
            adAssetListUI.OnSelectAssetCancel -= OnAssetSelectCancel;
            wallUI.SetAdObject(null);
        }
    }

}
