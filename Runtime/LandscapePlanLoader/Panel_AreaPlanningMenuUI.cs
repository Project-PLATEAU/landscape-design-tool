using UnityEngine.UIElements;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画編集画面のメニューパネルUIのプレゼンタークラス
    /// </summary>
    public class Panel_AreaPlanningMenuUI
    {
        private readonly LandscapePlanLoadManager landscapePlanLoadManager;
        private readonly LandscapeExportManager landscapeExportManager;

        public Panel_AreaPlanningMenuUI(VisualElement planning, PlanningUI planningUI)
        {
            landscapePlanLoadManager = new LandscapePlanLoadManager();
            landscapeExportManager = new LandscapeExportManager();

            // ボタンへ処理を登録
            VisualElement panel_AreaPlanningMenu = planning.Q<VisualElement>("Panel_AreaPlanningMenu");
            panel_AreaPlanningMenu.Q<Button>("ImportButton").RegisterCallback<ClickEvent>(ev => LoadSHPFile());
            panel_AreaPlanningMenu.Q<Button>("SaveButton").RegisterCallback<ClickEvent>(ev => ExportSHPFile());

            // 作成ボタンクリック時にエリア作成画面を表示
            planning.Q<VisualElement>("Panel_AreaPlanningMenu").Q<Button>("NewAreaButton").
                RegisterCallback<ClickEvent>(ev => planningUI.ChangePlanningPanelDisplay(PlanningUI.PlanningPanelStatus.RegisterAreaMain));
        }

        /// <summary>
        /// SHPファイルから景観区画データを読み込むメソッド
        /// </summary>
        void LoadSHPFile()
        {
            string path = landscapePlanLoadManager.BrowseFolder();  // ファイル選択ダイアログを表示
            if (path != null)
            {
                landscapePlanLoadManager.LoadShapefile(path);
            }
        }

        /// <summary>
        /// 景観区画データからSHPファイルを書き出すメソッド
        /// </summary>
        void ExportSHPFile()
        {
            string path = landscapeExportManager.OpenShapeExportDialog();  // ファイル保存ダイアログを表示
            if (path != null)
            {
                landscapeExportManager.WriteShapeFile(path);
            }
        }
    }
}
