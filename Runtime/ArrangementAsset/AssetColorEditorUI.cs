using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Wacton.Unicolour;

namespace Landscape2.Runtime
{
    public class AssetColorEditorUI
    {
        private const string UIColorPreview = "AreaPlanningColor";
        private const string UIHueNameDropdown = "HueList";
        private const string UIApplyButton = "OKButton";
        private const string UIResetButton = "CancelButton";
        private const string UIColSelectButton = "ColorSelectToggle";
        private const string UIColInputButton = "ColorInputToggle";
        private const string UIHueValueDoubleField = "Hue";
        private const string UIValueDoubleField = "Value";
        private const string UIChromaDoubleField = "Chroma";

        private readonly AssetColorEditor assetColorEditor = new();
        private VisualElement uiElement;
        private ColorEditorUI colorEditorUI;
        private VisualElement colorPreview;
        private DropdownField hueNameDropdown;
        private DoubleField hueValueDoubleField;
        private DoubleField valueDoubleField;
        private DoubleField chromaDoubleField;
        private Button applyButton;
        private Button resetButton;
        private RadioButton colorSelectToggle;
        private RadioButton colorInputToggle;
        private Label munsellValueLabel;
        private bool bColorEditToggle = false;
        private bool suppressCallback = false;

        // Drag state
        private bool dragging;
        private Vector2 dragOffsetInternal;

        private Color munsellColor;
        private string munsellValueText = "";


        public AssetColorEditorUI(VisualElement assetUIElement)
        {
            uiElement = assetUIElement.Q<VisualElement>("ColorEditGroup");
            colorPreview = uiElement.Q<VisualElement>(UIColorPreview);
            hueNameDropdown = uiElement.Q<DropdownField>(UIHueNameDropdown);
            hueValueDoubleField = uiElement.Q<DoubleField>(UIHueValueDoubleField);
            valueDoubleField = uiElement.Q<DoubleField>(UIValueDoubleField);
            chromaDoubleField = uiElement.Q<DoubleField>(UIChromaDoubleField);
            applyButton = uiElement.Q<Button>(UIApplyButton);
            resetButton = uiElement.Q<Button>(UIResetButton);
            colorSelectToggle = uiElement.Q<RadioButton>(UIColSelectButton);
            colorInputToggle = uiElement.Q<RadioButton>(UIColInputButton);
            munsellValueLabel = uiElement.Q<Label>("MunsellValue");
            if (uiElement != null)
            {
                var initial = assetColorEditor?.PreviewColor ?? Color.white;
                munsellColor = initial;
                colorEditorUI = new ColorEditorUI(uiElement, initial);
                // ColorEditorUI の色変更イベント購読（プレビュー & ドロップダウン同期）
                colorEditorUI.OnColorEdited += OnEditorColorChanged;
            }

            InitializeMunsellDropdowns();

            RegisterButtonHandlers();

            if (munsellValueLabel != null)
            {
                munsellValueText = munsellValueLabel.text;
            }
            ColorEditToggle(false);
            Hide();
        }

        public void SetEditTarget(GameObject target)
        {
            assetColorEditor.SetTarget(target);
            ConvertAppliedColorToMunsel();
        }

        public void Show()
        {
            uiElement.style.display = DisplayStyle.Flex;

            // プレビュー色（null の場合は白）
            var preview = assetColorEditor.PreviewColor ?? Color.white;
            SyncUI(preview, applyToEditor: true, suppressEvent: true);
            if (bColorEditToggle)
            {
                SetMansellTextInput();
            }
        }

