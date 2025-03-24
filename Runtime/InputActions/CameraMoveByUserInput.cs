using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using UnityEngine.Events;

namespace Landscape2.Runtime
{
    /// <summary>
    /// ユーザーの操作によってカメラを動かします。
    /// </summary>
    public class CameraMoveByUserInput : LandscapeInputActions.ICameraMoveActions, ISubComponent
    {
        private readonly CinemachineVirtualCamera camera;
        CameraMoveData cameraMoveSpeedData;
        private Vector2 horizontalMoveByKeyboard;
        private float verticalMoveByKeyboard;
        private Vector2 parallelMoveByMouse;
        private Vector2 rotateByMouse;
        private float zoomMoveByMouse;
        private LandscapeInputActions.CameraMoveActions input;
        private bool isParallelMoveByMouse;
        private bool isRotateByMouse;
        private GameObject cameraParent;
        private RaycastHit rotateHit, parallelHit;
        private float translationFactor = 1f;

        private static GameObject focusTarget;
        private static bool isFocusTriggered = false;

        private static bool isKeyboardActive = true;
        private static bool isMouseActive = true;

        public static bool IsCameraMoveActive { get; set; } = true;

        public static UnityEvent OnCameraMoved { get; private set; } = new();

        /// <summary>
        /// Start完了時に呼ばれるイベント
        /// </summary>
        public UnityEvent OnStartCompleted { get; private set; } = new();

        /// <summary>
        /// カメラの移動速度データを更新します
        /// </summary>
        /// <param name="newData">新しい移動速度データ</param>
        public void UpdateCameraMoveSpeedData(CameraMoveData newData)
        {
            if (newData == null)
            {
                Debug.LogError("CameraMoveDataがnullです");
                return;
            }
            cameraMoveSpeedData = newData;
        }

        /// <summary>
        /// キーボードでの移動を有効にするかどうか
        /// </summary>
        public static bool IsKeyboardActive
        {
            get => isKeyboardActive && IsCameraMoveActive;
            set
            {
                isKeyboardActive = value;
            }
        }

        /// <summary>
        /// マウスでの移動を有効にするかどうか
        /// </summary>
        public static bool IsMouseActive
        {
            get => isMouseActive && IsCameraMoveActive;
            set
            {
                isMouseActive = value;
            }
        }

        public CameraMoveByUserInput(CinemachineVirtualCamera camera)
        {
            this.camera = camera;
        }

        public void OnEnable()
        {
            // ユーザーの操作を受け取る準備
            input = new LandscapeInputActions.CameraMoveActions(new LandscapeInputActions());
            input.SetCallbacks(this);
            input.Enable();
        }

        public void OnDisable()
        {
            input.Disable();
        }

        /// <summary>
        /// InputActionsからカメラWASD移動のキーボード操作を受け取り、カメラをWASD移動します。
        /// </summary>
        public void OnHorizontalMoveCameraByKeyboard(InputAction.CallbackContext context)
        {
            if (!IsKeyboardActive)
            {
                horizontalMoveByKeyboard = Vector2.zero;
                return;
            }
            if (context.performed)
            {
                var delta = context.ReadValue<Vector2>();
                horizontalMoveByKeyboard = delta;
            }
            else if (context.canceled)
            {
                horizontalMoveByKeyboard = Vector2.zero;
            }
        }

        /// <summary>
        /// InputActionsからカメラ上下移動のキーボード操作を受け取り、カメラを上下移動します。
        /// </summary>
        /// <param name="context"></param>
        public void OnVerticalMoveCameraByKeyboard(InputAction.CallbackContext context)
        {
            if (!IsKeyboardActive)
            {
                verticalMoveByKeyboard = 0f;
                return;
            }
            if (context.performed)
            {
                var delta = context.ReadValue<float>();
                verticalMoveByKeyboard = delta;
            }
            else if (context.canceled)
            {
                verticalMoveByKeyboard = 0f;
            }
        }

