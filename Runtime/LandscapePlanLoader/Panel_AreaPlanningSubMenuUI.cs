using UnityEngine.UIElements;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画の編集画面サブメニューパネルUIのプレゼンタークラス
    /// </summary>
    public class Panel_AreaPlanningSubMenuUI
    {
        public Panel_AreaPlanningSubMenuUI(VisualElement planning, PlanningUI planningUI)
        {
            // 編集ボタンクリック時にエリア編集画面を表示
            planning.Q<VisualElement>("Panel_AreaPlanningSubMenu").Q<Button>("EditButton").
                RegisterCallback<ClickEvent>(ev => planningUI.ChangePlanningPanelDisplay(PlanningUI.PlanningPanelStatus.EditAreaMain));
        }
    }
}
