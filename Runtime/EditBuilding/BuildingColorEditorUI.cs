using Landscape2.Runtime.UiCommon;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.BuildingEditor
{
    /// <summary>
    /// 建物編集の色彩を編集するUI
    /// </summary>
    public class BuildingColorEditorUI : ISubComponent
    {
        VisualElement uiRoot;
        ColorEditorUI colorEditorUI;
        BuildingColorEditor buildingColorEditor;

        private readonly VisualTreeAsset colorEditor; // 色彩編集パネルのUXMLのテンプレート
        private VisualElement colorEditorClone; // 色彩編集パネルのUXMLのクローン

        // 地物型選択リスト
        private readonly DropdownField buildingField;

        // 色彩編集パネル表示ボタン
        private readonly Button colorButton;

        // Smoothnessスライダー
        private readonly Slider smoothnessSlider;

        // 変更ボタン
        private readonly Button okButton;

        // キャンセルボタン
        private readonly Button cancelButton;

        // リセットボタン
        private readonly Button resetButton;

        // 地物型選択リスト名前
        private const string UIBuildingField = "BuildingField";

        // 色彩編集パネル表示ボタン
        private const string UIColorButton = "ColorButton";

        // Smoothnessスライダー名前
        private const string UISmoothnessSlider = "SmoothnessSlider";

        // 変更ボタン名前
        private const string UIOKButton = "OKButton";

        // キャンセルボタン名前
        private const string UICancelButton = "CancelButton";

        // リセットボタン名前
        private const string UIResetButton = "ResetButton";

        // 地物型選択リストの文字列を管理する配列
        private string[] uiBuildingFields =
        {
            "要素全体", "壁", "屋根/屋上"
        };

        // 色彩編集パネル表示ボタンの色
        private Color colorButtonColor;

        public BuildingColorEditorUI(BuildingColorEditor buildingColorEditor, EditBuilding editBuilding, VisualElement uiRoot)
        {
            this.uiRoot = uiRoot;
            this.buildingColorEditor = buildingColorEditor;
            
            // 建物編集画面の建物選択イベントに登録
            editBuilding.OnBuildingSelected += SetFieldList;

            // 色彩編集パネルを生成
            colorEditor = Resources.Load<VisualTreeAsset>("UIColorEditor");
            colorEditorClone = colorEditor.CloneTree();
            uiRoot.Q<VisualElement>("Panel_MaterialEditor").Add(colorEditorClone);
            colorEditorUI = new ColorEditorUI(colorEditorClone, BuildingColorEditor.InitialColor);
            colorEditorClone.style.display = DisplayStyle.None;

            // 色彩編集パネルのイベント関数を登録
            colorEditorUI.OnColorEdited += UpdateColor;
            colorEditorUI.OnCloseButtonClicked += () =>
            {
                colorEditorClone.style.display = DisplayStyle.None;
            };

            buildingField = uiRoot.Q<DropdownField>(UIBuildingField);
            colorButton = uiRoot.Q<Button>(UIColorButton);
            smoothnessSlider = uiRoot.Q<Slider>(UISmoothnessSlider);
            okButton = uiRoot.Q<Button>(UIOKButton);
            cancelButton = uiRoot.Q<Button>(UICancelButton);
            resetButton = uiRoot.Q<Button>(UIResetButton);

            // UIの初期値の設定
            buildingField.choices.Clear();
            colorButton.style.backgroundColor = Color.white;

            // 地物型選択リストの値が変更されたとき
            buildingField.RegisterValueChangedCallback(evt =>
            {
                // 編集するマテリアルを切り替える
                buildingColorEditor.ChangeEditingMaterial(Array.IndexOf(uiBuildingFields, evt.newValue));
                // UIを反映
                UpdateEditorUI();
            });

            // 色彩編集パネル表示ボタンが押されたとき
            colorButton.clicked += () =>
            {
                // 色彩編集パネルを表示
                colorEditorClone.style.display = DisplayStyle.Flex;
            };

            // 変更ボタンが押されたとき
            okButton.clicked += () =>
            {
                // 色を変更する
                buildingColorEditor.EditMaterialColor(colorButtonColor, smoothnessSlider.value, (lowestLayerProjectName) =>
                {
                    // 警告を表示
                    SnackBarUI.Show($"{lowestLayerProjectName} の色彩が優先されています。", uiRoot);
                });
            };

            // キャンセルボタンが押されたとき
            cancelButton.clicked += () =>
            {
                // 編集中のマテリアルをリセット
                buildingColorEditor.ChangeEditingMaterial(-1);
                // UIをリセット
                colorEditorUI.ResetColorEditorUI(Color.white);
                ResetBuildingEditorUI();
                // 色彩編集パネルを非表示
                colorEditorClone.style.display = DisplayStyle.None;
            };

            // リセットボタンが押されたとき
            resetButton.clicked += () =>
            {
                // 建物の色をリセット
                buildingColorEditor.EditMaterialColor(BuildingColorEditor.InitialColor, BuildingColorEditor.InitialSmoothness, (lowestLayerProjectName) =>
                {
                    // 警告を表示
                    SnackBarUI.Show($"{lowestLayerProjectName} の色彩が優先されています。", uiRoot);
                });
                
                // UIをリセット
                colorEditorUI.ResetColorEditorUI(BuildingColorEditor.InitialColor);
                ResetBuildingEditorUI();
            };

            // 建物編集画面が閉じられたとき
            uiRoot.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (uiRoot.style.display == DisplayStyle.None)
                {
                    OnDisable();
                }
            });
        }

        // 建物選択時のイベント
        private void SetFieldList(GameObject targetObj, bool canEdit)
        {
            // ドロップダウンリストの要素数を取得
            int count = buildingColorEditor.SetMaterialList(targetObj);

            buildingField.choices.Clear();
            // ドロップダウンリストの要素数を更新
            for (int i = 0; i < count; i++)
            {
                buildingField.choices.Add(uiBuildingFields[i]);
            }

            // UIの初期値を設定
            ResetBuildingEditorUI();

            // UIを反映
            UpdateEditorUI();
            
            Show(canEdit);
        }

        private void Show(bool isVisible)
        {
            uiRoot.Q<VisualElement>("Panel_MaterialEditor").style.display = isVisible ?
                DisplayStyle.Flex : DisplayStyle.None;
        }

        // 色を更新
        private void UpdateColor(Color color)
        {
            colorButtonColor = color;
            colorButton.style.backgroundColor = color;
        }

        // 選択した要素の色をUIに反映
        private void UpdateEditorUI()
        {
            var color = buildingColorEditor.GetMaterialColor();
            // 色彩編集パネルのRGB値を反映
            colorEditorUI.ResetColorEditorUI(color);
            // Smoothnessスライダーの値を反映
            smoothnessSlider.value = buildingColorEditor.GetMaterialSmoothness();
        }

        // 建物編集UIの初期化
        private void ResetBuildingEditorUI()
        {
            // ドロップダウンリストの初期値を設定
            buildingField.value = uiBuildingFields[0];
            buildingColorEditor.ChangeEditingMaterial(0);
        }

        public void Start()
        {
        }
        public void Update(float deltaTime)
        {
        }
        public void OnEnable()
        {
        }
        public void OnDisable()
        {
            ResetBuildingEditorUI();
            // 色彩編集パネルを非表示
            colorEditorClone.style.display = DisplayStyle.None;
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
