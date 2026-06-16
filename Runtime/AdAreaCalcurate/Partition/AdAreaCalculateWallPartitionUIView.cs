using System;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public readonly struct SplitHeightInfo
    {
        public int Index { get; }
        public float Height { get; }

        public SplitHeightInfo(int index, float height)
        {
            Index = index;
            Height = height;
        }
    }

    public readonly struct SegmentAdAreaInfo
    {
        public int Index { get; }
        public float AdArea { get; }

        public SegmentAdAreaInfo(int index, float adArea)
        {
            Index = index;
            AdArea = adArea;
        }
    }

    /// <summary>
    /// 壁面分割 UI の View インターフェース（最小）
    /// </summary>
    public interface IAdAreaCalculateWallPartitionUIView
    {
        event Action<bool> ShowRequested;
        event Action<SplitHeightInfo> SplitHeightEdited;
        event Action AddSplitHeightRequested;
        event Action<int> DeleteSplitHeightRequested;
        event Action<SegmentAdAreaInfo> AdAreaEdited;

        void SetHeights(ReadOnlyArray<float> heights);

        /// <summary>
        /// 分割結果を更新
        /// </summary>
        void SetSegments(List<WallSegment> segments);

        /// <summary>
        /// 入力 UI の有効/無効を切替。
        /// </summary>
        void Show(bool enabled);
    }

    public class AdAreaCalculateWallPartitionUIView : IAdAreaCalculateWallPartitionUIView
    {
        /// <summary>
        /// 分割高さUI
        /// </summary>
        private class PartitionView
        {
            private readonly int index;
            private readonly VisualElement root;
            private readonly DoubleField heightField;
            private readonly Button upButton;
            private readonly Button downButton;
            private readonly Button deleteButton;

            private bool isRegistered = false;

            public float Height => (float)heightField.value;

            public event Action<int, float> HeightEdited;
            public event Action<int> DeleteButtonClicked;

            public PartitionView(VisualElement root, int index)
            {
                this.index = index;
                this.root = root;

                heightField = root.Q<DoubleField>();
                upButton = root.Q<Button>("UpButton");
                downButton = root.Q<Button>("DownButton");
                deleteButton = root.Q<Button>("DeleteButton");

                heightField.formatString = "F2";
                heightField.isDelayed = true;
            }

            public void Show(bool isShow)
            {
                if (isShow)
                {
                    root.style.display = DisplayStyle.Flex;
                    RegisterCallbacks();
                }
                else
                {
                    root.style.display = DisplayStyle.None;
                    UnregisterCallbacks();
                }
            }

            public void SetHeight(float height, bool notify = true)
            {
                if (notify)
                {
                    heightField.value = height;
                }
                else
                {
                    heightField.SetValueWithoutNotify(height);
                }
            }

            private void RegisterCallbacks()
            {
                if (isRegistered) return;

                heightField.RegisterValueChangedCallback(OnHeightChanged);
                upButton.clicked += OnUpButtonClicked;
                downButton.clicked += OnDownButtonClicked;
                if (deleteButton != null) deleteButton.clicked += OnDeleteButtonClicked;

                isRegistered = true;
            }

            private void UnregisterCallbacks()
            {
                if (!isRegistered) return;

                heightField.UnregisterValueChangedCallback(OnHeightChanged);
                upButton.clicked -= OnUpButtonClicked;
                downButton.clicked -= OnDownButtonClicked;
                if (deleteButton != null) deleteButton.clicked -= OnDeleteButtonClicked;

                isRegistered = false;
            }

            private void OnHeightChanged(ChangeEvent<double> v)
            {
                var newHeight = Mathf.Clamp((float)v.newValue, 0f, 1000f);
                heightField.SetValueWithoutNotify(newHeight);

                HeightEdited?.Invoke(index, newHeight);
            }

            private void OnUpButtonClicked()
            {
                heightField.value += 0.01f;
            }

            private void OnDownButtonClicked()
            {
                heightField.value -= 0.01f;
            }

            private void OnDeleteButtonClicked()
            {
                DeleteButtonClicked?.Invoke(index);
            }
        }

        /// <summary>
        /// 分割面積UI
        /// </summary>
        private class AreaView
        {
            private readonly int index;
            private readonly VisualElement root;
            private readonly Label heightLabel;
            private readonly Label widthLabel;
            private readonly Label areaLabel;
            private readonly DoubleField adAreaField;
            private readonly Button adAreaUpButton;
            private readonly Button adAreaDownButton;
            private readonly Label adAreaRatioLabel;

            private bool isRegistered = false;

            public event Action<int, float> AdAreaEdited;


            public AreaView(VisualElement root, int index)
            {
                this.root = root;
                this.index = index;

                heightLabel = root.Q<Label>("FaceHeight");
                widthLabel = root.Q<Label>("FaceWidth");
                areaLabel = root.Q<Label>("Area");
                adAreaField = root.Q<DoubleField>();
                adAreaUpButton = root.Q<Button>("UpButton");
                adAreaDownButton = root.Q<Button>("DownButton");
                adAreaRatioLabel = root.Q<Label>("PercentValue");

                adAreaField.formatString = "F2";
                adAreaField.isDelayed = true;
            }

            public void Show(bool isShow)
            {
                if (isShow)
                {
                    root.style.display = DisplayStyle.Flex;
                    RegisterCallbacks();
                }
                else
                {
                    root.style.display = DisplayStyle.None;
                    UnregisterCallbacks();
                }
            }

            public void SetHeight(float height)
            {
                heightLabel.text = height.ToString("F2");
            }

            public void SetWidth(float width)
            {
                widthLabel.text = width.ToString("F2");
            }

            public void SetArea(float area)
            {
                areaLabel.text = area.ToString("F2");
            }

            public void SetAdArea(float area, bool notify = true)
            {
                if (notify)
                {
                    adAreaField.value = area;
                }
                else
                {
                    adAreaField.SetValueWithoutNotify(area);
                }
            }

            public void SetAdAreaRatio(float ratio)
            {
                adAreaRatioLabel.text = ratio.ToString("F1");
            }

            private void RegisterCallbacks()
            {
                if (isRegistered) return;

                adAreaField.RegisterValueChangedCallback(OnAdAreaEdited);
                adAreaUpButton.clicked += OnAdAreaUpButtonClicked;
                adAreaDownButton.clicked += OnAdAreaDownButtonClicked;

                isRegistered = true;
            }

            private void UnregisterCallbacks()
            {
                if (!isRegistered) return;

                adAreaField.UnregisterValueChangedCallback(OnAdAreaEdited);
                adAreaUpButton.clicked -= OnAdAreaUpButtonClicked;
                adAreaDownButton.clicked -= OnAdAreaDownButtonClicked;

                isRegistered = false;
            }

            private void OnAdAreaEdited(ChangeEvent<double> v)
            {
                // var adArea = Mathf.Max(0, (float)v.newValue);
                var clampedAdArea = Mathf.Clamp((float)v.newValue, 0f, 1000f * 1000f);
                adAreaField.SetValueWithoutNotify(clampedAdArea);
                AdAreaEdited?.Invoke(index, clampedAdArea);
            }

            private void OnAdAreaUpButtonClicked()
            {
                adAreaField.value += 0.01f;
            }

            private void OnAdAreaDownButtonClicked()
            {
                // 0未満にはしない
                adAreaField.value = Math.Max(0, adAreaField.value - 0.01f);
            }
        }

        private const int MinPartitions = 1;
        private const int MaxPartitions = 3;

        public event Action<bool> ShowRequested = delegate { };
        public event Action<SplitHeightInfo> SplitHeightEdited = delegate { };
        public event Action AddSplitHeightRequested = delegate { };
        public event Action<int> DeleteSplitHeightRequested = delegate { };
        public event Action<SegmentAdAreaInfo> AdAreaEdited = delegate { };

        private readonly VisualElement panel;
        private readonly List<PartitionView> partitions = new();
        private readonly Button addPartitionButton;
        private readonly List<AreaView> areas = new();


        private bool isActive = false;
        private bool isVisible;
        private bool callbacksRegistered;

        public bool IsActive
        {
            get => isActive;
            set
            {
                if (isActive == value) return;
                isActive = value;
                if (!isActive)
                {
                    Show(false);
                }
            }
        }

        public AdAreaCalculateWallPartitionUIView(VisualElement root)
        {
            panel = root.Q("SplitPlanePanel");

            var partition1 = panel.Q<VisualElement>("PlaneSplitField01");
            Assert.IsNotNull(partition1, "PlaneSplitField01 is null");
            partitions.Add(new PartitionView(partition1, 0));

            var partition2 = panel.Q<VisualElement>("PlaneSplitField02");
            Assert.IsNotNull(partition2, "PlaneSplitField02");
            partitions.Add(new PartitionView(partition2, 1));

            var partition3 = panel.Q<VisualElement>("PlaneSplitField03");
            Assert.IsNotNull(partition3, "PlaneSplitField03 is null");
            partitions.Add(new PartitionView(partition3, 2));

            addPartitionButton = panel.Q<Button>("OKButton");
            Assert.IsNotNull(addPartitionButton, "OKButton is null");

            var area1 = panel.Q<VisualElement>("Plane_01");
            Assert.IsNotNull(area1, "Plane_01 is null");
            areas.Add(new AreaView(area1, 0));

            var area2 = panel.Q<VisualElement>("Plane_02");
            Assert.IsNotNull(area2, "Plane_02 is null");
            areas.Add(new AreaView(area2, 1));

            var area3 = panel.Q<VisualElement>("Plane_03");
            Assert.IsNotNull(area3, "Plane_03 is null");
            areas.Add(new AreaView(area3, 2));

            var area4 = panel.Q<VisualElement>("Plane_04");
            Assert.IsNotNull(area4, "Plane_04 is null");
            areas.Add(new AreaView(area4, 3));

            foreach (var area in areas)
            {
                area.Show(false);
            }
        }

        public void SetHeights(ReadOnlyArray<float> heights)
        {
            for (int i = 0; i < heights.Count; i++)
            {
                partitions[i].SetHeight(heights[i], notify: false);
            }

            for (int i = 0; i < partitions.Count; i++)
            {
                partitions[i].Show(i < heights.Count);
            }
            addPartitionButton.SetEnabled(heights.Count < MaxPartitions);
        }

        public void SetSegments(List<WallSegment> segments)
        {
            for (int i = 0; i < areas.Count; i++)
            {
                areas[i].Show(i < segments.Count);
                if (i < segments.Count)
                {
                    var segment = segments[i];
                    areas[i].SetHeight(segment.Height);
                    areas[i].SetWidth(segment.Width);
                    areas[i].SetArea(segment.Area);
                    areas[i].SetAdArea(segment.AdArea, notify: false);
                    areas[i].SetAdAreaRatio(segment.AdAreaRatio * 100f);
                }
            }
        }

        public void Show(bool isShow)
        {
            if (isVisible == isShow) return;
            isVisible = isShow;
            panel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            if (isShow)
            {
                RegisterCallbacks();
            }
            else
            {
                UnregisterCallbacks();
            }

            ShowRequested?.Invoke(isShow);
        }

        private void RegisterCallbacks()
        {
            if (callbacksRegistered) return; // D: idempotent registration
            for (int i = 0; i < partitions.Count; i++)
            {
                partitions[i].HeightEdited += OnPartitionHeightEdited;
                partitions[i].DeleteButtonClicked += OnPartitionDeleteButtonClicked;
            }
            addPartitionButton.clicked += OnAddPartitionButtonClicked;
            for (int i = 0; i < areas.Count; i++)
            {
                areas[i].AdAreaEdited += OnAdAreaEdited;
            }
            callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (!callbacksRegistered) return;
            for (int i = 0; i < partitions.Count; i++)
            {
                partitions[i].HeightEdited -= OnPartitionHeightEdited;
                partitions[i].DeleteButtonClicked -= OnPartitionDeleteButtonClicked;
            }
            addPartitionButton.clicked -= OnAddPartitionButtonClicked;
            for (int i = 0; i < areas.Count; i++)
            {
                areas[i].AdAreaEdited -= OnAdAreaEdited;
            }
            callbacksRegistered = false;
        }

        private void OnPartitionHeightEdited(int index, float newHeight)
        {
            SplitHeightEdited?.Invoke(new SplitHeightInfo(index, newHeight));
        }

        private void OnPartitionDeleteButtonClicked(int index)
        {
            DeleteSplitHeightRequested?.Invoke(index);
        }

        private void OnAddPartitionButtonClicked()
        {
            AddSplitHeightRequested?.Invoke();
        }

        private void OnAdAreaEdited(int index, float newAdArea)
        {
            AdAreaEdited?.Invoke(new SegmentAdAreaInfo(index, newAdArea));
        }
    }
}
