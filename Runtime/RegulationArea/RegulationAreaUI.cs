using Landscape2.Runtime.UiCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class RegulationAreaUI : ISubComponent
    {
        /// <summary> 景観計画エリアを新規作成するボタンです。 </summary>
        public Button AreaCreateButton { get; }

        /// <summary> エリアの新規作成モード中に表示すべきUIです。 </summary>
        public VisualElement AreaCreateModeUI { get; }

        /// <summary> エリアの新規作成モードで、輪郭線の選択を完了しエリアを決定するボタンです。 </summary>
        private readonly Button completeContourSelectButton;

        private IRAMode currentMode;
        public PositionClickedAction ClickedAction { get; }
        private const string UxmlFileName = "RegulationUI";

        /// <summary>
        /// シーン内にUIDocumentを生成します
        /// </summary>
        public static RegulationAreaUI CreateForScene()
        {
            var ui = new UIDocumentFactory().CreateWithUxmlName(UxmlFileName);
            return new RegulationAreaUI(ui, new RuntimePositionClickedAction());
        }

        /// <summary>
        /// EditorWindow内に生成します 
        /// </summary>
        public static RegulationAreaUI CreateForEditorWindow(VisualElement rootVisualElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>(UxmlFileName);
            visualTree.CloneTree(rootVisualElement);
            return new RegulationAreaUI(rootVisualElement, new EditModePositionClickedAction());
        }

        private RegulationAreaUI(VisualElement uiRoot, PositionClickedAction clickedAction)
        {
            AreaCreateButton = uiRoot.Q<Button>("AreaCreateButton");
            AreaCreateModeUI = uiRoot.Q("AreaCreateModeUI");
            completeContourSelectButton = AreaCreateModeUI.Q<Button>("CompleteContourSelectButton");
            AreaCreateButton.clicked += () => SwitchMode(new RACreateMode(this));
            completeContourSelectButton.clicked += CompleteContourSelect;
            SwitchMode(new RANormalMode(this));
            this.ClickedAction = clickedAction;
        }

        public void Start()
        {

        }
        public void Update(float deltaTime)
        {
            currentMode.Update();
        }

        public void SwitchMode(IRAMode nextMode)
        {
            currentMode = nextMode;
            nextMode.OnModeSwitch();
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        private void CompleteContourSelect()
        {
            if (currentMode is RACreateMode mode)
            {
                mode.CompleteContourSelect();
            }
            else
            {
                Debug.LogError("景観エリアの作成モードでないのに、エリア選択完了が通知されました。");
            }
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }

    /// <summary>
    /// <see cref="RegulationAreaUI"/>の動作モードを規定するインターフェイスです。
    /// RAはRegulationAreaの略です。
    /// </summary>
    public interface IRAMode
    {
        public void OnModeSwitch();
        public void Update();
    }

    public abstract class RAModeBase : IRAMode
    {
        protected RegulationAreaUI ui;

        protected RAModeBase(RegulationAreaUI ui)
        {
            this.ui = ui;
        }

        public abstract void OnModeSwitch();
        public abstract void Update();
    }

    public class RANormalMode : RAModeBase
    {

        public RANormalMode(RegulationAreaUI ui) : base(ui)
        {
        }

        public override void OnModeSwitch()
        {
            ui.AreaCreateButton.style.display = DisplayStyle.Flex;
            ui.AreaCreateModeUI.style.display = DisplayStyle.None;
        }

        public override void Update()
        {

        }
    }

    public class RACreateMode : RAModeBase
    {
        private RegulationArea regulationArea;

        public RACreateMode(RegulationAreaUI ui) : base(ui)
        {
            regulationArea = RegulationArea.Create(null);
#if UNITY_EDITOR
            Selection.activeObject = regulationArea.gameObject;
#endif
        }

        public override void OnModeSwitch()
        {
            ui.AreaCreateButton.style.display = DisplayStyle.None;
            ui.AreaCreateModeUI.style.display = DisplayStyle.Flex;
        }

        public override void Update()
        {
            AddVertexIfClicked();
            DrawCurrentArea();
        }

        private void AddVertexIfClicked()
        {
            // クリック時に頂点生成を行う
            if (!ui.ClickedAction.IsClicked()) return;
            Debug.Log("creating vertex");

            RaycastHit[] hits;
            Ray ray = ui.ClickedAction.MousePosToRay();

            hits = Physics.RaycastAll(ray, Mathf.Infinity);
            if (hits == null || hits.Length == 0)
                return;

            regulationArea.AddVertex(hits[0].point);
        }

        private void DrawCurrentArea()
        {
#if UNITY_EDITOR
            for (int i = 0; i < regulationArea.Vertices.Count; ++i)
            {
                Handles.color = Color.blue;
                // EditorGUI.BeginChangeCheck();
                // Vector3 pos = Handles.FreeMoveHandle(regulationArea.Vertices[i], Quaternion.identity, 10f, Vector3.zero, Handles.SphereHandleCap);
                // if (EditorGUI.EndChangeCheck())
                // {
                //     if (regulationArea.Vertices[i] == pos)
                //         continue;
                //
                //     regulationArea.TrySetVertexOnGround(i, pos);
                //     if (regulationArea.IsMeshGenerated)
                //         regulationArea.GenMesh();
                // }
            }
#endif
        }

        public void CompleteContourSelect()
        {
            if (regulationArea.IsValid())
            {
                regulationArea.GenMesh();
            }


            ui.SwitchMode(new RANormalMode(ui));
        }
    }

    /// <summary>
    /// クリックで場所を選択するとき、その操作がRuntime時かEdit時かの違いを吸収します。
    /// </summary>
    public abstract class PositionClickedAction
    {
        public abstract bool IsClicked();
        public abstract Ray MousePosToRay();
    }

    public class RuntimePositionClickedAction : PositionClickedAction
    {
        public override bool IsClicked()
        {
            return Input.GetMouseButtonDown(0);
        }

        public override Ray MousePosToRay()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                throw new Exception("Main camera is not found.");
            }
            return cam.ScreenPointToRay(Input.mousePosition);
        }
    }

    public class EditModePositionClickedAction : PositionClickedAction
    {
        public override bool IsClicked()
        {
#if UNITY_EDITOR
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
#endif
            var e = Event.current;
            return e.type == EventType.MouseDown && e.button == 0;
        }

        public override Ray MousePosToRay()
        {
#if UNITY_EDITOR
            var mousePos = Event.current.mousePosition;
            return HandleUtility.GUIPointToWorldRay(mousePos);
#else
            throw new Exception("Editor function called in runtime.");
#endif
        }
    }
}
