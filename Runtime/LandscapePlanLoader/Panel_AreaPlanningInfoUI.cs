using UnityEngine.UIElements;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画情報表示UIパネルのプレゼンタークラス
    /// </summary>
    public class Panel_AreaPlanningInfoUI
    {
        private readonly Label areaPlanningName;
        private readonly Label heightLimit;

        public Panel_AreaPlanningInfoUI(VisualElement planning, PlanningUI planningUI)
        {
            // エリア情報表示UIの取得
            VisualElement panel_areaPlanningInfo = planning.Q<VisualElement>("Panel_AreaPlanningInfo");
            areaPlanningName = panel_areaPlanningInfo.Q<Label>("AreaPlanningName");
            heightLimit = panel_areaPlanningInfo.Q<Label>("HeightLimit");

            planningUI.OnFocusedAreaChanged += SetAreaInfo; // エリア選択対象が変更されたときに表示情報を更新
            planningUI.OnChangeConfirmed += () => SetAreaInfo(planningUI.currentFocusedAreaIndex); // エリア編集が確定されたときに表示情報を更新
        }

        /// <summary>
        /// エリアの情報を表示するメソッド
        /// </summary>
        /// <param name="index"> 表示対象エリアのデータリスト番号 </param>
        /// <param name="isEditable"></param>
        void SetAreaInfo(int index, bool isEditable = true)
        {
            // エリアが選択されていない場合の表記
            if(index == -1)
            {
                areaPlanningName.text = "---";
                heightLimit.text = "---";
                return;
            }

            // エリアデータの取得し，UIに反映
            AreaProperty areaProperty = AreasDataComponent.GetProperty(index);
            areaPlanningName.text = areaProperty.Name;
            heightLimit.text = areaProperty.LimitHeight.ToString();
        }
    }
}
