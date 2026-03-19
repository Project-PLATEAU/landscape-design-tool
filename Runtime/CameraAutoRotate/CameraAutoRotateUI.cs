using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class CameraAutoRotateUI : ISubComponent
    {
        const string AutoRotateElementName = "Toggle_AutoRotate";
        Toggle toggle;

        readonly LandscapeCamera landscapeCamera;


        public CameraAutoRotateUI(CameraAutoRotate autoRotate, VisualElement globalNavi, LandscapeCamera landscapeCamera)
        {

            toggle = globalNavi.Q<Toggle>(AutoRotateElementName);

            toggle.RegisterValueChangedCallback((evt) =>
            {
                autoRotate?.ToggleRotate();
            });

            this.landscapeCamera = landscapeCamera;
            landscapeCamera.OnSetCameraCalled += HandleSetCameraCalled;
        }

        private void HandleSetCameraCalled()
        {
            var cameraState = landscapeCamera.cameraState;

            // 歩行者モードまたは歩行者視点選択モードの場合
            if (cameraState != LandscapeCameraState.PointOfView)
            {
                toggle.style.display = DisplayStyle.None;

                // カメラ回転モードをOFFにする
                if (toggle.value == true)
                    toggle.value = false;
            }
            else
            {
                toggle.style.display = DisplayStyle.Flex;
            }
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
