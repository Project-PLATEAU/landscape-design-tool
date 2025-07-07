using UnityEngine;
using UnityEngine.UIElements;
using Landscape2.Runtime.UiCommon;
using static Landscape2.Runtime.LandscapePlanLoader.PlanningUI;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画のエリア情報編集用UIパネルのプレゼンタークラス
    /// </summary>
    public class Panel_AreaPlanningEdit : Panel_AreaPlanningEditBaseUI
    {
        private readonly AreaEditManager areaEditManager;
        private AreaPlanningEdit areaPlanningEdit;
        private Button heightResetButton;

        private PlanningPanelStatus currentStatus = PlanningPanelStatus.Default;

        public Panel_AreaPlanningEdit(VisualElement planning, PlanningUI planningUI) : base(planning, planningUI)
        {
            // 親のパネルを取得
            panel_AreaPlanningEdit = planning.Q<VisualElement>("Panel_AreaPlanningEdit");

            areaEditManager = new AreaEditManager();
            areaPlanningEdit = new AreaPlanningEdit(areaEditManager, displayPinLine);

            // リスト要素クリック時に編集対象を更新
            planningUI.OnFocusedAreaChanged += RefreshEditor;

            // 頂点データ編集用Panelを生成
            panel_PointEditor = new UIDocumentFactory().CreateWithUxmlName("Panel_AreaEditing");
            GameObject.Find("Panel_AreaEditing").GetComponent<UIDocument>().sortingOrder = -1;
            panel_PointEditor.RegisterCallback<MouseDownEvent>(ev => OnClickPanel(ev));
            panel_PointEditor.RegisterCallback<MouseMoveEvent>(ev => OnDragPanel());
            panel_PointEditor.RegisterCallback<MouseUpEvent>(ev => OnReleasePanel());
            panel_PointEditor.style.display = DisplayStyle.None;

            base.InitializeUI();
            base.RegisterCommonCallbacks();
        }

        /// <summary>
        /// 景観計画区域編集パネルが開かれたときの処理
        /// </summary>
        protected override void OnDisplayPanel(PlanningPanelStatus status)
        {
            if (currentStatus == status)
                return; // 既に同じステータスのパネルが表示されている場合は何もしない

            if (status == PlanningPanelStatus.EditAreaMain)
            {
                panel_PointEditor.style.display = DisplayStyle.Flex;
                CameraMoveByUserInput.IsCameraMoveActive = true;
                areaPlanningEdit.CreatePinline();

                var color = planningUI.PopColorStack();
                pasteButton.SetEnabled(color != null);  // pasteボタンは色を取ってきて存在していたら最初から有効
                base.DisplaySnackbar("頂点ピンをドラッグすると形状を編集できます");
            }
            else if(panel_PointEditor.style.display == DisplayStyle.Flex)
            {
                areaPlanningEdit.ClearVertexEdit();
                panel_PointEditor.style.display = DisplayStyle.None;
                base.HideSnackbar();
            }
            currentStatus = status;
        }

        /// <summary>
        /// 制限高さの数値を増やすボタンの処理
        /// </summary>
        protected override void IncrementHeight()
        {
            if (areaEditManager.GetLimitHeight() == null) return;
            areaEditManager.ChangeHeight((float)areaEditManager.GetLimitHeight() + 1);  //インクリメント
            areaPlanningHeight.value = areaEditManager.GetLimitHeight().ToString(); //テキストフィールドに反映

        }

        /// <summary>
        /// 制限高さの数値を減らすボタンの処理
        /// </summary>
        protected override void DecrementHeight()
        {
            if (areaEditManager.GetLimitHeight() == null) return;
            areaEditManager.ChangeHeight((float)areaEditManager.GetLimitHeight() - 1);  //デクリメント
            areaPlanningHeight.value = areaEditManager.GetLimitHeight().ToString(); //テキストフィールドに反映

        }

        /// <summary>
        /// 制限高さの数値が直接入力されたときの処理
        /// </summary>
        /// <param name="evt"> 変更内容に関するデータ </param>
        protected override void InputHeight(ChangeEvent<string> evt)
        {
            // 入力値が数値で最大高さ以下の値の場合のみデータを更新
            if (float.TryParse(evt.newValue, out float value) && value <= areaEditManager.GetMaxHeight())
            {
                areaEditManager.ChangeHeight(value);

            }
            else
            {
                // 空欄以外の文字入力があった場合は元の値に戻す
                if (evt.newValue != "") areaPlanningHeight.value = evt.previousValue;
            }
        }

        /// <summary>
        /// エリア名が入力されたときの処理
        /// </summary>
        /// <param name="evt"> 変更内容に関するデータ </param>
        protected override void InputAreaName(ChangeEvent<string> evt)
        {
            areaEditManager.ChangeAreaName(evt.newValue);
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
                ColorEditorUI colorEditorUI = new ColorEditorUI(colorEditorClone, areaEditManager.GetColor());
                colorEditorUI.OnColorEdited += (newColor) =>
                {
                    areaPlanningColor.style.backgroundColor = newColor;
                    areaEditManager.ChangeColor(newColor);
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
        /// キャンセルボタンを押したときの処理
        /// </summary>
        protected override void OnCancelButtonClicked()
        {
            areaPlanningEdit.ClearVertexEdit();
        }

        /// <summary>
        /// OKボタンを押したときの処理
        /// </summary>
        protected override void OnOKButtonClicked()
        {
            // 色彩変更画面を閉じる
            isColorEditing = false;
            EditColor();

            // 頂点が編集されていたら
            if (areaPlanningEdit.IsVertexEdited())
            {
                // 頂点が交差しているか確認
                if (displayPinLine.IsIntersectedByLine())
                {
                    base.DisplaySnackbar("頂点が交差したエリアは作成できません");
                    return;
                }
                // 頂点の編集を適用
                areaPlanningEdit.ConfirmEditData();
            }

            //データベースに変更確定を反映
            areaEditManager.ConfirmUpdatedProperty();
            planningUI.InvokeOnChangeConfirmed();

            // 高さを反映
            areaEditManager.ApplyBuildingHeight(true);
        }

        /// <summary>
        /// 頂点編集パネルをクリックしたときの処理
        /// </summary>
        private void OnClickPanel(MouseDownEvent e)
        {
            if (areaPlanningEdit.IsClickPin()) // ピンをクリック 
            {
                if (e.clickCount == 2) // ダブルクリックした場合は頂点を削除
                {
                    areaPlanningEdit.DeleteVertex();
                }
                else // 通常クリックの場合は頂点を移動
                {
                    CameraMoveByUserInput.IsCameraMoveActive = false;
                }
            }
            else if (areaPlanningEdit.IsClickLine()) // ラインをクリック
            {
                CameraMoveByUserInput.IsCameraMoveActive = false;
                // 中点に頂点を追加
                areaPlanningEdit.AddVertexToLine();
            }
            else
            {
                CameraMoveByUserInput.IsCameraMoveActive = true;
            }
        }

        /// <summary>
        /// 頂点編集パネルをドラッグしたときの処理
        /// </summary>
        private void OnDragPanel()
        {
            areaPlanningEdit.OnDragPin();
        }

        /// <summary>
        /// 頂点編集パネルのクリックを解除したときの処理
        /// </summary>
        private void OnReleasePanel()
        {
            if (areaPlanningEdit.IsIntersected())
            {
                base.DisplaySnackbar("頂点が交差したエリアは作成できません");
                areaPlanningEdit.ResetVertexPosition();
            }
            areaPlanningEdit.ReleaseEditingPin();
            CameraMoveByUserInput.IsCameraMoveActive = true;
        }

        /// <summary>
        /// 編集用UXMLのパラメータ情報を更新
        /// </summary>
        /// <param name = "newIndex" > 新規に表示する地区データのリスト番号 </ param >
        /// <param name="isEditable"></param>
        void RefreshEditor(int newIndex, bool isEditable)
        {
            // 編集中の内容を破棄
            areaEditManager.ResetProperty();

            // 色彩変更画面を閉じる
            isColorEditing = false;
            EditColor();
            //isVertexEditing = false;
            // 頂点編集の内容を破棄
            areaPlanningEdit.ClearVertexEdit();

            // 編集対象を更新
            areaEditManager.SetEditTarget(newIndex);

            // 新しい編集対象のデータをUIに反映
            string name = areaEditManager.GetAreaName();
            float? height = areaEditManager.GetLimitHeight();
            areaPlanningName.value = name == null ? "" : name;
            areaPlanningHeight.value = height == null ? "" : height.ToString();
            areaPlanningColor.style.backgroundColor = areaEditManager.GetColor();
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
            if (newColor != null)
            {
                areaPlanningColor.style.backgroundColor = newColor.Value;
                areaEditManager.ChangeColor(newColor.Value);
            }
        }

    }
}
