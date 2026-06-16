using Landscape2.Runtime.DisplayInstallationRestrictionZones;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

namespace Landscape2.Runtime.AdRegulation
{
    public class RoadAdSettingUI : ISubComponent
    {
        //(string, string) begigMsg = ("規制範囲をしたい道路をドラッグしてください", "住所を入力すると該当エリアに移動できます" );
        //(string, string) rangeMsg = ("道路協会の広告物設置可能範囲を設定してください", "設置ができない広告物サムネイルはグレーアウトします。ドラッグで設置も可能です。" );
        //(string, string) beginMsg = ("広告物を配置後、右側の選択ボタンを押してください", "" );
        (string, string) dragMsg = ("確認したい範囲をドラッグして大きめに選択してください", "" );
        (string, string) rangeMsg = ("道路境界からの広告物設置規制距離を設定してください", "規制範囲上の広告物は色が変化します");


        /// <summary>
        /// 機能の有効化・無効化を行うフラグ
        /// 根本的な部分を制御する
        /// これがfalseの場合, OnDisable(), OnEnable()以外のpublicメソッドは何もしないとする。
        /// </summary>
        bool isEnable;

        // View
        private IRoadAdSettingView view;

        //private IPanel_RoadEdgePanelView panel_RoadEdgePanelView;
        private IDialog_TutorialInfoView dialog_TutorialInfoView;

        // 
        FromContainerUI formContainerUI;

        // Model
        private IDisplayInstallationRestrictionZonesModule model;

        // ModelのUnityフレームワーク風の更新処理用　コンポーネントが存在しない場合は処理しない。
        private ISubComponent modelComponent;
        private ISubComponent viewComponent;

        public RoadAdSettingUI(IRoadAdSettingView view,
            IDialog_TutorialInfoView dialog_TutorialInfoView,
            IDisplayInstallationRestrictionZonesModule model)
        {
            if (view == null)
            {
                Debug.LogError("Root VisualElement is null.");
                return;
            }

            if (model == null)
            {
                Debug.LogError("Module is null.");
                return;
            }

            this.view = view;

            //this.panel_RoadEdgePanelView = panel_RoadEdgePanelView;
            this.formContainerUI = new FromContainerUI(view.FormContainer);

            this.dialog_TutorialInfoView = dialog_TutorialInfoView;

            this.model = model;


            // subcomponetを利用している場合は取得する
            if (model is ISubComponent mcomp)
            {
                modelComponent = mcomp;
            }

            if (view is ISubComponent vcomp)
            {
                viewComponent = vcomp;
            }


        }

        /// <summary>
        /// 機能の有効化
        /// </summary>
        /// <param name="isActive"></param>
        public void Activate(bool isActive)
        {
            if (!isEnable)
                return;

            model?.Activate(isActive);
            view?.RequestActivate(isActive);

        }


        public void OnDisable()
        {
            isEnable = false;
            Terminate();
        }

        public void OnEnable()
        {
            Initialize();
            isEnable = true;
        }

        public void Start()
        {
            if (!isEnable)
                return;

            modelComponent?.Start();
            viewComponent?.Start();
        }

        public void Update(float deltaTime)
        {
            if (!isEnable)
                return;

            modelComponent?.Update(deltaTime);
            viewComponent?.Update(deltaTime);
        }

        public void LateUpdate(float deltaTime)
        {
            if (!isEnable)
                return;

            modelComponent?.LateUpdate(deltaTime);
            viewComponent?.LateUpdate(deltaTime);
        }

        private bool Initialize()
        {
            modelComponent?.OnEnable();
            viewComponent?.OnEnable();

            view.OnActivated += (bool isActive) =>
            {
                // 機能の有効化・無効化
                model.Activate(isActive);


                if (isActive)
                {
                    // 有効化時の処理
                    //panel_RoadEdgePanelView.Reset();
                    //panel_RoadEdgePanelView.Hide();
                    dialog_TutorialInfoView.Set(dragMsg.Item1, dragMsg.Item2);
                    dialog_TutorialInfoView.CurrentOwner = this;
                }
                else
                {
                    //panel_RoadEdgePanelView.Hide();
                    if (dialog_TutorialInfoView.CurrentOwner == this)
                    {
                        dialog_TutorialInfoView.Set("", "");
                        dialog_TutorialInfoView.CurrentOwner = null;
                    }
                }
            };

            view.OnChangedSelectingMode += (bool isSelecting) =>
            {
                // 選択モードの変更
                if (isSelecting)
                {
                    // 選択モードに入った時の処理
                    //panel_RoadEdgePanelView.Reset();
                    //panel_RoadEdgePanelView.Hide();
                    dialog_TutorialInfoView.Set(dragMsg.Item1, dragMsg.Item2);
                }
                else
                {
                    if (dialog_TutorialInfoView.CurrentOwner == this)
                    {
                        dialog_TutorialInfoView.Set(rangeMsg.Item1, rangeMsg.Item2);
                    }
                }
            };

            view.OnTransSelected += (List<MeshCollider> meshColliders) =>
            {
                // 道路が選択された時の処理

                // gameObject を抽出
                List<GameObject> gameObjects = meshColliders
                    .Select(mc => mc.gameObject)
                    .ToList();
                var _ = model.GenerateAreas(gameObjects);

                if (meshColliders.Count > 0)
                {
                    //panel_RoadEdgePanelView.Show();
                    dialog_TutorialInfoView.Set(rangeMsg.Item1, rangeMsg.Item2);
                }
            };

            formContainerUI.OnDistanceChanged += (float distance) =>
            {
                // 制限距離が変更された時の処理
                //Debug.Log($"Restriction distance changed: {distance}");
                model.SetRestrictionDistance(distance);
            };

            // ↓　別のuxmlに移動された
            //view.OnRestrictionDistanceChanged += (float distance) =>
            //{
            //    // 制限距離が変更された時の処理
            //    //Debug.Log($"Restriction distance changed: {distance}");
            //    model.SetRestrictionDistance(distance);
            //};
            return true;
        }

        private void Terminate()
        {
            viewComponent?.OnDisable();
            modelComponent?.OnDisable();
        }


    }

    public class FromContainerUI
    {
        public event System.Action<float> OnDistanceChanged;

        IFormContainerView view;
        public FromContainerUI(IFormContainerView view)
        {
            this.view = view;
            if (view == null)
            {
                Debug.LogError("FormContainerView is null.");
                return;
            }
            view.OnDistanceChanged += (float distance) =>
            {
                OnDistanceChanged?.Invoke(distance);
            };
        }
    }
}
