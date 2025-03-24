using Landscape2.Runtime.UiCommon;
using UnityEngine;
using UnityEngine.UIElements;
using static Landscape2.Runtime.LandscapePlanLoader.PlanningUI;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画作成画面のメニューパネルUIのプレゼンタークラス
    /// </summary>
    public class Panel_AreaPlanningRegister : Panel_AreaPlanningEditBaseUI
    {
        private readonly AreaPlanningRegister areaPlanningRegister;
        private bool isDragging = false;
        private const float wallMaxHeight = 300f;
        private float limitHeight;

        public Panel_AreaPlanningRegister(VisualElement planning, PlanningUI planningUI) : base(planning, planningUI)
        {
            areaPlanningRegister = new AreaPlanningRegister(displayPinLine);

            // 親のパネルを取得
            panel_AreaPlanningEdit = planning.Q<VisualElement>("Panel_AreaPlanningRegister");

            // 頂点データ作成パネルを生成
            panel_PointEditor = new UIDocumentFactory().CreateWithUxmlName("Panel_AreaRegistering");
            GameObject.Find("Panel_AreaRegistering").GetComponent<UIDocument>().sortingOrder = -1;
            panel_PointEditor.style.display = DisplayStyle.None;

            panel_PointEditor.RegisterCallback<MouseUpEvent>(ev => OnPointEditorClicked());
            panel_PointEditor.RegisterCallback<MouseDownEvent>(ev => isDragging = false);
            panel_PointEditor.RegisterCallback<MouseMoveEvent>(ev => isDragging = true);

            base.InitializeUI();
            base.RegisterCommonCallbacks();

            limitHeight = 15.0f; // 初期値15mで設定
            areaPlanningHeight.value = limitHeight.ToString();
        }

        /// <summary>
        /// 景観計画区域作成パネルが開かれたときの処理
        /// </summary>
        protected override void OnDisplayPanel(PlanningPanelStatus status)
        {
            if (status == PlanningPanelStatus.RegisterAreaMain)
            {
                panel_PointEditor.style.display = DisplayStyle.Flex;
                okButton.visible = false;

                var color = planningUI.PopColorStack();
                pasteButton.SetEnabled(color != null);  // pasteボタンは色を取ってきて存在していたら最初から有効

                base.DisplaySnackbar("地形をクリックして領域を作成してください");
            }
            else if(panel_PointEditor.style.display == DisplayStyle.Flex)
            {
                panel_PointEditor.style.display = DisplayStyle.None;
                areaPlanningRegister.ClearVertexEdit();
                base.HideSnackbar();
            }
        }

        /// <summary>
        /// 制限高さの数値を増やすボタンの処理
        /// </summary>
        protected override void IncrementHeight()
        {
            // 入力値が数値で最大高さ以下の値の場合のみデータを更新
            if (float.TryParse(areaPlanningHeight.value, out float value) && value <= wallMaxHeight)
            {
                limitHeight++;
                areaPlanningHeight.value = limitHeight.ToString(); //テキストフィールドに反映
            }
        }

        /// <summary>
        /// 制限高さの数値を減らすボタンの処理
        /// </summary>
        protected override void DecrementHeight()
        {
            // 入力値が数値で1以上の値の場合のみデータを更新
            if (float.TryParse(areaPlanningHeight.value, out float value) && value >= 1.0f)
            {
                limitHeight--;
                areaPlanningHeight.value = limitHeight.ToString(); //テキストフィールドに反映
            }
        }

        /// <summary>
        /// 制限高さの数値が直接入力されたときの処理
        /// </summary>
        /// <param name="evt"> 変更内容に関するデータ </param>
        protected override void InputHeight(ChangeEvent<string> evt)
        {
            // 入力値が数値で最大高さ以下かつ0以上の値の場合のみデータを更新
            if (float.TryParse(evt.newValue, out float value) && value <= wallMaxHeight && value >= 1.0f)
            {
                limitHeight = value;
            }
            else
            {
                // 空欄以外の文字入力があった場合は元の値に戻す
                if (evt.newValue != "") areaPlanningHeight.value = evt.previousValue;
            }
        }

        /// <summary>
        /// エリアの色彩編集を行う処理
        /// </summary>
        protected override void EditColor()
        {
            if (isColorEditing)
            {
                base.EditColor();

                // 色彩の変更を反映
                ColorEditorUI colorEditorUI = new ColorEditorUI(colorEditorClone, areaPlanningColor.resolvedStyle.backgroundColor);
                colorEditorUI.OnColorEdited += (newColor) =>
                {
                    areaPlanningColor.style.backgroundColor = newColor;
                };
                colorEditorUI.OnCloseButtonClicked += () =>
                {
                    isColorEditing = false;
                    EditColor();
                };
            }
            else
            {
                // 色彩変更画面を閉じる
                if (colorEditorClone != null) colorEditorClone.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// キャンセルボタンをクリックしたときの処理
        /// </summary>
        protected override void OnCancelButtonClicked()
        {
            areaPlanningRegister.ClearVertexEdit();
        }

        /// <summary>
        /// OKボタンをクリックしたときの処理
        /// </summary>
        protected override void OnOKButtonClicked()
        {
            if (areaPlanningRegister.IsIntersected())
            {
                base.DisplaySnackbar("頂点が交差したエリアは作成できません");
                return;
            }

            // 新規景観区画データを作成
            areaPlanningRegister.CreateAreaData(
                areaPlanningName.value,
                float.Parse(areaPlanningHeight.value),
                wallMaxHeight,
                areaPlanningColor.resolvedStyle.backgroundColor);

            areaPlanningRegister.ClearVertexEdit();
            planningUI.InvokeOnChangeConfirmed();

        }

        /// <summary>
        /// 頂点作成パネルをクリックしたときの処理
        /// </summary>
        private void OnPointEditorClicked()
        {
            if (!isDragging)
            {
                areaPlanningRegister.AddVertexIfClicked();
            }

            if (areaPlanningRegister.IsClosed())
            {
                okButton.visible = true;
            }
            isDragging = false;
        }

        protected override void OnCopyButtonClicked()
        {
            var color = areaPlanningColor.resolvedStyle.backgroundColor;
            planningUI.PushColorStack(color);

            pasteButton.SetEnabled(true);
        }

        protected override void OnPasteButtonClicked()
        {
            var newColor = planningUI.PopColorStack();
            Debug.Log($"newColor: {newColor}");
            if (newColor != null)
            {
                areaPlanningColor.style.backgroundColor = new StyleColor(newColor.Value);
            }
        }

    }
}
