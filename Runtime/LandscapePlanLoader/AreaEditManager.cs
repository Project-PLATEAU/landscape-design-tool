using iShape.Geometry.Polygon;
using UnityEngine;
using System.Collections.Generic;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 読み込んだ区画の編集を行うクラス
    /// </summary>
    public sealed class AreaEditManager
    {
        private AreaProperty editingAreaProperty;
        private int editingAreaIndex = -1;

        /// <summary>
        /// 区画IDを取得するメソッド
        /// </summary>
        public int GetAreaID()
        {
            if (editingAreaProperty == null) return -1;
            return editingAreaProperty.ID;
        }

        /// <summary>
        /// 区画の頂点の座標を取得するメソッド
        /// </summary>
        public Vector3 GetPointData(int pointIndex)
        {
            if (editingAreaProperty == null) return Vector3.zero;
            // PointDataの要素を返す
            return editingAreaProperty.PointData[0][pointIndex];
        }

        /// <summary>
        /// 区画の頂点の数を取得するメソッド
        /// </summary>
        public int GetPointCount()
        {
            if (editingAreaProperty == null) return 0;
            return editingAreaProperty.PointData[0].Count;
        }

        /// <summary>
        /// 区画の頂点の座標を変更し、オブジェクトに反映するメソッド
        /// </summary>
        /// <param name="newPoint">新規に設定する頂点の座標</param>
        public void EditPointData(List<Vector3> newPointData)
        { 
            if (editingAreaProperty == null) return;
            editingAreaProperty.PointData[0] = newPointData;
        }

        /// <summary>
        /// 区画の制限高さを変更し、オブジェクトに反映するメソッド
        /// </summary>
        /// <param name="newHeight">新規に設定する制限高さ</param>
        public void ChangeHeight(float newHeight)
        {
            if (editingAreaProperty == null) return;

            newHeight = Mathf.Clamp(newHeight, 0, editingAreaProperty.WallMaxHeight);
            editingAreaProperty.LimitHeight = newHeight;

             editingAreaProperty.SetLocalPosition(
                 new Vector3(
                 editingAreaProperty.Transform.localPosition.x,
                 newHeight,
                 editingAreaProperty.Transform.localPosition.z
                 ));

            editingAreaProperty.WallMaterial.SetFloat("_DisplayRate", newHeight / editingAreaProperty.WallMaxHeight);
            editingAreaProperty.WallMaterial.SetFloat("_LineCount", newHeight / editingAreaProperty.LineOffset);
            
            // 建物の高さも反映
            AreasDataComponent.ApplyBuildingHeight(editingAreaIndex, true);
        }

        /// <summary>
        /// 編集の対象となる区画データを指定するメソッド
        /// </summary>
        /// <param name="targetAreaIndex">対象区画のAreaPropertyリストの要素番号(-1の場合は指定を解除する)</param>
        public void SetEditTarget(int targetAreaIndex)
        {
            if(targetAreaIndex == -1)
            {
                editingAreaProperty = null;
            }
            else
            {
                editingAreaProperty = AreasDataComponent.GetProperty(targetAreaIndex);
            }

            editingAreaIndex = targetAreaIndex;
        }

        /// <summary>
        /// 区画の制限高さの最大値を取得するメソッド
        /// </summary>
        /// <returns>制限高さの最大値(編集対象未セット時はnullを返す)</returns>
        public float? GetMaxHeight()
        {
            if (editingAreaProperty == null) return null;
            return editingAreaProperty.WallMaxHeight;
        }

        /// <summary>
        /// 現在の制限高さ値を取得するメソッド
        /// </summary>
        /// <returns>現在の制限高さ値(編集対象未セット時はnullを返す)</returns>
        public float? GetLimitHeight()
        {
            if (editingAreaProperty == null) return null;
            return editingAreaProperty.LimitHeight;
        }

        /// <summary>
        /// 区画の壁マテリアルを取得するメソッド
        /// </summary>
        public Material GetWallMaterial()
        {
            if (editingAreaProperty == null) return null;
            return editingAreaProperty.WallMaterial;
        }

        /// <summary>
        /// 現在の区画名を取得するメソッド
        /// </summary>
        /// <returns>現在の区画名(編集対象未セット時はnullを返す)</returns>
        public string GetAreaName()
        {
            if (editingAreaProperty == null) return null;
            return editingAreaProperty.Name;
        }

        /// <summary>
        /// 区画の名前を変更するメソッド
        /// </summary>
        /// <param name="newName"> 新しいエリア名 </param>
        public void ChangeAreaName(string newName)
        {
            if (editingAreaProperty == null) return;
            editingAreaProperty.Name = newName;
        }

        /// <summary>
        /// 区画の色を取得するメソッド
        /// </summary>
        public Color GetColor()
        {
            if (editingAreaProperty == null) return Color.white;
            return editingAreaProperty.Color;
        }

        /// <summary>
        /// 区画の色を変更するメソッド
        /// </summary>
        /// <param name="newColor"></param>
        public void ChangeColor(Color newColor)
        {
            if (editingAreaProperty == null) return;
            editingAreaProperty.Color = newColor;
            editingAreaProperty.CeilingMaterial.SetColor("_Color", newColor);
            editingAreaProperty.WallMaterial.SetColor("_Color", newColor);
        }

        /// <summary>
        /// 対象区画の全プロパティを読み込み時の値に初期化するメソッド
        /// </summary>
        public void ResetProperty()
        {
            if(editingAreaIndex == -1) return;
            AreasDataComponent.TryResetProperty(editingAreaIndex);
        }

        /// <summary>
        /// 対象区画のプロパティを更新を確定されるメソッド
        /// </summary>
        public void ConfirmUpdatedProperty()
        {
            if (editingAreaIndex == -1) return;
            AreasDataComponent.TryUpdateSnapshotProperty(editingAreaIndex);
        }

        /// <summary>
        /// 対象区画の建物の高さを適用
        /// </summary>
        public void ApplyBuildingHeight(bool isApply)
        {
            if (editingAreaIndex == -1) return;
            AreasDataComponent.ApplyBuildingHeight(editingAreaIndex, isApply);
        }

        /// <summary>
        /// 対象区画のリスト番号を取得するメソッド
        /// </summary>
        public int GetEditingAreaIndex()
        {
            return editingAreaIndex;
        }
    }
}
