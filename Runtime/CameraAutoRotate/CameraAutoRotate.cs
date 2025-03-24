using System.Linq;
using Cinemachine;
using UnityEngine;

namespace Landscape2.Runtime
{
    public class CameraAutoRotate : ISubComponent
    {
        private enum State
        {
            Idle,
            Rotate,
        }

        private readonly GameObject gameObject;
        private readonly CinemachineVirtualCamera virtualCamera;
        private RaycastHit rotateHit;


        // このvirtualCameraに移る前のCinemacineVirtualCameraのGameObject
        private GameObject vcCamPrevious;
        private State state;

        public bool IsRotate => state == State.Rotate;

        public CameraAutoRotate()
        {
            var go = new GameObject
            {
                name = nameof(CameraAutoRotate)
            };
            virtualCamera = go.AddComponent<CinemachineVirtualCamera>();
            virtualCamera.m_StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Never;
            virtualCamera.m_Priority = 0;
            virtualCamera.m_Lens.FieldOfView = Camera.main.fieldOfView;

            gameObject = go;
        }



        public void ToggleRotate()
        {
            if (state == State.Idle)
            {
                StartRotate();
            }
            else
            {
                StopRotate();
            }
        }

        public void StartRotate()
        {
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            vcCamPrevious = brain.ActiveVirtualCamera.VirtualCameraGameObject;


            var tobj = Camera.main;
            var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0f));

            if (!Physics.Raycast(ray, out rotateHit))
            {
                Debug.LogWarning($"地表を見つけられない為rotateできません");
                return;
            }

            // カメラ回転を有効に
            state = State.Rotate;

            virtualCamera.Priority = brain.ActiveVirtualCamera.Priority + 1;

            gameObject.transform.position = vcCamPrevious.transform.position;
            gameObject.transform.rotation = vcCamPrevious.transform.rotation;
        }

        void UpdateRotate(float deltaTime)
        {
            // CameraMoveByUserInput.RotateCamera()を参考に実装
            // camera == virtualCamera
            // cameraTrans == 

            // RotateCamera(cameraMoveSpeedData.rotateSpeed * deltaTime * rotateByMouse, trans);

            // cameraTrans.RotateAround(rotateHit.point, Vector3.up, moveDelta.x);

            // float pitch = camera.transform.eulerAngles.x;
            // pitch = (pitch > 180) ? pitch - 360 : pitch;
            // float newPitch = Mathf.Clamp(pitch - moveDelta.y, 0, 85);
            // float pitchDelta = pitch - newPitch;
            // cameraTrans.RotateAround(rotateHit.point, camera.transform.right, -pitchDelta);

            Vector2 moveDelta = Vector2.one * 10f;

            gameObject.transform.RotateAround(rotateHit.point, Vector3.up, moveDelta.x * deltaTime);
        }


        public void StopRotate()
        {
            state = State.Idle;

            // 前のカメラに戻す
            virtualCamera.Priority = 0;

            if (vcCamPrevious != null)
            {
                vcCamPrevious.transform.SetPositionAndRotation(
                    gameObject.transform.position,
                    gameObject.transform.rotation
                );
            }
        }
        public void OnDisable()
        {
        }

        public void OnEnable()
        {
        }

        public void Update(float deltaTime)
        {
            if (state == State.Rotate)
            {
                UpdateRotate(deltaTime);
            }
        }

        public void Start()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }
    }
}
