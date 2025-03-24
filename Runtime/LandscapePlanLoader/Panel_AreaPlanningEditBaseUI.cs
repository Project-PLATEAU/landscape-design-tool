using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画のエリア情報編集/登録用のUIパネルの基底クラス
    /// </summary>
    public abstract class Panel_AreaPlanningEditBaseUI
    {
        protected readonly DisplayPinLine displayPinLine;
        protected readonly VisualElement planning;
        protected readonly PlanningUI planningUI;
        protected VisualElement panel_AreaPlanningEdit; // 親パネル
        protected VisualElement panel_PointEditor; // 頂点編集パネル
        protected Button okButton;    // OKボタン
        protected TextField areaPlanningName; // エリア名入力欄
        protected TextField areaPlanningHeight;   // 高さ入力欄
        protected VisualElement areaPlanningColor;    // 色彩表示欄
        protected VisualTreeAsset colorEditor;    // 色彩編集用のテンプレート
        protected VisualElement colorEditorClone; // 色彩編集用クローン
        protected VisualElement snackBarClone; // Snackbarのクローン

        protected Button copyButton;
        protected Button pasteButton;
        protected bool isColorEditing = false;

        public Panel_AreaPlanningEditBaseUI(VisualElement planning, PlanningUI planningUI)
        {
            this.planning = planning;
            this.planningUI = planningUI;

            colorEditor = Resources.Load<VisualTreeAsset>("UIColorEditor");
            CreateSnackbar();
            snackBarClone.Q<Button>("CloseButton").clicked += () => HideSnackbar();

            // displayPinLineコンポーネントがSceneに存在しない場合は生成
            displayPinLine = GameObject.FindAnyObjectByType<DisplayPinLine>();
            if (displayPinLine == null)
            {
                displayPinLine = new GameObject("PinLine").AddComponent<DisplayPinLine>();
            }
        }

        /// <summary>
        /// 共通UI要素の初期化処理
        /// </summary>
        protected virtual void InitializeUI()
        {
            areaPlanningName = panel_AreaPlanningEdit.Q<TextField>("AreaPlanningName");
            areaPlanningHeight = panel_AreaPlanningEdit.Q<TextField>("AreaPlanningHeight");
            areaPlanningColor = panel_AreaPlanningEdit.Q<VisualElement>("AreaPlanningColor");
        }

        /// <summary>
        /// UIイベントハンドラー登録
        /// </summary>
        protected virtual void RegisterCommonCallbacks()
        {
            panel_AreaPlanningEdit.Q<Button>("CancelButton").RegisterCallback<ClickEvent>(ev => planningUI.InvokeOnFocusedAreaChanged(-1)); // エリアフォーカスを外す
            panel_AreaPlanningEdit.Q<Button>("CancelButton").RegisterCallback<ClickEvent>(ev => OnCancelButtonClicked());

            copyButton = panel_AreaPlanningEdit.Q<Button>("ColorCopyButton");
            copyButton.SetEnabled(true);
            pasteButton = panel_AreaPlanningEdit.Q<Button>("ColorPasteButton");
            var color = planningUI.PopColorStack();
            pasteButton.SetEnabled(color != null);  // pasteボタンは色を取ってきて存在していたら最初から有効

            copyButton.RegisterCallback<ClickEvent>(ev => OnCopyButtonClicked());
            pasteButton.RegisterCallback<ClickEvent>(ev => OnPasteButtonClicked());

            okButton = panel_AreaPlanningEdit.Q<Button>("OKButton");
            okButton.RegisterCallback<ClickEvent>(ev => OnOKButtonClicked());
            areaPlanningHeight.RegisterValueChangedCallback(InputHeight);
            areaPlanningColor.RegisterCallback<ClickEvent>(ev => ToggleColorEditing());
            areaPlanningName.RegisterValueChangedCallback(InputAreaName);
            planningUI.OnChangePlanningPanelDisplay += OnDisplayPanel;
        }

        /// <summary>
        /// 色編集の表示切り替え
        /// </summary>
        protected void ToggleColorEditing()
        {
            isColorEditing = !isColorEditing;
            EditColor();
        }

        /// <summary>
        /// 景観計画区域編集（登録）パネルが開かれたときの処理
        /// </summary>
        protected abstract void OnDisplayPanel(PlanningUI.PlanningPanelStatus status);

        /// <summary>
        /// 制限高さの数値を増やすボタンの処理
        /// </summary>
        protected abstract void IncrementHeight();

        /// <summary>
        /// 制限高さの数値を減らすボタンの処理
        /// </summary>
        protected abstract void DecrementHeight();

        /// <summary>
        /// 制限高さが入力された時の共通処理
        /// </summary>
        protected abstract void InputHeight(ChangeEvent<string> evt);

        /// <summary>
        /// エリア名が入力されたときの処理
        /// </summary>
        /// <param name="evt"> 変更内容に関するデータ </param>
        protected virtual void InputAreaName(ChangeEvent<string> evt)
        { }

        /// <summary>
        /// 色彩編集の処理
        /// </summary>
        protected virtual void EditColor()
        {
            // 色彩変更パネルを画面中央に表示
            colorEditorClone = colorEditor.CloneTree();
            planning.Q<VisualElement>("CenterLower").Add(colorEditorClone);
        }

        /// <summary>
        /// キャンセルボタンが押されたときの処理
        /// </summary>
        protected abstract void OnCancelButtonClicked();

        /// <summary>
        /// OKボタンが押されたときの処理
        /// </summary>
        protected abstract void OnOKButtonClicked();


        /// <summary>
        /// copyボタンが押された時の処理
        /// </summary>
        protected abstract void OnCopyButtonClicked();

        /// <summary>
        /// pasteボタンが押された時の処理
        /// </summary>
        protected abstract void OnPasteButtonClicked();

        /// <summary>
        /// Snackbarを新しく生成する関数
        /// </summary>
        protected void CreateSnackbar()
        {
            if (planning.Q<VisualElement>("Snackbar") == null)
            {
                var snackBar = Resources.Load<VisualTreeAsset>("Snackbar");
                snackBarClone = snackBar.CloneTree();
                planning.Q<VisualElement>("CenterUpper").Add(snackBarClone);
            }
            snackBarClone = planning.Q<VisualElement>("Snackbar");
            snackBarClone.visible = false;
            snackBarClone.Q<Button>("CloseButton").visible = false;

        }

        /// <summary>
        /// Snackbarを表示する関数
        /// </summary>
        /// <param name="text">表示したい文章</param>
        protected void DisplaySnackbar(string text)
        {
            snackBarClone.Q<Label>("SnackbarText").text = text;
            snackBarClone.visible = true;
            snackBarClone.Q<Button>("CloseButton").visible = true;
        }

        /// <summary>
        /// Snackbarを非表示にする
        /// </summary>
        protected void HideSnackbar()
        {
            if (planning.Q<VisualElement>("Snackbar") != null)
            {
                snackBarClone.visible = false;
                snackBarClone.Q<Button>("CloseButton").visible = false;
            }
        }
    }
}
