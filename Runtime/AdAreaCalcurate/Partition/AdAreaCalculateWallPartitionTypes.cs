using System;
using System.Collections.Generic;
using Landscape2.Runtime.AdAreaCalcurateSub;
using UnityEngine.InputSystem.Utilities;

namespace Landscape2.Runtime
{
    public struct WallSegment
    {
        /// <summary>
        /// Segmentの開始高さ
        /// </summary>
        public float From { get; }

        /// <summary>
        /// Segmentの終了高さ
        /// </summary>
        public float To { get; }

        /// <summary>
        /// Segmentの高さ (To - From)
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Segmentの幅
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Segmentの面積
        /// </summary>
        public float Area { get; }

        /// <summary>
        /// Segmentの広告面積
        /// </summary>
        public float AdArea { get; private set; }

        /// <summary>
        /// Segmentの広告面積率 (AdArea / Area)
        /// </summary>
        public float AdAreaRatio { get; private set; }

        public WallSegment(float from, float to, float height, float width, float area, float adArea, float adAreaRatio)
        {
            From = from;
            To = to;
            Height = height;
            Width = width;
            Area = area;
            AdArea = adArea;
            AdAreaRatio = adAreaRatio;
        }

        public WallSegment(float from, float to, float height, float width, float area)
        {
            From = from;
            To = to;
            Height = height;
            Width = width;
            Area = area;

            AdArea = 0f;
            AdAreaRatio = 0f;
        }

        public void SetAdArea(float adArea)
        {
            AdArea = adArea;
            AdAreaRatio = (Area > 0f) ? AdArea / Area : 0f;
        }
    }

    /// <summary>
    /// 分割結果セット
    /// </summary>
    public readonly struct PartitionData
    {
        public ReadOnlyArray<float> SplitHeights { get; }
        public ReadOnlyArray<WallSegment> Segments { get; }

        public PartitionData(
            ReadOnlyArray<float> splitHeights,
            ReadOnlyArray<WallSegment> segments)
        {
            SplitHeights = splitHeights;
            Segments = segments;
        }
    }

    /// <summary>
    /// 壁面分割面積算出サービスインターフェース
    /// </summary>
    public interface IAdAreaCalculateWallPartition
    {
        event Action<PartitionData> Computed;

        ReadOnlyArray<float> SplitHeights { get; }

        void SetTarget(ReadOnlyArray<Wall> walls, ReadOnlyArray<OrientedBounds> bounds);
        void SetHeight(int index, float height);
        void AddSplitHeight();
        void DeleteSplitHeight(int index);
        void SetAdArea(int index, float area);

        void Reset();
    }
}