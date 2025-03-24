using UnityEngine.UIElements;
using Landscape2.Runtime.UiCommon;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

namespace Landscape2.Runtime
{

    public class LandscapeCameraUI : ISubComponent, LandscapeInputActions.ISelectCamPosActions
    {
        private LandscapeInputActions.SelectCamPosActions input;
        private LandscapeCamera landscapeCamera;
        private VisualElement uiRoot, uiRootWalkMode;
        private VisualElement[] subUiRoots;
        private bool isMouseOverUI = false;
        private Toggle toggleWalkMode;
        private bool isNotNoticeToggleWalkCallback = false;
        private Action<float> onUpdateAction;


        public LandscapeCameraUI(LandscapeCamera landscapeCamera, VisualElement uiRoot, VisualElement[] subMenuUxmls)
        {
            this.landscapeCamera = landscapeCamera;
            this.uiRoot = uiRoot;
            subUiRoots = subMenuUxmls;
            uiRootWalkMode = subMenuUxmls[(int)SubMenuUxmlType.WalkMode];
            uiRootWalkMode.style.display = DisplayStyle.None;
        }

        public void OnEnable()
        {
            toggleWalkMode = uiRoot.Q<Toggle>("Toggle_WalkMode");


            toggleWalkMode.RegisterValueChangedCallback(OnToggleWalkModeValueChanged);
            input = new LandscapeInputActions.SelectCamPosActions(new LandscapeInputActions());
            input.SetCallbacks(this);
            input.Enable();
            uiRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
            uiRoot.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
            foreach (var subUiRoot in subUiRoots)
            {
                subUiRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
                subUiRoot.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
            }
            landscapeCamera.OnSetCameraCalled += HandleSetCameraCalled;
        }

        /// <summary>
        /// 歩行者モードボタンが押されたら実行される関数。補講者モードじゃないとき切り替える。
        /// </summary>
        /// <param name="evt"></param>
        private void OnToggleWalkModeValueChanged(ChangeEvent<bool> evt)
        {
            if (!isNotNoticeToggleWalkCallback)
            {
                var snackbar = GameObject.Find("Snackbar");
                //既存のSnackbarがある場合は削除する
                if (snackbar != null)
                {
                    GameObject.Destroy(snackbar);
                }

                landscapeCamera.SwitchView();
                if (LandscapeCameraState.SelectWalkPoint == landscapeCamera.GetCameraState())
                    CreateSnackbar("マップをクリックすると歩行者視点に切り替わります");
            }
            else
            {
                isNotNoticeToggleWalkCallback = false;
            }

        }

        public void Update(float deltaTime)
        {
            onUpdateAction?.Invoke(deltaTime);
        }

        public void Start()
        {
        }

        public void OnDisable()
        {
            input.Disable();
            toggleWalkMode.UnregisterValueChangedCallback(OnToggleWalkModeValueChanged);
            uiRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
            uiRoot.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
            foreach (var subUiRoot in subUiRoots)
            {
                subUiRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
                subUiRoot.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
            }
            landscapeCamera.OnSetCameraCalled -= HandleSetCameraCalled;
        }

        /// <summary>
        /// InputActionsから左クリックを受け取り、歩行者モードに切り替える。
        /// </summary>
        /// <param name="context"></param>
        public void OnSelectPosByInput(InputAction.CallbackContext context)
        {

            if (context.started && LandscapeCameraState.SelectWalkPoint == landscapeCamera.GetCameraState() && !isMouseOverUI)
            {
                landscapeCamera.SwitchWalkerView();
            }
        }

        /// <summary>
        /// UI上でマウスが動いたら実行する関数
        /// </summary>
        /// <param name="evt"></param>

        private void OnPointerMove(PointerMoveEvent evt)
        {

            isMouseOverUI = true;
            landscapeCamera.OnUserInputTrigger(isMouseOverUI);
        }

        /// <summary>
        /// UI上からマウスが離れたら実行する関数
        /// </summary>
        /// <param name="evt"></param>
        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            isMouseOverUI = false;
            landscapeCamera.OnUserInputTrigger(isMouseOverUI);
        }

        /// <summary>
        /// カメラの状態(LandscapeCameraState)が変更されたら呼び出される関数
        /// </summary>
        private void HandleSetCameraCalled()
        {
            var cameraState = landscapeCamera.GetCameraState();
            if (cameraState == LandscapeCameraState.PointOfView)
            {
                if (toggleWalkMode.value != false)
                {
                    toggleWalkMode.value = false;
                    isNotNoticeToggleWalkCallback = true;
                }

            }
            else
            {
                if (toggleWalkMode.value != true)
                {
                    toggleWalkMode.value = true;
                    isNotNoticeToggleWalkCallback = true;
                }

                if (cameraState == LandscapeCameraState.Walker)
                {
                    uiRoot.Q<RadioButton>("MenuCamera").value = true;
                    uiRoot.Q<RadioButton>("SubMenuCameraList").value = true;
                    uiRootWalkMode.Q<VisualElement>("Title_CameraRegist").style.display = DisplayStyle.None;
                    uiRootWalkMode.Q<TemplateContainer>("Panel_WalkViewRegister").style.display = DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// Snackbarを新しく生成する関数
        /// </summary>
        /// <param name="text"></param>
        private void CreateSnackbar(string text)
        {
            GameObject.Destroy(GameObject.Find("Snackbar"));
            var snackBar = new UIDocumentFactory().CreateWithUxmlName("Snackbar");
            snackBar.Q<Label>("SnackbarText").text = text;
            snackBar.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
            snackBar.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
            snackBar.Q<Button>("CloseButton").clicked += () =>
            {
                snackBar.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
                snackBar.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
                snackBar.RemoveFromHierarchy();
                GameObject.Destroy(GameObject.Find("Snackbar"));
                isMouseOverUI = false;
            };

            // 一定時間後にSnackbarを削除するためのデリゲートを設定
            float elapsedTime = 0f;
            onUpdateAction += (deltaTime) =>
            {
                elapsedTime += deltaTime;
                if (elapsedTime >= 3f) // 3秒後に削除
                {
                    snackBar.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
                    snackBar.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
                    snackBar.RemoveFromHierarchy();
                    GameObject.Destroy(GameObject.Find("Snackbar"));
                    onUpdateAction = null; // 削除後はデリゲートを解除
                }
            };
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
