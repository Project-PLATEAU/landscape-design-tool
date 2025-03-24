using Landscape2.Runtime.Common;
using Landscape2.Runtime.UiCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 建物編集画面時の機能クラス
    /// </summary>
    public class EditBuilding : ISubComponent
    {
        // 建物が選択されたときのイベント関数
        public event Action<GameObject, bool> OnBuildingSelected = (targetObject, canEdit) => { };

        private GameObject targetObject;
        private GameObject highlightBox = null;
        private VisualElement uiRoot;

        private const string UIMaterialPanel = "Panel_MaterialEditor";
        private const string UIDeleteBuildingPanel = "Panel_DeleteBuilding";

        private const string UISelectDeleteButtonPanel = "ContextButtonGroup";
        private const string UISelectDeleteButton = "ContextButton";

        readonly List<VisualElement> panelList = new();

        public EditBuilding(VisualElement uiRoot)
        {
            this.uiRoot = uiRoot;
            var materialPanel = uiRoot.Q<VisualElement>(UIMaterialPanel);
            panelList.Add(materialPanel);
            var deleteBuildingPanel = uiRoot.Q<VisualElement>(UIDeleteBuildingPanel);
            panelList.Add(deleteBuildingPanel);

            // 建物編集画面が閉じられたとき
            uiRoot.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (uiRoot.style.display == DisplayStyle.None)
                {
                    OnDisable();
                }
            });
        }
        public void Update(float deltaTime)
        {
            // 建物編集画面時の処理
            if (uiRoot.style.display == DisplayStyle.Flex)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    var cbg = panelList.Where(x => x.name == UISelectDeleteButton).FirstOrDefault();
                    if (cbg == null)
                    {
                        var contextButtonGroup = uiRoot.parent.Q<VisualElement>(UISelectDeleteButtonPanel);
                        var deleteButton = contextButtonGroup.Q<Button>(UISelectDeleteButton);
                        if (deleteButton != null)
                        {
                            panelList.Add(deleteButton);
                        }
                    }

                    // マウスの座標がパネルの範囲内ある場合は反応しない
                    foreach (var panel in panelList)
                    {
                        var panelBound = panel.worldBound;
                        panelBound.position = new Vector2(panelBound.position.x, Screen.height - panelBound.position.y - panelBound.size.y);
                        if (panelBound.Contains(Input.mousePosition))
                        {
                            return;
                        }
                    }
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit = new RaycastHit();

                    if (Physics.Raycast(ray, out hit))
                    {
                        // 建築物をクリックした場合
                        if (hit.collider.gameObject.name.Contains("bldg_"))
                        {
                            SetTargetObject(hit.collider.gameObject);
                        }
                    }
                    else
                    {
                        targetObject = null;
                    }
                }
            }
        }

        // 建物選択時のハイライトボックスを生成する
        private void CreateHighlightBox()
        {
            if (highlightBox == null)
            {
                var bbox = Resources.Load("bbox") as GameObject;
                highlightBox = GameObject.Instantiate(bbox);

                MeshFilter mf = highlightBox.GetComponent<MeshFilter>();
                mf.mesh.SetIndices(mf.mesh.GetIndices(0), MeshTopology.LineStrip, 0);
            }

            var meshColider = targetObject.GetComponent<MeshCollider>();
            var bounds = meshColider.bounds;

            highlightBox.transform.localPosition = bounds.center;
            highlightBox.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
        }

        public GameObject GetTargetObject()
        {
            return targetObject;
        }

        public void SetTargetObject(GameObject obj)
        {
            // layerがIgnoreであればプロジェクト外なので、編集不可
            bool canEdit = !LayerMaskUtil.IsIgnore(obj);
            if (!canEdit)
            {
                SnackBarUI.Show(SnackBarUI.NotEditWarning, 
                    uiRoot.Q<VisualElement>("CenterContainer"));
            }
            targetObject = obj;
            CreateHighlightBox();
            
            OnBuildingSelected(targetObject, canEdit);
        }

        public void Start()
        {
        }
        public void OnEnable()
        {
        }
        public void OnDisable()
        {
            targetObject = null;

            if (highlightBox)
            {
                GameObject.Destroy(highlightBox);
                highlightBox = null;
            }
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
