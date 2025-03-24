using System.Collections.Generic;
using Landscape2.Runtime.CameraPositionMemory;
using Landscape2.Runtime.UiCommon;
using Landscape2.Runtime.WeatherTimeEditor;
using Landscape2.Runtime.LandscapePlanLoader;
using Landscape2.Runtime.BuildingEditor;
using UnityEngine.UIElements;
using UnityEngine;
using Cinemachine;
using Landscape2.Runtime.GisDataLoader;
using Landscape2.Runtime.WalkerMode;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.InputSystem;
using System;
using UnityEngine.InputSystem.Processors;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 実行時の機能である<see cref="ISubComponent"/>をここにまとめて、UpdateやOnEnable等を呼び出します。
    /// </summary>

    public enum SubMenuUxmlType
    {
        Menu = -1,
        EditBuilding,
        Asset,
        Bim,
        Gis,
        Planning,
        Analytics,
        CameraList,
        CameraEdit,
        WalkMode,
    }

    public class LandscapeSubComponents : MonoBehaviour
    {

        const int CAMERA_FARCLIP_VALUE = 4000;

        private List<ISubComponent> subComponents;
        // 現在開かれているサブメニュー機能
        private SubMenuUxmlType subMenuUxmlType = SubMenuUxmlType.Menu;
        // サブメニューのuxmlを管理するする配列
        VisualElement[] subMenuUxmls;

        private void Awake()
        {
            var uiRoot = new UIDocumentFactory().CreateWithUxmlName("GlobalNavi_Main");
            // GlobalNavi_Main.uxmlのSortOrderを設定
            GameObject.Find("GlobalNavi_Main").GetComponent<UIDocument>().sortingOrder = 1;

            // サブメニューのuxmlを生成して非表示
            subMenuUxmls = new VisualElement[Enum.GetNames(typeof(SubMenuUxmlType)).Length - 1];
            for (int i = 0; i < subMenuUxmls.Length; i++)
            {
                subMenuUxmls[i] = new UIDocumentFactory().CreateWithUxmlName(((SubMenuUxmlType)i).ToString());
                subMenuUxmls[i].style.display = DisplayStyle.None;
            }

            // MainCameraを取得
            GameObject mainCamera = Camera.main.gameObject;

            // MainCameraにCinemachineBrainがアタッチされていない場合は追加
            if (mainCamera.GetComponent<CinemachineBrain>() == null)
            {
                mainCamera.AddComponent<CinemachineBrain>();
            }

            //俯瞰視点用のカメラの生成と設定
            GameObject mainCam = new GameObject("PointOfViewCamera");
            CinemachineVirtualCamera mainCamVC = mainCam.AddComponent<CinemachineVirtualCamera>();
            mainCamVC.m_Lens.FieldOfView = 60;
            mainCamVC.m_Lens.NearClipPlane = 0.3f;
            mainCamVC.m_Lens.FarClipPlane = CAMERA_FARCLIP_VALUE;

            //歩行者視点用のオブジェクトの生成と設定
            GameObject walker = new GameObject("Walker");
            CharacterController characterController = walker.AddComponent<CharacterController>();
            characterController.slopeLimit = 90;
            characterController.stepOffset = 0.3f;
            characterController.skinWidth = 0.05f;

            //歩行者視点用のカメラの生成と設定
            GameObject walkerCam = new GameObject("WalkerCamera");
            CinemachineVirtualCamera walkerCamVC = walkerCam.AddComponent<CinemachineVirtualCamera>();
            walkerCamVC.m_Lens.FieldOfView = 60;
            walkerCamVC.m_Lens.NearClipPlane = 0.3f;

            walkerCamVC.m_Lens.FarClipPlane = CAMERA_FARCLIP_VALUE;
            walkerCamVC.Priority = 9;
            walkerCamVC.m_StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Never;
            walkerCamVC.AddCinemachineComponent<CinemachineTransposer>();
            CinemachineInputProvider walkerCamInput = walkerCam.AddComponent<CinemachineInputProvider>();

            // 歩行者視点時カメラ回転の移動量補正
            var ia = new DefaultInputActions();
            {
                var cameraMoveSpeedData = Resources.Load<CameraMoveData>("CameraMoveSpeedData");

                float val = cameraMoveSpeedData.walkerCameraRotateSpeed;
                string overrideProcessor = $"ClampVector2Processor(minX={-val}, minY={-val}, maxX={val}, maxY={val})";

                ia.Player.Look.ApplyBindingOverride(
                    new InputBinding
                    {
                        overrideProcessors = overrideProcessor
                    });
            }
            walkerCamInput.XYAxis = InputActionReference.Create(ia.Player.Look);

            walkerCam.SetActive(false);
            walkerCam.SetActive(true);
            walkerCamVC.Follow = walker.transform;

            var landscapeCamera = new LandscapeCamera(mainCamVC, walkerCamVC, walker);
            var walkerMoveByUserInput = new WalkerMoveByUserInput(walkerCamVC, walker);
            var cameraPositionMemory = new CameraPositionMemory.CameraPositionMemory(mainCamVC, walkerCamVC, landscapeCamera);

            // 建物設定
            var cityModelHandler = new CityModelHandler();
            var editBuilding = new EditBuilding(subMenuUxmls[(int)SubMenuUxmlType.EditBuilding]);

            var saveSystem = new SaveSystem(uiRoot);
            LandscapePlanSaveSystem.SetEvent(saveSystem);
            var buildingSaveLoadSystem = new BuildingSaveLoadSystem();
            buildingSaveLoadSystem.SetEvent(saveSystem);

            var cameraAutoRotate = new CameraAutoRotate();
            var gisDataLoaderUI = new GisDataLoaderUI(subMenuUxmls[(int)SubMenuUxmlType.Gis], saveSystem);

            // 必要な機能をここに追加します
            subComponents = new List<ISubComponent>
            {
                new GlobalNaviHeader(uiRoot, subMenuUxmls, saveSystem),
                new CameraMoveByUserInput(mainCamVC),
                new LandscapeCameraUI(landscapeCamera, uiRoot,subMenuUxmls),
                walkerMoveByUserInput,
                new CameraPositionMemoryUI(cameraPositionMemory, subMenuUxmls, walkerMoveByUserInput,saveSystem, uiRoot),
                new PlanningUI(subMenuUxmls[(int)SubMenuUxmlType.Planning],uiRoot),
                new ArrangementAsset(subMenuUxmls[(int)SubMenuUxmlType.Asset],saveSystem, landscapeCamera),
                //RegulationAreaUI.CreateForScene(),
                new WeatherTimeEditorUI(new WeatherTimeEditor.WeatherTimeEditor(),uiRoot),
                saveSystem,
                editBuilding,
                new BuildingColorEditorUI(new BuildingColorEditor(),editBuilding,subMenuUxmls[(int)SubMenuUxmlType.EditBuilding]),
                new BuildingTRSEditor(editBuilding,subMenuUxmls[(int)SubMenuUxmlType.EditBuilding],landscapeCamera),
                new VisualizeHeightUI(new VisualizeHeight(),uiRoot,landscapeCamera),
                cameraAutoRotate,
                new CameraAutoRotateUI(cameraAutoRotate,uiRoot),
                new BIMImport(subMenuUxmls[(int)SubMenuUxmlType.Bim],saveSystem),
                new LineOfSight(saveSystem,subMenuUxmls[(int)SubMenuUxmlType.Analytics]),
                new TextureSwitch(uiRoot),
                new WalkerModeUI(subMenuUxmls[(int)SubMenuUxmlType.WalkMode], landscapeCamera, walkerMoveByUserInput),
                new VisualSettingConfig(uiRoot,new VisualSettingConfigUI(uiRoot)),
                new ToolTip(uiRoot),
            };

        }

        private void Start()
        {
            foreach (var c in subComponents)
            {
                c.Start();
            }
        }

        private void OnEnable()
        {
            foreach (var c in subComponents)
            {
                c.OnEnable();
            }
        }

        private void Update()
        {
            foreach (var c in subComponents)
            {
                c.Update(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            foreach (var c in subComponents)
            {
                c.LateUpdate(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            foreach (var c in subComponents)
            {
                c.OnDisable();
            }
        }

        public SubMenuUxmlType GetSubMenuUxmlType()
        {
            return subMenuUxmlType;
        }

        public void SetSubMenuUxmlType(SubMenuUxmlType type)
        {
            subMenuUxmlType = type;
        }
    }
}