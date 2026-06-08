using Landscape2.Runtime.DisplayInstallationRestrictionZones;
using PLATEAU.CityInfo;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace Landscape2.Runtime.AdRegulation
{
    public interface IRoadAdSettingView
    {
        // 機能を有効/無効化するリクエストを送る　有効化された場合は OnActivate イベントが呼ばれる
        public void RequestActivate(bool isActive);

        // 有効/無効化された
        public event System.Action<bool> OnActivated;

        // 範囲選択モードが有効化された
        public event System.Action<bool> OnChangedSelectingMode;

        // 道路が選択された時
        public event Action<List<MeshCollider>> OnTransSelected;

        public IFormContainerView FormContainer { get; }

    }

    public class RoadAdSettingView : ISubComponent, IRoadAdSettingView
    {
        private VisualElement root;
        private VisualElement SettingPanel { get => root.Q<VisualElement>("SettingPanel"); }

        private bool IsEnable { get; set; } = false;
        private bool IsSelectingMode { get => dragSelection.IsEnable; set => dragSelection.IsEnable = value; }

        // アセットリスト　todo 子要素でもないのでPresenter経由で操作、イベントの購読の方がいいかも
        private AdAssetListUI adAssetListUI1;

        private IFormContainerView formContainerView;
        IFormContainerView IRoadAdSettingView.FormContainer { get => formContainerView; }


        public RoadAdSettingView(VisualElement root, IAdRegulationMouseOperationView mouseOperationView, AdAssetListUI adAssetListUI, DynamicTile.INotifyUpdated dynamicTileUpdater)
        {
            this.root = root;

            // 広告物のサイズ設定は初期状態では非表示
            SettingPanel.style.display = DisplayStyle.None;

            if (root == null)
            {
                Debug.LogError("Root VisualElement is null.");
                return;
            }

            // 範囲選択機能を生成
            dragSelection = new DragSelectionView(root, mouseOperationView, "UI/SelectionAreaBox");

            this.adAssetListUI1 = adAssetListUI;

            // rootに属するこのクラスで扱うuxmlを取得
            uxmls = new RoadAdSettingUXMLs(root);

            formContainerView = new FormContainerView(uxmls.FormContainer, defaultV:2.0f);

            //adAssetListUI.OnSelectAsset += (asset) =>
            //{
            //    // アセットリストから選択された時に道路選択モードをキャンセルする
            //    if (IsSelectingMode == true)
            //    {
            //        UICallback.OnChangedSelectingMode(this, false); // 範囲選択モードを無効化
            //    }

            //};

            //adAssetListUI.OnPutAsset += (asset) =>
            //{
            //    // アセットリストからアセットを配置した後に道路が選択されていない場合は道路選択モードを有効にする？
            //    // デフォルトが無効なので勝手に変わるのは分かりずらいかも　元の状態に戻すのが自然？

            //    //if (IsSelectingMode == false)
            //    //{
            //    //    UICallback.OnChangedSelectingMode(this, true); // 範囲選択モードを有効化
            //    //}
            //};

            var instTranFilter = dynamicTileUpdater?.FindFromInstantiatedFileter<DynamicTile.TranMeshColliderGroupFilter>();
            var destroyTranFilter = dynamicTileUpdater?.FindFromBeforeUnloadFileter<DynamicTile.TranMeshColliderGroupFilter>();
            if (instTranFilter != null && destroyTranFilter != null)
            {
                instTranFilter.EvUpdated += (o) =>
                {
                    dragSelection.AddSelectable(o);
                };
                destroyTranFilter.EvUpdated += (o) =>
                {
                    dragSelection.RemoveSelectable(o);
                };
            }

        }

        public void OnEnable(bool isEnable)
        {
            if (isEnable)
            {
                root.style.display = DisplayStyle.Flex;
            }
            else
            {
                root.style.display = DisplayStyle.None;
            }

            IsEnable = isEnable;
            UICallback.OnActivate(this, isEnable);

        }

        private RoadAdSettingUXMLs uxmls;

        // 範囲選択機能
        private IDragSelectionView dragSelection;

        // IDisplayInstallationRestrictionZonesViewの明示的既述だとアクセッサも既述しないといけないので簡易既述
        public event Action<List<MeshCollider>> OnTransSelected;
        public event Action<bool> OnActivated;
        public event System.Action<bool> OnChangedSelectingMode;

        void ISubComponent.LateUpdate(float deltaTime)
        {
        }

        void ISubComponent.OnDisable()
        {
        }

        void ISubComponent.OnEnable()
        {
        }

        void ISubComponent.Start()
        {
            dragSelection.Start();
            dragSelection.OnSelectionChanged += (selected) =>
            {
                UICallback.OnSelectionChanged(this, selected);
            };

            // 選択ボタンが押された時の処理　
            uxmls.SelectButton.RegisterCallback<ClickEvent>(evt =>
            {
                UICallback.OnChangedSelectingMode(this, !IsSelectingMode);  // 範囲選択モードの切り替え
            });
        }
        private List<MeshCollider> CollectTranObjs()
        {
            var plateauOjbs = UnityEngine.Object.FindObjectsByType<PLATEAUCityObjectGroup>(FindObjectsSortMode.None);
            var trans = new List<MeshCollider>(plateauOjbs.Length);

            foreach (var item in plateauOjbs)
            {
                if (Common.CityObjectUtil.IsTran(item.gameObject))
                {
                    var col = item.GetComponent<MeshCollider>();
                    if (col == null)
                    {
                        continue;
                    }
                    trans.Add(col);
                }
            }

            return trans;
        }

        void ISubComponent.Update(float deltaTime)
        {
        }

        void IRoadAdSettingView.RequestActivate(bool isActive)
        {
            throw new NotImplementedException("新UIではこのフローは想定されていない。");

            //if (this.IsEnable == isActive)
            //{
            //    // 既に同じ状態なら何もしない
            //    return;
            //}

            //// 建物を選択できる状態か？を設定する
            //this.IsEnable = isActive;

        }

        /// <summary>
        /// UIイベント時に呼び出すコールバックをまとめたクラス
        /// UI経由のみで呼ばれるような処理を書く
        /// また、処理フローを分かりやすくするため
        /// 余計な機能は積みたくないのでフィールドを持たない静的クラスとして定義
        /// </summary>
        private static class UICallback
        {
            public static void OnActivate(RoadAdSettingView self, bool isActive)
            {
                ChangeMode(self, isActive);

                if (isActive)
                {
                    self.adAssetListUI1.HideAndCancel(); // アセットリストを表示する
                    self.uxmls.UpdateDisplayStatus();
                    //self.uxmls.SetSelectingMode(true);// 範囲が選択されていない状態なので範囲選択モードを有効化
                    //self.IsSelectingMode = true;
                }
                else
                {
                    // 機能が無効化された時、範囲選択の状態をリセットする
                    self.dragSelection.ResetSelectionStatus();
                    self.uxmls.ResetUIStatus(); // UIの初期値に戻す
                }

                self.OnActivated?.Invoke(isActive);
            }

            public static void OnChangedSelectingMode(RoadAdSettingView self, bool isSelectingMode)
            {
                // 道路を選択していない状態で選択モードを終了できないようにする？
                //if (isSelectingMode == false)
                //{
                //}

                // 範囲選択モードの有効化/無効化
                ChangeMode(self, isSelectingMode);
            }

            //public static void OnChangedRestrictionDistance(RoadAdSettingView self, float distance)
            //{
            //    // 制限距離が変更された時の処理
            //    //Debug.Log($"Restriction distance changed: {distance}");
            //    self.OnRestrictionDistanceChanged?.Invoke(distance);
            //}

            public static void OnSelectionChanged(RoadAdSettingView self, List<MeshCollider> meshColliders)
            {
                self.OnTransSelected?.Invoke(meshColliders);
                if (meshColliders.Count > 0)
                {
                    //self.uxmls.SetSelectingMode(false); // 道路が選択されたので、範囲選択モードを無効化
                    self.IsSelectingMode = false;
                    ChangeMode(self, false);
                }
            }

            private static void ChangeMode(RoadAdSettingView self, bool isSelectingMode)
            {
                if (isSelectingMode)
                {
                    // 機能が有効な時、カメラの移動を無効化する
                    CameraMoveByUserInput.CameraScriptOwner = self;
                    CameraMoveByUserInput.IsCameraMoveActive = false;

                    self.adAssetListUI1.CancelAssetPlacement(); // アセットの配置をキャンセルする
                    self.adAssetListUI1?.HideAndCancel(); // アセットリストを非表示にする
                    self.uxmls.SelectButton.text = "キャンセル";
                }
                else
                {
                    if (CameraMoveByUserInput.CameraScriptOwner == self)
                    {
                        CameraMoveByUserInput.IsCameraMoveActive = true;
                        CameraMoveByUserInput.CameraScriptOwner = null;
                    }

                    self.adAssetListUI1?.Show(); // アセットリストを表示する
                    self.uxmls.SelectButton.text = "選択";
                }

                self.IsSelectingMode = isSelectingMode;
                self.OnChangedSelectingMode?.Invoke(isSelectingMode); // 範囲選択モードの有効化/無効化イベントを呼び出す
            }

        }


        /// <summary>
        // rootに属するこのクラスで扱うuxmlをまとめたクラス
        /// </summary>
        private class RoadAdSettingUXMLs
        {
            public RoadAdSettingUXMLs(VisualElement root)
            {
                

                //EnableToggle = root.Q<Toggle>("EnableToggle");
                //EnableSelectingMode = root.Q<Toggle>("EnableSelectingRoadModeToggle");
                //SettingPanel = root.Q<VisualElement>("SettingPanel"); // GroupBox
                //DistanceField = root.Q<FloatField>("DistanceField");
                //UpdateDisplayStatus();
                //EnableSelectingMode.value = false; // 範囲選択モードを無効化

                Root = root;

                SelectButton = root.Q<Button>("OKButton");
                RoadEdgeSettingPanel = root.Q<VisualElement>("RoadEdgeDistanceFieldPanel");
                FormContainer = RoadEdgeSettingPanel.Q<VisualElement>("WideContainer");

            }

            public IFormContainerView FormContainerView { get; private set; }

            public VisualElement Root { get; private set; }

            public VisualElement RoadEdgeSettingPanel { get; private set; }
            public VisualElement FormContainer { get; private set; }
            public Button SelectButton { get; private set; }

            //public Toggle EnableToggle { get; private set; }
            //public Toggle EnableSelectingMode { get; private set; }
            //public VisualElement SettingPanel { get; private set; } // GroupBox
            //public FloatField DistanceField { get; private set; } // 制限距離の入力フィールド

            private struct DefaultUIValue
            {
                public DefaultUIValue(RoadAdSettingUXMLs panel)
                {
                    //EnableToggle = panel.EnableToggle.value;
                    //EnableSelectingMode = panel.EnableSelectingMode.value;
                    //DistanceField = panel.DistanceField.value;
                }

                public void Set(RoadAdSettingUXMLs panel)
                {
                    //panel.EnableToggle.value = EnableToggle;
                    //panel.EnableSelectingMode.value = EnableSelectingMode;
                    //panel.DistanceField.value = DistanceField;
                }

                //public bool EnableToggle { get; private set; }
                //public bool EnableSelectingMode { get; private set; }
                //public float DistanceField { get; private set; }
            }
            private DefaultUIValue defaultUIValue;

            public void ResetUIStatus()
            {
                defaultUIValue.Set(this);
                UpdateDisplayStatus();
            }

            public void UpdateDisplayStatus()
            {
                //SettingPanel.style.display = EnableToggle.value ? DisplayStyle.Flex : DisplayStyle.None;    // 設定パネルを表示状態を機能有効トグルと同期
            }
        }

    }
}
