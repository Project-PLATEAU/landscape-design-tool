using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Landscape2.Runtime.UiCommon;
using System;
using System.Linq;

namespace Landscape2.Runtime
{
    public class LineOfSightUI
    {
        public class ViewStateControl
        {
            protected VisualElement titleElement;
            protected VisualElement rootElement;

            public ViewStateControl(VisualElement title, VisualElement root)
            {
                titleElement = title;
                rootElement = root;
            }

            public void Show(bool state)
            {
                if (rootElement == null)
                {
                    return;
                }
                rootElement.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;

                // こっちは設定されている時
                if (titleElement != null)
                {
                    titleElement.style.display = rootElement.style.display;
                }
            }
        }
        class ListView : ViewStateControl
        {
            Dictionary<string, Button> buttonIndex = new();

            public int ButtonNum => buttonIndex.Count();

            public ListView(VisualElement title, VisualElement root) : base(title, root)
            {
                Initialize();
            }

            public void Show(bool show)
            {
                rootElement.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                titleElement.style.display = rootElement.style.display;
            }

            public void Initialize()
            {
                var scrollView = rootElement.Q<ScrollView>("Panel");
                var containerList = scrollView.Query<TemplateContainer>().ToList();

                foreach (var c in containerList)
                {
                    scrollView.Remove(c);
                }

                ShowEmptyMessage(true);
            }

            public void ShowEmptyMessage(bool show)
            {
                var emptyMessage = rootElement.Q<Label>("Dialogue");
                emptyMessage.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public void AddButton(Button button, string name)
            {
                var scrollView = rootElement.Q<ScrollView>("Panel");
                ShowEmptyMessage(false);

                if (buttonIndex.ContainsKey(name))
                {
                    RemoveButton(name);
                }
                buttonIndex.Add(name, button);
                scrollView.Add(button);
            }

            void RemoveButton(VisualElement button)
            {
                var scrollView = rootElement.Q<ScrollView>("Panel");
                var childList = scrollView.Children();
                var childCount = childList.Count();

                scrollView.Remove(button);

                if (childCount <= 2) // Dialogue含めて2つ
                {
                    ShowEmptyMessage(true);
                }

            }

            public void RemoveButton(string name)
            {
                // var scrollView = rootElement.Q<ScrollView>("Panel");
                if (buttonIndex.TryGetValue(name, out var button))
                {
                    RemoveButton(button);
                    buttonIndex.Remove(name);
                }
            }

            public void ClearList()
            {
                var scrollView = rootElement.Q<ScrollView>("Panel");
                var buttonList = scrollView.Query<Button>().ToList();
                foreach (var button in buttonList)
                {
                    RemoveButton(button.name);
                }
            }
        }
        private LineOfSight lineOfSight;
        private ViewPoint viewPoint;
        private Landmark landmark;
        private AnalyzeViewPoint analyzeViewPoint;
        private AnalyzeLandmark analyzeLandmark;
        private VisualElement viewPointListPanel;
        private VisualElement landMarkListPanel;
        private VisualElement analyzeSettingPanel;
        private VisualElement analyzeListPanel;
        private VisualElement analyzeSettingPanelTitle;
        private VisualElement beforeElement;


        ListView viewPointList_View;
        ListView landmarkList_View;
        ListView analyzeList_View;

        Stack<VisualElement> viewElementStack = new();

        // FIXME: ListView側に持たせたので削除して良い
        Dictionary<Button, string> entryViewpointButton = new();

        // FIXME: ListView側に持たせたので削除して良い
        Dictionary<Button, string> entryLandmarkButton = new();

        // FIXME: ListView側に持たせたので削除して良い
        Dictionary<Button, string> entryAnalyzeButtonFromButton = new();

        SnackBar snackbar;


        private StyleSheet uiStyleCommon;

        public void OnEnable(LineOfSight lineOfSightInstance, ViewPoint viewPointInstance, Landmark landmarkInstance, AnalyzeViewPoint analyzeViewPointInstance, AnalyzeLandmark analyzeLandmarkInstance, VisualElement element)
        {
            lineOfSight = lineOfSightInstance;
            viewPoint = viewPointInstance;
            landmark = landmarkInstance;
            analyzeViewPoint = analyzeViewPointInstance;
            analyzeLandmark = analyzeLandmarkInstance;

            viewPointListPanel = element.Q<VisualElement>("ViewPointList");
            landMarkListPanel = element.Q<VisualElement>("LandmarkList");
            analyzeSettingPanel = element.Q<VisualElement>("AnalyzeSetting");
            analyzeListPanel = element.Q<VisualElement>("AnalyzeList");
            analyzeSettingPanelTitle = element.Q<VisualElement>("Title_AnalyzeSetting");

            var viewpointListTitle = element.Q<VisualElement>("Title_ViewList");
            var landmarkListTitle = element.Q<VisualElement>("Title_LandmarkList");
            var analyzeListTitle = element.Q<VisualElement>("Title_AnalyzeList");
            viewPointList_View = new(viewpointListTitle, viewPointListPanel);
            landmarkList_View = new(landmarkListTitle, landMarkListPanel);
            analyzeList_View = new(analyzeListTitle, analyzeListPanel);


            snackbar = new(element);

            //AsyncOperationHandle<StyleSheet> uiStyleCommonHandle = Addressables.LoadAssetAsync<StyleSheet>("UIStyleCommon");
            //uiStyleCommon = await uiStyleCommonHandle.Task;

            InitializeAnalyzeSettingPanel();
        }

        public void HideSnackbar()
        {
            snackbar.Hide();
        }


