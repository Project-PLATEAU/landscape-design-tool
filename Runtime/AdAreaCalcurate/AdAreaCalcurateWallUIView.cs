using Landscape2.Runtime.AdRegulation;
using PlateauToolkit.Sandbox.Runtime;
using ProceduralToolkit;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{

    public interface IAdAreaCalcurateWallUI
    {
        float WallAreaWidth { get; set; }
        float WallAreaHeight { get; set; }

        /// <summary>
        ///  壁面が変更されたら呼ばれるcallback
        /// </summary>
        event System.Action<float, float> OnChangeWallAreaSize;

        void SetAdObject(GameObject obj);

        void PutAdObject(GameObject obj);

        void SetWallAreaBounds(OrientedBounds bounds);
    }

    interface IFormContaierViewPlus
    {
        void Reset();
    }

    class FormContainerViewText : IFormContaierViewPlus
    {
        public event Action<string> OnValueChanged;
        public event Func<string, string> OnUpButtonClicked;
        public event Func<string, string> OnDownButtonClicked;

        TextField textField;

        Button up;
        Button down;

        string defaultFieldValue;

        public void Reset()
        {
            textField.value = defaultFieldValue;
        }

        public FormContainerViewText(VisualElement root, string defaultValue = "")
        {
            defaultFieldValue = defaultValue;

            textField = root.Q<TextField>();
            up = root.Q<Button>("UpButton");
            down = root.Q<Button>("DownButton");
            textField.isDelayed = true;

            textField.RegisterValueChangedCallback(evt =>
            {
                OnValueChanged?.Invoke(evt.newValue);
            });

            up.clicked += () =>
            {
                var result = OnUpButtonClicked?.Invoke(textField.value);
                textField.value = result;
            };
            down.clicked += () =>
            {
                var result = OnDownButtonClicked?.Invoke(textField.value);
                textField.value = result;
            };
        }
    }

    public class FormContainerViewInteger : IFormContaierViewPlus
    {
        public event System.Action<int> OnValueChanged;
        int fieldDefaultValue;
        int changeValue = 0;
        int valueMax;
        int valueMin;

        IntegerField field;
        Button up;
        Button down;


        public FormContainerViewInteger(VisualElement root, int defaultValue = 0, int changeValue = 1, int min = 0, int max = 100)
        {
            fieldDefaultValue = defaultValue;
            this.changeValue = changeValue;
            valueMax = max;
            valueMin = min;

            field = root.Q<IntegerField>();
            field.SetValueWithoutNotify(defaultValue);

            field.RegisterValueChangedCallback(evt =>
            {
                field.value = math.clamp(evt.newValue, min, max);
                OnValueChanged?.Invoke(field.value);
            });

            up = root.Q<Button>("UpButton");
            down = root.Q<Button>("DownButton");

            up.clicked += () =>
            {
                var v = field.value;
                field.value = math.min(v + changeValue, max);
            };
            down.clicked += () =>
            {
                var v = field.value;
                field.value = math.max(v - changeValue, min);
            };
        }

        public void Reset()
        {
            field.value = fieldDefaultValue;
        }
    }



    public interface IAdAreaCalcurateWallUIView
    {
        event System.Action<float> OnAdAreaWidthChanged;
        event System.Action<float> OnAdAreaHeightChanged;

        // マンセル色線をクリックした際に呼び出される
        event System.Action OnMunsellColorLineClicked;

        // 広告物に対して色を変更した
        event System.Action<Color> OnAdObjectColorChanged;

        // 壁面広告位置上端距離変更

        event System.Action<float> OnAdUpHeightValueChanged;

        // 壁面広告位置下端距離変更
        event System.Action<float> OnAdDownHeightValueChanged;

        //event System.Action<int> OnStoreNumChanged;

        void SetPresenter(IAdAreaCalcurateWallUI presenter);

        void Show(bool isShow);

        bool IsShow { get; }

        /// <summary>
        /// 
        /// ToDo広告物の上端下端で位置を合わせる必要がありそう
        /// </summary>
        /// <param name="wallHeight"></param>
        /// <param name="adObjPos">0以上 wallHeight以下</param>
        void SetAdObjectToBuildingWall(GameObject adObj, OrientedBounds wallBounds);
    }

    public class AdAreaCalcurateWallUIView : IAdAreaCalcurateWallUIView
    {
        const string basePanelName = "WallInfoPanel";

        class AdAreaSubContainer
        {
            protected VisualElement rootElement;

            public AdAreaSubContainer(VisualElement rootElement)
            {
                this.rootElement = rootElement;
            }

            public T GetElement<T>(string elementName) where T : VisualElement
            {
                return rootElement.Q<T>(elementName);
            }

            public void Show(bool isShow)
            {
                rootElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public virtual void Reset()
            {

            }

        }

        /// <summary>
        /// 壁面サイズ
        /// </summary>
        class WallSizeContainer : AdAreaSubContainer
        {
            const string faceWidthLabelName = "FaceWidth";
            const string faceHeightLabelName = "FaceHeight";
            const string areaLabelName = "Area";

            float _faceWidth = 0f;
            float _faceHeight = 0f;

            public float FaceWidth
            {
                get => _faceWidth;
                set
                {
                    _faceWidth = value;
                    faceWidthLabel.text = $"{_faceWidth:F2}";

                    areaSizeLabel.text = $"{_faceWidth * _faceHeight:F2}";
                }
            }

            public float FaceHeight
            {
                get => _faceHeight;
                set
                {
                    _faceHeight = value;
                    faceHeightLabel.text = $"{_faceHeight:F2}";

                    areaSizeLabel.text = $"{_faceWidth * _faceHeight:F2}";
                }
            }

            Label faceWidthLabel;
            Label faceHeightLabel;
            Label areaSizeLabel;

            public WallSizeContainer(VisualElement rootElement) : base(rootElement)
            {
                faceWidthLabel = GetElement<Label>(faceWidthLabelName);
                faceHeightLabel = GetElement<Label>(faceHeightLabelName);
                areaSizeLabel = GetElement<Label>(areaLabelName);


                FaceWidth = 0f;
                FaceHeight = 0f;
            }

            public override void Reset()
            {
                FaceWidth = 0f;
                FaceHeight = 0f;
            }
        };

        /// <summary>
        /// 寸法/面積
        /// </summary>
        class AdAreaContainer : AdAreaSubContainer
        {
            const float incrementValue = 0.1f;
            const float decrementValue = -incrementValue;

            const string adAreaContainerName = "AdAreaContainer";

            const string areaSizeLabelName = "AdArea";
            const string widthFormName = "widthInputArea"; // root.Q<VisualElement>(widthFormName).Q<FloatField>(); が返してくる筈
            const string heightFormName = "heightInputArea"; // root.Q<VisualElement>(heightFormName).Q<FloatField>(); が返してくる筈

            //public float _width = 2.5f;
            //public float _height = 2.5f;

            private PlateauSandboxAdvertisement adObj;

            public float Width
            {
                get => adObj?.transform.localScale.x ?? 0f;
                set
                {
                    if (adObj == null)
                        return;

                    var v = value;
                    if (v < 0)
                    {
                        v = 0;
                    }
                    var s = adObj.transform.localScale;
                    adObj.transform.localScale = new Vector3(v, s.y, s.z);
                    OnWidthValueChanged?.Invoke(v);
                    UpdateAreaSizeLabel();
                }
            }

            public float Height
            {
                get => adObj?.transform.localScale.y ?? 0f;
                set
                {
                    if (adObj == null)
                        return;

                    var v = value;
                    if (v < 0)
                    {
                        v = 0;
                    }
                    var s = adObj.transform.localScale;
                    adObj.transform.localScale = new Vector3(s.x, v, s.z);
                    OnHeightValueChanged?.Invoke(v);
                    UpdateAreaSizeLabel();
                }
            }

            private void UpdateAreaSizeLabel()
            {
                areaSize.text = $"{Width * Height:F2}";
            }

            Label areaSize;

            FormContainerView widthContainer;

            FormContainerView heightContainer;


            public event System.Action<float> OnWidthValueChanged;

            public event System.Action<float> OnHeightValueChanged;

            public void SetAdObj(PlateauSandboxAdvertisement plateauSandboxAdvertisement)
            {
                adObj = plateauSandboxAdvertisement;
                (widthContainer as IFormContainerView)?.SetWithoutNotify(Width);
                (heightContainer as IFormContainerView)?.SetWithoutNotify(Height);
                UpdateAreaSizeLabel();
            }

            public AdAreaContainer(VisualElement rootElement) : base(rootElement)
            {
                var container = rootElement.Q<VisualElement>(adAreaContainerName);
                Debug.Log($"rootElement: {rootElement.name} / {container.name}");

                areaSize = container.Q<Label>(areaSizeLabelName);

                //
                var widthForm = container.Q<VisualElement>(widthFormName);
                var heightForm = container.Q<VisualElement>(heightFormName);
                Debug.Log($"widthForm[{widthFormName}]:({widthForm})");
                Debug.Log($"heightForm[{heightFormName}]:({heightForm})");

                widthContainer = new(widthForm, 0f, 0.01f, 500f);
                heightContainer = new(heightForm, 0f, 0.01f, 500f);

                widthContainer.OnDistanceChanged += (v) =>
                {
                    Width = v;
                };
                heightContainer.OnDistanceChanged += (v) =>
                {
                    Height = v;
                };

                UpdateAreaSizeLabel();
            }

            public override void Reset()
            {
                adObj = null;
            }
        }

        private class AdColorControl
        {
            public void SetAdObj(PlateauSandboxAdvertisement adObj)
            {
                this.adObj = adObj;
            }

            public void Reset()
            {
                this.adObj = null;
            }

            public void ChangeColor(Color color)
            {
                if (adObj == null)
                    return;
                Common.MansellColorUtil.SetBaseColor(adObj.gameObject, color, true);
            }

            private PlateauSandboxAdvertisement adObj;

        }

        // マンセル表
        class MunsellColorContainer : AdAreaSubContainer
        {

            const string hueElementName = "HVCHue";
            const string valueElementName = "HVCValue";
            const string chromaElementName = "HVCChroma";


            const string colorPullDownName = "ColorPullDown";

            const string munsellValueLabelName = "MunsellValue";

            const string munsellColorImageName = "MunsellColorImage";

            float _hue = 0;
            string _colorName = "";
            int _value = 0;
            int _chroma = 0;

            public DropdownField colorPullDown;

            public Label munsellValueLabel;

            public VisualElement munsellColorImage;

            public event Action<Color> OnAdObjectColorChanged;

            IFormContainerView hueContainer;
            IFormContainerView valueContainer;
            IFormContainerView chromaContainer;

            public System.Action OnColorBarClicked;

            public void ApplyColorToPreview(Color color)
            {
                munsellColorImage.style.backgroundColor = color;
                this.munsellValueLabel.text = GenerateMunsellColorValue();
                OnAdObjectColorChanged?.Invoke(color);
            }

            public MunsellColorContainer(VisualElement rootElement) : base(rootElement)
            {
                Debug.Log($"{rootElement.name}");

                // 初期値の設定
                _hue = 2.5f;
                //_colorName = "R";     // プルダウンの最初の値を初期値として利用　下の方で対応
                _value = 8;
                _chroma = 10;

                // 2.5,5,7.5,10
                var hueElem = GetElement<VisualElement>(hueElementName);
                hueContainer = new FormContainerView(hueElem, defaultV: _hue, unit: 2.5f, max: 10.0f, min: 2.5f, format: "F1",
                    converter: new FloatSnapConverter() { Step = 2.5f });
                hueContainer.OnDistanceChanged += (v) =>
                {
                    _hue = v;
                    var col = Common.MansellColorUtil.MunsellToColor(_hue, _colorName, _value, _chroma);
                    ApplyColorToPreview(col);
                };

                // 1～10
                var valueElem = GetElement<VisualElement>(valueElementName);
                valueContainer = new FormContainerView(valueElem, defaultV: _value, unit: 1, max: 9, min: 1, format: "F0");
                valueContainer.OnDistanceChanged += (v) =>
                {
                    _value = (int)v;
                    var col = Common.MansellColorUtil.MunsellToColor(_hue, _colorName, _value, _chroma);
                    ApplyColorToPreview(col);
                };

                // 1～14
                var chromaElem = GetElement<VisualElement>(chromaElementName);
                chromaContainer = new FormContainerView(chromaElem, defaultV: _chroma, unit: 1, max: 14, min: 1, format: "F0");
                chromaContainer.OnDistanceChanged += (v) =>
                {
                    _chroma = (int)v;
                    var col = Common.MansellColorUtil.MunsellToColor(_hue, _colorName, _value, _chroma);
                    ApplyColorToPreview(col);
                };

                colorPullDown = GetElement<DropdownField>(colorPullDownName);
                _colorName = colorPullDown.text;

                // 基本色
                var options = new List<string>
                {
                    "R", "YR", "Y", "GY", "G", "BG", "B", "PB", "P", "RP"
                };
                colorPullDown.choices = options;
                colorPullDown.RegisterValueChangedCallback<string>((e) =>
                {
                    _colorName = e.newValue;
                    var col = Common.MansellColorUtil.MunsellToColor(_hue, _colorName, _value, _chroma);
                    ApplyColorToPreview(col);
                });


                munsellValueLabel = GetElement<Label>(munsellValueLabelName);


                munsellColorImage = GetElement<VisualElement>(munsellColorImageName);
                munsellColorImage.RegisterCallback<ClickEvent>(evt =>
                {
                    Debug.Log($"click area");
                    OnColorBarClicked?.Invoke();
                });
            }

            private string GenerateMunsellColorValue()
            {
                // hueが2.5,5,7.5,10
                // colornameがR,YR,Y,GY,G,BG,B,PB,P,RP
                // valueが9,8,7,6,5,4,3,2,1
                // chromaが0,2,4,6,8,10,12,14

                return $"{_hue}{_colorName} {_value}/{_chroma}";
            }

        }

        // 設置位置
        class DisplayPositionContainer : AdAreaSubContainer
        {
            const float inclementValue = 0.01f;

            const string topInputAreaName = "topInputArea";
            const string bottomInputAreaName = "bottomInputArea";

            public float Top
            {
                get
                {
                    if (adObj == null)
                        return 0;

                    // 広告物の位置から Topを計算
                    var wallBottom = wallBounds.center.y - wallBounds.extents.y;

                    // TODO: localscaleではなくboundsからの計算のほうが良いかもしれないが一旦このまま
                    var adTop = (adObj.transform.position.y + adObj.transform.localScale.y);
                    return adTop - wallBottom;
                }
                set
                {
                    if (adObj == null)
                        return;

                    // size～
                    //var v = Mathf.Clamp(value, adObj.transform.localScale.y, wallBounds.extents.y * 2.0f);
                    var v = Mathf.Max(value, adObj.transform.localScale.y);
                    v = v - adObj.transform.localScale.y; // 広告物の中心位置を考慮
                    var wallBottom = wallBounds.center.y - wallBounds.extents.y;
                    var p = adObj.transform.position;
                    adObj.transform.position = new Vector3(p.x, wallBottom + v, p.z);


                    // 壁面高さからのオフセットなので建物高さを何かで保持し、そこから計算する
                    // ここで必要な値は
                    // 1. 壁面高さ
                    // 2. 広告アセットサイズ
                    // topの値が変化すると、合わせてbottomも変化する => bottomInputField.setvaluewithoutnotify()

                    (topContainer as IFormContainerView)?.SetWithoutNotify(Top);
                    (bottomContainer as IFormContainerView)?.SetWithoutNotify(Bottom);
                    OnPositionChanged?.Invoke(Top, Bottom);
                }
            }

            public float Bottom
            {
                get
                {
                    if (adObj == null)
                        return 0;

                    // 広告物の位置から Bottomを計算
                    var wallBottom = wallBounds.center.y - wallBounds.extents.y;
                    return adObj.transform.position.y - wallBottom;

                }
                set
                {
                    if (adObj == null)
                        return;

                    // 0～
                    //var v = Mathf.Clamp(value, 0f, wallBounds.extents.y * 2.0f - adObj.transform.localScale.y);
                    var v = Mathf.Max(value, 0f);

                    var wallBottom = wallBounds.center.y - wallBounds.extents.y;
                    var p = adObj.transform.position;
                    adObj.transform.position = new Vector3(p.x, wallBottom + v, p.z);

                    //var wallBottom = wallBounds.center.y - wallBounds.extents.y;

                    //var v = Mathf.Min(wallBounds.extents.y * 2.0f - adObj.transform.localScale.y, value);
                    //var p = adObj.transform.position;
                    //adObj.transform.position = new Vector3(p.x, wallBottom + v, p.z);

                    // TODO: 壁面高さからのオフセットなので建物高さを何かで保持し、そこから計算する
                    // ここで必要な値は
                    // 1. 壁面高さ
                    // 2. 広告アセットサイズ
                    // bottomの値が変化すると、合わせてtopも変化する => topnputField.setvaluewithoutnotify()

                    (topContainer as IFormContainerView)?.SetWithoutNotify(Top);
                    (bottomContainer as IFormContainerView)?.SetWithoutNotify(Bottom);
                    OnPositionChanged?.Invoke(Top, Bottom);
                }
            }

            public float Height
            {
                get => wallBounds.Size.y;
            }

            public void SetAdObjectToBuildingWall(GameObject adObj, OrientedBounds wallBounds)
            {
                // 広告物の上端下端で設定するならbounds必要

                //if (adObj.TryGetComponent<PlateauSandboxAdvertisement>(out var ad) == false)
                //{
                //    Debug.LogWarning("PlateauSandboxAdvertisementのみ対応");
                //    return;
                //}

                //if (Common.AdvertisementObjectUtil.GetBoundsFromPlateauSandboxAdvertisement(ad, out var adBounds) == false)
                //{
                //    Debug.LogWarning("faild GetBoundsFromPlateauSandboxAdvertisement(ad, out var adBounds)");
                //    return;
                //}                

                this.adObj = adObj;
                this.wallBounds = wallBounds;

                (topContainer as IFormContainerView)?.SetWithoutNotify(Top);
                (bottomContainer as IFormContainerView)?.SetWithoutNotify(Bottom);
                OnPositionChanged?.Invoke(Top, Bottom);

            }

            public GameObject adObj;

            private OrientedBounds wallBounds;

            public event System.Action<float, float> OnPositionChanged;

            FormContainerView topContainer;
            FormContainerView bottomContainer;

            public DisplayPositionContainer(VisualElement rootElement) : base(rootElement)
            {
                var topElem = GetElement<VisualElement>(topInputAreaName);
                topContainer = new(topElem, 0f, 0.01f, 100f, 0f);
                topContainer.OnDistanceChanged += (v) =>
                {
                    Top = v;
                };

                var bottomElem = GetElement<VisualElement>(bottomInputAreaName);
                bottomContainer = new(bottomElem, 0f, 0.01f, 100f, 0f);

                bottomContainer.OnDistanceChanged += (v) =>
                {
                    Bottom = v;
                };
            }

            public override void Reset()
            {
                adObj = null;
                Top = Top;
                Bottom = Bottom;
            }
        }

        // 設置面積に対する面積割合
        class PerAreaInfoContainer : AdAreaSubContainer
        {
            const string storeInputAreaName = "storeInputArea";

            const string perStoreLabelName = "perStoreArea";
            const string perAdAreaPercentageLabelName = "perStorePercentage";


            public int StoreNum
            {
                get;
                private set;
            } = 1;

            /// <summary>
            /// 店舗数
            /// 1〜
            /// </summary>
            /// <summary>
            /// 壁面面積 / 店舗数で、1店舗辺りの壁面面積 (壁面面積 / 店舗数)
            /// </summary>
            Label perStoreAreaLabel;

            /// <summary>
            /// 建物面積のうちの広告物割合 (広告面積 / 壁面面積) * 100
            /// </summary>
            Label perAdAreaPercentageLabel;


            FormContainerViewInteger storeNumContainer;

            float? storeWallArea = null;

            GameObject adObj;

            /// <summary>
            /// 壁面面積
            /// </summary>
            public float StoreWallArea
            {
                get => storeWallArea ?? 0f;
                set
                {
                    storeWallArea = value;
                    var swa = storeWallArea ?? 0f;
                    var storeArea = swa / StoreNum;
                    PerStoreArea = storeArea;

                    if (adObj != null)
                    {
                        var adObjArea = adObj.transform.localScale.x * adObj.transform.localScale.y;
                        PerStorePercentage = adObjArea / storeArea * 100f;
                    }
                    else
                    {
                        PerStorePercentage = 0f;
                    }
                }
            }

            float PerStoreArea
            {
                set
                {
                    perStoreAreaLabel.text = value.ToString("F2");
                }
            }

            float PerStorePercentage
            {
                set
                {
                    perAdAreaPercentageLabel.text = value.ToString("F2");
                }
            }


            //public event System.Action<int> OnStoreNumChanged;

            public PerAreaInfoContainer(VisualElement rootElement) : base(rootElement)
            {
                // storeInputAreaコンテナ内のIntegerFieldとボタンを取得
                var storeInputArea = GetElement<VisualElement>(storeInputAreaName);

                storeNumContainer = new(storeInputArea, 1, 1, 1, 99);
                perStoreAreaLabel = GetElement<Label>(perStoreLabelName);
                perAdAreaPercentageLabel = GetElement<Label>(perAdAreaPercentageLabelName);

                // 1店舗当たりの壁面面積(m^2)
                PerStoreArea = 0f;
                // 1店舗当たりの壁面割合(%)
                PerStorePercentage = 0f;

                storeNumContainer.OnValueChanged += (v) =>
                {
                    StoreNum = v;

                    if (storeWallArea.HasValue)
                    {
                        StoreWallArea = storeWallArea.Value;
                    }

                    //OnStoreNumChanged?.Invoke(StoreNum);
                };
            }

            /// <summary>
            /// wallArea,StoreNumを元に壁面面積を計算
            /// </summary>
            /// <param name="wallArea"></param>
            public void UpdateStoreAreaPerStoreNum(float wallArea)
            {
                StoreWallArea = wallArea;
            }

            public void SetAdObj(GameObject adObj)
            {
                this.adObj = adObj;
                StoreWallArea = StoreWallArea;
            }

            public override void Reset()
            {
                SetAdObj(null);
            }

        }

        // rootのpanel。VisualElement.Q<VisualElement>("BuildingAdSetting")を渡される想定
        VisualElement panel;

        WallSizeContainer wallSizeContainer;

        AdAreaContainer adAreaContainer;

        // 広告物の色変更機能:ToDo ViewではなくUI側で持ちたい
        AdColorControl adColorControl;

        MunsellColorContainer munsellColorContainer;

        DisplayPositionContainer displayPositionContainer;

        PerAreaInfoContainer perAreaInfoContainer;

        public event Action<float> OnAdAreaWidthChanged;
        public event Action<float> OnAdAreaHeightChanged;

        public event Action OnMunsellColorLineClicked;

        public event Action<Color> OnAdObjectColorChanged;


        public event Action<float> OnAdUpHeightValueChanged;
        public event Action<float> OnAdDownHeightValueChanged;

        //public event Action<int> OnStoreNumChanged;

        VisualElement centerPanel;

        ColorEditorUI colorEditorUI;

        public bool IsShow => panel.style.display == DisplayStyle.Flex;

        public AdAreaCalcurateWallUIView(VisualElement root, VisualElement centerPanel)
        {
            panel = root.Q<VisualElement>(basePanelName);
            this.centerPanel = centerPanel;
            Debug.Log($"root: {root.name} / {panel.name}");
        }

        public void Show(bool isShow)
        {
            Debug.Log($"AdAreaCalcurateWallUIView show :{isShow}");
            panel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            centerPanel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isShow)
            {
                colorEditorUI?.Show(false);

                wallSizeContainer?.Reset();
                adAreaContainer?.Reset();
                adColorControl?.Reset();
                munsellColorContainer?.Reset();
                displayPositionContainer?.Reset();
                perAreaInfoContainer?.Reset();
            }
        }

        public void SetPresenter(IAdAreaCalcurateWallUI presenter)
        {
            // InfoArea
            wallSizeContainer = new WallSizeContainer(panel);
            // AdAreaContainer
            adAreaContainer = new AdAreaContainer(panel);

            // 色変更機能 ToDo:ViewではなくUI側に持たせたい。
            adColorControl = new AdColorControl();

            // MuncellColorContainer
            var munsellColorContainerElement = panel.Q<VisualElement>("MuncellColorContainer");
            munsellColorContainer = new MunsellColorContainer(munsellColorContainerElement);

            // Position
            var displayPositionContainerElement = panel.Q<VisualElement>("Position");
            displayPositionContainer = new DisplayPositionContainer(displayPositionContainerElement);

            // PerAreaInfo
            var perAreaInfoContainerElement = panel.Q<VisualElement>("PerAreaInfo");
            perAreaInfoContainer = new PerAreaInfoContainer(perAreaInfoContainerElement);

            // それぞれイベントマッピング

            // 壁面面積パラメータは変更があったら呼び出されるので受け取って対応
            presenter.OnChangeWallAreaSize += ((width, height) =>
            {
                wallSizeContainer.FaceWidth = width;
                wallSizeContainer.FaceHeight = height;

                Debug.Log($"OnChangeWallAreaSize width:{width} height:{height}");
                perAreaInfoContainer.StoreWallArea = width * height;
            });

            // 看板は値を受けとって反映
            // +でpresenterに値の変更を通知する
            adAreaContainer.OnWidthValueChanged += (v) =>
            {
                OnAdAreaWidthChanged?.Invoke(v);

            };
            adAreaContainer.OnHeightValueChanged += (v) =>
            {
                OnAdAreaHeightChanged?.Invoke(v);
                displayPositionContainer.Top = displayPositionContainer.Top;
                displayPositionContainer.Bottom = displayPositionContainer.Bottom;
            };

            // munsell値は後で
            munsellColorContainer.OnColorBarClicked += () =>
            {
                if (colorEditorUI == null)
                {
                    colorEditorUI = ColorEditorFactory(centerPanel);
                }
                colorEditorUI.Show(true);

                OnMunsellColorLineClicked?.Invoke();
            };

            // 広告物の色変更
            munsellColorContainer.OnAdObjectColorChanged += OnAdObjectColorChanged;
            munsellColorContainer.OnAdObjectColorChanged += adColorControl.ChangeColor;

            // 上下高さ
            displayPositionContainer.OnPositionChanged += (top, bottom) =>
            {
                OnAdUpHeightValueChanged?.Invoke(top);
                OnAdDownHeightValueChanged?.Invoke(bottom);
            };

            //// 店舗数
            //perAreaInfoContainer.OnStoreNumChanged += (v) =>
            //{
            //    Debug.Log($"OnStoreNumChanged: {v}");
            //    var area = presenter.WallAreaWidth * presenter.WallAreaHeight;

            //    perAreaInfoContainer.UpdateStoreAreaPerStoreNum(area);

            //    OnStoreNumChanged?.Invoke(v);
            //};
        }

        void SetAdObjectToBuildingWall(GameObject adObj, OrientedBounds wallBounds)
        {
            if (adObj.TryGetComponent<PlateauSandboxAdvertisement>(out var ad))
            {
                adAreaContainer.SetAdObj(ad);
                adColorControl.SetAdObj(ad);
            }
            displayPositionContainer.SetAdObjectToBuildingWall(adObj, wallBounds);

            // perAreaInfoにも渡す
            perAreaInfoContainer.SetAdObj(adObj);
        }

        ColorEditorUI ColorEditorFactory(VisualElement parentElement)
        {
            var initialColor = new Color32(186, 186, 186, 255);
            var colorEditor = Resources.Load<VisualTreeAsset>("UIColorEditor");
            VisualElement colorEditorClone = colorEditor.CloneTree();
            parentElement.Add(colorEditorClone);
            var colorEditorUI = new ColorEditorUI(colorEditorClone, initialColor);
            colorEditorClone.style.display = DisplayStyle.None;

            // button
            colorEditorUI.OnColorEdited += (color) =>
            {
                // OnMunsellColorChanged?.Invoke(color);
                munsellColorContainer.ApplyColorToPreview(color);
            };
            colorEditorUI.OnCloseButtonClicked += () =>
            {
                colorEditorClone.style.display = DisplayStyle.None;
            };
            return colorEditorUI;
        }

        void IAdAreaCalcurateWallUIView.SetAdObjectToBuildingWall(GameObject adObj, OrientedBounds wallBounds)
        {
            SetAdObjectToBuildingWall(adObj, wallBounds);
        }
    }
}
