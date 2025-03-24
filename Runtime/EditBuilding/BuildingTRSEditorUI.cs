using Landscape2.Runtime.Common;
using Landscape2.Runtime.UiCommon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BuildingTRSEditorUI : ISubComponent
    {

        const string TRSElementName = "Context_Edit";

        string transButtonName = "MoveButton";
        string rotateButtonName = "RotateButton";

        string scaleButtonName = "ScaleButton";

        string deleteButtonName = "ContextButton";

        string succeedButtonName = "Button";


        VisualElement trsVisualElement;

        VisualElement editContext;

        GameObject currentSelect;

        public System.Action OnClickTransButton
        {
            get; set;
        }

        public System.Action OnClickRotateButton
        {
            get; set;
        }

        public System.Action OnClickScaleButton
        {
            get; set;
        }

        public System.Action OnClickDeleteButton
        {
            get; set;
        }

        public BuildingTRSEditorUI(EditBuilding editBuilding, VisualElement element)
        {
            trsVisualElement = new UIDocumentFactory().CreateWithUxmlName("ContextButtonGroup");

            var uiElement = trsVisualElement;

            editContext = uiElement.Q<VisualElement>("Context_Edit");

            var transButton = editContext.Q<RadioButton>(transButtonName);
            var rotateButton = editContext.Q<RadioButton>(rotateButtonName);
            var scaleButton = editContext.Q<RadioButton>(scaleButtonName);


            transButton.RegisterCallback<ClickEvent>(e => OnClickTransButton?.Invoke());
            rotateButton.RegisterCallback<ClickEvent>(e => OnClickRotateButton?.Invoke());
            scaleButton.RegisterCallback<ClickEvent>(e => OnClickScaleButton?.Invoke());

            // FIXME: trs掛けられないので一旦消す。後で直す
            editContext.style.display = DisplayStyle.None;

            // 削除ボタン
            var deleteButton = uiElement.Q<Button>(deleteButtonName);
            deleteButton.clicked += () =>
            {
                OnClickDeleteButton?.Invoke();
            };

            // 不要なUIを非表示にしておく
            var succeedButton = uiElement.Q<Button>("");
            succeedButton.style.display = DisplayStyle.None;

            editBuilding.OnBuildingSelected += (go, canEdit) =>
            {
                if (!canEdit)
                {
                    Show(false);
                    return;
                }
                CalcUIDisplayPosition(go);

                currentSelect = go;

                Show(true);   
            };


            // 建物編集を抜けたらdisableにする
            element.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (element.style.display == DisplayStyle.None)
                {
                    OnDisable();
                }
            });


            Show(false);

        }

        void CalcUIDisplayPosition(GameObject obj)
        {
            // 建物のTransform.positionをScreen座標に変換
            // var bldgBounds = bldgItem.Key.transform.GetComponent<MeshCollider>().bounds;
            // var topPos = new Vector3(bldgBounds.center.x, bldgBounds.max.y, bldgBounds.center.z);
            // var screenPos = RuntimePanelUtils.CameraTransformWorldToPanel(visualizeHeightPanel.panel, topPos, Camera.main);

            // var vp = Camera.main.WorldToViewportPoint(topPos);
            // bool isActive = vp.x >= 0.0f && vp.x <= 1.0f && vp.y >= 0.0f && vp.y <= 1.0f && vp.z >= 0.0f;
            // bool isInScreen = screenPos.x > pinOffsetx && screenPos.x < Screen.width - pinOffsetx && screenPos.y > headerOffset + pinOffsety;
            // bool isNearCamera = Vector2.Distance(new Vector2(topPos.x,topPos.z), new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z)) < cameraDistance;

            // // ヘッダーパネルをのぞくScreenに映る範囲内の場合は表示
            // if (isActive && isInScreen && isNearCamera)
            // {
            //     bldgItem.Value.style.display = DisplayStyle.Flex;
            //     heightPinList.Add(bldgItem.Value);

            //     //HeightPinの位置を設定
            //     var xPos = screenPos.x - pinOffsetx;
            //     var yPos = screenPos.y - pinOffsety;
            //     bldgItem.Value.style.translate = new Translate() { x = xPos, y = yPos };
            // }

            var bounds = obj.GetComponent<MeshCollider>().bounds;
            var wp = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            var screenPos = RuntimePanelUtils.CameraTransformWorldToPanel(editContext.panel, wp, Camera.main);

            var vp = Camera.main.WorldToViewportPoint(wp);
            bool isActive = 0f <= vp.x && vp.x <= 1f && 0f <= vp.y && vp.y <= 1f && 0f <= vp.z;

            var xcenter = 80f / 2f;

            trsVisualElement.style.translate = new Translate() { x = screenPos.x - xcenter, y = screenPos.y };
        }

        public void OnDisable()
        {
            Show(false);
            currentSelect = null;
        }

        public void OnEnable()
        {
        }

        public void Update(float deltaTime)
        {
            if (currentSelect != null)
            {
                CalcUIDisplayPosition(currentSelect);
            }
        }

        public void Start()
        {
        }

        public void Show(bool state)
        {
            if (trsVisualElement == null)
            {
                return;
            }

            trsVisualElement.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void LateUpdate(float deltaTime)
        {
        }
    }
}