        private void InitializeAnalyzeSettingPanel()
        {
            ShowAnalyzeSettingPanel(true);

            InitializeNew_PointMenu();
            InitializeNew_Viewpoint();
            InitializeNew_Landmark();
            InitializeNew_Analyze_Select();
            InitializeNew_Analyze_Viewpoint();
            InitializeNew_Analyze_Landmark();
            InitializeSlider_Viewpoint();
            InitializeSlider_Landmark();
            InitializeEdit_Viewpoint();
            InitializeEdit_Landmark();
            InitializeEdit_Analyze_Viewpoint();
            InitializeEdit_Analyze_Landmark();

            // デフォルト設定のパネル表示位置を調整
            var panel = landMarkListPanel.Q<VisualElement>("UIPanel");
            if (panel != null)
            {
                // 仮で非表示にしているけど、本来は右メニューに行くべき?
                panel.style.display = DisplayStyle.None;
            }

            ViewDefaultPanels();
        }

        private void PushElement(VisualElement current)
        {
            viewElementStack.Push(current);
            current.style.display = DisplayStyle.None;
        }

        private VisualElement PopElement()
        {
            if (viewElementStack.TryPop(out var popElement))
            {
                popElement.style.display = DisplayStyle.Flex;
                return popElement;
            }
            return popElement ?? null;
        }

        private void ShowAnalyzeSettingPanel(bool state)
        {
            analyzeSettingPanel.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            analyzeSettingPanelTitle.style.display = analyzeSettingPanel.style.display;
        }

        private void SetAnalyzeViewpointTarget(string target)
        {
            var new_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Viewpoint");
            var viewpointName = new_Analyze_Viewpoint.Q<Label>("ViewpointName");

            viewpointName.text = target;
        }

        private void SetAnalyzeLandmarkTarget(string target)
        {
            var new_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Viewpoint");
            var landmarkName = new_Analyze_Viewpoint.Q<Label>("LandmarkName");

            landmarkName.text = target;
        }

        private void SetAnalyzeLandmark_LandmarkTarget(string target)
        {
            var new_Analyze_Landmark = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Landmark");
            var landmarkName = new_Analyze_Landmark.Q<Label>("LandmarkName");

            landmarkName.text = target;

        }

        private void SetAnalyzeLandmark_ViewingAngleTarget(int upper, int under)
        {
            var new_Analyze_Landmark = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Landmark");
            var upperLabel = new_Analyze_Landmark.Q<Label>("Upper");
            var underLabel = new_Analyze_Landmark.Q<Label>("Under");

            upperLabel.text = upper.ToString();
            underLabel.text = under.ToString();
        }


        private void SetAnalyzeViewingAngleTarget(int horizontal, int vertical)
        {
            var new_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Viewpoint");
            var horizontalLabel = new_Analyze_Viewpoint.Q<Label>("horizontal");
            var verticalLabel = new_Analyze_Viewpoint.Q<Label>("vertical");

            horizontalLabel.text = $"{horizontal}";
            verticalLabel.text = $"{vertical}";
        }

        private void SetAnalyzeLandmarkViewingAngleTarget(int upper, int under)
        {
            var new_Analyze_Landmark = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Landmark");
            var upperLabel = new_Analyze_Landmark.Q<Label>("Upper");
            var underLabel = new_Analyze_Landmark.Q<Label>("Under");

            upperLabel.text = $"{upper}";
            underLabel.text = $"{under}";
        }

        private void SetNewAnalyzeSelectPanelButtonState(VisualElement newAnalyzeSelect)
        {
            var viewpointLandmarkButton = newAnalyzeSelect.Q<Button>("SelectButton_Viewpoint");
            var landmarkButton = newAnalyzeSelect.Q<Button>("SelectButton_Landmark");

            viewpointLandmarkButton.SetEnabled(0 < viewPointList_View.ButtonNum && 0 < landmarkList_View.ButtonNum);
            landmarkButton.SetEnabled(0 < landmarkList_View.ButtonNum);
        }


