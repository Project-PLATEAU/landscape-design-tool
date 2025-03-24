using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{
    [CreateAssetMenu(fileName = "CameraMoveSpeedData", menuName = "Landscape-Design-Tool-2/CameraMoveSpeedData")]
    public class CameraMoveData : ScriptableObject
    {
        public float horizontalMoveSpeed = 300f;
        public float verticalMoveSpeed = 300f;
        public float parallelMoveSpeed = 0.15f;
        public float zoomMoveSpeed = 10f;
        public float rotateSpeed = 30f;
        public float zoomLimit = 20f;
        public float heightLimitY = 25f;
        public float walkerMoveSpeed = 20f;
        public float walkerOffsetYSpeed = 10f;

        [Tooltip("0.1〜1.0で入れて下さい")]
        public float walkerCameraRotateSpeed = 1f;
    }
}
