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
            landscapeCamera.FocusPoint(target, () =>
            {
                focusFinishCallback?.Invoke(target.gameObject);
            }, distance);
        }


    }
}