        /// <summary>
        /// 見通し解析編集のパネル表示設定。
        /// panelNameで指定したVisualElementを表示。それ以外を非表示にする
        /// </summary>
        /// <param name="panelName"></param>
        /// <return>指定したpanelのVisualElement</return>
        private VisualElement SetCurrentAnalyzeSettingPanel(string panelName)
        {
            var aspRoot = analyzeSettingPanel.Q<VisualElement>("Panel");
            var childList = aspRoot.Children();

            VisualElement currentElement = null;

            System.Text.StringBuilder sb = new();

            sb.AppendLine($"panelName: {panelName}");

            /// 毎回回しちゃうので改善の余地あり
            foreach (var c in childList)
            {
                c.style.display = DisplayStyle.None;
                sb.Append($"\t{c.name}");
                if (c.name == panelName)
                {
                    sb.Append($"... bingo! {panelName}");
                    currentElement = c;
                    c.style.display = DisplayStyle.Flex;
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
            return currentElement;
        }

        /// <summary>
        /// 新しくピンを作成
        /// 見通し解析
        /// 「見通し解析編集」の所
        /// </summary>
        private void InitializeNew_PointMenu()
        {
            var new_PointMenu = analyzeSettingPanel.Q<VisualElement>("New_PointMenu");
            var new_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Viewpoint");
            var new_Landmark = analyzeSettingPanel.Q<VisualElement>("New_Landmark");
            var new_Analyze_Select = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Select");

            var newViewpoint_Button = new_PointMenu.Q<Button>("NewViewpoint_Button");
            newViewpoint_Button.clicked += () =>
            {
                // 視点場ピン作成
                Debug.Log($"視点場ピン作成: {newViewpoint_Button.name}");
                snackbar.ShowMessage("マップをクリックして視点を登録して下さい");
                FocusAnalyzeSettingPanel();
                var elem = SetCurrentAnalyzeSettingPanel("New_Viewpoint");
                var heightValueTextField = elem.Q<TextField>("heightValueTextField");
                heightValueTextField.value = $"{1.5f}";

                lineOfSight.SetMode(LineOfSightType.viewPoint);
                PushElement(new_PointMenu);
                beforeElement = new_PointMenu;
            };
            var newLandmark_Button = new_PointMenu.Q<Button>("NewLandmark_Button");
            newLandmark_Button.clicked += () =>
            {
                // 眺望対象作成
                Debug.Log($"眺望対象ピン作成: {newLandmark_Button.name}");
                snackbar.ShowMessage("マップをクリックして眺望対象を登録して下さい");
                FocusAnalyzeSettingPanel();

                var elem = SetCurrentAnalyzeSettingPanel("New_Landmark");
                elem.Q<TextField>("heightValueTextField").value = $"0";

                lineOfSight.SetMode(LineOfSightType.landmark);
                PushElement(new_PointMenu);
                beforeElement = new_PointMenu;
            };
            var newAnalyze_Button = new_PointMenu.Q<Button>("NewAnalyze_Button");
            newAnalyze_Button.clicked += () =>
            {
                // 新規解析作成
                Debug.Log($"新規解析作成: {newLandmark_Button.name}");

                FocusAnalyzeSettingPanel();
                var panel = SetCurrentAnalyzeSettingPanel("New_Analyze_Select");
                SetNewAnalyzeSelectPanelButtonState(panel);

                PushElement(new_PointMenu);
                beforeElement = new_PointMenu;
            };
        }

        /// <summary>
        /// 視点場新規作成
        /// </summary>
        private void InitializeNew_Viewpoint()
        {
            var new_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Viewpoint");

            var backButton = new_Viewpoint.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                snackbar.Hide();
                viewPoint.OnDisable();
                new_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };

            var cancelButton = new_Viewpoint.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                snackbar.Hide();
                viewPoint.OnDisable();
                new_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
            var okButton = new_Viewpoint.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                var buttonName = viewPoint.CreatePoint();
                if (buttonName == "")
                {
                    return;
                }

                snackbar.Hide();
                CreateViewPointButton(buttonName);
                new_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
        }

        /// <summary>
        /// 見通し解析
        /// 解析したいタイプを選択して下さい。の所
        /// </summary>
        private void InitializeNew_Landmark()
        {
            var new_Landmark = analyzeSettingPanel.Q<VisualElement>("New_Landmark");

            var backButton = new_Landmark.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                snackbar.Hide();
                landmark.OnDisable();
                new_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };

            var cancelButton = new_Landmark.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                snackbar.Hide();
                landmark.OnDisable();
                new_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
            var okButton = new_Landmark.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                var buttonName = landmark.CreatePoint();
                if (buttonName == "")
                {
                    return;
                }
                snackbar.Hide();
                CreateLandmarkButton(buttonName);
                new_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
        }

        /// <summary>
        /// 解析したいタイプを選んで下さい
        /// 視点場 -> 眺望対象 解析作成
        /// </summary>
        private void InitializeNew_Analyze_Select()
        {
            var new_Analyze_Select = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Select");
            var new_Analyze_Landmark = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Landmark");
            var new_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Viewpoint");

            var backButton = new_Analyze_Select.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                new_Analyze_Select.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
            var selectButton_Landmark = new_Analyze_Select.Q<Button>("SelectButton_Landmark");

            selectButton_Landmark.clicked += () =>
            {
                // 眺望対象
                SetCurrentAnalyzeSettingPanel("New_Analyze_Landmark");
                lineOfSight.SetMode(LineOfSightType.analyzeLandmark);
                PushElement(new_Analyze_Select);
                beforeElement = new_Analyze_Select;
            };
            var selectButton_Viewpoint = new_Analyze_Select.Q<Button>("SelectButton_Viewpoint");
            selectButton_Viewpoint.clicked += () =>
            {
                // 視点場 -> 眺望対象
                SetCurrentAnalyzeSettingPanel("New_Analyze_Viewpoint");

                SetAnalyzeLandmarkTarget("選択されていません");
                SetAnalyzeViewpointTarget("選択されていません");
                SetAnalyzeViewingAngleTarget(0, 0);

                lineOfSight.SetMode(LineOfSightType.analyzeViewPoint);
                PushElement(new_Analyze_Select);
                beforeElement = new_Analyze_Select;
            };
        }

        private void InitializeNew_Analyze_Viewpoint()
        {
            var new_Analyze_Select = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Select");
            var new_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Viewpoint");
            var slider_Viewpoint = analyzeSettingPanel.Q<VisualElement>("Slider_Viewpoint");

            var backButton = new_Analyze_Viewpoint.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                var panel = SetCurrentAnalyzeSettingPanel("New_Analyze_Select");
                SetNewAnalyzeSelectPanelButtonState(panel);

                lineOfSight.SetMode(LineOfSightType.main);
                analyzeViewPoint.ClearLineOfSight();
                analyzeViewPoint.ClearSetMode();
            };

            var selectViewpointButton = new_Analyze_Viewpoint.Q<Button>("SelectViewpointButton");
            selectViewpointButton.clicked += () =>
            {
                snackbar.ShowMessage("リストから選択して下さい");
                ShowAnalyzeSettingPanel(false);
                viewPointList_View.Show(true);//viewPointListPanel.style.display = DisplayStyle.Flex;
                analyzeViewPoint.SetViewPoint();
                PushElement(new_Analyze_Viewpoint); // ?
                beforeElement = new_Analyze_Viewpoint;
            };

            var selectLandmarkButton = new_Analyze_Viewpoint.Q<Button>("SelectLandmarkButton");
            selectLandmarkButton.clicked += () =>
            {
                snackbar.ShowMessage("リストから選択して下さい");
                // analyzeSettingPanel.style.display = DisplayStyle.None;
                ShowAnalyzeSettingPanel(false);
                landmarkList_View.Show(true);// landMarkListPanel.style.display = DisplayStyle.Flex;
                analyzeViewPoint.SetLandMark();
                PushElement(new_Analyze_Viewpoint); // ?
                beforeElement = new_Analyze_Viewpoint;
            };

