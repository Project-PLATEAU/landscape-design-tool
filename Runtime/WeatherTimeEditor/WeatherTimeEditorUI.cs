using Landscape2.Runtime.UiCommon;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.WeatherTimeEditor
{
    /// <summary>
    /// 天候・時間帯を変更するためのUI
    /// </summary>
    public class WeatherTimeEditorUI : ISubComponent
    {
        private readonly WeatherTimeEditor weatherTimeEditor;

        // 天候変更グループ
        private readonly GroupBox weatherGroup;
        // 時間帯変更スライダー
        private readonly Slider timeSlider;
        // 時間帯変更スライダー
        private readonly Label timeLabel;
        // 天候変更ボタン名前
        private const string UIWeatherButton = "Weather";
        // 時間帯変更スライダー名前
        private const string UITimeSlider = "TimeSlider";
        // 時間帯変更ラベル名前
        private const string UITimeLabel = "TimeText";

        public WeatherTimeEditorUI(WeatherTimeEditor weatherTimeEditor, VisualElement uiRoot)
        {
            this.weatherTimeEditor = weatherTimeEditor;

            weatherGroup = uiRoot.Q<GroupBox>(UIWeatherButton);
            timeSlider = uiRoot.Q<Slider>(UITimeSlider);
            timeLabel = uiRoot.Q<Label>(UITimeLabel);

            // 時間帯の初期値の設定
            timeSlider.lowValue = 0f; //0:00
            timeSlider.highValue = 1f; //24:00
            timeSlider.value = 0.5f; //12:00
            timeLabel.text = this.weatherTimeEditor.GetTimeString(timeSlider.value);

            // 天候変更ボタンリスト
            var weatherButtons = weatherGroup.Children();

            //天候変更ボタンの値が変更されたとき
            weatherButtons.ToList().ForEach(b =>
            {
                var button = b as RadioButton;
                button.RegisterValueChangedCallback(evt =>
                {
                    if (button.value == true)
                    {
                        // 天候を変更
                        this.weatherTimeEditor.SwitchWeather(button.tabIndex);
                        // 時間帯を更新
                        this.weatherTimeEditor.EditTime(timeSlider.value);
                    }
                });
            });

            // 時間帯変更スライダーの値が変更されたとき
            timeSlider.RegisterValueChangedCallback(evt =>
            {
                // 時間帯を更新
                this.weatherTimeEditor.EditTime(evt.newValue);
                // 表示される時刻を更新
                timeLabel.text = this.weatherTimeEditor.GetTimeString(timeSlider.value);
            });
        }
        public void Update(float deltaTime)
        {
            this.weatherTimeEditor.OnUpdate();
        }
        public void OnEnable()
        {
        }
        public void OnDisable()
        {
        }
        public void Start()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