        public void Hide()
        {
            uiElement.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// マンセル色票に対応する各 Dropdown の選択肢を初期化する。
        /// </summary>
        private void InitializeMunsellDropdowns()
        {
            // Hue Value
            hueValueDoubleField.value = 0.1;
            chromaDoubleField.value = 0.0;

            // Hue Name
            hueNameDropdown.choices = new List<string>(MunsellColorUtility.HueNames);
            hueNameDropdown.choices.Add("N"); // 無彩色用
            hueNameDropdown.value = "N";

            // Value (distinct, descending)
            valueDoubleField.value = 9.0;

            // Chroma (distinct, ascending)
            chromaDoubleField.value = 0.0;

            // 値変更イベント登録 (四つのいずれか変更で再計算)
            hueNameDropdown.RegisterValueChangedCallback(_ =>
            {
                if(suppressCallback)
                    return;
                OnMunsellDropdownChanged();
            });
            hueValueDoubleField.RegisterValueChangedCallback(_ =>
            {
                if (hueValueDoubleField.value < 0.1)
                {
                    hueValueDoubleField.value = 0.1;
                }
                else if (hueValueDoubleField.value > 10.0)
                {
                    hueValueDoubleField.value = 10.0;
                }
                hueValueDoubleField.value = Mathf.Round((float)(hueValueDoubleField.value * 10.0)) / 10.0;
                if (suppressCallback)
                    return;
                OnMunsellDropdownChanged();
            });
            valueDoubleField.RegisterValueChangedCallback(_ =>
            {
                if (valueDoubleField.value < 0.0)
                {
                    valueDoubleField.value = 0.0;
                }
                else if (valueDoubleField.value > 10.0)
                {
                    valueDoubleField.value = 10.0;
                }
                valueDoubleField.value = Mathf.Round((float)(valueDoubleField.value * 10.0)) / 10.0;
                if (suppressCallback)
                    return;
                OnMunsellDropdownChanged();
            });
            chromaDoubleField.RegisterValueChangedCallback(_ =>
            {
                if (chromaDoubleField.value < 0.0)
                {
                    chromaDoubleField.value = 0.0;
                }
                else if (chromaDoubleField.value > 26.0)
                {
                    chromaDoubleField.value = 26.0;
                }
                chromaDoubleField.value = Mathf.Round((float)(chromaDoubleField.value * 10.0)) / 10.0;
                if (suppressCallback)
                    return;
                OnMunsellDropdownChanged();
            });
        }

        /// <summary>
        /// ColorEditorUI からの色変更時ハンドラ
        /// </summary>
        private void OnEditorColorChanged(Color color)
        {
            // ライブプレビューとして AssetColorEditor へ反映
            assetColorEditor?.UpdatePreviewColor(color);
            // 外部起点なので Editor 側へ再適用不要 (RGB スライダーが既に最新)
            SyncUI(color, applyToEditor: false, suppressEvent: true);
            if (!bColorEditToggle)
            {
                munsellColor = color;
                if (munsellValueLabel != null)
                {
                    munsellValueText = munsellValueLabel.text;
                }
            }
        }

        /// <summary>
        /// プレビュー / Dropdown / (必要なら) ColorEditorUI を同期する共通処理
        /// </summary>
        /// <param name="color">同期したい色</param>
        /// <param name="applyToEditor">ColorEditorUI.ApplyColor を呼ぶか</param>
        /// <param name="suppressEvent">ApplyColor 時にイベント抑制するか</param>
        private void SyncUI(Color color, bool applyToEditor, bool suppressEvent)
        {
            colorPreview.style.backgroundColor = color;
            if (applyToEditor)
            {
                colorEditorUI?.ApplyColor(color, suppressEvent);
                munsellColor = color;
                if (munsellValueLabel != null)
                {
                    munsellValueText = munsellValueLabel.text;
                }
            }
        }

        /// <summary>
        /// ボタンイベント登録。
        /// </summary>
        private void RegisterButtonHandlers()
        {
            if (applyButton != null)
            {
                applyButton.clicked += () =>
                {
                    if (bColorEditToggle)
                    {
                        bool bOutGamut = false;
                        var hn = hueNameDropdown.value;
                        var hv = hueValueDoubleField.value;
                        var v = valueDoubleField.value;
                        var c = chromaDoubleField.value;

                        if(hn == "N")
                        {
                            hv = 0.1;
                            c = 0.0;
                            suppressCallback = true;
                            hueValueDoubleField.value = hv;
                            chromaDoubleField.value = c;
                            suppressCallback = false;
                        }
                        var unicolour = new Unicolour(ColourSpace.Munsell,
                            Hue.FromMunsell(hv, hn),
                            v, c
                        );
                        if (hn != "N" && !unicolour.IsInRgbGamut)
                        {
                            bOutGamut = true;
                            unicolour = unicolour.MapToRgbGamut();
                        }
                        var color = new Color((float)unicolour.Rgb.R, (float)unicolour.Rgb.G, (float)unicolour.Rgb.B);
                        if (bOutGamut)
                        {
                            suppressCallback = true;
                            var newMunsell = ConvertColorToMunsell(color);
                            (hv, hn) = newMunsell.HueNotation;
                            v = newMunsell.V;
                            c = newMunsell.C;
                            hueNameDropdown.value = hn;
                            hueValueDoubleField.value = hv;
                            valueDoubleField.value = v;
                            chromaDoubleField.value = c;
                            suppressCallback = false;

                            assetColorEditor?.UpdatePreviewColor(color);
                            SyncUI(color, applyToEditor: false, suppressEvent: true);
                        }
                    }
                    assetColorEditor?.CommitPreview();
                    var applied = assetColorEditor?.AppliedColor ?? Color.white;
                    // コミット後に UI を適用色へ (外部変更なしなのでイベント抑制)
                    SyncUI(applied, applyToEditor: !bColorEditToggle, suppressEvent: true);
                    if (bColorEditToggle)
                    {
                        SetMansellTextInput();
                    }
                };
            }
            if(resetButton != null)
            {
                // 色をリセット
                resetButton.clicked += () =>
                {
                    var resetColor = Color.white;
                    assetColorEditor?.UpdatePreviewColor(null);
                    assetColorEditor?.CommitPreview();
                    SyncUI(resetColor, applyToEditor: true, suppressEvent: true);
                    suppressCallback = true;
                    hueNameDropdown.value = "N";
                    hueValueDoubleField.value = 0.1;
                    valueDoubleField.value = 10.0;
                    chromaDoubleField.value = 0.0;
                    suppressCallback = false;
                    if (bColorEditToggle)
                    {
                        SetMansellTextInput();
                    }
                };
            }

            if(colorSelectToggle != null)
            {
                colorSelectToggle.RegisterCallback<ClickEvent>(evt =>
                {
                    if (!bColorEditToggle)
                        return;
                    ColorEditToggle(false);
                });
            }
            if(colorInputToggle != null)
            {
                colorInputToggle.RegisterCallback<ClickEvent>(evt =>
                {
                    if(bColorEditToggle)
                        return;
                    ColorEditToggle(true);
                });
            }
        }

        /// <summary>
        /// ドロップダウン (HueValue / HueName / Value / Chrosma) のいずれかが変更された時に最も近い / 該当するマンセル色を検索し、
        /// プレビュー・ColorEditorUI・ターゲットへ反映する。
        /// </summary>
        private void OnMunsellDropdownChanged()
        {
            if (!bColorEditToggle)
                return;

            if (hueNameDropdown == null || hueValueDoubleField == null || valueDoubleField == null || chromaDoubleField == null) return;

            // 入力取得
            var hn = hueNameDropdown.value;
            var hv = hueValueDoubleField.value;
            var v = valueDoubleField.value;
            var c = chromaDoubleField.value;
            var unicolour = new Unicolour(ColourSpace.Munsell,
                Hue.FromMunsell(hv, hn),
                v,c
            );

            if (hn != "N" && !unicolour.IsInRgbGamut)
            {
                unicolour = unicolour.MapToRgbGamut();
            }
            var color = new Color((float)unicolour.Rgb.R, (float)unicolour.Rgb.G, (float)unicolour.Rgb.B);
            SetMansellTextInput();

            // ライブプレビュー & UI 同期 (ColorEditorUI 側へ適用、イベント抑制 true でループ防止)
            assetColorEditor?.UpdatePreviewColor(color);
            SyncUI(color, applyToEditor: false, suppressEvent: true);
        }

        private void ColorEditToggle(bool inputMode)
        {
            bColorEditToggle = inputMode;
            var inputGroups = uiElement.Q<VisualElement>("ColorInputGroup");
            var selectGroups = uiElement.Q<VisualElement>("ColorSelectGroup");
            if (inputMode)
            {
                if (inputGroups != null)
                {
                    inputGroups.style.display = DisplayStyle.Flex;
                }
                if (selectGroups != null)
                {
                    selectGroups.style.display = DisplayStyle.None;
                }
                OnMunsellDropdownChanged();
            }
            else
            {
                munsellValueLabel.text = munsellValueText;
                if (inputGroups != null)
                {
                    inputGroups.style.display = DisplayStyle.None;
                }
                if (selectGroups != null)
                {
                    selectGroups.style.display = DisplayStyle.Flex;
                }
                OnEditorColorChanged(munsellColor);
            }
        }

        private void ConvertAppliedColorToMunsel()
        {
            if (assetColorEditor == null) return;
            var color = assetColorEditor?.AppliedColor ?? Color.white;
            var munsell = ConvertColorToMunsell(color);
            (var hv, var hn) = munsell.HueNotation;
            var v = munsell.V;
            var c = munsell.C;
            if (c < 0.1)
            {
                hn = "N";
                hv = 0.1;
                c = 0.0;
            }

            // 少数第一位までに丸める
            hv = (double)Mathf.Round((float)(hv * 10.0)) / 10.0;
            v = (double)Mathf.Round((float)(v * 10.0)) / 10.0;
            c = (double)Mathf.Round((float)(c * 10.0)) / 10.0;

            SyncUI(color, applyToEditor: true, suppressEvent: true);

            if (!bColorEditToggle)
            {
                suppressCallback = true;
            }
            hueNameDropdown.value = hn;
            hueValueDoubleField.value = hv;
            valueDoubleField.value = v;
            chromaDoubleField.value = c;
            suppressCallback = false;
            if (bColorEditToggle)
            {
                SetMansellTextInput();
            }
        }

        private Munsell ConvertColorToMunsell(Color color)
        {
            var unicolour = new Unicolour(ColourSpace.Rgb, color.r, color.g, color.b);
            return unicolour.Munsell;
        }

        private void SetMansellTextInput()
        {
            var hv = hueValueDoubleField.value;
            var hn = hueNameDropdown.value;
            var v = valueDoubleField.value;
            var c = chromaDoubleField.value;

            // 少数第一位までに丸める
            hv = Mathf.Round((float)(hv * 10.0)) / 10.0;
            v = Mathf.Round((float)(v * 10.0)) / 10.0;
            c = Mathf.Round((float)(c * 10.0)) / 10.0;
            if(hn == "N"){
                // 無彩色扱い
                hv = 0.1;
                c = 0.0;
            }

            if (munsellValueLabel != null)
            {
                munsellValueLabel.text = (hn == "N") ? $"{hn} {v:0.#}" : $"{hv:0.#}{hn} {v:0.#}/{c:0.#}";
            }
        }
    }
}