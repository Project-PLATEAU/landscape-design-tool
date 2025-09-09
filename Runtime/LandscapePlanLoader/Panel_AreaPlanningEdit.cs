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

        // クリック数のオフセット。クリック消費を管理するために使用する。
        // クリック消費はビューポート内のクリックや頂点移動を対象とする。
        // カメラ移動のマウスドラッグやUIToolkit類のボタンのクリックなどでは消費しない。
        // 例えば　1回目のクリックで頂点追加(clickCountOffset = 1)、そのまま頂点の削除を行いたい場合は3回目のクリック(clickCountOffset + 2)で頂点削除を行うことが出来るようになる。
        private int clickCountOffset = 0;
        private bool isNeedReoffsetClickCount = false;  // クリック数のオフセットが必要かどうか。頂点の移動や削除を行った場合はオフセットをリセットする。

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
        /// 頂点編集パネルが開いている時にマウスクリックしたときの処理
        /// </summary>
        private void OnClickPanel(MouseDownEvent e)
        {
            // 初回クリック時やフラグ有効時にクリック消費状態をリセット
            if (e.clickCount == 1 || isNeedReoffsetClickCount)
            {
                clickCountOffset = e.clickCount - 1; // e.clickCount == 1 : 0, isNeedReoffsetClickCount : どこかでフラグが有効化された際のクリック数
                isNeedReoffsetClickCount = false;
            }

            // これ以降の処理ではクリック消費の対象となる操作が行われるとする。例外があれば そこでfalseを設定する。
            isNeedReoffsetClickCount = true;

            if (areaPlanningEdit.SelectPinOnScreen()) // ピンをクリック 
            {
                if (e.clickCount == clickCountOffset + 2) // ダブルクリックした場合は頂点を削除
                {
                    areaPlanningEdit.DeleteVertex();
                }
                else // 通常クリックの場合は頂点を移動
                {
                    isNeedReoffsetClickCount = false; // ここでフラグを有効化するとダブルクリックの前にリセットされる。 代わりにドラッグが行われた際にフラグを有効化している。
                    CameraMoveByUserInput.IsCameraMoveActive = false;
                }
            }
            else if (areaPlanningEdit.SelectLineOnScreen()) // ラインをクリック
            {
                CameraMoveByUserInput.IsCameraMoveActive = false;
                // 中点に頂点を追加
                areaPlanningEdit.AddVertexToLine();
            }
            else
            {
                CameraMoveByUserInput.IsCameraMoveActive = true;    // 消しても景観計画区域の編集時には動作したが別の機能でのフラグの扱いや機能増築時を考慮した設計に必要かもかもしれないので残しておく。
            }
        }

        /// <summary>
        /// 頂点編集パネルが開いている時にマウス移動したときの処理
        /// </summary>
        private void OnDragPanel()
        {
            if (areaPlanningEdit.IsClickedPin())
            {
                isNeedReoffsetClickCount = true; // 頂点の移動や削除を行った場合はクリック数のオフセットをリセットする必要がある。
            }
            areaPlanningEdit.OnDragPin();
        }

        /// <summary>
        /// 頂点編集パネルが開いている時にクリックを解除したときの処理
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
