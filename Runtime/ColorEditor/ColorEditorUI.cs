using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using System;


namespace Landscape2.Runtime
{
    /// <summary>
    /// 色彩変更用のUI
    /// </summary>
    public class ColorEditorUI : ISubComponent
    {
        struct MunselValue
        {
            public string hueValue; // "2.5,5,7.5,10"
            public string hueName; // "R","YR"等
            public int value; // 9〜1?
            public string chroma;   // 2〜18で2刻み
        }

        // 色が変更されたときのイベント関数
        public event Action<Color> OnColorEdited = color => { };
        // 閉じるボタンが押されたときのイベント関数
        public event Action OnCloseButtonClicked = () => { };

        // 色相変更スライダー
        private readonly SliderInt hueSlider;
        // RGB変更スライダー：R
        private readonly SliderInt rSlider;
        // RGB変更スライダー：G
        private readonly SliderInt gSlider;
        // RGB変更スライダー：B
        private readonly SliderInt bSlider;
        // 閉じるボタン
        private readonly Button closeButton;
        // マンセル表パネル
        private readonly VisualElement munsellPanel;

        private Label munsellValueLabel;


        // 色相変更スライダー名前
        private const string UIHueSlider = "HueSlider";
        // RGB変更スライダー名前：R
        private const string UIRSlider = "RSlider";
        // RGB変更スライダー名前：G
        private const string UIGSlider = "GSlider";
        // RGB変更スライダー名前：B
        private const string UIBSlider = "BSlider";
        // 閉じるボタン名前
        private const string UICloseButton = "CloseButton";
        // マンセル表パネル名前
        private const string UIMunsellPanel = "MunsellPanel";



        // マンセル値
        private const string UIHVC_MunsellValueLabel = "MunsellValue";


        // マンセル表の色セット
        private List<List<string>> codemap = new List<List<string>>();
        MunsellData munsellData = new MunsellData();
        // ボタンフォーカス時のボーダー色
        private Color focusColor = new Color(1f, 1f, 0f, 1f);
        // 最後にフォーカスされたボタン
        private Button lastFocusButton;
        // 現在選択されているマンセル表の色
        private Color munselColor;
        // 初期化時の色
        private Color initialColor;

        // 外部同期 (ApplyColor) 時に内部イベント発火を抑制するフラグ（最小変更方針で追加）
        private bool suppressCallbacks = false; // TODO: 将来的に他イベントにも適用するなら共通化検討 (命名規約対応)

        Dictionary<string, MunselValue> munselValueDict = new();

        VisualElement rootElement;

        public ColorEditorUI(VisualElement uiRoot, Color initialColor = default)
        {
            this.initialColor = initialColor;

            rootElement = uiRoot;

            // 色彩変更UIの各要素を取得
            hueSlider = uiRoot.Q<SliderInt>(UIHueSlider);
            rSlider = uiRoot.Q<SliderInt>(UIRSlider);
            gSlider = uiRoot.Q<SliderInt>(UIGSlider);
            bSlider = uiRoot.Q<SliderInt>(UIBSlider);
            closeButton = uiRoot.Q<Button>(UICloseButton);
            munsellPanel = uiRoot.Q<VisualElement>(UIMunsellPanel);

            munsellValueLabel = uiRoot.Q<Label>(UIHVC_MunsellValueLabel);

            // 色相の初期値の設定
            hueSlider.lowValue = 0;
            hueSlider.highValue = 39;
            hueSlider.value = 0;

            // RGB値からマンセル値のテーブルを作る
            CreateHV_CTable();


            // RGBの初期値の設定
            SetColorSlider(initialColor);

            // 色相を初期化
            EditHue(hueSlider.value);

            var rgb = UnityEngine.ColorUtility.ToHtmlStringRGB(munselColor);
            SetMunsellValueLabel(rgb);



            // 色相変更スライダーの値が変更されたとき
            hueSlider.RegisterValueChangedCallback(evt =>
            {
                // 色相を変更
                EditHue(hueSlider.value);
            });

            // RGB変更スライダーの値が変更されたとき
            rSlider.RegisterValueChangedCallback(evt =>
            {
                if (suppressCallbacks) return; // 最小リファクタ：外部同期時はイベント抑制
                Color newColor = new Color(evt.newValue / 255f, gSlider.value / 255f, bSlider.value / 255f);
                OnColorEdited(newColor);
            });

            gSlider.RegisterValueChangedCallback(evt =>
            {
                if (suppressCallbacks) return;
                Color newColor = new Color(rSlider.value / 255f, evt.newValue / 255f, bSlider.value / 255f);
                OnColorEdited(newColor);
            });

            bSlider.RegisterValueChangedCallback(evt =>
            {
                if (suppressCallbacks) return;
                Color newColor = new Color(rSlider.value / 255f, gSlider.value / 255f, evt.newValue / 255f);
                OnColorEdited(newColor);
            });

            // マンセル表のボタンがクリックされたとき
            munsellPanel.Children().ToList().ForEach(v =>
            {
                var panel = v as VisualElement;
                panel.Children().ToList().ForEach(b =>
                {
                    var button = b as Button;
                    button.clicked += () =>
                    {
                        // マンセル表のボタンの色を取得
                        munselColor = button.style.backgroundColor.value;
                        // ボタンのボーダーをフォーカス用の色にする
                        EditBorderColor(button, focusColor);

                        // 最後にフォーカスされたボタンのボーダー色を非フォーカス用の色にする
                        if (lastFocusButton != null && button != lastFocusButton)
                        {
                            EditBorderColor(lastFocusButton, Color.clear);
                        }

                        // 最後にフォーカスされたボタンを更新
                        lastFocusButton = button;
                        //　色彩スライダーの値を更新
                        SetColorSlider(munselColor);

                        // munsel値に反映
                        SetMunsellValueLabel(UnityEngine.ColorUtility.ToHtmlStringRGB(munselColor));

                        // 最小リファクタ: マンセルボタン選択時も外部へ通知
                        if (!suppressCallbacks)
                        {
                            OnColorEdited(munselColor);
                        }
                    };
                });
            });

            if (closeButton != null)
            {
                // 閉じるボタンが押されたとき
                closeButton.clicked += () =>
                {
                    // 閉じるボタンが押されたときのイベントを発火
                    OnCloseButtonClicked();
                };
            }
        }

