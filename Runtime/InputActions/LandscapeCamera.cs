using Cinemachine;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Landscape2.Runtime
{
    public class LandscapeCamera
    {

        private CinemachineVirtualCamera vcam1;
        private CinemachineVirtualCamera vcam2;
        private GameObject walker;
        private RaycastHit hit;

        public LandscapeCameraState cameraState { get; private set; }
        public event Action OnSetCameraCalled;

        public LandscapeCamera(CinemachineVirtualCamera vcam1, CinemachineVirtualCamera vcam2, GameObject walker)
        {
            cameraState = LandscapeCameraState.PointOfView;
            this.vcam1 = vcam1;
            this.vcam2 = vcam2;
            this.walker = walker;
            SwitchCamera(vcam1, vcam2);
        }

        /// <summary>
        /// カメラの状態を取得する
        /// </summary>
        /// <returns></returns>
        public LandscapeCameraState GetCameraState()
        {
            return cameraState;
        }

        /// <summary>
        /// 歩行者視点カメラのPositionを変更する
        /// </summary>
        /// <param name="pos"></param>
        public void SetWalkerPos(Vector3 pos)
        {
            var cc = walker.GetComponent<CharacterController>();
            cc.enabled = false;
            walker.transform.position = pos;
            cc.enabled = true;
        }

        /// <summary>
        /// 歩行者視点カメラのPositionを取得する
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWalkerPos()
        {
            return walker.transform.position;
        }

        /// <summary>
        /// カメラの状態を変更する
        /// </summary>
        /// <param name="camState"></param>
        public void SetCameraState(LandscapeCameraState camState)
        {
            cameraState = camState;

            if (camState != LandscapeCameraState.Walker)
            {
                CameraMoveByUserInput.IsKeyboardActive = true;
                CameraMoveByUserInput.IsMouseActive = true;
                WalkerMoveByUserInput.IsActive = false;
                SwitchCamera(vcam1, vcam2);
            }
            else
            {
                CameraMoveByUserInput.IsKeyboardActive = false;
                CameraMoveByUserInput.IsMouseActive = false;
                WalkerMoveByUserInput.IsActive = true;
                SwitchCamera(vcam2, vcam1);
            }
            OnSetCameraCalled?.Invoke();
        }

        /// <summary>
        /// CinemachineVirtualCameraを切り替える
        /// </summary>
        /// <param name="activeCamera"></param>
        /// <param name="inactiveCamera"></param>
        private void SwitchCamera(CinemachineVirtualCamera activeCamera, CinemachineVirtualCamera inactiveCamera)
        {
            activeCamera.Priority = 10;
            inactiveCamera.Priority = 0;
        }

        /// <summary>
        /// 歩行者視点以外のカメラに切り替える
        /// </summary>
        public void SwitchView()
        {
            if (cameraState == LandscapeCameraState.PointOfView)
            {
                cameraState = LandscapeCameraState.SelectWalkPoint;
                OnSetCameraCalled?.Invoke();
                CameraMoveByUserInput.IsKeyboardActive = false;
                CameraMoveByUserInput.IsMouseActive = false;
                WalkerMoveByUserInput.IsActive = false;
            }
            else if (cameraState == LandscapeCameraState.SelectWalkPoint || cameraState == LandscapeCameraState.Walker)
            {
                cameraState = LandscapeCameraState.PointOfView;
                OnSetCameraCalled?.Invoke();
                CameraMoveByUserInput.IsKeyboardActive = true;
                CameraMoveByUserInput.IsMouseActive = true;
                WalkerMoveByUserInput.IsActive = false;
                SwitchCamera(vcam1, vcam2);
            }
        }

        /// <summary>
        /// UI上にマウスがあるときカメラ操作を行わないようにする
        /// </summary>
        /// <param name="onUi"></param>
        public void OnUserInputTrigger(bool onUi)
        {
            if (onUi)
            {
                CameraMoveByUserInput.IsKeyboardActive = false;
                CameraMoveByUserInput.IsMouseActive = false;
                WalkerMoveByUserInput.IsActive = false;
            }
            else
            {
                if (cameraState == LandscapeCameraState.PointOfView)
                {
                    CameraMoveByUserInput.IsKeyboardActive = true;
                    CameraMoveByUserInput.IsMouseActive = true;
                    WalkerMoveByUserInput.IsActive = false;
                }
                else if (cameraState == LandscapeCameraState.SelectWalkPoint)
                {
                    CameraMoveByUserInput.IsKeyboardActive = false;
                    CameraMoveByUserInput.IsMouseActive = false;
                    WalkerMoveByUserInput.IsActive = false;
                }
                else
                {
                    CameraMoveByUserInput.IsKeyboardActive = false;
                    CameraMoveByUserInput.IsMouseActive = false;
                    WalkerMoveByUserInput.IsActive = true;
                }
            }
        }

        /// <summary>
        /// 歩行者視点カメラに切り替える
        /// </summary>
        /// <returns></returns>
        public bool SwitchWalkerView()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool canRaycast = Physics.Raycast(ray, out hit);
            if (canRaycast)
            {
                SwitchCamera(vcam2, vcam1);

                var cc = walker.GetComponent<CharacterController>();
                cc.enabled = false;
                walker.transform.position = new Vector3(hit.point.x, hit.point.y + 1.0f, hit.point.z);
                //var pov = vcam2.GetCinemachineComponent<CinemachinePOV>();
                //var vcam1Euler = vcam1.transform.rotation.eulerAngles;
                //pov.m_VerticalAxis.Value = 0f;
                //pov.m_HorizontalAxis.Value = vcam1Euler.y;
                cc.enabled = true;

                WalkerMoveByUserInput.IsActive = true;
                cameraState = LandscapeCameraState.Walker;
                OnSetCameraCalled?.Invoke();
            }
            return canRaycast;
        }

        /// <summary>
        /// ターゲット位置にフォーカスする
        /// </summary>
        /// <param name="target"></param>
        /// <param name="endCallback"></param>
        /// <param name="distanceMultiplier">距離の倍率</param>
        public void FocusPoint(Transform target, UnityAction endCallback = null, float distanceMultiplier = 4f)
        {
            if (cameraState != LandscapeCameraState.PointOfView)
            {
                return;
            }
            FocusPointAsync(target, distanceMultiplier, endCallback);
        }

        private async void FocusPointAsync(Transform target, float distanceMultiplier, UnityAction endCallback = null)
        {
            const float upAngle = 45f; // 斜めから見下ろす
            const float duration = 0.5f; // 移動時間

            var startRotation = vcam1.transform.rotation;
            var startPosition = vcam1.transform.position;

            // カメラの回転
            var endRotation = Quaternion.LookRotation(target.transform.position - vcam1.transform.position);
            endRotation = Quaternion.Euler(new Vector3(upAngle, endRotation.eulerAngles.y, endRotation.eulerAngles.z));

            // カメラの距離
            var distance = 12.5f * distanceMultiplier; // 元は50f。meshを持っていなくてもdistanceMultiplierを設定したかったので修正
            var renderer = GetTargetMesh(target.gameObject);
            if (renderer != null)
            {
                // メッシュのサイズからカメラの距離を決定
                distance = renderer.bounds.size.magnitude * distanceMultiplier;
            }

            // カメラの位置
            var endPosition = target.transform.position - endRotation * Vector3.forward * distance; // 一定の距離を保つ

            // カメラの移動
            var elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                vcam1.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
                vcam1.transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                await Task.Yield(); // フレームの終わりまで待機
            }
            vcam1.transform.rotation = endRotation;
            vcam1.transform.position = endPosition;

            endCallback?.Invoke();
        }

        private Renderer GetTargetMesh(GameObject target)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer as MeshRenderer;
            }

            if (target.TryGetComponent<LODGroup>(out var meshLod))
            {
                var lods = meshLod.GetLODs();
                if (lods.Length > 0)
                {
                    var renderers = lods[0].renderers;
                    if (renderers.Length > 0)
                    {
                        return renderers[0];
                    }
                }
            }

            var rendererChildren = target.GetComponentsInChildren<Renderer>();
            if (rendererChildren.Length > 0)
            {
                return rendererChildren[0];
            }
            return null;
        }
    }
}
