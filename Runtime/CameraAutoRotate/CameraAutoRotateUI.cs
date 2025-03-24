using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class CameraAutoRotateUI : ISubComponent
    {
        const string AutoRotateElementName = "Toggle_AutoRotate";
        Toggle toggle;


        public CameraAutoRotateUI(CameraAutoRotate autoRotate, VisualElement globalNavi)
        {

            toggle = globalNavi.Q<Toggle>(AutoRotateElementName);

            toggle.RegisterValueChangedCallback((evt) =>
            {
                autoRotate?.ToggleRotate();
            });

        }

        public void LateUpdate(float deltaTime)
        {
        }

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
        }

        public void Start()
        {
        }

        public void Update(float deltaTime)
        {
        }
    }
}
