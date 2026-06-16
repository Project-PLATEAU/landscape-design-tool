using System;
using System.Collections.Generic;
using System.Linq;
using Landscape2.Runtime.AdAreaCalcurateSub;
using UnityEngine.InputSystem.Utilities;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 壁面分割 Presenter。
    /// ISubComponent ライフサイクルでイベント購読/解除を管理する。
    /// viewの表示非表示だけはAdAreaCalcurateUIで管理する。
    /// </summary>
    public class AdAreaCalculateWallPartitionUI : ISubComponent
    {
        private readonly IAdAreaCalculateModel model;
        private readonly AdAreaCalculateWallPartition partitionModel;
        private readonly IAdAreaCalculateWallPartitionUIView view;
        private readonly IAdAreaCalculateWallPartitionVisualizer visualizer; // 高さ帯可視化
        private bool subscribed;

        public AdAreaCalculateWallPartitionUI(
            IAdAreaCalculateModel model,
            AdAreaCalculateWallPartition partitionModel,
            IAdAreaCalculateWallPartitionUIView view,
            IAdAreaCalculateWallPartitionVisualizer visualizer)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.partitionModel = partitionModel ?? throw new ArgumentNullException(nameof(partitionModel));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
        }

        private void RegisterCallbacks()
        {
            if (subscribed) return;
            model.OnWallSelected += OnWallSelected;
            view.SplitHeightEdited += OnSplitHeightEdited;
            view.AddSplitHeightRequested += OnAddSplitHeightRequested;
            view.DeleteSplitHeightRequested += OnDeleteHeightRequested;
            view.ShowRequested += OnShowRequested;
            view.AdAreaEdited += OnAdAreaEdited;
            partitionModel.Computed += OnComputed;
            subscribed = true;
        }

        private void UnregisterCallbacks()
        {
            if (!subscribed) return;
            partitionModel.Computed -= OnComputed;
            view.AdAreaEdited -= OnAdAreaEdited;
            view.ShowRequested -= OnShowRequested;
            view.DeleteSplitHeightRequested -= OnDeleteHeightRequested;
            view.AddSplitHeightRequested -= OnAddSplitHeightRequested;
            view.SplitHeightEdited -= OnSplitHeightEdited;
            model.OnWallSelected -= OnWallSelected;
            subscribed = false;
        }

        private void OnWallSelected(IAdAreaCalculateModel.SelectMode mode, ReadOnlyArray<Wall> walls, ReadOnlyArray<OrientedBounds> bounds)
        {
            if (mode != IAdAreaCalculateModel.SelectMode.PartitionWall) return;

            partitionModel.SetTarget(walls, bounds);
            view.Show(walls.Count > 0);
            visualizer.ShowHeights(partitionModel.SplitHeights, partitionModel.WallBoundsList);
        }

        private void OnSplitHeightEdited(SplitHeightInfo info)
        {
            partitionModel.SetHeight(info.Index, info.Height);
            var heights = partitionModel.SplitHeights;
            var bounds = partitionModel.WallBoundsList;
            visualizer.ShowHeights(heights, bounds);
        }

        private void OnAddSplitHeightRequested()
        {
            partitionModel.AddSplitHeight();
        }

        private void OnDeleteHeightRequested(int index)
        {
            partitionModel.DeleteSplitHeight(index);
        }

        private void OnShowRequested(bool isShow)
        {
            if (isShow)
            {
                view.SetHeights(partitionModel.SplitHeights);
                if (partitionModel.WallBoundsList.Count > 0)
                {
                    visualizer.ShowHeights(partitionModel.SplitHeights, partitionModel.WallBoundsList);
                }
            }
            else
            {
                visualizer.Clear();
                partitionModel.Reset();
            }
        }

        private void OnAdAreaEdited(SegmentAdAreaInfo info)
        {
            partitionModel.SetAdArea(info.Index, info.AdArea);
        }

        private void OnComputed(PartitionData data)
        {
            view.SetSegments(data.Segments.ToList());
            view.SetHeights(partitionModel.SplitHeights);
            if (partitionModel.WallBoundsList.Count > 0)
            {
                visualizer.ShowHeights(data.SplitHeights, partitionModel.WallBoundsList);
            }
        }

        // ISubComponent implementations (explicit for consistency with existing pattern)
        void ISubComponent.OnEnable()
        {
            RegisterCallbacks();
        }

        void ISubComponent.OnDisable()
        {
            UnregisterCallbacks();
        }

        void ISubComponent.Start() { }
        void ISubComponent.Update(float deltaTime) { }
        void ISubComponent.LateUpdate(float deltaTime) { }
    }
}