        /// <summary>
        /// InputActionsからマウスの左クリックドラッグを受け取り、カメラを平行移動します。
        /// </summary>
        /// <param name="context"></param>
        public void OnParallelMoveCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive)
            {
                isParallelMoveByMouse = false;
                parallelMoveByMouse = Vector2.zero;
                return;
            }
            if (context.started)
            {
                isParallelMoveByMouse = true;
                if (Camera.main == null)
                {
                    Debug.LogError("カメラが必要です");
                    return;
                }
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out parallelHit))
                {
                    translationFactor = parallelHit.distance * cameraMoveSpeedData.parallelMoveSpeed;
                }
                else
                {
                    translationFactor = 1f;
                }

            }
            else if (context.canceled)
            {
                isParallelMoveByMouse = false;
                parallelMoveByMouse = Vector2.zero;
            }
        }

        /// <summary>
        /// InputActionsからのマウスのスクロールを受け取り、カメラを前後移動します。
        /// </summary>
        /// <param name="context"></param>
        public void OnZoomMoveCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive)
            {
                zoomMoveByMouse = 0f;
                return;
            }
            if (context.performed)
            {
                var delta = context.ReadValue<float>();
                zoomMoveByMouse = delta;
            }
            else if (context.canceled)
            {
                zoomMoveByMouse = 0f;
            }
        }

        /// <summary>
        /// InputActionsからのマウスのスクロールを受け取り、カメラを注視点を元に回転します。
        /// </summary>
        /// <param name="context"></param>
        public void OnRotateCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive)
            {
                isRotateByMouse = false;
                rotateByMouse = Vector2.zero;
            }
            if (context.started)
            {
                if (Camera.main == null)
                {
                    Debug.LogError("カメラが必要です");
                    return;
                }
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out rotateHit))
                {
                    isRotateByMouse = true;
                }
            }
            else if (context.canceled)
            {
                isRotateByMouse = false;
                rotateByMouse = Vector2.zero;
            }
        }

        public void Start()
        {
            //カメラ回転用オブジェクト準備
            cameraParent = new GameObject("CameraParent");
            camera.transform.SetParent(cameraParent.transform);
            camera.transform.position = new Vector3(0, 0, 0);
            cameraMoveSpeedData = Resources.Load<CameraMoveData>("CameraMoveSpeedData");
            cameraParent.transform.SetPositionAndRotation(new Vector3(0, 215, 0), Quaternion.Euler(new Vector3(45, 0, 0)));

            // Start完了を通知
            OnStartCompleted.Invoke();
        }

        public void LateUpdate(float deltaTime)
        {
            var trans = cameraParent.transform;
            parallelMoveByMouse = Mouse.current.delta.ReadValue();
            rotateByMouse = Mouse.current.delta.ReadValue();

            if (isFocusTriggered)
            {
                FocusOnObjectInternal(trans);
                isFocusTriggered = false;
            }

            MoveCameraHorizontal(cameraMoveSpeedData.horizontalMoveSpeed * deltaTime * horizontalMoveByKeyboard, trans);
            MoveCameraVertical(cameraMoveSpeedData.verticalMoveSpeed * deltaTime * verticalMoveByKeyboard, trans);
            MoveCameraParallel(parallelMoveByMouse, trans);
            MoveCameraZoom(cameraMoveSpeedData.zoomMoveSpeed * zoomMoveByMouse, trans);
            RotateCamera(cameraMoveSpeedData.rotateSpeed * rotateByMouse, trans);
            if (cameraParent.transform.position.y < cameraMoveSpeedData.heightLimitY)
            {
                cameraParent.transform.position = new Vector3(cameraParent.transform.position.x, cameraMoveSpeedData.heightLimitY, cameraParent.transform.position.z);
            }

            if (camera.transform.hasChanged)
            {
                OnCameraMoved.Invoke();
            }
        }

        /// <summary>
        /// カメラ水平移動
        /// </summary>
        private void MoveCameraHorizontal(Vector2 moveDelta, Transform cameraTrans)
        {
            var dir = new Vector3(moveDelta.x, 0.0f, moveDelta.y);
            var rot = camera.transform.eulerAngles;
            dir = Quaternion.Euler(new Vector3(0.0f, rot.y, rot.z)) * dir;
            cameraTrans.position -= dir;
        }

        /// <summary>
        /// カメラ垂直移動
        /// </summary>
        private void MoveCameraVertical(float moveDelta, Transform cameraTrans)
        {
            var dir = new Vector3(0.0f, moveDelta, 0.0f);
            cameraTrans.position += dir;
        }

        /// <summary>
        /// カメラ平行移動
        /// </summary>
        /// <param name="moveDelta"></param>
        /// <param name="cameraTrans"></param>
        private void MoveCameraParallel(Vector2 moveDelta, Transform cameraTrans)
        {
            if (!isParallelMoveByMouse || !IsCameraMoveActive) return;

            var dir = new Vector3(-moveDelta.x, 0.0f, -moveDelta.y) * translationFactor;
            var rotY = camera.transform.eulerAngles.y;
            dir = Quaternion.Euler(new Vector3(0, rotY, 0)) * dir;
            cameraTrans.position += dir;
        }

        /// <summary>
        /// カメラズーム
        /// </summary>
        /// <param name="moveDelta"></param>
        /// <param name="cameraTrans"></param>
        /// <param name="timeDelta"></param>
        private void MoveCameraZoom(float moveDelta, Transform cameraTrans)
        {
            var dir = new Vector3(0.0f, 0.0f, moveDelta);
            var rot = camera.transform.eulerAngles;
            dir = Quaternion.Euler(new Vector3(rot.x, rot.y, rot.z)) * dir;
            cameraTrans.position += dir;
            if (moveDelta < 0.0f || (cameraTrans.position - camera.transform.position).magnitude > cameraMoveSpeedData.zoomLimit)
            {
                camera.transform.position += dir;
            }
        }

        /// <summary>
        /// カメラ回転
        /// </summary>
        /// <param name="moveDelta"></param>
        /// <param name="cameraTrans"></param>
        private void RotateCamera(Vector2 moveDelta, Transform cameraTrans)
        {
            if (!isRotateByMouse) return;

            cameraTrans.RotateAround(rotateHit.point, Vector3.up, moveDelta.x);

            float pitch = camera.transform.eulerAngles.x;
            pitch = (pitch > 180) ? pitch - 360 : pitch;
            float newPitch = Mathf.Clamp(pitch - moveDelta.y, 0, 85);
            float pitchDelta = pitch - newPitch;
            cameraTrans.RotateAround(rotateHit.point, camera.transform.right, -pitchDelta);
        }

        public void Update(float deltaTime)
        {
        }

        public static void FocusOnObject(GameObject target)
        {
            focusTarget = target;
            isFocusTriggered = true;
        }

        private void FocusOnObjectInternal(Transform cameraTrans)
        {
            if (focusTarget == null)
            {
                Debug.LogError("ターゲットオブジェクトが指定されていません。");
                return;
            }

            // オブジェクトのバウンディングボックスを計算
            Bounds bounds = CalculateBounds(focusTarget);

            // オブジェクトの中心位置
            Vector3 targetPosition = bounds.center;

            // カメラの角度を設定（上から45度見下ろす）
            float angle = 45f;
            Quaternion rotation = Quaternion.Euler(angle, 0, 0);
            cameraTrans.rotation = rotation;

            // カメラからオブジェクトまでの距離を計算
            var mainCamera = Camera.main;
            float distance = CalculateCameraDistance(mainCamera, bounds, angle);

            // カメラの位置を設定
            Vector3 direction = rotation * Vector3.back;
            cameraTrans.transform.position = targetPosition + direction * distance;

            // カメラがオブジェクトを注視するように設定
            cameraTrans.transform.LookAt(targetPosition);
        }

        private Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError("指定されたオブジェクトにRendererが存在しません。");
                return new Bounds(obj.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private float CalculateCameraDistance(Camera camera, Bounds bounds, float angle)
        {
            // オブジェクトの最大サイズを取得
            float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            // カメラの視野角をラジアンに変換
            float fov = camera.fieldOfView;
            float fovRad = fov * Mathf.Deg2Rad;

            // アスペクト比を取得
            float aspect = camera.aspect;

            // カメラの垂直視野角を計算
            float cameraHeightAtDistance = objectSize / (2f * Mathf.Tan(fovRad / 2f));

            // カメラの距離を計算（角度を考慮）
            float distance = cameraHeightAtDistance / Mathf.Cos(angle * Mathf.Deg2Rad);

            return distance;
        }
    }
}
