using Landscape2.Runtime.UiCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BIMLayoutUI
    {
        public enum TRSToggle
        {
            Transform,
            Rotate,
            Scale,
        }

        const string transButtonName = "MoveButton";
        const string rotateButtonName = "RotateButton";

        const string scaleButtonName = "ScaleButton";
        const string deleteButtonName = "ContextButton";
        const string succeedButtonName = "Button";

        public bool IsShow => element.style.display == DisplayStyle.Flex;

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

        public System.Action OnClickOKButton
        {
            get; set;
        }

        TRSToggle trsState;
        public TRSToggle TRSToggleState
        {
            get => trsState;
            set
            {
                var transButton = editContext.Q<RadioButton>(transButtonName);
                var rotateButton = editContext.Q<RadioButton>(rotateButtonName);
                var scaleButton = editContext.Q<RadioButton>(scaleButtonName);

                transButton.value = false;
                rotateButton.value = false;
                scaleButton.value = false;
                switch (value)
                {
                    case TRSToggle.Transform:
                        transButton.value = true;
                        break;
                    case TRSToggle.Rotate:
                        rotateButton.value = true;
                        break;
                    case TRSToggle.Scale:
                        scaleButton.value = true;
                        break;

                }

                trsState = value;
            }
        }


        VisualElement editContext;

        GameObject target;

        Dictionary<GameObject, Bounds> boundsCache = new();


        VisualElement element;
        public BIMLayoutUI(VisualElement rootElement)
        {
            Initialize(rootElement);
            Show(false);
        }

        void Initialize(VisualElement rootElement)
        {
            var uiElement = new UIDocumentFactory().CreateWithUxmlName("ContextButtonGroup");

            editContext = uiElement.Q<VisualElement>("Context_Edit");

            var transButton = editContext.Q<RadioButton>(transButtonName);
            var rotateButton = editContext.Q<RadioButton>(rotateButtonName);
            var scaleButton = editContext.Q<RadioButton>(scaleButtonName);

            transButton.RegisterCallback<ClickEvent>(e => OnClickTransButton?.Invoke());
            rotateButton.RegisterCallback<ClickEvent>(e => OnClickRotateButton?.Invoke());
            scaleButton.RegisterCallback<ClickEvent>(e => OnClickScaleButton?.Invoke());

            // 削除ボタン
            var deleteButton = uiElement.Q<Button>(deleteButtonName);
            deleteButton.clicked += () =>
            {
                OnClickDeleteButton?.Invoke();
            };

            // 不要なUIを非表示にしておく
            var succeedButton = uiElement.Q<Button>("");
            succeedButton.clicked += () =>
            {
                OnClickOKButton?.Invoke();
            };

            element = uiElement;

        }

        /// <summary>
        /// meshRendererからGameObjectのboundsを取得する
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private Bounds CalculateBounds(GameObject gameObject)
        {
            // MeshRenderer をすべて取得
            var renderers = gameObject.GetComponentsInChildren<MeshRenderer>();

            if (renderers.Length <= 0)
            {
                Debug.LogWarning($"No MeshRenderers found in {gameObject.name}");
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            // 最初の MeshRenderer の Bounds を基準に初期化
            Bounds combinedBounds = renderers[0].bounds;

            // 他の Renderer の Bounds を統合
            foreach (var renderer in renderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }

            return combinedBounds;
        }

        void CalcUIDisplayPosition(GameObject obj)
        {
            Bounds bounds;
            if (!boundsCache.TryGetValue(obj, out bounds))
            {
                bounds = CalculateBounds(obj);
                boundsCache.Add(obj, bounds);
            }

            var wp = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            var offset = obj.transform.position - wp;
            var screenPos = RuntimePanelUtils.CameraTransformWorldToPanel(editContext.panel, wp + offset, Camera.main);

            var vp = Camera.main.WorldToViewportPoint(wp);
            bool isActive = 0f <= vp.x && vp.x <= 1f && 0f <= vp.y && vp.y <= 1f && 0f <= vp.z;

            var xcenter = 0f; // elementのuiのpixel数の半分を設定する筈

            element.style.translate = new Translate() { x = screenPos.x - xcenter, y = screenPos.y };
        }

        public void Show(bool show)
        {
            element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetTarget(GameObject targetObj)
        {
            target = targetObj;
            if (target != null)
            {
                CalcUIDisplayPosition(target);
            }
        }

        public void ReleaseTarget()
        {
            SetTarget(null);
        }


        public void Update(float deltaTime)
        {
            if (target != null)
            {
                CalcUIDisplayPosition(target);
            }
        }

    }
}