            var settingButton = new_Analyze_Viewpoint.Q<Button>("SettingButton");
            settingButton.clicked += () =>
            {
                // new_Analyze_Viewpoint.style.display = DisplayStyle.None;
                // slider_Viewpoint.style.display = DisplayStyle.Flex;
                SetCurrentAnalyzeSettingPanel("Slider_Viewpoint");
                analyzeViewPoint.SetAnalyzeRange();
                PushElement(new_Analyze_Viewpoint);
                beforeElement = new_Analyze_Viewpoint;
            };
            var cancelButton = new_Analyze_Viewpoint.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                // new_Analyze_Viewpoint.style.display = DisplayStyle.None;
                // new_Analyze_Select.style.display = DisplayStyle.Flex;
                var panel = SetCurrentAnalyzeSettingPanel("New_Analyze_Select");
                SetNewAnalyzeSelectPanelButtonState(panel);

                lineOfSight.SetMode(LineOfSightType.main);
                analyzeViewPoint.ClearLineOfSight();
                analyzeViewPoint.ClearSetMode();
            };
            var okButton = new_Analyze_Viewpoint.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                analyzeViewPoint.ClearLineOfSight();
                var analyzeViewPointElements = analyzeViewPoint.RegisterAnalyzeData();
                if (analyzeViewPointElements.Equals(AnalyzeViewPointElements.Empty))
                {
                    Debug.LogWarning($"analyzeViewPointElementに登録ができませんでした");
                    return;
                }
                CreateAnalyzeViewPointButton(analyzeViewPointElements);
                new_Analyze_Viewpoint.style.display = DisplayStyle.None;
                analyzeViewPoint.ClearSetMode();
                ViewDefaultPanels();
            };
        }

        /// <summary>
        /// 眺望視点解析
        /// </summary>
        private void InitializeNew_Analyze_Landmark()
        {
            var new_Analyze_Select = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Select");
            var new_Analyze_Landmark = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Landmark");
            var slider_Landmark = analyzeSettingPanel.Q<VisualElement>("Slider_Landmark");

            var backButton = new_Analyze_Landmark.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                new_Analyze_Landmark.style.display = DisplayStyle.None;
                new_Analyze_Select.style.display = DisplayStyle.Flex;

                lineOfSight.SetMode(LineOfSightType.main);
                analyzeLandmark.ClearLineOfSight();
                analyzeLandmark.ClearSetMode();
            };

            // 眺望対象 選択
            var selectLandmarkButton = new_Analyze_Landmark.Q<Button>("SelectLandmarkButton");
            selectLandmarkButton.clicked += () =>
            {
                ShowAnalyzeSettingPanel(false);// analyzeSettingPanel.style.display = DisplayStyle.None;
                landmarkList_View.Show(true);// landMarkListPanel.style.display = DisplayStyle.Flex;
                analyzeLandmark.SetLandMark();
                PushElement(new_Analyze_Landmark);
                beforeElement = new_Analyze_Landmark;
            };

            var settingButton = new_Analyze_Landmark.Q<Button>("SettingButton");
            // 視野角 設定
            settingButton.clicked += () =>
            {
                new_Analyze_Landmark.style.display = DisplayStyle.None;
                SetCurrentAnalyzeSettingPanel("Slider_Landmark"); // slider_Landmark.style.display = DisplayStyle.Flex;

                var landmarkName = new_Analyze_Landmark.Q<Label>("LandmarkName");

                analyzeLandmark.ClearSetMode();
                var result = analyzeLandmark.SetTargetLandmark(landmarkName.text);
                Debug.Log($"{landmarkName.text} result: {result}");
                analyzeLandmark.SetAnalyzeRange();
                PushElement(new_Analyze_Landmark);
                beforeElement = new_Analyze_Landmark;
            };

            var cancelButton = new_Analyze_Landmark.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                new_Analyze_Landmark.style.display = DisplayStyle.None;
                var panel = SetCurrentAnalyzeSettingPanel("New_Analyze_Select");//new_Analyze_Select.style.display = DisplayStyle.Flex;
                SetNewAnalyzeSelectPanelButtonState(panel);

                analyzeLandmark.ClearLineOfSight();
                analyzeLandmark.ClearSetMode();
            };
            var okButton = new_Analyze_Landmark.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                analyzeLandmark.ClearLineOfSight();
                var analyzeLandmarkElements = analyzeLandmark.RegisterAnalyzeData();
                if (analyzeLandmarkElements.Equals(AnalyzeLandmarkElements.Empty))
                {
                    return;
                }
                CreateAnalyzeLandmarkButton(analyzeLandmarkElements);
                new_Analyze_Landmark.style.display = DisplayStyle.None;
                analyzeLandmark.ClearSetMode();
                ViewDefaultPanels();
            };
        }

        private void InitializeSlider_Viewpoint()
        {
            var slider_Viewpoint = analyzeSettingPanel.Q<VisualElement>("Slider_Viewpoint");

            var backButton = slider_Viewpoint.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                slider_Viewpoint.style.display = DisplayStyle.None;
                var elem = PopElement();
                if (elem != null)
                {
                    elem.style.display = DisplayStyle.Flex;
                }
                beforeElement.style.display = DisplayStyle.Flex;
            };
            var okButton = slider_Viewpoint.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                var vslider = slider_Viewpoint.Q<SliderInt>("VerticalSlider");
                var hslider = slider_Viewpoint.Q<SliderInt>("HorizontalSlider");

                var spanSlider = slider_Viewpoint.Q<SliderInt>("RaySpanSlider");

                SetAnalyzeViewingAngleTarget(hslider.value, vslider.value);

                slider_Viewpoint.style.display = DisplayStyle.None;
                var elem = PopElement();
                if (elem != null)
                {
                    elem.style.display = DisplayStyle.Flex;
                }
                beforeElement.style.display = DisplayStyle.Flex;
            };
        }
        private void InitializeSlider_Landmark()
        {
            var slider_Landmark = analyzeSettingPanel.Q<VisualElement>("Slider_Landmark");

            var backButton = slider_Landmark.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                slider_Landmark.style.display = DisplayStyle.None;
                var elem = PopElement();
                if (elem != null)
                {
                    elem.style.display = DisplayStyle.Flex;
                }
                beforeElement.style.display = DisplayStyle.Flex;
            };
            var okButton = slider_Landmark.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                var uslider = slider_Landmark.Q<SliderInt>("UpperSlider");
                var dslider = slider_Landmark.Q<SliderInt>("DownSlider");

                slider_Landmark.style.display = DisplayStyle.None;
                SetAnalyzeLandmarkViewingAngleTarget(uslider.value, dslider.value);
                var elem = PopElement();
                if (elem != null)
                {
                    elem.style.display = DisplayStyle.Flex;
                }
                beforeElement.style.display = DisplayStyle.Flex;
            };
        }
        private void InitializeEdit_Viewpoint()
        {
            var edit_Viewpoint = analyzeSettingPanel.Q<VisualElement>("Edit_Viewpoint");

            var backButton = edit_Viewpoint.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                viewPoint.RestorePoint();
                edit_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();

            };
            var cancelButton = edit_Viewpoint.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                viewPoint.RestorePoint();
                edit_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
            var deleteButton = edit_Viewpoint.Q<Button>("deleteButton");
            deleteButton.clicked += () =>
            {
                DeletePoint(LineOfSightType.viewPoint);
            };
            var okButton = edit_Viewpoint.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                var editButtonName = viewPoint.EditPoint();
                var beforeName = editButtonName.beforeName;
                var afterName = editButtonName.afterName;
                if (afterName == "")
                {
                    return;
                }
                // 変更前のボタンの削除
                viewPointList_View.RemoveButton(beforeName);
                var removeButton = entryViewpointButton.FirstOrDefault(x => x.Value == beforeName).Key;
                if (removeButton != null)
                {
                    entryViewpointButton.Remove(removeButton);
                }

                // 変更後のボタンの生成
                CreateViewPointButton(afterName);
                // UIの変更
                edit_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };



        }
        private void InitializeEdit_Landmark()
        {
            var edit_Landmark = analyzeSettingPanel.Q<VisualElement>("Edit_Landmark");

            var backButton = edit_Landmark.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                landmark.RestorePoint();
                edit_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };

            var cancelButton = edit_Landmark.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                landmark.RestorePoint();
                edit_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };

            var deleteButton = edit_Landmark.Q<Button>("deleteButton");
            deleteButton.clicked += () =>
            {
                DeletePoint(LineOfSightType.landmark);
            };
            var okButton = edit_Landmark.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                var editButtonName = landmark.EditPoint();
                var beforeName = editButtonName.beforeName;
                var afterName = editButtonName.afterName;
                if (afterName == "")
                {
                    return;
                }
                // 変更前のボタンの削除
                landmarkList_View.RemoveButton(beforeName);
                var removeButton = entryViewpointButton.FirstOrDefault(x => x.Value == beforeName).Key;
                if (removeButton != null)
                {
                    entryViewpointButton.Remove(removeButton);
                }
                // 変更後のボタンの生成
                CreateLandmarkButton(afterName);
                // UIの変更
                edit_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
        }
        private void InitializeEdit_Analyze_Viewpoint()
        {
            var edit_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("Edit_Analyze_Viewpoint");
            var slider_Viewpoint = analyzeSettingPanel.Q<VisualElement>("Slider_Viewpoint");

            var backButton = edit_Analyze_Viewpoint.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                analyzeViewPoint.ClearLineOfSight();
                edit_Analyze_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
            var selectViewpointButton = edit_Analyze_Viewpoint.Q<Button>("SelectViewpointButton");
            selectViewpointButton.clicked += () =>
            {
                ShowAnalyzeSettingPanel(false);
                viewPointList_View.Show(true);
                analyzeViewPoint.SetViewPoint();
                beforeElement = edit_Analyze_Viewpoint;
            };
            var selectLandmarkButton = edit_Analyze_Viewpoint.Q<Button>("SelectLandmarkButton");
            selectLandmarkButton.clicked += () =>
            {
                // analyzeSettingPanel.style.display = DisplayStyle.None;
                // landMarkListPanel.style.display = DisplayStyle.Flex;
                ShowAnalyzeSettingPanel(false);
                landmarkList_View.Show(true);
                analyzeViewPoint.SetLandMark();
                beforeElement = edit_Analyze_Viewpoint;
            };
            var settingButton = edit_Analyze_Viewpoint.Q<Button>("SettingButton");
            settingButton.clicked += () =>
            {
                edit_Analyze_Viewpoint.style.display = DisplayStyle.None;
                slider_Viewpoint.style.display = DisplayStyle.Flex;
                analyzeViewPoint.SetAnalyzeRange();
                beforeElement = edit_Analyze_Viewpoint;
            };
            var deleteButton = edit_Analyze_Viewpoint.Q<Button>("deleteButton");
            deleteButton.clicked += () =>
            {
                DeleteAnalyzeViewPoint();
            };
            var cancelButton = edit_Analyze_Viewpoint.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                analyzeViewPoint.ClearLineOfSight();
                edit_Analyze_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
            var okButton = edit_Analyze_Viewpoint.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                var analyzeViewPointElements = analyzeViewPoint.EditAnalyzeData();
                var deleteButtonName = analyzeViewPointElements.deleteButtonName;
                var analyzeViewPointData = analyzeViewPointElements.analyzeViewPointData;
                RemoveAnalyzeListButton(deleteButtonName);

                if (analyzeViewPointData.Equals(AnalyzeViewPointElements.Empty))
                {
                    Debug.LogWarning($"analyzeViewPointDataが空です");
                    return;
                }
                CreateAnalyzeViewPointButton(analyzeViewPointData);
                edit_Analyze_Viewpoint.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
        }
        private void InitializeEdit_Analyze_Landmark()
        {
            var edit_Analyze_Landmark = analyzeSettingPanel.Q<VisualElement>("Edit_Analyze_Landmark");
            var slider_Landmark = analyzeSettingPanel.Q<VisualElement>("Slider_Landmark");

            var backButton = edit_Analyze_Landmark.Q<Button>("BackButton");
            backButton.clicked += () =>
            {
                analyzeLandmark.ClearLineOfSight();
                edit_Analyze_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
            var selectLandmarkButton = edit_Analyze_Landmark.Q<Button>("SelectLandmarkButton");
            selectLandmarkButton.clicked += () =>
            {
                // analyzeSettingPanel.style.display = DisplayStyle.None;
                // landMarkListPanel.style.display = DisplayStyle.Flex;
                ShowAnalyzeSettingPanel(false);
                landmarkList_View.Show(true);
                analyzeLandmark.SetLandMark();
                PushElement(edit_Analyze_Landmark);
                beforeElement = edit_Analyze_Landmark;
            };
            var settingButton = edit_Analyze_Landmark.Q<Button>("SettingButton");
            settingButton.clicked += () =>
            {
                edit_Analyze_Landmark.style.display = DisplayStyle.None;
                slider_Landmark.style.display = DisplayStyle.Flex;

                var landmarkNameLabel = edit_Analyze_Landmark.Q<Label>("LandmarkName");

                var result = analyzeLandmark.SetTargetLandmark(landmarkNameLabel.text);
                Debug.Log($"{landmarkNameLabel.text} result: {result}");
                analyzeLandmark.SetAnalyzeRange();
                PushElement(edit_Analyze_Landmark);
                beforeElement = edit_Analyze_Landmark;
            };
            var deleteButton = edit_Analyze_Landmark.Q<Button>("deleteButton");
            deleteButton.clicked += () =>
            {
                DeleteAnalyzeLandmark();
            };
            var cancelButton = edit_Analyze_Landmark.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                analyzeLandmark.ClearLineOfSight();
                edit_Analyze_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };

            // 解析情報追加
            var okButton = edit_Analyze_Landmark.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                var analyzeLandmarkElements = analyzeLandmark.EditAnalyzeData();
                var deleteButtonName = analyzeLandmarkElements.deleteButtonName;
                var analyzeLandmarkData = analyzeLandmarkElements.analyzeLandmarkData;

                RemoveAnalyzeListButton(deleteButtonName);
                if (analyzeLandmarkData.Equals(AnalyzeLandmarkElements.Empty))
                {
                    Debug.LogWarning($"analyzeLandmarkDataは空の様です");
                    return;
                }
                CreateAnalyzeLandmarkButton(analyzeLandmarkData);
                edit_Analyze_Landmark.style.display = DisplayStyle.None;
                ViewDefaultPanels();
            };
        }
        public void CreateViewPointButton(string buttonName)
        {
            var list_ViewPoint = new UIDocumentFactory().CreateWithUxmlName("List_ViewPoint");
            var newButton = list_ViewPoint.Q<Button>("List");
            var label = list_ViewPoint.Q<Label>("Label");

            //newButton.name = buttonName;
            label.text = buttonName;
            newButton.clicked += () =>
            {
                Debug.Log($"viewpoint {buttonName} clicked");
                snackbar.Hide();
                // 選択する場面に応じて処理を分ける

                // モードが2つあって
                // analyzeSettingPanelが
                //  - New_PointMenuの時
                //  - New_Analyze_ViewPointの時
                if (analyzeSettingPanel.style.display == DisplayStyle.Flex)
                {
                    if (!viewPoint.CanEdit(buttonName))
                    {
                        snackbar.ShowMessage(SnackBarUI.NotEditWarning);
                        return;
                    }

                    //  - New_PointMenuの時
                    lineOfSight.SetMode(LineOfSightType.viewPoint);
                    viewPoint.ButtonAction(buttonName);
                    viewPoint.InitializeEditPoint();

                    SetCurrentAnalyzeSettingPanel("Edit_Viewpoint");
                }
                else
                {
                    //  - New_Analyze_ViewPointの時
                    lineOfSight.SetMode(LineOfSightType.main);
                    // 選択したButtonを格納
                    SetAnalyzeViewpointTarget(entryViewpointButton[newButton]);//SetAnalyzeViewpointTarget(newButton.name);

                    // パネルの表示を元に戻す
                    SetCurrentAnalyzeSettingPanel("New_Analyze_Viewpoint");
                    ShowAnalyzeSettingPanel(true);
                }
                FocusAnalyzeSettingPanel();

            };
            entryViewpointButton.Add(newButton, buttonName);
            viewPointList_View.AddButton(newButton, buttonName);
        }
        public void CreateLandmarkButton(string buttonName)
        {

            var list_Landmark = new UIDocumentFactory().CreateWithUxmlName("List_Landmark");
            var newButton = list_Landmark.Q<Button>("List");
            var label = list_Landmark.Q<Label>("Label");
            label.text = buttonName;
            //newButton.name = buttonName;
            newButton.clicked += () =>
            {
                snackbar.Hide();
                // ここは3つ
                // analyzeSettingPanelが表示されている時

                // 表示されていない時でNew_Analyze_Viewpointの時
                // 表示されていない時でNew_Analyze_Landmarkの時
                Debug.Log($"landmark {buttonName} clicked");
                if (analyzeSettingPanel.style.display == DisplayStyle.Flex)
                {
                    if (!landmark.CanEdit(buttonName))
                    {
                        snackbar.ShowMessage(SnackBarUI.NotEditWarning);
                        return;
                    }
                    lineOfSight.SetMode(LineOfSightType.landmark);
                    landmark.ButtonAction(buttonName);
                    landmark.InitializeEditPoint();
                    SetCurrentAnalyzeSettingPanel("Edit_Landmark");
                }
                else if (beforeElement.name.Contains("Landmark"))
                {
                    // 視点場/眺望対象 解析時
                    lineOfSight.SetMode(LineOfSightType.main);
                    SetCurrentAnalyzeSettingPanel("New_Analyze_Landmark");
                    SetAnalyzeLandmark_LandmarkTarget(entryLandmarkButton[newButton]); //newButton.name);
                }
                else
                {
                    // 眺望対象解析時
                    lineOfSight.SetMode(LineOfSightType.main);
                    SetCurrentAnalyzeSettingPanel("New_Analyze_Viewpoint");
                    SetAnalyzeLandmarkTarget(entryLandmarkButton[newButton]);//newButton.name);
                }

                // analyzeSettingPanel.style.display = DisplayStyle.Flex;
                ShowAnalyzeSettingPanel(true);
                FocusAnalyzeSettingPanel();
            };
            entryLandmarkButton.Add(newButton, buttonName);
            landmarkList_View.AddButton(newButton, buttonName);
        }

        /// <summary>
        /// 視点場 -> 眺望解析ボタン作成
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="buttonName"></param>
        public void CreateAnalyzeViewPointButton(AnalyzeViewPointElements elements, string buttonName = "")
        {
            var list_Analyze = new UIDocumentFactory().CreateWithUxmlName("List_Analyze_Viewpoint");

            var landmarkLabel = list_Analyze.Q<Label>("LandmarkName");
            var viewPointLabel = list_Analyze.Q<Label>("ViewpointName");

            var upwardLabel = list_Analyze.Q<Label>("Upward");
            var underLabel = list_Analyze.Q<Label>("Under");

            var newButton = list_Analyze.Q<Button>("List");

            landmarkLabel.text = elements.endPosName;
            viewPointLabel.text = elements.startPosName;

            upwardLabel.text = elements.rangeHeight.ToString();
            underLabel.text = elements.rangeWidth.ToString();

            if (string.IsNullOrEmpty(buttonName))
            {
                buttonName = elements.startPosName + elements.endPosName + elements.rangeWidth + elements.rangeHeight;
            }

            newButton.clicked += () =>
            {
                if (!analyzeViewPoint.CanEdit(buttonName))
                {
                    snackbar.ShowMessage(SnackBarUI.NotEditWarning);
                    return;
                }

                lineOfSight.SetMode(LineOfSightType.analyzeViewPoint);
                analyzeViewPoint.ButtonAction(elements);
                // UI
                FocusAnalyzeSettingPanel();
                analyzeList_View.Show(true);
                //var new_PointMenu = analyzeSettingPanel.Q<VisualElement>("New_PointMenu");
                //new_PointMenu.style.display = DisplayStyle.None;
                var edit_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("Edit_Analyze_Viewpoint");
                SetCurrentAnalyzeSettingPanel("Edit_Analyze_Viewpoint");
                edit_Analyze_Viewpoint.Q<Label>("ViewpointName").text = elements.startPosName;
                edit_Analyze_Viewpoint.Q<Label>("LandmarkName").text = elements.endPosName;
            };

            analyzeList_View.AddButton(newButton, buttonName);
            entryAnalyzeButtonFromButton.Add(newButton, buttonName);
            //entryAnalyzeButtonFromName.Add(buttonName, newButton);
        }

        /// <summary>
        /// 眺望対象解析ボタン作成
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="buttonName"></param>
        public void CreateAnalyzeLandmarkButton(AnalyzeLandmarkElements elements, string buttonName = "")
        {
            var list_Analyze = new UIDocumentFactory().CreateWithUxmlName("List_Analyze_Landmark");
            var newButton = list_Analyze.Q<Button>("List");

            if (string.IsNullOrEmpty(buttonName))
            {
                buttonName = elements.startPosName + elements.rangeUp + elements.rangeDown;
            }

            var landmarkNameLabel = list_Analyze.Q<Label>("LandmarkName");
            var upwardLabel = list_Analyze.Q<Label>("Upward");
            var underLabel = list_Analyze.Q<Label>("Under");

            upwardLabel.text = elements.rangeUp.ToString();
            underLabel.text = elements.rangeDown.ToString();


            landmarkNameLabel.text = elements.startPosName;
            newButton.clicked += () =>
            {
                if (!analyzeLandmark.CanEdit(buttonName))
                {
                    snackbar.ShowMessage(SnackBarUI.NotEditWarning);
                    return;
                }

                lineOfSight.SetMode(LineOfSightType.analyzeLandmark);
                analyzeLandmark.ButtonAction(elements);
                // UI
                FocusAnalyzeSettingPanel();
                analyzeList_View.Show(true);
                var edit_Analyze_Landmark = analyzeSettingPanel.Q<VisualElement>("Edit_Analyze_Landmark");
                SetCurrentAnalyzeSettingPanel("Edit_Analyze_Landmark");// edit_Analyze_Landmark.style.display = DisplayStyle.Flex;
                edit_Analyze_Landmark.Q<Label>("LandmarkName").text = elements.startPosName;
            };
            analyzeList_View.AddButton(newButton, buttonName);
            entryAnalyzeButtonFromButton.Add(newButton, buttonName);
        }

        public void RemoveAnalyzeListButton(string keyName)
        {
            analyzeList_View.RemoveButton(keyName);

            var button = entryAnalyzeButtonFromButton.Where(x => x.Value == keyName).FirstOrDefault();

            if (button.Key != null)
            {
                entryAnalyzeButtonFromButton.Remove(button.Key);
            }

            // var button = entryAnalyzeButtonFromButton.FirstOrDefault(b => b.Value == keyName).Key;
            // if (button != null)
            // {
            //     entryAnalyzeButtonFromButton.Remove(button);
            // }
            //var scrollView = analyzeListPanel.Q<ScrollView>("Panel");
            //var button = scrollView.Q<Button>(keyName);
            //scrollView.Remove(button);
        }
        public void ClearButton(LineOfSightType lineOfSightType)
        {
            ListView scrollView = null;
            if (lineOfSightType == LineOfSightType.viewPoint)
            {
                scrollView = viewPointList_View;
            }
            else if (lineOfSightType == LineOfSightType.landmark)
            {
                scrollView = landmarkList_View;
            }
            else if (lineOfSightType == LineOfSightType.analyzeViewPoint || lineOfSightType == LineOfSightType.analyzeLandmark)
            {
                scrollView = analyzeList_View;
            }

            scrollView.ClearList();
        }
        private void FocusAnalyzeSettingPanel()
        {
            // FIXME: viewpontlist/landmarklist/analyzeListのタイトルも消す

            viewPointList_View.Show(false);
            landmarkList_View.Show(false);
            analyzeList_View.Show(false);
        }

        private void ViewDefaultPanels()
        {
            SetCurrentAnalyzeSettingPanel("New_PointMenu");
            viewPointList_View.Show(true);
            landmarkList_View.Show(true);
            analyzeList_View.Show(true);
            lineOfSight.SetMode(LineOfSightType.main);
        }

        /// <summary>
        /// 視点場 / 眺望対象の削除
        /// </summary>
        public void DeletePoint(LineOfSightType type, string pointName = "")
        {
            VisualElement viewpoint = null;
            (string deleteButtonName, List<string> removedAnalyzeKeyNameList) deleteData = (null, new List<string>() { });
            switch (type)
            {
                case LineOfSightType.viewPoint:
                    deleteData = string.IsNullOrEmpty(pointName) ? viewPoint.DeletePoint() : viewPoint.DeletePoint(pointName);
                    viewPointList_View.RemoveButton(deleteData.deleteButtonName);
                    viewpoint = analyzeSettingPanel.Q<VisualElement>("Edit_Viewpoint");
                    break;
                case LineOfSightType.landmark:
                    deleteData = string.IsNullOrEmpty(pointName) ? landmark.DeletePoint() : landmark.DeletePoint(pointName);
                    landmarkList_View.RemoveButton(deleteData.deleteButtonName);
                    viewpoint = analyzeSettingPanel.Q<VisualElement>("Edit_Landmark");
                    break;
                case LineOfSightType.main:
                case LineOfSightType.analyzeViewPoint:
                case LineOfSightType.analyzeLandmark:
                    return;
            }

            // ボタンの削除
            var removeButton = entryViewpointButton.FirstOrDefault(x => x.Value == deleteData.deleteButtonName).Key;
            if (removeButton != null)
            {
                entryViewpointButton.Remove(removeButton);
            }
            foreach (string keyName in deleteData.removedAnalyzeKeyNameList)
            {
                RemoveAnalyzeListButton(keyName);
            }
            // UIの変更
            viewpoint.style.display = DisplayStyle.None;
            snackbar.ShowMessage("削除しました");
            ViewDefaultPanels();
        }

        /// <summary>
        /// 解析視点の削除
        /// </summary>
        public void DeleteAnalyzeViewPoint(string keyName = "")
        {
            var edit_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("Edit_Analyze_Viewpoint");

            if (string.IsNullOrEmpty(keyName))
            {
                keyName = analyzeViewPoint.DeleteAnalyzeData();
            }
            RemoveAnalyzeListButton(keyName);
            snackbar.ShowMessage("削除しました");
            edit_Analyze_Viewpoint.style.display = DisplayStyle.None;
            ViewDefaultPanels();
        }

        /// <summary>
        /// 解析眺望対象の削除
        /// </summary>
        public void DeleteAnalyzeLandmark(string keyName = "")
        {
            if (string.IsNullOrEmpty(keyName))
            {
                keyName = analyzeLandmark.DeleteAnalyzeData();
            }
            RemoveAnalyzeListButton(keyName);
            snackbar.ShowMessage("削除しました");
            ViewDefaultPanels();
        }

        /// <summary>
        /// 編集画面が表示されていたら、閉じる
        /// </summary>
        public bool TryCloseEditView(LineOfSightType type)
        {
            switch (type)
            {
                case LineOfSightType.viewPoint:
                    {
                        var editViewpoint = analyzeSettingPanel.Q<VisualElement>("Edit_Viewpoint");
                        if (editViewpoint.style.display == DisplayStyle.Flex)
                        {
                            viewPoint.RestorePoint();
                            editViewpoint.style.display = DisplayStyle.None;
                            ViewDefaultPanels();
                            return true;
                        }
                        break;
                    }
                case LineOfSightType.landmark:
                    {
                        var editLandmark = analyzeSettingPanel.Q<VisualElement>("Edit_Landmark");
                        if (editLandmark.style.display == DisplayStyle.Flex)
                        {
                            landmark.RestorePoint();
                            editLandmark.style.display = DisplayStyle.None;
                            ViewDefaultPanels();
                            return true;
                        }
                    }
                    break;
                case LineOfSightType.analyzeViewPoint:
                    {
                        var editAnalyzeViewpoint = analyzeSettingPanel.Q<VisualElement>("Edit_Analyze_Viewpoint");
                        if (editAnalyzeViewpoint.style.display == DisplayStyle.Flex)
                        {
                            analyzeViewPoint.ClearLineOfSight();
                            editAnalyzeViewpoint.style.display = DisplayStyle.None;
                            ViewDefaultPanels();
                            return true;
                        }
                    }
                    break;
                case LineOfSightType.analyzeLandmark:
                    {
                        var editAnalyzeLandmark = analyzeSettingPanel.Q<VisualElement>("Edit_Analyze_Landmark");
                        if (editAnalyzeLandmark.style.display == DisplayStyle.Flex)
                        {
                            analyzeLandmark.ClearLineOfSight();
                            editAnalyzeLandmark.style.display = DisplayStyle.None;
                            ViewDefaultPanels();
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public void Update()
        {
            snackbar.Update();

            if (analyzeSettingPanel.style.display == DisplayStyle.None)
            {
                HideSnackbar();
            }
        }
    }
}