        /// <summary>
        /// 外部 (AssetColorEditorUI 等) から色を適用し UI 状態 (RGB スライダー / Hue / ボタン選択 / ラベル) を同期する最小公開 API。
        /// </summary>
        /// <param name="color">適用したい sRGB Color</param>
        /// <param name="suppressEvent">true の場合 OnColorEdited を発火させない</param>
        public void ApplyColor(Color color, bool suppressEvent = true)
        {
            suppressCallbacks = suppressEvent; // スライダーイベント抑制

            // RGB スライダー値更新
            SetColorSlider(color);

            // 最近傍マンセル色を新 Utility から取得 (存在しない場合は既存状態維持)
            var nearest = MunsellColorUtility.FindNearest(color);
            if (nearest.HasValue)
            {
                var entry = nearest.Value;

                // Hue スライダー index 算出 & 変更 (変化時のみ)
                int idx = MunsellColorUtility.ComputeHueSliderIndex(entry);
                if (idx >= 0 && idx != hueSlider.value)
                {
                    hueSlider.value = idx; // コールバックで EditHue() が呼ばれる
                }
                else
                {
                    // index 変化なしでもボタン配色を最新に明示更新
                    EditHue(idx);
                }

                // 選択色更新
                munselColor = entry.Color;

                // マンセルラベル (既存 munselValueDict 利用ではなく entry 直接表記)
                munsellValueLabel.text = (entry.HueName == "N") ? $"{entry.HueName} {entry.Value}"
                    : $"{entry.HueValue}{entry.HueName} {entry.Value}/{entry.Chroma}";

                // ボタンフォーカス更新 (最小変更: 全走査)
                Button focusedButton = null;
                foreach (var row in munsellPanel.Children())
                {
                    var ve = row as VisualElement;
                    foreach (var child in ve.Children())
                    {
                        if (child is Button btn)
                        {
                            var styleBg = btn.style.backgroundColor;
                            // UI Toolkit StyleColor は .keyword で None の場合があるため簡易比較
                            if (styleBg.value == entry.Color)
                            {
                                focusedButton = btn;
                                break;
                            }
                        }
                    }
                    if (focusedButton != null) break;
                }

                if (focusedButton != null)
                {
                    if (lastFocusButton != null && lastFocusButton != focusedButton)
                    {
                        EditBorderColor(lastFocusButton, Color.clear);
                    }
                    EditBorderColor(focusedButton, focusColor);
                    lastFocusButton = focusedButton;
                }
            }
            else
            {
                // 見つからない場合はラベルのみ "-" にリセット (Hue/ボタンは維持)
                munsellValueLabel.text = "-";
            }

            suppressCallbacks = false;

            // suppressEvent=false の場合は最終的な色を通知 (一度だけ)
            if (!suppressEvent)
            {
                OnColorEdited(new Color(rSlider.value / 255f, gSlider.value / 255f, bSlider.value / 255f));
            }
        }

