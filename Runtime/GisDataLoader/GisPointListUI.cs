using Landscape2.Runtime.UiCommon;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// GISデータのポイントリストのプレゼンター
    /// </summary>
    public class GisPointListUI
    {
        private VisualElement rootPanel;
        private ScrollView scrollPanel;
        private VisualTreeAsset itemVisualTreeAsset;
        private GisPointInfos gisPointInfos;
        
        private readonly List<VisualElement> gisPointList = new();
        
        public GisPointListUI(GisPointInfos gisPointInfos, VisualElement root)
        {
            rootPanel = root.Q<VisualElement>("Panel_GisList");
            scrollPanel = rootPanel.Q<ScrollView>("Panel");
            
            this.gisPointInfos = gisPointInfos;
            
            // リストのアイテムのVisualTreeAssetをロード
            itemVisualTreeAsset = Resources.Load<VisualTreeAsset>("List_Gis");
            
            RegisterEvents();
        }
        
        public void RegisterEvents()
        {
            gisPointInfos.OnDeleteAll.AddListener(DeleteAll);
            gisPointInfos.OnCreate.AddListener((attributeID) =>
            {
                var info = gisPointInfos.GetByAttributeFirst(attributeID);
                if (info != null)
                {
                    CreateItem(info);
                }
            });
            gisPointInfos.OnUpdateEditable.AddListener(SetEditable);
            gisPointInfos.OnDelete.AddListener((attributeID) =>
            {
                Delete(attributeID, false);
            });
        }
        
        private string GetItemName(string attributeID) => $"GIS_{attributeID}";
        private string GetAttributeID(string itemName) => itemName.Replace("GIS_", "");

        private void CreateItem(GisPointInfo info)
        {
            var item = itemVisualTreeAsset.CloneTree();
            
            // IDを使って名前を決定
            item.name = GetItemName(info.AttributeID);
            
            // 属性タイトルセット
            var gisName = item.Q<Label>("GisName");
            gisName.text = info.DisplayName;
            
            // ピンの色セット
            var pin = item.Q<VisualElement>("Icon_Pin");
            pin.style.unityBackgroundImageTintColor = new StyleColor(info.Color);
            
            // 非表示ボタン
            var toggleButton = item.Q<Toggle>("Toggle_HideList");
            toggleButton.RegisterValueChangedCallback((evt) =>
            {
                // 地図上のピン表示切り替え
                gisPointInfos.SetShow(info.AttributeID, !evt.newValue);
                
                // プロジェクトに通知
                ProjectSaveDataManager.Edit(ProjectSaveDataType.GisData, info.ID.ToString());
            });
            
            // 削除ボタン
            var deleteButton = item.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>((evt) =>
            {
                // 確認ポップアップ表示
                ModalUI.ShowSelectModal("データ削除します", "本当によろしいですか？", ModalUI.SelectModalType.Info, null, () =>
                {
                    // データ削除
                    gisPointInfos.Delete(info.AttributeID);
                    Delete(info.AttributeID);
                });
            });
            
            gisPointList.Add(item);
            
            // Viewに追加
            scrollPanel.Add(item);
            
            // データがある場合はNoData非表示
            ShowNoData(false);
            
            // 編集不可設定
            SetEditable(info.AttributeID, ProjectSaveDataManager.ProjectSetting.IsEditMode);
        }

        private void SetEditable(string attributeID, bool isEditable)
        {
            var item = gisPointList.Find(i => i.name == GetItemName(attributeID));
            
            // 削除できないように
            item.Q<Button>("DeleteButton").style.display = isEditable ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Delete(string attributeID, bool isShowCompletePopup = true)
        {
            // Indexからアイテムを削除
            var item = gisPointList.Find(i => i.name == GetItemName(attributeID));
            if (item != null)
            {
                scrollPanel.Remove(item);
                gisPointList.Remove(item);
            }
            
            // データがない場合はNoData表示
            if (gisPointList.Count == 0)
            {
                ShowNoData(true);
            }

            if (isShowCompletePopup)
            {
                // ポップアップ表示
                ModalUI.ShowModal("削除しました", "データを削除しました", true, false);
            }
        }

        private void DeleteAll(List<string> deleteAttributeIDs)
        {
            foreach (var deleteAttributeID in deleteAttributeIDs)
            {
                Delete(deleteAttributeID, false);
            }
        }
        
        private void ShowNoData(bool isShow)
        {
            var noDataLabel = rootPanel.Q<Label>("No_Data");
            noDataLabel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}