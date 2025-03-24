using Landscape2.Runtime.BuildingEditor;
using Landscape2.Runtime.UiCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BuildingDeleteListUI : ISubComponent
    {
        public class DeleteListElement : IDisposable
        {
            public System.Action<GameObject> OnListClick { get; set; }
            public System.Action<GameObject> OnButtonClick { get; set; }

            public VisualElement Element { get; private set; }

            GameObject target;



            public DeleteListElement(TemplateContainer container, GameObject obj)
            {
                target = obj;
                var button = container.Q<Toggle>("Toggle_HideList");

                button.RegisterCallback<ChangeEvent<bool>>(evt =>
                {
                    OnButtonClick?.Invoke(target);
                });

                var listElement = container.Q<VisualElement>("List");

                listElement.RegisterCallback<ClickEvent>(evt =>
                {
                    OnListClick?.Invoke(target);
                });

                var nameLabel = container.Q<Label>("Name");
                nameLabel.text = obj.name;

                Element = container;
            }

            public void Dispose()
            {
                target = null;
            }
        }

        /// <summary>
        /// リスト内目玉ボタン押下時コールバック
        /// </summary>
        public System.Action<GameObject> OnClickShowButton { get; set; }

        /// <summary>
        /// リスト選択時コールバック
        /// </summary>
        public System.Action<GameObject> OnClickListElement { get; set; }


        VisualElement rootElement;

        VisualElement listRootElement;


        Label emptyLabel;


        VisualTreeAsset listObjectInstance = Resources.Load<VisualTreeAsset>("List_DeleteBuilding");


        DeleteListElement ListElementFactory(GameObject obj)
        {
            var listObj = listObjectInstance.CloneTree();
            var elem = new DeleteListElement(listObj, obj);

            return elem;
        }


        public BuildingDeleteListUI(VisualElement element, BuildingTRSEditor editor)
        {
            rootElement = element.Q<VisualElement>("Panel_DeleteBuilding");
            emptyLabel = rootElement.Q<Label>("Dialogue");

            var listRoot = rootElement.Q<VisualElement>("unity-content-container");
            listRootElement = listRoot;
            var list = listRoot.Query<TemplateContainer>().ToList();

            // 既存のリストは全て削除
            for (int i = 0; i < list.Count; ++i)
            {
                var content = list[i];
                listRoot.Remove(content);
            }


            // 非表示処理
            element.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (element.style.display == DisplayStyle.None)
                {
                    OnDisable();
                }
                else if (element.style.display == DisplayStyle.Flex)
                {
                    OnEnable();
                }
            });
            
            // ビルディングデータのロード完了時
            BuildingsDataComponent.BuildingDataLoaded += UpdateBuildings;
            
            // ビルディングデータの削除時
            BuildingsDataComponent.BuildingDataDeleted += DeleteBuildings;
            
            Show(false);
        }


        public void AppendList(GameObject obj, bool isVisible)
        {
            if (listRootElement == null)
            {
                Debug.LogWarning($"listRootElementがnullです");
                return;
            }

            var elem = ListElementFactory(obj);
            if (isVisible)
            {
                elem.OnButtonClick += (go) =>
                {
                    var parentName = elem.Element.parent != null ? elem.Element.parent.name : "null";

                    listRootElement.Remove(elem.Element);
                    OnClickShowButton?.Invoke(go);
                };
            }
            else
            {
                elem.Element.Q<Toggle>("Toggle_HideList").style.display = DisplayStyle.None;
            }

            elem.OnListClick += (go) =>
            {
                OnClickListElement?.Invoke(go);
            };

            listRootElement.Add(elem.Element);
        }

        private void UpdateBuildings()
        {
            listRootElement.Clear();
            for (int i = 0; i < BuildingsDataComponent.GetPropertyCount(); i++)
            {
                var property = BuildingsDataComponent.GetProperty(i);
                AppendBuilding(property);
            }
        }

        private void AppendBuilding(BuildingProperty property)
        {
            if (!property.IsDeleted)
            {
                return;
            }

            var cityObjectGroup = CityModelHandler.GetCityObjectGroup(property.GmlID);
            if (cityObjectGroup == null)
            {
                return;
            }
            AppendList(cityObjectGroup.gameObject, property.IsEditable);
        }

        private void DeleteBuildings(List<BuildingProperty> deleteBuildings, string projectID)
        {
            listRootElement.Clear();
            for (int i = 0; i < BuildingsDataComponent.GetPropertyCount(); i++)
            {
                var property = BuildingsDataComponent.GetProperty(i);
                if (deleteBuildings.Any(n => n.ID == property.ID))
                {
                    continue;
                }
                AppendBuilding(property);
            }
        }

        public void RemoveList(int index)
        {
        }

        public void UpdateList(List<GameObject> hideObjectList)
        {


        }

        public void ShowListEmpty(bool show)
        {
            emptyLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }


        public void OnDisable()
        {
            Show(false);
        }

        public void OnEnable()
        {
            Show(true);
        }

        public void Update(float deltaTime)
        {
        }

        public void Start()
        {
        }

        public void Show(bool flag)
        {
            if (rootElement != null)
            {
                rootElement.style.display = flag ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void LateUpdate(float deltaTime)
        {
        }
    }
}
