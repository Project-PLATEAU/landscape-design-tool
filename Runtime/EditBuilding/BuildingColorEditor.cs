using iShape.Geometry.Polygon;
using Landscape2.Runtime.Common;
using PLATEAU.CityInfo;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace Landscape2.Runtime.BuildingEditor
{
    /// <summary>
    /// 建築物の色彩を編集
    /// UIは<see cref="BuildingColorEditorUI"/>が担当
    /// </summary>
    /// 
    public class BuildingColorEditor : ISubComponent
    {
        // 色彩を変更する対象のオブジェクト
        private GameObject targetObject;
        // 編集中のマテリアル
        private List<Material> editingMaterials = new List<Material>();

        // 地物型リストのマテリアル
        private List<Material> buildingFieldMaterials = new List<Material>();

        // Renderer.materialによって複製されたマテリアルを管理するリスト
        private List<Material> copiedMaterials = new List<Material>();

        // 色彩編集パネル表示ボタンの初期色
        private static readonly Color initialColor = new Color32(186, 186, 186, 255);
        public static Color InitialColor { get => initialColor; }
        // Smoothnessスライダーの初期値
        private static readonly float initialSmoothness = 0.7f;
        public static float InitialSmoothness { get => initialSmoothness; }

        public BuildingColorEditor()
        {
            BuildingsDataComponent.BuildingDataLoaded += LoadBuildingColor;
            BuildingsDataComponent.BuildingDataDeleted += DeleteBuildingColor;
        }
        // UIで色を変更したときに呼び出される
        // 建築物のRGB値を変更
        public void EditMaterialColor(Color color, float smoothness, Action<string> showWarning = null)
        {
            if (targetObject != null)
            {
                // Renderer.materialにアクセスすることでマテリアルを複製し，元のMaterialを変更せずに色を変更する
                // 選択中のマテリアルの色を変更
                if (editingMaterials != null)
                {
                    // editingMaterialsの要素でループ
                    foreach (var mat in editingMaterials)
                    {
                        // 色とsmoothnessを変更
                        mat.color = color;
                        mat.SetFloat("_Smoothness", smoothness);
                    }
                    RecordMaterialColor(targetObject, buildingFieldMaterials);
                    
                    // 最小レイヤーの値を取得して更新
                    var gmlID = CityObjectUtil.GetGmlID(targetObject);
                    var minLayerValues = BuildingsDataComponent.GetMinLayerPropertyValues(gmlID);
                    foreach (var mat in editingMaterials)
                    {
                        // 色とsmoothnessを変更
                        mat.color = minLayerValues.colors!.First();
                        mat.SetFloat("_Smoothness", minLayerValues.smoothness!.First());
                    }

                    // 最小のレイヤーでなければsnackbarを表示
                    var lowestProjectInfo = BuildingsDataComponent.GetLowestLayerProjectNamesByGmlID(gmlID);
                    if (!string.IsNullOrEmpty(lowestProjectInfo.projectName) && 
                        lowestProjectInfo.projectID != ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID)
                    {
                        showWarning?.Invoke(lowestProjectInfo.projectName);
                    }
                }
            }
        }

        // 建物が選択されたときに呼び出される
        // 選択中のオブジェクトのマテリアルを編集中リストに設定
        public int SetMaterialList(GameObject targetObject)
        {
            buildingFieldMaterials.Clear();
            this.targetObject = targetObject;

            buildingFieldMaterials.AddRange(this.targetObject.GetComponent<MeshRenderer>().materials);

            foreach (var mat in editingMaterials)
            {
                // 編集中のマテリアルをリストに追加
                if (!copiedMaterials.Contains(mat))
                {
                    copiedMaterials.Add(mat);
                }
            }
            // 編集中のマテリアルをリストに追加
            ChangeEditingMaterial(-1);

            // マテリアル分けされているかそうでないかでリストの要素数を変更
            if (buildingFieldMaterials.Count == 1)
            {
                return 1;
            }
            else
            {
                // buildingFieldMaterialsの先頭2つ(壁面，屋根面)以降の要素を削除
                buildingFieldMaterials.RemoveRange(2, buildingFieldMaterials.Count - 2);
                return 3;
            }
        }

        // 地物型選択リストが変更された時に呼び出される
        // 編集中のマテリアルを変更
        public void ChangeEditingMaterial(int id)
        {
            editingMaterials.Clear();

            // "要素全体"が選択された場合，全てのマテリアルを編集対象にする
            if (id == 0)
            {
                editingMaterials.AddRange(buildingFieldMaterials);
            }
            else if (id > 0)
            {
                // 選択された要素のみを編集対象にする
                editingMaterials.Add(buildingFieldMaterials[id - 1]);
            }
        }

        // マテリアルのSmoothnessを変更
        public void EditMaterialSmoothness(float value)
        {
            // 選択中のマテリアルのSmoothnessを変更
            if (editingMaterials.Count != 0)
            {
                foreach (var mat in editingMaterials)
                {
                    mat.SetFloat("_Smoothness", value);
                }
            }
        }

        // 選択された要素の色を取得
        public Color GetMaterialColor()
        {
            if (editingMaterials.Count == 2) // 要素全体
            {
                if (editingMaterials[0].color == editingMaterials[1].color)
                {
                    return editingMaterials[0].color;
                }
            }
            else if (editingMaterials.Count == 1) // 壁面，屋根面
            {
                return editingMaterials[0].color;
            }
            // 未選択のとき，もしくは壁面と屋根面の色が異なる場合
            return initialColor;
        }

        // 選択された要素のSmoothnessを取得
        public float GetMaterialSmoothness()
        {
            if (editingMaterials.Count == 2) // 要素全体
            {
                if (editingMaterials[0].GetFloat("_Smoothness") == editingMaterials[1].GetFloat("_Smoothness"))
                {
                    return editingMaterials[0].GetFloat("_Smoothness");
                }
            }
            else if (editingMaterials.Count == 1) // 壁面，屋根面
            {
                return editingMaterials[0].GetFloat("_Smoothness");
            }
            // 未選択のとき，もしくは壁面と屋根面の色が異なる場合
            return 0f;
        }

        // 建物の色彩とSmoothnessをロード
        private void LoadBuildingColor()
        {
            var buildings = GameObject.FindObjectsOfType<PLATEAUCityObjectGroup>();

            if (buildings == null)
            {
                Debug.LogWarning("建築物モデルが見つかりませんでした。");
                return;
            }

            PLATEAUCityObjectGroup targetBuilding = null;

            // データ数を取得
            int dataCount = BuildingsDataComponent.GetPropertyCount();
            for (int i = 0; i < dataCount; i++)
            {
                var property = BuildingsDataComponent.GetProperty(i);
                
                // 最小のレイヤー値を取得
                var minValues = BuildingsDataComponent.GetMinLayerPropertyValues(property.GmlID);
                
                string gmlID = property.GmlID;
                List<Color> colors = minValues.colors;
                List<float> smoothness = minValues.smoothness;

                // Scene上の建物からGMLIDに一致する建物を取得
                foreach (var building in buildings)
                {
                    var cityObjs = building.GetAllCityObjects();
                    foreach (var cityObj in cityObjs)
                    {
                        if (cityObj.GmlID == gmlID)
                        {
                            targetBuilding = building;
                            break;
                        }
                    }
                }

                if (targetBuilding == null)
                {
                    Debug.LogWarning("gmlID:" + gmlID + "が見つかりませんでした。");
                    return;
                }

                // 建物の色を適用
                var mats = targetBuilding.gameObject.GetComponent<MeshRenderer>().materials;
                if (mats == null)
                {
                    Debug.LogWarning("建築物のマテリアルが見つかりませんでした。");
                    return;
                }

                // Scene上の建物のマテリアルの数と保存されている色の数が一致しない場合保存数が少ない方に合わせる
                int matCount = mats.Length < colors.Count ? mats.Length : colors.Count;
                for (int j = 0; j < matCount; j++)
                {
                    mats[j].color = colors[j];
                    mats[j].SetFloat("_Smoothness", smoothness[j]);
                }
            }
        }
        
        private void DeleteBuildingColor(List<BuildingProperty> deleteBuildings, string projectID)
        {
            foreach (var deleteBuilding in deleteBuildings)
            {
                var cityObjectGroup = CityModelHandler.GetCityObjectGroup(deleteBuilding.GmlID);
                if (cityObjectGroup == null)
                {
                    continue;
                }

                var minLayerValues = BuildingsDataComponent.GetMinLayerPropertyValues(deleteBuilding.GmlID, projectID);
                // 最小レイヤーを設定
                SetBuildingParameter(
                    cityObjectGroup.gameObject,
                    new BuildingProperty(deleteBuilding.GmlID,
                        minLayerValues.colors,
                        minLayerValues.smoothness,
                        minLayerValues.isDeleted));
            }
        }

        private void ResetBuildingParameter(GameObject targetBuilding)
        {
            var mats = targetBuilding.gameObject.GetComponent<MeshRenderer>().materials;
            if (mats == null)
            {
                Debug.LogWarning("建築物のマテリアルが見つかりませんでした。");
                return;
            }
            foreach (var material in mats)
            {
                // 初期値にセット
                material.color = InitialColor;
                material.SetFloat("_Smoothness", InitialSmoothness);
            }
        }
        
        private void SetBuildingParameter(GameObject targetBuilding, BuildingProperty property)
        {
            var mats = targetBuilding.gameObject.GetComponent<MeshRenderer>().materials;
            if (mats == null)
            {
                Debug.LogWarning("建築物のマテリアルが見つかりませんでした。");
                return;
            }

            int matCount = mats.Length < property.ColorData.Count ? mats.Length : property.ColorData.Count;
            for (int i = 0; i < matCount; i++)
            {
                mats[i].color = property.ColorData[i];
                mats[i].SetFloat("_Smoothness", property.SmoothnessData[i]);
            }
        }
        
        // 色彩編集内容を記録する
        public void RecordMaterialColor(GameObject buildingObj, List<Material> mats)
        {
            string gmlID = CityObjectUtil.GetGmlID(buildingObj);
            if (gmlID == null)
            {
                Debug.LogWarning("gmlIDが見つかりませんでした。");
                return;
            }

            var colors = new List<Color>();
            var smoothness = new List<float>();
            foreach (var mat in mats)
            {
                colors.Add(mat.color);
                smoothness.Add(mat.GetFloat("_Smoothness"));
            }

            // 既に登録されている建物ならば
            if (BuildingsDataComponent.IsContainsProperty(gmlID))
            {
                // 変更内容を記録
                BuildingsDataComponent.TryApplyBuildingEdit(gmlID, colors, smoothness);
            }
            else
            {
                //新規のBuildingPropertyを生成
                var buildingProperty = new BuildingProperty(
                    gmlID,
                    colors,
                    smoothness,
                    false
                    );
                BuildingsDataComponent.AddNewProperty(buildingProperty);
            }
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
            // copiedMaterialsのマテリアルを破棄
            foreach (var mat in copiedMaterials)
            {
                GameObject.Destroy(mat);
            }
            copiedMaterials.Clear();
            targetObject = null;
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
