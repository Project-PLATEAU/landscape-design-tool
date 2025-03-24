using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Landscape2.Runtime.UiCommon;
using PlateauToolkit.Sandbox.Runtime;

namespace Landscape2.Runtime
{
    public class ArrangementAssetUI
    {
        private VisualElement UIElement;
        private ArrangementAsset arrangementAsset;
        private CreateMode createMode;
        private EditMode editMode;
        private GameObject editTarget;
        private VisualElement editPanel;
        private VisualElement assetListScrollView;

        // 一括配置
        private BulkArrangementAssetUI bulkArrangementAssetUI;

        // 広告
        private AdvertisementRenderer advertisementRenderer;

        // 建物UI
        private ArrangementBuildingEditorUI arrangementBuildingEditorUI;

        // アセット一覧
        private ArrangementAssetListUI arrangementAssetListUI;

        public ArrangementAssetUI(
            VisualElement element,
            ArrangementAsset arrangementAssetInstance,
            CreateMode createModeInstance,
            EditMode editModeInstance,
            AdvertisementRenderer advertisementRendererInstance,
            LandscapeCamera landscapeCamera,
            AssetsSubscribeSaveSystem subscribeSaveSystem)
        {
            UIElement = element;
            arrangementAsset = arrangementAssetInstance;
            createMode = createModeInstance;
            editMode = editModeInstance;
            advertisementRenderer = advertisementRendererInstance;
            editPanel = UIElement.Q<VisualElement>("EditPanel");
            bulkArrangementAssetUI = new BulkArrangementAssetUI(UIElement);
            assetListScrollView = UIElement.Q<ScrollView>("AssetListScrollView");
            arrangementBuildingEditorUI = new ArrangementBuildingEditorUI(element);
            arrangementAssetListUI = new ArrangementAssetListUI(element, landscapeCamera);
            arrangementAssetListUI.OnDeleteAsset.AddListener((target) =>
            {
                if (editTarget == null)
                {
                    // 編集中でなければそのまま消す
                    GameObject.Destroy(target);
                    return;
                }
                DeleteAsset();
            });
            
            // プロジェクトからの通知イベント
            subscribeSaveSystem.SaveLoadHandler.OnDeleteAssets.AddListener(OnDeleteAssets);
            subscribeSaveSystem.SaveLoadHandler.OnChangeEditableState.AddListener(OnChangeEditableState);
      
            RegisterEditButtonAction();

            // デフォルトでは非表示
            editPanel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// 編集ボタンの各機能を登録する
        /// </summary>
        private void RegisterEditButtonAction()
        {
            var moveButton = editPanel.Q<RadioButton>("MoveButton");
            moveButton.RegisterCallback<ClickEvent>(evt =>
            {
                editMode.CreateRuntimeHandle(editTarget, TransformType.Position);
            });
            var rotateButton = editPanel.Q<RadioButton>("RotateButton");
            rotateButton.RegisterCallback<ClickEvent>(evt =>
            {
                editMode.CreateRuntimeHandle(editTarget, TransformType.Rotation);
            });
            var scaleButton = editPanel.Q<RadioButton>("ScaleButton");
            scaleButton.RegisterCallback<ClickEvent>(evt =>
            {
                editMode.CreateRuntimeHandle(editTarget, TransformType.Scale);
            });

            // 画像読み込み
            var fileButton = editPanel.Q<Button>("FileButton");
            fileButton.clicked += () =>
            {
                var filePath = advertisementRenderer.SelectFile(true);
                if (!string.IsNullOrEmpty(filePath))
                {
                    advertisementRenderer.Render(editTarget, filePath);
                }
            };
            var fileContainer = editPanel.Q<VisualElement>("FileContainer");
            fileContainer.style.display = DisplayStyle.None; // デフォルトでは非表示

            // 動画読み込み
            var movieButton = editPanel.Q<Button>("MovieButton");
            movieButton.clicked += () =>
            {
                var filePath = advertisementRenderer.SelectFile(false);
                if (!string.IsNullOrEmpty(filePath))
                {
                    advertisementRenderer.Render(editTarget, filePath);
                }
            };
            var movieContainer = editPanel.Q<VisualElement>("MovieContainer");
            movieContainer.style.display = DisplayStyle.None; // デフォルトでは非表示

            var deleteButton = editPanel.Q<Button>("ContextButton");
            deleteButton.clicked += DeleteAsset;
        }

        private void DeleteAsset()
        {
            editMode.DeleteAsset(editTarget);
            editPanel.style.display = DisplayStyle.None;
            ResetEditButton();

            // 建物UIを非表示
            arrangementBuildingEditorUI.ShowPanel(false);

            // リストから削除
            arrangementAssetListUI.RemoveAsset(editTarget.GetInstanceID());
            
            // プロジェクトへ追加
            ProjectSaveDataManager.Delete(ProjectSaveDataType.Asset, editTarget.GetInstanceID().ToString());
        }

        /// <summary>
        /// 編集モードから離れた時に移動モードに戻す
        /// </summary>
        public void ResetEditButton()
        {
            var context_Edit = UIElement.Q<GroupBox>("Context_Edit");
            var radioButtons = context_Edit.Query<RadioButton>().ToList();
            foreach (var radioButton in radioButtons)
            {
                radioButton.value = false;
            }
            var moveButton = editPanel.Q<RadioButton>("MoveButton");
            moveButton.value = true;
        }


        /// <summary>
        /// アセットのカテゴリーの切り替えの登録
        /// </summary>
        public void RegisterCategoryPanelAction(string buttonName, IList<GameObject> assetsList, IList<Texture2D> assetsPicture)
        {
            var assetCategory = UIElement.Q<RadioButton>(buttonName);
            assetCategory.RegisterCallback<ClickEvent>(evt =>
            {
                CreateButton(assetsList, assetsPicture);
            });
        }

        /// <summary>
        /// アセットのボタンの作製
        /// </summary>
        public void CreateButton(IList<GameObject> assetList, IList<Texture2D> assetPictureList)
        {
            assetListScrollView.style.display = DisplayStyle.Flex;
            assetListScrollView.Clear();

            VisualElement flexContainer = new VisualElement();
            // ussに移行すべき
            flexContainer.style.flexDirection = FlexDirection.Row;
            flexContainer.style.flexWrap = Wrap.Wrap;
            flexContainer.style.justifyContent = Justify.SpaceBetween;
            flexContainer.style.justifyContent = Justify.FlexStart;


            foreach (GameObject asset in assetList)
            {
                var assetPicture = assetPictureList[0];
                // 写真を見つける
                foreach (var picture in assetPictureList)
                {
                    Debug.Log(picture.name);
                    if (picture.name == asset.name)
                    {
                        assetPicture = picture;
                        break;
                    }
                }
                // ボタンの生成
                Button newButton = new Button()
                {
                    name = "Thumbnail_Asset" // ussにスタイルが指定してある
                };

                newButton.style.width = Length.Percent(30f);

                newButton.style.backgroundImage = new StyleBackground(assetPicture);
                newButton.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
                newButton.style.backgroundColor = Color.clear;

                newButton.AddToClassList("AssetButton");
                newButton.clicked += () =>
                {
                    arrangementAsset.SetMode(ArrangeModeName.Create);
                    createMode.SetAsset(asset.name, assetList);
                };
                flexContainer.Add(newButton);
            }
            assetListScrollView.Add(flexContainer);

            bulkArrangementAssetUI.Show(false);
        }

        /// <summary>
        /// インポートボタンアクションの登録
        /// </summary>
        public void RegisterImportButtonAction()
        {
            var importButton = UIElement.Q<RadioButton>("AssetCategory_Import");
            importButton.RegisterCallback<ClickEvent>((evt) =>
            {
                // アセットリストは非表示
                assetListScrollView.style.display = DisplayStyle.None;

                // 一括配置用のUIを表示
                bulkArrangementAssetUI.Show(true);
            });
        }

        /// <summary>
        /// 編集パネルの表示・非表示を管理
        /// </summary>
        public void DisplayEditPanel(bool isDisplay)
        {
            if (isDisplay)
            {
                editPanel.style.display = DisplayStyle.Flex;
                arrangementBuildingEditorUI.TryShowPanel(editTarget);
                TryDisplayFileButton();
            }
            else
            {
                editPanel.style.display = DisplayStyle.None;
                arrangementBuildingEditorUI.ShowPanel(false);
            }
        }

        public void SetEditTarget(GameObject target)
        {
            editTarget = target;
            ResetEditButton();
        }

        /// <summary>
        /// ファイルボタンの表示・非表示を管理
        /// </summary>
        private void TryDisplayFileButton()
        {
            var fileContainer = editPanel.Q<VisualElement>("FileContainer");
            var movieContainer = editPanel.Q<VisualElement>("MovieContainer");

            // 広告のアセットのみファイルボタンを表示
            var isShow = editTarget != null && editTarget.GetComponent<PlateauSandboxAdvertisement>() != null;
            fileContainer.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            movieContainer.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private void OnDeleteAssets(List<GameObject> deleteAssets)
        {
            foreach (var target in deleteAssets)
            {
                if (editTarget == target)
                {
                    DeleteAsset();
                }
                ArrangementAssetListUI.OnCancelAsset.Invoke(target);
            }
        }
        
        private void OnChangeEditableState(
            List<GameObject> editableAssets,
            List<GameObject> notEditableAssets)
        {
            foreach (var asset in editableAssets)
            {
                arrangementAssetListUI.SetEditable(true, asset.GetInstanceID());
            }
            
            foreach (var asset in notEditableAssets)
            {
                arrangementAssetListUI.SetEditable(false, asset.GetInstanceID());
            }
        }
    }
}
