using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画のセーブデータ項目
    /// </summary>
    [Serializable]
    public struct PlanAreaSaveData
    {
        [SerializeField] private int id;
        [SerializeField] private string name;
        [SerializeField] private float limitHeight;
        [SerializeField] private float lineOffset;
        [SerializeField] private Color color;
        [SerializeField] private float wallMaxHeight;
        [SerializeField] private List<List<Vector3>> pointData;

        public int Id { get => id;}
        public string Name { get => name;}
        public float LimitHeight { get => limitHeight;}
        public float LineOffset { get => lineOffset;}
        public Color Color { get => color;}
        public float WallMaxHeight { get => wallMaxHeight;}
        public List<List<Vector3>> PointData { get => pointData;}

        public PlanAreaSaveData(int id, string name, float limitHeight, float lineOffset, Color color, float wallMaxHeight, List<List<Vector3>> pointData)
        {
            this.id = id;
            this.name = name;
            this.limitHeight = limitHeight;
            this.lineOffset = lineOffset;
            this.color = color;
            this.wallMaxHeight = wallMaxHeight;
            this.pointData = pointData;
        }
    }
}
