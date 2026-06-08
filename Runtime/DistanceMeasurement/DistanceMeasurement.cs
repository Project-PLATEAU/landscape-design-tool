using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class DistanceMeasurement : ISubComponent
    {
        private const string ToggleElementName = "Toggle_DistanceMeasurement";

        private const string PanelElementName = "Panel_DistanceMeasurement";

        private Toggle toggle;

        private bool isMeasuring = false;

        private Vector3? firstPoint = null;
        private Vector3? secondPoint = null;

        private GameObject mainLineObj;
        private GameObject verticalLineObj;
        private GameObject horizontalLineObj;

        private LineRenderer mainLine;
        private LineRenderer verticalLine;
        private LineRenderer horizontalLine;

        private GameObject firstPointObj;
        private GameObject secondPointObj;

        private TextField mainLineText;
        private TextField verticalLineText;
        private TextField horizontalLineText;

        private VisualElement textContainer;
        private VisualElement valueContainer;
        private VisualElement actionContainer;

        private readonly LandscapeCamera landscapeCamera;

        public DistanceMeasurement(VisualElement globalNavi, LandscapeCamera landscapeCamera)
        {
            toggle = globalNavi.Q<Toggle>(ToggleElementName);

            toggle.RegisterValueChangedCallback((evt) =>
            {
                if (!evt.newValue)
                {
                    var distanceMeasurementUXML = globalNavi.Q<VisualElement>(PanelElementName);

                    distanceMeasurementUXML.style.display = DisplayStyle.None;

                    isMeasuring = false;

                    ResetMeasurement();

                    distanceMeasurementUXML.RemoveFromHierarchy();
                }
                else
                {
                    var visualTreeAsset = Resources.Load<VisualTreeAsset>(PanelElementName);

                    var distanceMeasurementUXML = visualTreeAsset.CloneTree().Q<VisualElement>(PanelElementName);

                    distanceMeasurementUXML.pickingMode = PickingMode.Ignore; // CloneTreeで生成された要素はデフォルトではクリックイベントを受け取る？明示的に無視するように設定

                    globalNavi.Add(distanceMeasurementUXML);

                    var button = distanceMeasurementUXML.Q<Button>("OKButton");

                    button.clicked += () =>
                    {
                        ResetMeasurement();

                        SetPanelState(true);
                    };

                    mainLineText = distanceMeasurementUXML.Q<TextField>("MainLine");
                    verticalLineText = distanceMeasurementUXML.Q<TextField>("VerticalLine");
                    horizontalLineText = distanceMeasurementUXML.Q<TextField>("HorizontalLine");

                    textContainer = distanceMeasurementUXML.Q<VisualElement>("TextContainer");
                    valueContainer = distanceMeasurementUXML.Q<VisualElement>("ValueContainer");
                    actionContainer = distanceMeasurementUXML.Q<VisualElement>("ActionContainer");

                    SetPanelState(true);

                    isMeasuring = true;
                }
            });

            this.landscapeCamera = landscapeCamera;
            this.landscapeCamera.OnSetCameraCalled += HandleSetCameraCalled;
        }

        private void HandleSetCameraCalled()
        {
            var cameraState = landscapeCamera.cameraState;

            // 歩行者モードまたは歩行者視点選択モードの場合
            if (cameraState != LandscapeCameraState.PointOfView)
            {
                toggle.style.display = DisplayStyle.None;

                // 2点間距離測定を非表示に
                if (toggle.value == true)
                    toggle.value = false;
            }
            else
            {
                toggle.style.display = DisplayStyle.Flex;
            }

        }

        public void Start()
        {
        }

        public void Update(float deltaTime)
        {
            if (!isMeasuring)
                return;

            var camera = Camera.main;

            if (camera == null)
            {
                return;
            }

            // UI上にカーソルがある場合は処理しない
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0))
            {
                if (firstPoint != null && secondPoint != null)
                {
                    return;
                }

                Ray ray = camera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 worldPos = hit.point;

                    if (firstPoint == null)
                    {
                        firstPoint = worldPos;

                        CreateLine(ref mainLineObj, ref mainLine, worldPos, "MainDistanceLine", Color.white, isMain: true);
                        CreateLine(ref verticalLineObj, ref verticalLine, worldPos, "VerticalLine", new Color32(0xEE, 0x78, 0x00, 0xFF));
                        CreateLine(ref horizontalLineObj, ref horizontalLine, worldPos, "HorizontalLine", new Color32(0xEE, 0x78, 0x00, 0xFF));

                        CreateMarker(ref firstPointObj, worldPos, "FirstPointMarker");
                        CreateMarker(ref secondPointObj, worldPos, "SecondPointMarker");

                        SetPanelState(false);
                    }
                    else if (secondPoint == null)
                    {
                        secondPoint = worldPos;

                        var p1 = firstPoint.Value;
                        var p2 = secondPoint.Value;

                        // Ctrl キーが押されていればスナップ
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            Vector3 delta = p2 - p1;
                            float absX = Mathf.Abs(delta.x);
                            float absY = Mathf.Abs(delta.y);
                            float absZ = Mathf.Abs(delta.z);

                            // 一番大きな軸だけ動かし、他は固定
                            if (absX >= absY && absX >= absZ)
                                p2 = new Vector3(p2.x, p1.y, p1.z); // X軸だけ変化
                            else if (absY >= absX && absY >= absZ)
                                p2 = new Vector3(p1.x, p2.y, p1.z); // Y軸だけ変化
                            else
                                p2 = new Vector3(p1.x, p1.y, p2.z); // Z軸だけ変化
                        }

                        secondPointObj.transform.position = p2;

                        // 高い方のY座標
                        float highestY = Mathf.Max(p1.y, p2.y);

                        // Horizontal line
                        Vector3 horizStart = new Vector3(p1.x, highestY, p1.z);
                        Vector3 horizEnd = new Vector3(p2.x, highestY, p2.z);
                        SetLinePosition(horizontalLine, horizStart, horizEnd);

                        // Vertical line
                        Vector3 vertStart;
                        Vector3 vertEnd;
                        if (p2.y > p1.y)
                        {
                            vertStart = horizStart;
                            vertEnd = p1;
                        }
                        else
                        {
                            vertStart = horizEnd;
                            vertEnd = p2;
                        }
                        SetLinePosition(verticalLine, vertStart, vertEnd);

                        // Main line
                        SetLinePosition(mainLine, p1, p2);

                        // テキストフィールドに距離を表示
                        mainLineText.value = $"{Vector3.Distance(p1, p2):F2}";
                        verticalLineText.value = $"{Vector3.Distance(vertStart, vertEnd):F2}";
                        horizontalLineText.value = $"{Vector3.Distance(horizStart, horizEnd):F2}";
                    }
                }
            }

            if (firstPoint != null && secondPoint == null && mainLine != null)
            {
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var p1 = firstPoint.Value;
                    var p2 = hit.point;

                    // Ctrl キーが押されていればスナップ
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        Vector3 delta = p2 - p1;
                        float absX = Mathf.Abs(delta.x);
                        float absY = Mathf.Abs(delta.y);
                        float absZ = Mathf.Abs(delta.z);

                        // 一番大きな軸だけ動かし、他は固定
                        if (absX >= absY && absX >= absZ)
                            p2 = new Vector3(p2.x, p1.y, p1.z); // X軸だけ変化
                        else if (absY >= absX && absY >= absZ)
                            p2 = new Vector3(p1.x, p2.y, p1.z); // Y軸だけ変化
                        else
                            p2 = new Vector3(p1.x, p1.y, p2.z); // Z軸だけ変化
                    }

                    secondPointObj.transform.position = p2;

                    // 高い方のY座標
                    float highestY = Mathf.Max(p1.y, p2.y);

                    // Horizontal line
                    Vector3 horizStart = new Vector3(p1.x, highestY, p1.z);
                    Vector3 horizEnd = new Vector3(p2.x, highestY, p2.z);
                    SetLinePosition(horizontalLine, horizStart, horizEnd);

                    // Vertical line
                    Vector3 vertStart;
                    Vector3 vertEnd;
                    if (p2.y > p1.y)
                    {
                        vertStart = horizStart;
                        vertEnd = p1;
                    }
                    else
                    {
                        vertStart = horizEnd;
                        vertEnd = p2;
                    }
                    SetLinePosition(verticalLine, vertStart, vertEnd);

                    // Main line
                    SetLinePosition(mainLine, p1, p2);

                    // テキストフィールドに距離を表示
                    mainLineText.value = $"{Vector3.Distance(p1, p2):F2}";
                    verticalLineText.value = $"{Vector3.Distance(vertStart, vertEnd):F2}";
                    horizontalLineText.value = $"{Vector3.Distance(horizStart, horizEnd):F2}";
                }
            }

            if (mainLine != null)
            {
                Vector3 p0 = mainLine.GetPosition(0);
                Vector3 p1 = mainLine.GetPosition(1);
                Vector3 center = (p0 + p1) * 0.5f;
                float distance = Vector3.Distance(camera.transform.position, center);

                float scale = distance * 0.01f;
                mainLine.startWidth = scale;
                mainLine.endWidth = scale;
            }
            if (verticalLine != null)
            {
                Vector3 p0 = verticalLine.GetPosition(0);
                Vector3 p1 = verticalLine.GetPosition(1);
                Vector3 center = (p0 + p1) * 0.5f;
                float distance = Vector3.Distance(camera.transform.position, center);

                float scale = distance * 0.003f;
                verticalLine.startWidth = scale;
                verticalLine.endWidth = scale;
            }
            if (horizontalLine != null)
            {
                Vector3 p0 = horizontalLine.GetPosition(0);
                Vector3 p1 = horizontalLine.GetPosition(1);
                Vector3 center = (p0 + p1) * 0.5f;
                float distance = Vector3.Distance(camera.transform.position, center);

                float scale = distance * 0.003f;
                horizontalLine.startWidth = scale;
                horizontalLine.endWidth = scale;
            }

            if (firstPointObj != null)
            {
                float scale = Vector3.Distance(camera.transform.position, firstPointObj.transform.position) * 0.01f;
                firstPointObj.transform.localScale = Vector3.one * scale;
            }
            if (secondPointObj != null)
            {
                float scale = Vector3.Distance(camera.transform.position, secondPointObj.transform.position) * 0.01f;
                secondPointObj.transform.localScale = Vector3.one * scale;
            }
        }

        public void LateUpdate(float deltaTime)
        {
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        private void CreateLine(ref GameObject obj, ref LineRenderer line, Vector3 start, string name, Color color, bool isMain = false)
        {
            obj = new GameObject(name);
            line = obj.AddComponent<LineRenderer>();
            if (isMain)
                SetupMainLineRenderer(line);
            else
                SetupHelperLineRenderer(line, color);

            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, start);
        }

        private void CreateMarker(ref GameObject obj, Vector3 position, string name)
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * 0.1f;
            obj.GetComponent<Collider>().enabled = false;
            obj.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/DistanceMeasurementMarker");
        }

        private void SetupMainLineRenderer(LineRenderer line)
        {
            line.material = Resources.Load<Material>("Materials/DistanceMeasurementMain");
            line.startWidth = 1.0f;
            line.endWidth = 1.0f;
            line.useWorldSpace = true;
            line.textureMode = LineTextureMode.Tile;
            line.textureScale = new Vector2(0.25f, 1.0f);
            line.alignment = LineAlignment.View;
            // line.alignment = LineAlignment.TransformZ;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // line.gameObject.transform.rotation = Quaternion.Euler(90, 0, 0);

        }

        private void SetupHelperLineRenderer(LineRenderer line, Color color)
        {
            line.material = Resources.Load<Material>("Materials/DistanceMeasurementHelper");
            line.startColor = color;
            line.endColor = color;
            line.startWidth = 0.1f;
            line.endWidth = 0.1f;
            line.useWorldSpace = true;
        }

        private void SetLinePosition(LineRenderer line, Vector3 start, Vector3 end)
        {
            if (line == null) return;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private void ResetMeasurement()
        {
            GameObject.Destroy(firstPointObj);
            GameObject.Destroy(secondPointObj);

            firstPointObj = null;
            secondPointObj = null;

            GameObject.Destroy(mainLineObj);
            GameObject.Destroy(verticalLineObj);
            GameObject.Destroy(horizontalLineObj);

            mainLine = null;
            verticalLine = null;
            horizontalLine = null;

            mainLineObj = null;
            verticalLineObj = null;
            horizontalLineObj = null;

            firstPoint = null;
            secondPoint = null;
        }

        private void SetPanelState(bool isReady)
        {
            textContainer.style.display = isReady ? DisplayStyle.Flex : DisplayStyle.None;
            valueContainer.style.display = isReady ? DisplayStyle.None : DisplayStyle.Flex;
            actionContainer.style.display = isReady ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}