using PLATEAU.Util.Async;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using System;

namespace Landscape2.Editor
{
    /// <summary>
    /// 景観ツールのInitialSettingsWindowのエントリーポイントです。
    /// </summary>
    public class InitialSettingsWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset visualTreeAsset = default;
        [SerializeField] private Texture checkTexture;
        [SerializeField] private Texture errorTexture;
        private InitialSettings initialSettings = new InitialSettings();
        private VisualElement uiRoot;
        private Button runButton; // 実行ボタン
        private Button updateButton; // 更新ボタン

        private const string UIRunButton = "RunButton"; // 初期設定実行ボタン名前
        private const string UIUpdateButton = "UpdateButton"; // 更新ボタン名前
        private const string UIImportCheck = "ImportCheckColumn"; // 都市モデルインポート済み判定欄名前
        private const string UIImportHelpbox = "ImportHelpboxColumn"; // 都市モデルインポート済み判定Helpbox欄名前
        private const string UICityObjectCheck = "CityObjectCheckColumn"; // 都市オブジェクトが配置されているかの判定欄名前
        private const string UICityObjectHelpbox = "CityObjectHelpboxColumn"; // 都市オブジェクトが配置されているかの判定Helpbox欄名前
        private const string UISubComponentsCheck = "SubComponentsCheckColumn"; // SubCompornentsが生成されたかの判定欄名前
        private const string UISubComponentsHelpbox = "SubComponentsHelpboxColumn"; // SubCompornentsが生成されたかの判定Helpbox欄名前
        private const string UIMainCameraCheck = "MainCameraCheckColumn"; // MainCameraが生成されたかの判定欄名前
        private const string UIMainCameraHelpbox = "MainCameraHelpboxColumn"; // MainCameraが生成されたかの判定Helpbox欄名前
        private const string UIEnvironmentCheck = "EnvironmentCheckColumn"; // Environmentが生成されたかの判定欄名前
        private const string UIEnvironmentHelpbox = "EnvironmentHelpboxColumn"; // Environmentが生成されたかの判定Helpbox欄名前
        private const string UIMaterialAdjustCheck = "MaterialAdjustCheckColumn"; // マテリアル分けが実行されたかの判定欄名前
        private const string UIMaterialAdjustHelpbox = "MaterialAdjustHelpboxColumn"; // マテリアル分けが実行されたかの判定Helpbox欄名前

        private HelpBox initialSettingsHelpBox;
        private HelpBox importCheckHelpBox;
        private HelpBox cityObjectCheckHelpBox;
        private HelpBox subCompornentsCheckHelpBox;
        private HelpBox mainCameraCheckHelpBox;
        private HelpBox environmentCheckHelpBox;
        private HelpBox materialAdjustCheckHelpBox;

        private Image importCheckImage;
        private Image cityObjectCheckImage;
        private Image subCompornentsCheckImage;
        private Image mainCameraCheckImage;
        private Image environmentCheckImage;
        private Image materialAdjustCheckImage;

        private List<bool> checkList; // 初期設定実行可能かの判定用リスト

        [MenuItem("PLATEAU/InitialSettings")]
        public static void Open()
        {
            var window = GetWindow<InitialSettingsWindow>("InitialSettings");
            window.Show();
        }

        public void CreateGUI()
        {
            uiRoot = rootVisualElement;
            VisualElement labelFromUXML = visualTreeAsset.Instantiate();
            uiRoot.Add(labelFromUXML);
            runButton = uiRoot.Q<Button>(UIRunButton);
            updateButton = uiRoot.Q<Button>(UIUpdateButton);
            checkList = new List<bool>();
            runButton.SetEnabled(false);

            initialSettingsHelpBox = new HelpBox("初期設定が完了しています", HelpBoxMessageType.Info);
            importCheckHelpBox = new HelpBox("都市モデルがインポートされているか確認してください", HelpBoxMessageType.Error);
            cityObjectCheckHelpBox = new HelpBox("都市オブジェクトが配置されているか確認してください", HelpBoxMessageType.Error);
            subCompornentsCheckHelpBox = new HelpBox("SubCompornentsの生成が失敗しました", HelpBoxMessageType.Error);
            mainCameraCheckHelpBox = new HelpBox("MainCameraの生成が失敗しました", HelpBoxMessageType.Error);
            environmentCheckHelpBox = new HelpBox("Environmentの生成が失敗しました", HelpBoxMessageType.Error);
            materialAdjustCheckHelpBox = new HelpBox("マテリアル分けの実行が失敗しました", HelpBoxMessageType.Error);


            importCheckImage = new Image();
            cityObjectCheckImage = new Image();
            subCompornentsCheckImage = new Image();
            mainCameraCheckImage = new Image();
            environmentCheckImage = new Image();
            materialAdjustCheckImage = new Image();

            // チェック項目をすべて満たしている場合初期設定を実行できるようにする
            if (IsInitialSettingsPossible() == true)
            {
                runButton.SetEnabled(true);
            }

            // 初期設定実行ボタンが押されたとき
            runButton.clicked += () =>
            {
                // 実行前チェックリストが満たしていない場合処理を実行しない
                if (IsInitialSettingsPossible() == false)
                {
                    return;
                }

                // 初期設定を実行
                runButton.SetEnabled(false);
                ExecInitialSettings().ContinueWithErrorCatch();
            };

            // 更新ボタンが押されたとき
            updateButton.clicked += () =>
            {
                // チェック項目をすべて満たしている場合初期設定を実行できるようにする
                if (IsInitialSettingsPossible() == true)
                {
                    runButton.SetEnabled(true);
                }
                else
                {
                    runButton.SetEnabled(false);
                }
            };
        }

