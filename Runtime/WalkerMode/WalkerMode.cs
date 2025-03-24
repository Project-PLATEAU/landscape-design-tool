using PLATEAU.CityInfo;
using PLATEAU.Geometries;
using System;
using UnityEngine;

namespace Landscape2.Runtime.WalkerMode
{
    public enum WalkerOrientationType
    {
        Up,
        Down,
        Right,
        Left
    }
    
    /// <summary>
    /// 歩行者モードのモデル
    /// </summary>
    public class WalkerMode
    {
        private LandscapeCamera landscapeCamera;
        private WalkerMoveByUserInput walkerMoveByUserInput;
        
        // 上下回転
        private const float VerticalRotateAngle = 15f;
        
        // 左右回転
        private const float HorizontalRotateAngle = 45f;
        
        public WalkerMode(LandscapeCamera landscapeCamera, WalkerMoveByUserInput walkerMoveByUserInput)
        {
            this.landscapeCamera = landscapeCamera;
            this.walkerMoveByUserInput = walkerMoveByUserInput;
        }
        
        public Vector3 GetWalkerPosition()
        {
            return landscapeCamera.GetWalkerPos();
        }

        public float GetInclination()
        {
            var walkerCameraTransform = walkerMoveByUserInput.GetWalkerCameraTransform();
            var angle = walkerCameraTransform.eulerAngles.x;
         
            // 180から-180の角度で表示
            if (angle > 180f)
            {
                angle -= 360f;
            }
            // 上を向いたらプラスになるように、符号を逆転
            return -angle;
        }

        public bool IsWalkerMode()
        {
            return landscapeCamera.GetCameraState() == LandscapeCameraState.Walker;
        }

        public void RotateWalker(WalkerOrientationType orientationType)
        {
            var addAngle = Vector2.zero;
            switch (orientationType)
            {
                case WalkerOrientationType.Up:
                    addAngle = Vector2.up * VerticalRotateAngle;
                    break;
                case WalkerOrientationType.Down:
                    addAngle = Vector2.down * VerticalRotateAngle;
                    break;
                case WalkerOrientationType.Right:
                    addAngle = Vector2.right * HorizontalRotateAngle;
                    break;
                case WalkerOrientationType.Left:
                    addAngle = Vector2.left * HorizontalRotateAngle;
                    break;
            }
            walkerMoveByUserInput.AddRotateWithDuration(addAngle);
        }
    }
}