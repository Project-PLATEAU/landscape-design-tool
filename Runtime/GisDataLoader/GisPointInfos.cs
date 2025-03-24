using Landscape2.Runtime.UiCommon;
using PLATEAU.CityGML;
using PLATEAU.CityInfo;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// GISデータのポイントリスト
    /// </summary>
    public class GisPointInfos
    {
        public List<GisPointInfo> Points { get; private set; } = new();
        
        public UnityEvent<string> OnCreate = new();
        public UnityEvent<string, bool> OnUpdateDisplay = new();
        public UnityEvent<string> OnDelete = new();
        public UnityEvent<List<string>> OnDeleteAll = new();
        public UnityEvent<string, bool> OnUpdateEditable = new();
        
        private const string ErrorTitle = "GISデータ登録";
        private int pointIndex = 0;
        
        public void Regist(string name, List<GisData> gisDataList, int selectAttributeIndex, Color color)
        {
            // 属性ごとのユニークなIDを生成
            var attributeID = System.Guid.NewGuid().ToString();
            
            foreach (var gisData in gisDataList)
            {
                var facilityName = gisData.Attributes[selectAttributeIndex].Value;
                
                // 施設の位置を取得
                var facilityPosition = GetFacilityPosition(gisData.WorldPoints[0]);
                if (facilityPosition == Vector3.zero)
                {
                    Debug.LogWarning($"{facilityName} 施設の位置が取得できませんでした。");
                    continue;
                }

                // プロジェクトに通知
                ProjectSaveDataManager.Add(ProjectSaveDataType.GisData, pointIndex.ToString());
                
                // ポイント情報を登録
                Points.Add(new GisPointInfo(
                    pointIndex,
                    attributeID,
                    facilityName,
                    name,
                    facilityPosition,
                    color,
                    true
                ));
                pointIndex++;
            }

            OnCreate.Invoke(attributeID);
            
            // 完了
            ModalUI.ShowModal("データ読み込み完了", "GISデータを登録しました", true, false, () =>
            {
            }); 
        }

        public GisPointInfo Get(int ID)
        {
            return Points.FirstOrDefault(p => p.ID == ID);
        }

        public GisPointInfo GetByAttributeFirst(string attributeID)
        {
            return Points.FirstOrDefault(p => p.AttributeID == attributeID);
        }

        public List<GisPointInfo> GetAttributeAll(string attributeID)
        {
            return Points.FindAll(p => p.AttributeID == attributeID);
        }

        public void SetShow(string attributeID, bool isVisible, bool isListView = false)
        {
            var points = Points.FindAll(p => p.AttributeID == attributeID);
            foreach (var point in points)
            {
                point.SetShow(isVisible);
            }
            
            OnUpdateDisplay.Invoke(attributeID, isVisible);
        }
        
        public void SetEditable(string attributeID, bool isEditable)
        {
            OnUpdateEditable.Invoke(attributeID, isEditable);
        }

        public void Delete(string attributeID)
        {
            // プロジェクトに通知
            foreach (var gisPointInfo in Points.FindAll(p => p.AttributeID == attributeID))
            {
                ProjectSaveDataManager.Delete(ProjectSaveDataType.GisData, gisPointInfo.ID.ToString());
            }
            
            Points.RemoveAll(p => p.AttributeID == attributeID);
            
            OnDelete.Invoke(attributeID);
        }
        
        public void DeleteAll()
        {
            var deleteAttributeIDs = new List<string>();
            foreach (var gisPointInfo in Points)
            {
                if (ProjectSaveDataManager.TryCheckData(
                        ProjectSaveDataType.GisData,
                        ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID,
                        gisPointInfo.ID.ToString(),
                        false))
                {
                    deleteAttributeIDs.Add(gisPointInfo.AttributeID);
                    ProjectSaveDataManager.Delete(ProjectSaveDataType.GisData, gisPointInfo.ID.ToString());
                }
            }

            // 重複チェック
            deleteAttributeIDs = deleteAttributeIDs.Distinct().ToList();
            
            // 削除
            Points.RemoveAll(point => deleteAttributeIDs.Contains(point.AttributeID));
            OnDeleteAll.Invoke(deleteAttributeIDs);
        }
        
        public void AddPoints(List<GisPointInfo> points, string projectID)
        {
            foreach (var gisPointInfo in points)
            {
                // プロジェクトに通知
                ProjectSaveDataManager.Add(ProjectSaveDataType.GisData, pointIndex.ToString(), projectID, false);
                
                // ポイント情報を登録
                Points.Add(new GisPointInfo(
                    pointIndex,
                    gisPointInfo.AttributeID,
                    gisPointInfo.FacilityName,
                    gisPointInfo.DisplayName,
                    gisPointInfo.FacilityPosition,
                    gisPointInfo.Color,
                    true
                ));
                pointIndex++; 
                
            }
            
            var addAttributeIDs = points.Select(point => point.AttributeID).Distinct();
            foreach (var addAttributeID in addAttributeIDs)
            {
                OnCreate.Invoke(addAttributeID);
            }
        }
        
        private Vector3 GetFacilityPosition(Vector3 worldPosition)
        {
            // レイの設定
            Vector3 rayDirection = Vector3.down;
            const float RayLength = 10000f;
            var rayOrigin = new Vector3(worldPosition.x, RayLength, worldPosition.z);
        
            // 該当の位置からレイを飛ばして、該当の施設を取得
            var ray = new Ray(rayOrigin, Vector3.down);

            // レイキャスト地面を突き抜けるように長めに設定
            RaycastHit[] results = Physics.RaycastAll(ray, RayLength * 2);
            Vector3 hitPosition = Vector3.zero;
            foreach (RaycastHit rayCastHit in results)
            {
                if (rayCastHit.transform.TryGetComponent(out PLATEAUCityObjectGroup cityObject))
                {
                    hitPosition = rayCastHit.transform.position;
                    if (IsFacility(cityObject))
                    {
                        var bounds = cityObject.GetComponent<MeshCollider>().bounds;
                        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                    }
                }
            }
            
            return hitPosition;
        }
        
        private bool IsFacility(PLATEAUCityObjectGroup cityObject)
        {
            // 建物が含まれていれば施設と判定
            return cityObject.CityObjects.rootCityObjects.Any(o => o.CityObjectType == CityObjectType.COT_Building);
        }
    }
}