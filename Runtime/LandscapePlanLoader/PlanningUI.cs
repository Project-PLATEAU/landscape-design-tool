using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画の編集画面UIのプレゼンタークラス
    /// </summary>
    public class PlanningUI : ISubComponent
    {
        private readonly VisualElement title_AreaPlanningInfo;
        private readonly VisualElement title_AreaPlanningList;
        private readonly VisualElement title_AreaPlanningEditMenu;
        private readonly VisualElement panel_AreaPlanningInfo;
        private readonly VisualElement panel_AreaPlanningList;
        private readonly VisualElement panel_AreaPlanningMenu;
        private readonly VisualElement panel_AreaPlanningSubMenu;
        private readonly VisualElement panel_AreaPlanningRegister;
        private readonly VisualElement panel_AreaPlanningEdit;

        private Color? pickedColor = null;
        private VisualElement uiRoot;


        // 選択中のエリアのindex番号
        public int currentFocusedAreaIndex { get; private set; }    // -1: エリア未選択時

        public event Action<int, bool> OnFocusedAreaChanged = delegate { };   // リストからエリアが選択されたときのイベント
        public event Action OnChangeConfirmed = delegate { };   // エリア編集が確定されたときのイベント
        public event Action<PlanningPanelStatus> OnChangePlanningPanelDisplay = delegate { };   // 景観区画編集画面のパネル表示を切り替えたときのイベント

        /// <summary>
        /// パネル表示のステータス
        /// </summary>
        public enum PlanningPanelStatus
        {
            Default,
            ListForcused,
            RegisterAreaMain,
            EditAreaMain,
        }

        public PlanningUI(VisualElement planning, VisualElement uiRoot)
        {
            currentFocusedAreaIndex = -1;   // エリアが選択されていない状態に設定
            this.uiRoot = uiRoot;

            // 各UIパネル制御クラスのインスタンス生成
            new Panel_AreaPlanningListUI(planning, this);
            new Panel_AreaPlanningMenuUI(planning, this);
            new Panel_AreaPlanningSubMenuUI(planning, this);
            new Panel_AreaPlanningInfoUI(planning, this);
            new Panel_AreaPlanningRegister(planning, this);
            new Panel_AreaPlanningEdit(planning, this);

            // 各UIパネルのルートを取得
            title_AreaPlanningInfo = planning.Q<VisualElement>("LeftUpper").Q<VisualElement>("Title_Left");
            title_AreaPlanningList = planning.Q<VisualElement>("LeftLower").Q<VisualElement>("Title_Left");
            title_AreaPlanningEditMenu = planning.Q<VisualElement>("RightUpper").Q<VisualElement>("Title_Right");
            panel_AreaPlanningInfo = planning.Q<VisualElement>("Panel_AreaPlanningInfo");
            panel_AreaPlanningList = planning.Q<VisualElement>("Panel_AreaPlanningList");
            panel_AreaPlanningMenu = planning.Q<VisualElement>("Panel_AreaPlanningMenu");
            panel_AreaPlanningSubMenu = planning.Q<VisualElement>("Panel_AreaPlanningSubMenu");
            panel_AreaPlanningRegister = planning.Q<VisualElement>("Panel_AreaPlanningRegister");
            panel_AreaPlanningEdit = planning.Q<VisualElement>("Panel_AreaPlanningEdit");

            // 選択中のエリアが変更されたときにUIを切り替え
            OnFocusedAreaChanged += (index, isEditable) =>
            {
                if (index == -1 || !isEditable)
                {
                    ChangePlanningPanelDisplay(PlanningPanelStatus.Default); //エリア未選択時
                }
                else
                {
                    ChangePlanningPanelDisplay(PlanningPanelStatus.ListForcused);  //エリア選択時
                }
            };
            // エリア編集が確定されたときに、UIをリスト選択時の画面に戻す
            OnChangeConfirmed += () => ChangePlanningPanelDisplay(PlanningUI.PlanningPanelStatus.Default);

            InvokeOnFocusedAreaChanged(-1); // エリア未選択の状態でUIを初期化

            planning.RegisterCallback<GeometryChangedEvent>(ev => ChangePlanningPanelDisplay(PlanningPanelStatus.Default));
        }

        /// <summary>
        /// 景観区画編集画面のパネル表示を切り替えるメソッド
        /// </summary>
        /// <param name="status"> 表示するパネルステータス </param>
        public void ChangePlanningPanelDisplay(PlanningPanelStatus status)
        {
            // デフォルトのオプションを設定する関数群
            var displaySettings = new Dictionary<VisualElement, DisplayStyle>
            {
                { title_AreaPlanningInfo, DisplayStyle.Flex},
                { title_AreaPlanningList , DisplayStyle.Flex},
                { title_AreaPlanningEditMenu , DisplayStyle.Flex},
                { panel_AreaPlanningInfo , DisplayStyle.Flex},
                { panel_AreaPlanningList , DisplayStyle.Flex},
                { panel_AreaPlanningMenu , DisplayStyle.Flex},
                { panel_AreaPlanningSubMenu , DisplayStyle.None},
                { panel_AreaPlanningRegister , DisplayStyle.None},
                { panel_AreaPlanningEdit , DisplayStyle.None},
                { uiRoot , DisplayStyle.Flex }
            };

            var isSkipDisplaySetting = true;

            // パネル表示切り替えイベントを発火
            OnChangePlanningPanelDisplay(status);

            // ステータスに応じて表示設定を変更
            switch (status)
            {
                // 初期状態
                case PlanningPanelStatus.Default:
                    break;

                // リストからエリアが選択された状態
                case PlanningPanelStatus.ListForcused:
                    displaySettings[panel_AreaPlanningSubMenu] = DisplayStyle.Flex;
                    break;

                // エリア作成時の状態
                case PlanningPanelStatus.RegisterAreaMain:
                    displaySettings[panel_AreaPlanningMenu] = DisplayStyle.None;
                    displaySettings[panel_AreaPlanningRegister] = DisplayStyle.Flex;
                    displaySettings[uiRoot] = DisplayStyle.None;
                    break;

                // エリア編集時の状態
                case PlanningPanelStatus.EditAreaMain:
                    displaySettings[panel_AreaPlanningMenu] = DisplayStyle.None;
                    displaySettings[panel_AreaPlanningEdit] = DisplayStyle.Flex;
                    displaySettings[uiRoot] = DisplayStyle.None;
                    break;

                default:
                    isSkipDisplaySetting = false;
                    break;
            }

            if (isSkipDisplaySetting == false)
            {
                return;
            }


            void SetDisplayStyle(VisualElement element, DisplayStyle style)
            {
                if (element != null)
                {
                    element.style.display = style;
                }
            }

            foreach (var kvp in displaySettings)
            {
                SetDisplayStyle(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// colorを保持する
        /// </summary>
        /// <param name="col"></param>
        public void PushColorStack(Color col)
        {
            pickedColor = col;
        }

        /// <summary>
        /// 保持したcolorの値を返す
        /// </summary>
        /// <returns></returns>
        public Color? PopColorStack()
        {
            return pickedColor;
        }

        /// <summary>
        /// エリアが選択されたときの処理を発火するメソッド
        /// </summary>
        /// <param name="index">選択されたエリアのindex番号</param>
        /// <param name="isInteractable"></param>
        public void InvokeOnFocusedAreaChanged(int index, bool isInteractable = true)
        {
            currentFocusedAreaIndex = index;
            OnFocusedAreaChanged(index, isInteractable);
        }

        /// <summary>
        /// エリア編集が確定されたときの処理を発火するメソッド
        /// </summary>
        public void InvokeOnChangeConfirmed()
        {
            OnChangeConfirmed();
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
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
