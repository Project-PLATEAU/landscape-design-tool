using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{
    public class GameObjectFocus
    {
        protected LandscapeCamera landscapeCamera;

        private const float focusDuration = 1.0f;
        private bool isFocusing = false;

        private bool preIsCameraMoveActive = false;

        public System.Action<GameObject> focusFinishCallback = new(_ => { });

        public GameObjectFocus(LandscapeCamera landscapeCamera)
        {
            this.landscapeCamera = landscapeCamera;
        }

        public void FocusFinish()
        {
            isFocusing = false;
        }

        public void Focus(Transform target, float distance = 4f)
        {
            if (isFocusing)
            {
                Debug.Log($"isFocusing Cancel");
                return;
            }

            if (landscapeCamera.cameraState != LandscapeCameraState.PointOfView)
            {
                Debug.Log($"camerastate is not PointOfView : {landscapeCamera.cameraState}");
                return;
            }
            isFocusing = true;
            DeactivateCameraMoveByUserInput(); // フォーカス前にカメラ操作を無効化
            landscapeCamera.FocusPoint(target, () =>
            {
                UndoCameraMoveByUserInput(); // フォーカス後にカメラ操作を復元
                focusFinishCallback?.Invoke(target.gameObject);
            }, distance);
        }


        private void DeactivateCameraMoveByUserInput()
        {
            preIsCameraMoveActive = CameraMoveByUserInput.IsCameraMoveActive;
            CameraMoveByUserInput.IsCameraMoveActive = false;

        }

        private void UndoCameraMoveByUserInput()
        {
            CameraMoveByUserInput.IsCameraMoveActive = preIsCameraMoveActive;

        }

    }
}