        public void Show(bool isShow)
        {
            rootElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // 色相を変更する
        private void EditHue(int val)
        {
            // 色相セットを変更
            HueValue(val);
            int i = 0;
            // マンセル表のボタンの色を変更する
            munsellPanel.Children().ToList().ForEach(v =>
            {
                int j = 0;
                var panel = v as VisualElement;
                panel.Children().ToList().ForEach(b =>
                {
                    var button = b as Button;

                    Color newColor = Color.white;
                    if ((j < 1 && i < munsellData.codemapN.Count && munsellData.codemapN[i].Count > 0) || (j >= 1 && i < codemap.Count && j - 1 < codemap[i].Count))
                    {
                        if (ColorUtility.TryParseHtmlString("#" + (j < 1 ? munsellData.codemapN[i][j] : codemap[i][j - 1]), out newColor))
                        {
                            button.style.backgroundColor = newColor;
                        }

                        button.style.backgroundColor = newColor;
                        // ボタンをアクティブ化
                        button.SetEnabled(true);
                    }
                    else
                    {
                        // ボタンを非アクティブ化
                        button.SetEnabled(false);
                        // ボタンの色を無色にする
                        button.style.backgroundColor = new StyleColor(Color.clear);
                    }

                    // 現在選択されている色のボタンの場合、ボタンのボーダーをフォーカス状態にする
                    if (newColor == munselColor)
                    {
                        EditBorderColor(button, focusColor);
                    }
                    else
                    {
                        EditBorderColor(button, Color.clear);
                    }
                    j++;
                });
                i++;
            });
        }
        // 色相の値によってマンセル表の色セットを変更
        public void HueValue(int val)
        {
            switch (val)
            {
                case 0:
                    codemap = munsellData.codemap25R; break;
                case 1:
                    codemap = munsellData.codemap5R; break;
                case 2:
                    codemap = munsellData.codemap75R; break;
                case 3:
                    codemap = munsellData.codemap10R; break;
                case 4:
                    codemap = munsellData.codemap25YR; break;
                case 5:
                    codemap = munsellData.codemap5YR; break;
                case 6:
                    codemap = munsellData.codemap75YR; break;
                case 7:
                    codemap = munsellData.codemap10YR; break;
                case 8:
                    codemap = munsellData.codemap25Y; break;
                case 9:
                    codemap = munsellData.codemap5Y; break;
                case 10:
                    codemap = munsellData.codemap75Y; break;
                case 11:
                    codemap = munsellData.codemap10Y; break;
                case 12:
                    codemap = munsellData.codemap25GY; break;
                case 13:
                    codemap = munsellData.codemap5GY; break;
                case 14:
                    codemap = munsellData.codemap75GY; break;
                case 15:
                    codemap = munsellData.codemap10GY; break;
                case 16:
                    codemap = munsellData.codemap25G; break;
                case 17:
                    codemap = munsellData.codemap5G; break;
                case 18:
                    codemap = munsellData.codemap75G; break;
                case 19:
                    codemap = munsellData.codemap10G; break;
                case 20:
                    codemap = munsellData.codemap25BG; break;
                case 21:
                    codemap = munsellData.codemap5BG; break;
                case 22:
                    codemap = munsellData.codemap75BG; break;
                case 23:
                    codemap = munsellData.codemap10BG; break;
                case 24:
                    codemap = munsellData.codemap25B; break;
                case 25:
                    codemap = munsellData.codemap5B; break;
                case 26:
                    codemap = munsellData.codemap75B; break;
                case 27:
                    codemap = munsellData.codemap10B; break;
                case 28:
                    codemap = munsellData.codemap25PB; break;
                case 29:
                    codemap = munsellData.codemap5PB; break;
                case 30:
                    codemap = munsellData.codemap75PB; break;
                case 31:
                    codemap = munsellData.codemap10PB; break;
                case 32:
                    codemap = munsellData.codemap25P; break;
                case 33:
                    codemap = munsellData.codemap5P; break;
                case 34:
                    codemap = munsellData.codemap75P; break;
                case 35:
                    codemap = munsellData.codemap10P; break;
                case 36:
                    codemap = munsellData.codemap25RP; break;
                case 37:
                    codemap = munsellData.codemap5RP; break;
                case 38:
                    codemap = munsellData.codemap75RP; break;
                case 39:
                    codemap = munsellData.codemap10RP; break;
            }
        }

        // ボタンのボーダーの色を指定した色に変更する：引数はColor
        private void EditBorderColor(Button button, Color color)
        {
            button.style.borderBottomColor = new StyleColor(color);
            button.style.borderTopColor = new StyleColor(color);
            button.style.borderLeftColor = new StyleColor(color);
            button.style.borderRightColor = new StyleColor(color);
        }

        // 色彩スライダーの値を設定する
        private void SetColorSlider(Color color)
        {
            rSlider.value = (int)(color.r * 255f);
            gSlider.value = (int)(color.g * 255f);
            bSlider.value = (int)(color.b * 255f);
        }

        private void SetMunsellValueLabel(string rgb)
        {
            if (munselValueDict.TryGetValue(rgb, out var munselValue))
            {
                munsellValueLabel.text = (munselValue.hueName == "N") ? $"{munselValue.hueName} {munselValue.value.ToString()}"
                    : $"{munselValue.hueValue}{munselValue.hueName} {munselValue.value.ToString()}/{munselValue.chroma}";
            }
            else
            {
                munsellValueLabel.text = "-";

            }


        }

        // UIをリセットする
        public void ResetColorEditorUI(Color color)
        {
            SetColorSlider(color);

            // マンセル表ボタンのフォーカス状態をリセット
            if (lastFocusButton != null)
            {
                EditBorderColor(lastFocusButton, Color.clear);
                lastFocusButton = null;
            }

            // 色相スライダーの値をリセット
            hueSlider.value = 0;

            munsellValueLabel.text = "-";
        }

        // 初期化時の色をセットする
        public void SetInitialColor(Color color)
        {
            initialColor = color;
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

        void CreateHV_CTable()
        {
            List<List<List<List<string>>>> table = new()
            {
                new() { munsellData.codemap25R, munsellData.codemap5R,munsellData.codemap75R,munsellData.codemap10R},
                new() { munsellData.codemap25YR, munsellData.codemap5YR,munsellData.codemap75YR,munsellData.codemap10YR},
                new() { munsellData.codemap25Y, munsellData.codemap5Y,munsellData.codemap75Y,munsellData.codemap10Y},
                new() { munsellData.codemap25GY, munsellData.codemap5GY,munsellData.codemap75GY,munsellData.codemap10GY},
                new() { munsellData.codemap25G, munsellData.codemap5G,munsellData.codemap75G,munsellData.codemap10G},
                new() { munsellData.codemap25BG, munsellData.codemap5BG,munsellData.codemap75BG,munsellData.codemap10BG},
                new() { munsellData.codemap25B, munsellData.codemap5B,munsellData.codemap75B,munsellData.codemap10B},
                new() { munsellData.codemap25PB, munsellData.codemap5PB,munsellData.codemap75PB,munsellData.codemap10PB},
                new() { munsellData.codemap25P, munsellData.codemap5P,munsellData.codemap75P,munsellData.codemap10P},
                new() { munsellData.codemap25RP, munsellData.codemap5RP,munsellData.codemap75RP,munsellData.codemap10RP},
            };

            List<string> colorNameTable = new() { "R", "YR", "Y", "GY", "G", "BG", "B", "PB", "P", "RP" };

            int colorNameTableIndex = 0;
            foreach (var colorTable in table)
            {
                int hueCount = 1;
                foreach (var munselTable in colorTable)
                {
                    int value = 9; // カウントダウンしていく
                    foreach (var colTbl in munselTable)
                    {
                        int chroma = 0;
                        foreach (var col in colTbl)
                        {
                            chroma += 2;
                            float hue = 2.5f * (float)hueCount;

                            string hueString = $"{(int)hue}";
                            if ((hueCount & 0x01) != 0)
                            {
                                // 奇数の時は2.5,7.5の筈
                                hueString = $"{hue:F1}";
                            }

                            munselValueDict.Add(col, new MunselValue() { hueValue = hueString, hueName = colorNameTable[colorNameTableIndex], value = value, chroma = $"{chroma}" });
                        }
                        value--;

                    }
                    hueCount++;
                }
                colorNameTableIndex++;
            }

            // 無彩色
            int valueN = 9; // カウントダウンしていく
            foreach (var colTbl in munsellData.codemapN)
            {
                foreach(var col in colTbl)
                {
                    munselValueDict.Add(col, new MunselValue() { hueValue = "", hueName = "N", value = valueN, chroma = "0" });
                }
                valueN--;
            }
        }


    }
}
