using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.WalkerMode
{
    /// <summary>
    /// 歩行者モードのプレゼンター
    /// </summary>
    public class WalkerModeUI : ISubComponent
    {
        private WalkerModeCoordinateUI coordinateUI;
        private WalkerModeOrientationUI orientationUI;
        private WalkerModeInclinationUI inclinationUI;
        private LandscapeCamera landscapeCamera;

        public WalkerModeUI(VisualElement root, LandscapeCamera landscapeCamera, WalkerMoveByUserInput walkerMoveByUserInput)
        {
            this.landscapeCamera = landscapeCamera;

            var walkerMode = new WalkerMode(this.landscapeCamera, walkerMoveByUserInput);
            coordinateUI = new(root, walkerMode);
            orientationUI = new(root, walkerMode);
            inclinationUI = new(root, walkerMode);

            this.landscapeCamera.OnSetCameraCalled += OnSetCameraCalled;
        }

        private void OnSetCameraCalled()
        {
            var isShow = landscapeCamera.GetCameraState() == LandscapeCameraState.Walker;
            coordinateUI.Show(isShow);
            orientationUI.Show(isShow);
            inclinationUI.Show(isShow);
        }

        public void Update(float deltaTime)
        {
            if (coordinateUI != null)
            {
                coordinateUI.Update(deltaTime);
            }
            if (inclinationUI != null)
            {
                inclinationUI.Update(deltaTime);
            }
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        public void Start()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }
    }
}