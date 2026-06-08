using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    public interface IPanel_RoadEdgePanelView
    {
        public IFormContainerView FormContainer { get; }
        public void Reset();
        public void Show();
        public void Hide();

    }

    public class Panel_RoadEdgePanelView : IPanel_RoadEdgePanelView
    {
        VisualElement root;

        UXMLs uxmls;
        IFormContainerView formContainer;

        IFormContainerView IPanel_RoadEdgePanelView.FormContainer => formContainer;

        public Panel_RoadEdgePanelView(VisualElement root)
        {
            this.root = root;
            uxmls = new UXMLs(root);
            formContainer = new FormContainerView(uxmls.FormContainer);

            // defaultで非表示
            root.style.display = DisplayStyle.None;
        }

        private class UXMLs
        {
            public VisualElement FormContainer { get; private set; }
            public UXMLs(VisualElement root)
            {
                // ここでUXMLの要素を取得する
                // 例: var someElement = root.Q<VisualElement>("SomeElementName");

                FormContainer = root.Q<VisualElement>("FormContainer");
            }
        }

        void IPanel_RoadEdgePanelView.Reset()
        {
            formContainer.Reset();
        }

        void IPanel_RoadEdgePanelView.Show()
        {
            root.style.display = DisplayStyle.Flex;
        }

        void IPanel_RoadEdgePanelView.Hide()
        {
            root.style.display = DisplayStyle.None;
        }
    }
    public interface IFormContainerView
    {
        public event System.Action<float> OnDistanceChanged;

        // 値の設定　最終的な値を返す
        public float SetWithoutNotify(float v);

        void Reset();
    }

    /// <summary>
    /// 広告制限幅　5.0 m 上下ボタン　のような構成になっている要素を扱うためのクラス
    /// いろいろなところで使えそうなので、ここに定義する。
    /// </summary>
    public class FormContainerView : IFormContainerView
    {
        public interface IValueConverter
        {
            public float Convert(float v);
        };
        IValueConverter converter;

        // 設定している距離が変更された
        public event Action<float> OnDistanceChanged;

        public FloatField DistanceField { get; private set; }

        public Button UpButton { get; private set; }
        public Button Downbutton { get; private set; }

        public float LastValue { get; private set; } = 0f;
        public float DefaultValue { get; private set; } = 5.0f;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="unit">編集単位 0.1mとか</param>
        public FormContainerView(VisualElement root,
            float defaultV = 5.0f, float unit = 0.1f,
            float max = 20f, float min = 0f,
            string format = "F2", bool isDelayed = true, 
            IValueConverter converter = null)
        {
            DistanceField = root.Q<FloatField>();
            UpButton = root.Q<Button>("UpButton");
            Downbutton = root.Q<Button>("DownButton");
            if (format != null) // 指定がある場合はフォーマットを上書き 
            {
                DistanceField.formatString = format;
            }
            DistanceField.isDelayed = isDelayed; // フォーマットを適用するために遅延入力を有効にする

            DistanceField.SetValueWithoutNotify(defaultV);
            LastValue = DistanceField.value;
            DefaultValue = defaultV;

            UpButton.clicked += () =>
            {
                DistanceField.value = Mathf.Min(DistanceField.value + unit, max);
            };
            Downbutton.clicked += () =>
            {
                DistanceField.value = Mathf.Max(DistanceField.value - unit, min);
            };

            //DistanceField.RegisterCallback<KeyDownEvent>((e) =>
            //{

            //});

            DistanceField.RegisterValueChangedCallback((ChangeEvent<float> e) =>
            {
                var v = e.newValue;
                if (converter != null)
                {
                    v = converter.Convert(v);
                    DistanceField.SetValueWithoutNotify(v);
                }

                // 最小値、最大値間にクランプ
                var newV = Mathf.Clamp(v, min, max);
                if (newV != v)
                {
                    DistanceField.SetValueWithoutNotify(newV);
                    v = newV;
                }

                OnDistanceChanged?.Invoke(v);
            });
        }

        void IFormContainerView.Reset()
        {
            DistanceField.value = DefaultValue;
            LastValue = DefaultValue;
        }

        float IFormContainerView.SetWithoutNotify(float v)
        {
            DistanceField.SetValueWithoutNotify(v);
            return DistanceField.value;
        }
    }

    public class FloatSnapConverter : FormContainerView.IValueConverter
    {
        public float Step { get; set; }

        float FormContainerView.IValueConverter.Convert(float v)
        {
            float snapped = Mathf.Round(v / Step) * Step;

            //// 誤差をチェックして、ズレがあるときだけ値を更新
            //if (!Mathf.Approximately(v, snapped))
            //{
            //    return snapped;
            //}

            return snapped;
        }
    }

}
