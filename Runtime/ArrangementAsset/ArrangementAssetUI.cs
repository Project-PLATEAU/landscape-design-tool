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

        // アセットカラーエディター
        private readonly AssetColorEditorUI assetColorEditorUI;

        public ArrangementAssetUI(
            VisualElement element,
            ArrangementAsset arrangementAssetInstance,
            CreateMode createModeInstance,
            EditMode editModeInstance,
            AdvertisementRenderer advertisementRendererInstance,
            LandscapeCamera landscapeCamera,
            AssetsSubscribeSaveSystem subscribeSaveSystem,
            AssetColorEditorUI assetColorEditorUIInstance)
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
                if (editTarget == target)
                {
                    // 編集中アセットを消す
                    DeleteEditingAsset();
                    return;
                }
                GameObject.Destroy(target);
            });

            assetColorEditorUI = assetColorEditorUIInstance;

            // プロジェクトからの通知イベント
            subscribeSaveSystem.SaveLoadHandler.OnDeleteAssets.AddListener(OnDeleteAssets);
            subscribeSaveSystem.SaveLoadHandler.OnChangeEditableState.AddListener(OnChangeEditableState);

            RegisterEditButtonAction();

            var lib = UIElement.Q<VisualElement>("AssetLibraryGroup");
            lib.style.display = DisplayStyle.Flex;

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
            var libButton = editPanel.Q<RadioButton>("Toggle_Library");
            libButton.RegisterCallback<ClickEvent>(evt =>
            {
                var lib = UIElement.Q<VisualElement>("AssetLibraryGroup");
                if (lib != null)
                {
                    lib.style.display = DisplayStyle.Flex;
                }
                assetColorEditorUI.Hide();
            });
            var colorButton = editPanel.Q<RadioButton>("Toggle_ColorEdit");
            colorButton.RegisterCallback<ClickEvent>(evt =>
            {
                var lib = UIElement.Q<VisualElement>("AssetLibraryGroup");
                if (lib != null)
                {
                    lib.style.display = DisplayStyle.None;
                }
                ShowAssetColorEditor();
            });
            libButton.value = true;
            colorButton.value = false;

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

            var deleteButton = editPanel.Q<Button>("IconDelete");
            deleteButton.clicked += DeleteEditingAsset;
        }

        private void DeleteEditingAsset()
        {
            var id = editTarget.GetInstanceID();

            // リストから削除
            arrangementAssetListUI.RemoveAsset(id);

            // 建物UIを非表示
            arrangementBuildingEditorUI.ShowPanel(false);

            editMode.DeleteAsset(editTarget);
            editTarget = null;
            editPanel.style.display = DisplayStyle.None;
            ResetEditButton(true);

            // プロジェクトへ追加
            ProjectSaveDataManager.Delete(ProjectSaveDataType.Asset, id.ToString());
        }

        public void Update()
        {
            arrangementAssetListUI.UpdateItemLabels();
        }

        /// <summary>
        /// 編集モードから離れた時に移動モードに戻す
        /// </summary>
        public void ResetEditButton(bool end)
        {
            var context_Edit = UIElement.Q<GroupBox>("Context_Edit");
            var radioButtons = context_Edit.Query<RadioButton>().ToList();
            foreach (var radioButton in radioButtons)
            {
                radioButton.value = false;
            }
            var moveButton = editPanel.Q<RadioButton>("MoveButton");
            moveButton.value = true;

            if (end)
            {
                var libToggle = UIElement.Q<RadioButton>("Toggle_Library");
                libToggle.value = true;
                var colorToggle = UIElement.Q<RadioButton>("Toggle_ColorEdit");
                colorToggle.value = false;

                var lib = UIElement.Q<VisualElement>("AssetLibraryGroup");
                if (lib != null)
                {
                    lib.style.display = DisplayStyle.Flex;
                }
                assetColorEditorUI.Hide();
            }
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
                var colorToggle = UIElement.Q<RadioButton>("Toggle_ColorEdit");
                if (colorToggle.value)
                {
                    colorToggle.value = false;
                    var libToggle = UIElement.Q<RadioButton>("Toggle_Library");
                    libToggle.value = true;
                    var lib = UIElement.Q<VisualElement>("AssetLibraryGroup");
                    if (lib != null)
                    {
                        lib.style.display = DisplayStyle.Flex;
                    }
                    assetColorEditorUI.Hide();
                }
                editPanel.style.display = DisplayStyle.None;
                arrangementBuildingEditorUI.ShowPanel(false);
            }
        }

        public void SetEditTarget(GameObject target)
        {
            editTarget = target;
            assetColorEditorUI.SetEditTarget(editTarget);
            ResetEditButton(false);
        }

        /// <summary>
        /// ファイルボタンの表示・非表示を管理
        /// </summary>
        private void TryDisplayFileButton()
        {
            var fileContainer = editPanel.Q<VisualElement>("FileContainer");
            var movieContainer = editPanel.Q<VisualElement>("MovieContainer");

            // 広告のアセットのみファイルボタンを表示
            // var isShow = editTarget != null && editTarget.GetComponent<PlateauSandboxAdvertisement>() != null;
            var isShow = editTarget != null
                && (editTarget.TryGetComponent<PlateauSandboxAdvertisement>(out var _)
                    || editTarget.GetComponentInChildren<PlateauSandboxAdvertisementScaled>() != null);
            fileContainer.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            movieContainer.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowAssetColorEditor()
        {
            editMode.ClearHandleObject();
            assetColorEditorUI.Show();
        }

        private bool IsAdAsset(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            var ad = obj.GetComponent<PlateauSandboxAdvertisement>();
            var adScaled = obj.GetComponentInChildren<PlateauSandboxAdvertisementScaled>();

            return ad != null || adScaled != null;
        }

        private void OnDeleteAssets(List<GameObject> deleteAssets)
        {
            foreach (var target in deleteAssets)
            {
                if (editTarget == target)
                {
                    DeleteEditingAsset();
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
