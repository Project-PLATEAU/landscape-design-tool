using Landscape2.Runtime.SaveData;
using Landscape2.Runtime.UiCommon;
using System;
using System.Collections.Generic;
using ToolBox.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.CameraPositionMemory
{
    /// <summary>
    /// ファイルに書き出す際に使用するカメラ位置を表す構造体
    /// </summary>
    public struct CameraPositionData
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool isSaved;
        public string name;
        public string cameraState;
        public float offSetY;

        public CameraPositionData(SlotData data)
        {
            position = data.position;
            rotation = data.rotation;
            isSaved = data.isSaved;
            name = data.name;
            cameraState = data.cameraState.ToString();
            offSetY = data.offSetY;
        }
    }

    /// <summary>
    /// カメラ位置の記憶と復元に関するUIと機能を管理します。
    /// ボタン押下時のメイン処理は <see cref="CameraPositionMemory"/>に委譲します。
    /// </summary>
    public class CameraPositionMemoryUI : ISubComponent
    {
        private const string CameraListContainerName = "unity-content-container";
        private const string CameraRegister = "Panel_CameraViewRegister";
        private const string CameraEditor = "Panel_CameraViewEditor";
        private const string WalkRegister = "Panel_WalkViewRegister";
        private const string WalkEditor = "Panel_WalkViewEditor";
        private readonly CameraPositionMemory cameraPositionMemory;
        private VisualElement uiRootCameraList, uiRootCameraEdit, uiRootWalkMode;
        private VisualTreeAsset cameraPositionList = Resources.Load<VisualTreeAsset>("List_CameraView");
        private bool isListSelected = false, isWalkEditorOffsetYFieldFocused = false;
        private int selectedSlotData;
        private TextField registerTextField, editorTextField, walkRegisterNameField, walkEditorNameField, walkRegisterOffsetYField, walkEditorOffsetYField;
        private Button registerOKButton, editorOKButton, walkRegisterOKButton, walkEditorOKButton, walkControllerW, walkControllerS, walkControllerA, walkControllerD, walkModeQuit;
        private Label walkControllerHeightText;
        private GroupBox walkSpeedGroup;
        private RadioButton walkSpeed1RadioButton, walkSpeed2RadioButton, walkSpeed3RadioButton;
        private CameraMoveData cameraMoveSpeedData;
        private float defaultWalkSpeed;
        private GameObject[] snackbars, modals;
        private Action<float> onUpdateAction;

        private VisualElement rootElement;


        public CameraPositionMemoryUI(CameraPositionMemory cameraPositionMemory, VisualElement[] subMenuUxmls, WalkerMoveByUserInput walkerMoveByUserInput, SaveSystem saveSystem, VisualElement uiRoot)
        {
            saveSystem.SaveEvent += SaveInfo;
            saveSystem.LoadEvent += LoadInfo;
            saveSystem.DeleteEvent += DeleteInfo;
            saveSystem.ProjectChangedEvent += SetProjectInfo;

            this.cameraPositionMemory = cameraPositionMemory;
            uiRootCameraList = subMenuUxmls[(int)SubMenuUxmlType.CameraList];
            uiRootCameraEdit = subMenuUxmls[(int)(SubMenuUxmlType.CameraEdit)];
            uiRootWalkMode = subMenuUxmls[(int)(SubMenuUxmlType.WalkMode)];

            registerOKButton = uiRootCameraEdit.Q<TemplateContainer>(CameraRegister).Q<Button>("OKButton");
            registerOKButton.clicked += () => OnClickedSaveButton(selectedSlotData);

            editorOKButton = uiRootCameraEdit.Q<TemplateContainer>(CameraEditor).Q<Button>("OKButton");
            editorOKButton.clicked += () => OnClickedRenameButton(selectedSlotData);

            walkRegisterOKButton = uiRootWalkMode.Q<TemplateContainer>(WalkRegister).Q<Button>("OKButton");
            walkRegisterOKButton.clicked += () => OnClickedWalkSaveButton(selectedSlotData);

            walkEditorOKButton = uiRootWalkMode.Q<TemplateContainer>(WalkEditor).Q<Button>("OKButton");
            walkEditorOKButton.clicked += () => OnClickedWalkRenameButton(selectedSlotData);

            registerTextField = uiRootCameraEdit.Q<TemplateContainer>(CameraRegister).Q<TextField>();
            editorTextField = uiRootCameraEdit.Q<TemplateContainer>(CameraEditor).Q<TextField>();
            walkRegisterNameField = uiRootWalkMode.Q<TemplateContainer>(WalkRegister).Q<TextField>("CameraViewName");
            walkEditorNameField = uiRootWalkMode.Q<TemplateContainer>(WalkEditor).Q<TextField>("CameraViewName");
            walkRegisterOffsetYField = uiRootWalkMode.Q<TemplateContainer>(WalkRegister).Q<TextField>("CameraHeight");
            walkEditorOffsetYField = uiRootWalkMode.Q<TemplateContainer>(WalkEditor).Q<TextField>("CameraHeight");
            walkRegisterOffsetYField.RegisterCallback<FocusInEvent>(OnFocusIn);
            walkRegisterOffsetYField.RegisterCallback<FocusOutEvent>(OnFocusOut);

            walkRegisterOffsetYField.RegisterValueChangedCallback(OnChangedOffsetYValue);

            uiRootWalkMode.Q<TemplateContainer>(WalkRegister).Q<Button>("UpButton").clicked += () => OnClickedOffsetYUPButton();
            uiRootWalkMode.Q<TemplateContainer>(WalkRegister).Q<Button>("DownButton").clicked += () => OnClickedOffsetYDOWNButton();
            uiRootWalkMode.Q<TemplateContainer>(WalkEditor).Q<Button>("UpButton").clicked += () => OnClickedOffsetYUPButton();
            uiRootWalkMode.Q<TemplateContainer>(WalkEditor).Q<Button>("DownButton").clicked += () => OnClickedOffsetYDOWNButton();

            walkControllerHeightText = uiRootWalkMode.Q<Label>("HeightText");
            var walkControllerHeightUp = uiRootWalkMode.Q<TemplateContainer>("Panel_WalkController").Q<Button>("UpButton");
            var walkControllerHeightDown = uiRootWalkMode.Q<TemplateContainer>("Panel_WalkController").Q<Button>("DownButton");
            walkControllerHeightUp.clicked += () => OnClickedOffsetYUPButton();
            walkControllerHeightDown.clicked += () => OnClickedOffsetYDOWNButton();

            walkControllerW = uiRootWalkMode.Q<Button>("FPSForwardButton");
            walkControllerS = uiRootWalkMode.Q<Button>("FpsBackButton");
            walkControllerA = uiRootWalkMode.Q<Button>("FpsLeftButton");
            walkControllerD = uiRootWalkMode.Q<Button>("FpsRightButton");
            walkModeQuit = uiRootWalkMode.Q<VisualElement>("ActionContainer").Q<Button>("OKButton");
            walkSpeedGroup = uiRootWalkMode.Q<GroupBox>("TabMenuGroup");
            walkSpeed1RadioButton = walkSpeedGroup.Q<RadioButton>("SpeedButtonx1");
            walkSpeed2RadioButton = walkSpeedGroup.Q<RadioButton>("SpeedButtonx2");
            walkSpeed3RadioButton = walkSpeedGroup.Q<RadioButton>("SpeedButtonx3");

            walkModeQuit.clicked += () =>
            {
                uiRoot.Q<Toggle>("Toggle_WalkMode").value = false;
            };

            walkControllerW.focusable = false;
            walkControllerS.focusable = false;
            walkControllerA.focusable = false;
            walkControllerD.focusable = false;

            walkControllerW.clicked += () =>
            {
                walkerMoveByUserInput.MoveWASD(cameraMoveSpeedData.walkerMoveSpeed * 0.5f * new Vector2(0.0f, -1.0f));
            };
            walkControllerS.clicked += () =>
            {
                walkerMoveByUserInput.MoveWASD(cameraMoveSpeedData.walkerMoveSpeed * 0.5f * new Vector2(0.0f, 1.0f));

            };
            walkControllerA.clicked += () =>
            {
                walkerMoveByUserInput.MoveWASD(cameraMoveSpeedData.walkerMoveSpeed * 0.5f * new Vector2(1.0f, 0.0f));
            };
            walkControllerD.clicked += () =>
            {
                walkerMoveByUserInput.MoveWASD(cameraMoveSpeedData.walkerMoveSpeed * 0.5f * new Vector2(-1.0f, 0.0f));
            };

            cameraMoveSpeedData = Resources.Load<CameraMoveData>("CameraMoveSpeedData");
            defaultWalkSpeed = cameraMoveSpeedData.walkerMoveSpeed;
            walkSpeed1RadioButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    cameraMoveSpeedData.walkerMoveSpeed = defaultWalkSpeed;
                }
            });
            walkSpeed2RadioButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    cameraMoveSpeedData.walkerMoveSpeed = defaultWalkSpeed * 2;
                }
            });
            walkSpeed3RadioButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    cameraMoveSpeedData.walkerMoveSpeed = defaultWalkSpeed * 3;
                }
            });

            rootElement = uiRoot;

            UpdateButtonState();
        }

        /// <summary>
        /// SaveSystemでSaveしたときに実行する関数
        /// 保存したカメラ位置をファイルに書き出す
        /// </summary>
        public void SaveInfo(string projectID)
        {
            List<CameraPositionData> saveCameraPositionData = new List<CameraPositionData>();
            foreach (SlotData camData in cameraPositionMemory.GetSlotDatas())
            {
                if (!string.IsNullOrEmpty(projectID))
                {
                    if (!ProjectSaveDataManager.TryCheckData(ProjectSaveDataType.CameraPosition, projectID, camData.id))
                    {
                        // 該当のプロジェクトでなければ保存しない
                        continue;
                    }
                }
                saveCameraPositionData.Add(new CameraPositionData(camData));
            }
            DataSerializer.Save("CameraPositionDatas", saveCameraPositionData);
        }

        /// <summary>
        /// SaveSystemでLoadされたときに実行する関数
        /// ファイルから保存したカメラ位置を読み込む
        /// </summary>
        public void LoadInfo(string projectID)
        {
            List<CameraPositionData> loadCameraPositionDatas = DataSerializer.Load<List<CameraPositionData>>("CameraPositionDatas");
            if (loadCameraPositionDatas != null)
            {
                this.cameraPositionMemory.GetSlotDatas().Clear();
                foreach (CameraPositionData camData in loadCameraPositionDatas)
                {
                    var slotData = new SlotData(camData.position, camData.rotation, camData.isSaved, camData.name, SlotData.CameraStateStringToEnum(camData.cameraState), camData.offSetY);

                    this.cameraPositionMemory.GetSlotDatas().Add(slotData);

                    // プロジェクトに通知
                    ProjectSaveDataManager.Add(ProjectSaveDataType.CameraPosition, slotData.id, projectID, false);
                }
                UpdateButtonState();
            }
            else
            {
                Debug.LogError("No saved project cameraPosition data found");
            }
        }

        private void DeleteInfo(string projectID)
        {
            var deleteIds = new List<string>();
            for (int i = 0; i < cameraPositionMemory.GetSlotDatas().Count; i++)
            {
                var slotData = cameraPositionMemory.GetSlotDatas()[i];
                if (ProjectSaveDataManager.TryCheckData(
                        ProjectSaveDataType.CameraPosition,
                        projectID,
                        slotData.id,
                        false))
                {
                    deleteIds.Add(slotData.id);
                }
            }

            foreach (var deleteId in deleteIds)
            {
                cameraPositionMemory.Delete(deleteId);
            }
            UpdateButtonState();
        }

        private void SetProjectInfo(string projectID)
        {
            isListSelected = false; // 選択状態を解除
            UpdateButtonState();
        }

        public void Start()
        {

        }

        public void Update(float deltaTime)
        {

            if (registerTextField.value == "")
            {
                registerOKButton.SetEnabled(false);
            }
            else
            {
                registerOKButton.SetEnabled(true);
            }

            if (editorTextField.value == "")
            {
                editorOKButton.SetEnabled(false);
            }
            else
            {
                editorOKButton.SetEnabled(true);
            }

            if (walkRegisterNameField.value == "" || walkRegisterOffsetYField.value == "")
            {
                walkRegisterOKButton.SetEnabled(false);
            }
            else
            {
                walkRegisterOKButton.SetEnabled(true);
            }

            if (walkEditorNameField.value == "" || walkEditorOffsetYField.value == "")
            {
                walkEditorOKButton.SetEnabled(false);
            }
            else
            {
                walkEditorOKButton.SetEnabled(true);
            }

            if ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) && !EventSystem.current.IsPointerOverGameObject() && isListSelected)
            {
                isListSelected = false;
                UpdateButtonState();
            }
            UpdateOffsetY();
            onUpdateAction?.Invoke(deltaTime);
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
            cameraMoveSpeedData.walkerMoveSpeed = defaultWalkSpeed;
        }

        /// <summary>
        /// 俯瞰視点UIにおいて「カメラ位置を記憶」ボタンが押された時、カメラ位置を記憶する
        /// </summary>
        private void OnClickedSaveButton(int slotId)
        {
            cameraPositionMemory.Save(slotId, registerTextField.value);
            UpdateButtonState();
            CreateModal("視点登録完了", "視点を登録しました");
        }

        /// <summary>
        /// 歩行者視点UIにおいて「カメラ位置を記憶」ボタンが押されたとき、カメラ位置を記憶する
        /// </summary>
        /// <param name="slotId"></param>
        private void OnClickedWalkSaveButton(int slotId)
        {
            cameraPositionMemory.Save(slotId, walkRegisterNameField.value);
            UpdateButtonState();
            CreateModal("視点登録完了", "視点を登録しました");
        }


        /// <summary>
        /// 俯瞰視点UIにおいて「カメラ位置を復元」ボタンが押された時、カメラ位置を復元する
        /// </summary>
        private void OnClickedRestoreButton(int slotId, bool canEdit)
        {
            cameraPositionMemory.Restore(slotId);

            if (!canEdit)
            {
                isListSelected = false;
            }
            else
            {
                if (!isListSelected)
                    isListSelected = true;
                selectedSlotData = slotId;
                editorTextField.value = cameraPositionMemory.GetName(slotId);
            }
            UpdateButtonState();
        }

        /// <summary>
        /// 歩行者視点UIにおいて「カメラ位置を復元」ボタンが押された時、カメラ位置を復元する
        /// </summary>
        /// <param name="slotId"></param>
        private void OnClickedWalkRestoreButton(int slotId, bool canEdit)
        {
            cameraPositionMemory.Restore(slotId);

            if (!canEdit)
            {
                isListSelected = false;
            }
            else
            {
                if (!isListSelected)
                    isListSelected = true;
                selectedSlotData = slotId;
                walkEditorNameField.value = cameraPositionMemory.GetName(slotId);
                walkEditorOffsetYField.value = cameraPositionMemory.GetOffsetY().ToString();
            }
            UpdateButtonState();
        }

        /// <summary>
        /// 俯瞰視点カメラの保存データの名前を変更する
        /// </summary>
        /// <param name="slotId"></param>
        private void OnClickedRenameButton(int slotId)
        {
            SlotData prevSlot = cameraPositionMemory.GetSlotData(slotId);
            string nextSlotName = editorTextField.value;
            Debug.Log(slotId + " " + nextSlotName);
            cameraPositionMemory.SetSlotData(slotId, new SlotData(prevSlot.position, prevSlot.rotation, prevSlot.isSaved, nextSlotName, prevSlot.cameraState, prevSlot.offSetY));
            UpdateButtonState();
            CreateSnackbar("登録内容を更新しました");
        }

        /// <summary>
        /// 歩行者視点カメラの保存データの名前を変更する
        /// </summary>
        /// <param name="slotId"></param>
        private void OnClickedWalkRenameButton(int slotId)
        {
            SlotData prevSlot = cameraPositionMemory.GetSlotData(slotId);
            string nextSlotName = walkEditorNameField.value;
            Debug.Log(slotId + " " + nextSlotName);
            cameraPositionMemory.SetSlotData(slotId, new SlotData(prevSlot.position, prevSlot.rotation, prevSlot.isSaved, nextSlotName, prevSlot.cameraState, cameraPositionMemory.GetOffsetY()));
            UpdateButtonState();
            CreateSnackbar("登録内容を更新しました");
        }

        /// <summary>
        /// 歩行者カメラの高さを上げるボタンが押されたときに実行する関数
        /// </summary>
        private void OnClickedOffsetYUPButton()
        {
            cameraPositionMemory.SetOffsetY(cameraPositionMemory.GetOffsetY() + 0.1f);
            UpdateOffsetY();
        }

        /// <summary>
        /// 歩行者カメラの高さを下げるボタンが押されたときに実行する関数
        /// </summary>
        private void OnClickedOffsetYDOWNButton()
        {
            if (cameraPositionMemory.GetOffsetY() - 0.1f > 0)
                cameraPositionMemory.SetOffsetY(cameraPositionMemory.GetOffsetY() - 0.1f);
            UpdateOffsetY();
        }

        /// <summary>
        /// 歩行者カメラの高さを更新する関数
        /// </summary>
        private void UpdateOffsetY()
        {
            cameraPositionMemory.SetOffsetY(cameraPositionMemory.GetOffsetY());
            if (!isWalkEditorOffsetYFieldFocused)
            {
                walkRegisterOffsetYField.value = cameraPositionMemory.GetOffsetY().ToString();
                walkEditorOffsetYField.value = cameraPositionMemory.GetOffsetY().ToString();
                walkControllerHeightText.text = cameraPositionMemory.GetOffsetY().ToString();
            }

        }

        /// <summary>
        /// 歩行者視点編集UIのカメラ高さからフォーカスされたら実行される関数
        /// </summary>
        /// <param name="evt"></param>
        private void OnFocusIn(FocusInEvent evt)
        {
            isWalkEditorOffsetYFieldFocused = true;
        }

        /// <summary>
        /// 歩行者視点編集UIのカメラ高さからフォーカスが外れたら実行される関数
        /// </summary>
        /// <param name="evt"></param>
        private void OnFocusOut(FocusOutEvent evt)
        {
            isWalkEditorOffsetYFieldFocused = false;
        }

        /// <summary>
        /// 歩行者カメラの高さが更新されたら実行する関数
        /// </summary>
        /// <param name="evt"></param>
        private void OnChangedOffsetYValue(ChangeEvent<string> evt)
        {
            if (float.TryParse(evt.newValue, out float value))
            {
                if (value >= 0)
                {
                    cameraPositionMemory.SetOffsetY(value);
                }
                else
                {
                    walkEditorOffsetYField.value = "0";
                    walkRegisterOffsetYField.value = "0";
                    walkControllerHeightText.text = "0";
                }
            }
        }

        /// <summary>
        /// カメラ保存機能UIを更新する関数
        /// 保存データがあるかどうかによってボタンのテキストとスタイルを切り替えます
        /// </summary>
        private void UpdateButtonState()
        {
            uiRootWalkMode.Q<VisualElement>("Title_CameraRegist").style.display = DisplayStyle.Flex;
            if (isListSelected)
            {
                uiRootCameraEdit.Q<TemplateContainer>(CameraRegister).style.display = DisplayStyle.None;
                uiRootCameraEdit.Q<TemplateContainer>(CameraEditor).style.display = DisplayStyle.Flex;
                uiRootWalkMode.Q<TemplateContainer>(WalkRegister).style.display = DisplayStyle.None;
                uiRootWalkMode.Q<TemplateContainer>(WalkEditor).style.display = DisplayStyle.Flex;
            }
            else
            {
                uiRootCameraEdit.Q<TemplateContainer>(CameraRegister).style.display = DisplayStyle.Flex;
                uiRootCameraEdit.Q<TemplateContainer>(CameraEditor).style.display = DisplayStyle.None;
                uiRootWalkMode.Q<TemplateContainer>(WalkRegister).style.display = DisplayStyle.Flex;
                uiRootWalkMode.Q<TemplateContainer>(WalkEditor).style.display = DisplayStyle.None;
            }

            //保存したカメラの位置リストUIのリセット
            var editButtons = uiRootCameraEdit.Q<VisualElement>(CameraListContainerName).Query<TemplateContainer>().ToList();
            editButtons.ForEach((button) =>
            {
                uiRootCameraEdit.Q<VisualElement>(CameraListContainerName).Remove(button);
            });

            var listButtons = uiRootCameraList.Q<VisualElement>(CameraListContainerName).Query<TemplateContainer>().ToList();
            listButtons.ForEach((button) =>
            {
                uiRootCameraList.Q<VisualElement>(CameraListContainerName).Remove(button);
            });
            registerTextField.value = "";

            var walkListButtons = uiRootWalkMode.Q<VisualElement>(CameraListContainerName).Query<TemplateContainer>().ToList();
            walkListButtons.ForEach((button) =>
            {
                uiRootWalkMode.Q<VisualElement>(CameraListContainerName).Remove(button);
            });
            walkRegisterNameField.value = "";
            walkRegisterOffsetYField.value = cameraPositionMemory.GetOffsetY().ToString();
            walkControllerHeightText.text = cameraPositionMemory.GetOffsetY().ToString();

            //UI構築
            int slotCount = cameraPositionMemory.GetSlotDatas().Count;
            if (slotCount < 1)
            {
                uiRootCameraList.Q<Label>("Dialogue").style.display = DisplayStyle.Flex;
                uiRootCameraEdit.Q<Label>("Dialogue").style.display = DisplayStyle.Flex;
                uiRootWalkMode.Q<Label>("Dialogue").style.display = DisplayStyle.Flex;
            }
            else
            {
                uiRootCameraList.Q<Label>("Dialogue").style.display = DisplayStyle.None;
                uiRootCameraEdit.Q<Label>("Dialogue").style.display = DisplayStyle.None;
                uiRootWalkMode.Q<Label>("Dialogue").style.display = DisplayStyle.None;
            }

            //CameraListのボタン構築
            for (int i = 0; i < slotCount; i++)
            {
                var slotData = cameraPositionMemory.GetSlotData(i);
                int slotIndex = i;
                var buttonUi = cameraPositionList.CloneTree();
                uiRootCameraList.Q<VisualElement>(CameraListContainerName).Add(buttonUi);
                buttonUi.focusable = false;
                var cameraState = slotData.cameraState;
                var isEditable = IsEditableCurrentProject(slotData.id);

                buttonUi.Q<VisualElement>("DeleteButton").visible = false;
                if (cameraState != LandscapeCameraState.Walker)
                {
                    buttonUi.Q<VisualElement>("Circle_Icon_Camera").style.display = DisplayStyle.Flex;
                    buttonUi.Q<VisualElement>("Circle_Icon_Walk").style.display = DisplayStyle.None;
                }
                else
                {
                    buttonUi.Q<VisualElement>("Circle_Icon_Camera").style.display = DisplayStyle.None;
                    buttonUi.Q<VisualElement>("Circle_Icon_Walk").style.display = DisplayStyle.Flex;
                }
                buttonUi.Q<Label>().text = cameraPositionMemory.GetName(i);
                buttonUi.Q<Button>().clicked += () => OnClickedRestoreButton(slotIndex, isEditable);

            }

            //CameraEditのボタン構築
            for (int i = 0; i < slotCount; i++)
            {
                var slotData = cameraPositionMemory.GetSlotData(i);

                int slotIndex = i;
                var buttonUi = cameraPositionList.CloneTree();
                uiRootCameraEdit.Q<VisualElement>(CameraListContainerName).Add(buttonUi);
                buttonUi.focusable = false;
                var cameraState = slotData.cameraState;
                var isEditable = IsEditableCurrentProject(slotData.id);

                buttonUi.Q<VisualElement>("DeleteButton").visible = isEditable;
                if (cameraState != LandscapeCameraState.Walker)
                {
                    buttonUi.Q<VisualElement>("Circle_Icon_Camera").style.display = DisplayStyle.Flex;
                    buttonUi.Q<VisualElement>("Circle_Icon_Walk").style.display = DisplayStyle.None;
                }
                else
                {
                    buttonUi.Q<VisualElement>("Circle_Icon_Camera").style.display = DisplayStyle.None;
                    buttonUi.Q<VisualElement>("Circle_Icon_Walk").style.display = DisplayStyle.Flex;
                }
                buttonUi.Q<Label>().text = cameraPositionMemory.GetName(i);
                buttonUi.Q<Button>().clicked += () => OnClickedRestoreButton(slotIndex, isEditable); // ここに i を渡してはいけないことに注意
                buttonUi.Q<Button>("DeleteButton").clicked += () => OnClickedDeleteButton(slotData.id);
            }

            //WalkListのボタン構築
            for (int i = 0; i < slotCount; i++)
            {
                var slotData = cameraPositionMemory.GetSlotData(i);
                var cameraState = slotData.cameraState;
                if (cameraState == LandscapeCameraState.Walker)
                {
                    int slotIndex = i;
                    var buttonUi = cameraPositionList.CloneTree();
                    uiRootWalkMode.Q<VisualElement>(CameraListContainerName).Add(buttonUi);
                    buttonUi.focusable = false;
                    var isEditable = IsEditableCurrentProject(slotData.id);

                    buttonUi.Q<VisualElement>("DeleteButton").visible = isEditable;
                    buttonUi.Q<VisualElement>("Circle_Icon_Camera").style.display = DisplayStyle.None;
                    buttonUi.Q<VisualElement>("Circle_Icon_Walk").style.display = DisplayStyle.Flex;
                    buttonUi.Q<Label>().text = cameraPositionMemory.GetName(i);
                    buttonUi.Q<Button>().clicked += () => OnClickedWalkRestoreButton(slotIndex, isEditable); // ここに i を渡してはいけないことに注意
                    buttonUi.Q<Button>("DeleteButton").clicked += () => OnClickedDeleteButton(slotData.id);
                }
            }
        }

        private bool IsEditableCurrentProject(string id)
        {
            return ProjectSaveDataManager.TryCheckData(
                ProjectSaveDataType.CameraPosition,
                ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID,
                id);
        }

        /// <summary>
        /// 保存したカメラ視点を削除する関数
        /// </summary>
        /// <param name="slotID"></param>
        private void OnClickedDeleteButton(string slotID)
        {
            cameraPositionMemory.Delete(slotID);
            UpdateButtonState();
            CreateSnackbar("視点を削除しました");
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
            snackBar.Q<Button>("CloseButton").clicked += () =>
            {
                snackBar.RemoveFromHierarchy();
                GameObject.Destroy(GameObject.Find("Snackbar"));
            };

            // 一定時間後にSnackbarを削除するためのデリゲートを設定
            float elapsedTime = 0f;
            onUpdateAction += (deltaTime) =>
            {
                elapsedTime += deltaTime;
                if (elapsedTime >= 3f) // 3秒後に削除
                {
                    GameObject.Destroy(GameObject.Find("Snackbar"));
                    onUpdateAction = null; // 削除後はデリゲートを解除
                }
            };
        }

        /// <summary>
        /// Modalを新しく生成する関数
        /// </summary>
        /// <param name="title"></param>
        /// <param name="context"></param>
        private void CreateModal(string title, string context)
        {
            GameObject.Destroy(GameObject.Find("Modal"));
            var modal = new UIDocumentFactory().CreateWithUxmlName("Modal");
            var icons = modal.Q<VisualElement>("icons").Q<VisualElement>("icons").Query<VisualElement>().ToList();
            foreach (var icon in icons)
            {
                icon.style.display = DisplayStyle.None;
            }
            modal.Q<VisualElement>("Icon_Success").style.display = DisplayStyle.Flex;
            modal.Q<Label>("ModalTitle").text = title;
            modal.Q<Label>("ModalText").text = context;
            modal.Q<Button>("OKButton").clicked += () =>
            {
                modal.Clear();
                GameObject.Destroy(GameObject.Find("Modal"));
            };

            modal.parent?.Remove(modal);
            rootElement.Add(modal);
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