        // 初期設定可能かどうか判定する
        private bool IsInitialSettingsPossible()
        {
            // 実行前チェックリストを更新
            UpdateCheckList();
            // チェック項目をすべて満たしている場合初期設定を実行できるようにする
            if (checkList.Contains(false) == false)
            {
                checkList.Clear();
                return true;
            }
            checkList.Clear();
            return false;
        }

        // 実行前チェックリストの更新処理
        private void UpdateCheckList()
        {
            // 初期設定が未実行かの判定
            var isSubComponents = initialSettings.IsSubComponentsNotExists();
            checkList.Add(isSubComponents);

            if (isSubComponents == true)
            {
                if (uiRoot.Contains(initialSettingsHelpBox))
                {
                    uiRoot.Remove(initialSettingsHelpBox);
                }
            }
            else
            {
                uiRoot.Add(initialSettingsHelpBox);
            }

            // 都市モデルインポート済みかの判定
            var isImport = initialSettings.IsImportCityModelExists();
            checkList.Add(isImport);
            AddCheckListUI(isImport, UIImportCheck, UIImportHelpbox, importCheckHelpBox, importCheckImage);

            // 都市オブジェクトが配置されているかの判定
            var isCityObject = initialSettings.IsCityObjectGroupExists();
            checkList.Add(isCityObject);
            AddCheckListUI(isCityObject, UICityObjectCheck, UICityObjectHelpbox, cityObjectCheckHelpBox, cityObjectCheckImage);
        }

        // チェックリストのUI処理
        private void AddCheckListUI(bool isCheck, string checkUI, string helpBoxUI, HelpBox helpbox, Image checkImage)
        {
            var checkColumn = uiRoot.Q<VisualElement>(checkUI);
            var helpboxColumn = uiRoot.Q<VisualElement>(helpBoxUI);
            checkColumn.Add(checkImage);

            checkList.Add(isCheck);
            if (isCheck == true)
            {
                checkImage.image = checkTexture;
                if (helpboxColumn.Contains(helpbox))
                {
                    helpboxColumn.Remove(helpbox);
                }
            }
            else
            {
                checkImage.image = errorTexture;
                helpboxColumn.Add(helpbox);
            }
        }

        // 初期設定を実行したときの処理
        private async Task ExecInitialSettings()
        {
            // SubComponentsを生成
            initialSettings.CreateSubComponents();
            AddCheckListUI(true, UISubComponentsCheck, UISubComponentsHelpbox, subCompornentsCheckHelpBox, subCompornentsCheckImage);

            // MainCameraが存在しない場合生成
            initialSettings.CreateMainCamera();
            AddCheckListUI(true, UIMainCameraCheck, UIMainCameraHelpbox, mainCameraCheckHelpBox, mainCameraCheckImage);

            // Environmentが存在しない場合生成
            var isCreateEnvironmentPossible = initialSettings.IsCreateEnvironmentPossible();
            initialSettings.CreateEnvironment();
            AddCheckListUI(isCreateEnvironmentPossible, UIEnvironmentCheck, UIEnvironmentHelpbox, environmentCheckHelpBox, environmentCheckImage);

            // マテリアル分け
            try
            {
                await initialSettings.ExecMaterialAdjust();
            }
            catch
            {
                AddCheckListUI(false, UIMaterialAdjustCheck, UIMaterialAdjustHelpbox, materialAdjustCheckHelpBox, materialAdjustCheckImage);
            }
            AddCheckListUI(true, UIMaterialAdjustCheck, UIMaterialAdjustHelpbox, materialAdjustCheckHelpBox, materialAdjustCheckImage);

            if (!initialSettings.IsBIMImportMaterialReferenceExists())
            {
                initialSettings.CreateBIMImportMaterialSetting();
            }

            // 初期設定が完了したことをUIに表示
            uiRoot.Add(initialSettingsHelpBox);
        }
    }
}
