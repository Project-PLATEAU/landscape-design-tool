using Landscape2.Runtime.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UIElements;
// セーブ、ロード用

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 視点場のポイントに関するクラス
    /// </summary>
    public class ViewPoint : LineOfSightModeClass
    {
        const float iconScale = 5f;
        const float markerYOffset = 2.5f * iconScale; // markerは上にずらす

        private Camera cam;
        private Ray ray;
        private GameObject setPointMarker;
        private GameObject createPointMarker;
        private GameObject viewPointMarkers;
        private VisualElement new_Viewpoint;
        private VisualElement edit_ViewPoint;

        private Sprite viewPointIconSprite;
        private Vector3 editPointMarkerPos;
        private Vector3 editPointOffsetPos;
        private string editPointName;
        private bool isMouseOverUI;
        private LineOfSightDataComponent lineOfSightDataComponent;

        private LineOfSightPosUtil posUtil;

        double inputLatitude;
        double inputLongitude;

        public ViewPoint(LineOfSightDataComponent lineOfSightDataComponentInstance)
        {
            viewPointMarkers = new GameObject("ViewPointMarkers");
            lineOfSightDataComponent = lineOfSightDataComponentInstance;

            posUtil = new();
        }
        public override async void OnEnable(VisualElement element)
        {
            new_Viewpoint = element.Q<VisualElement>("New_Viewpoint");
            edit_ViewPoint = element.Q<VisualElement>("Edit_Viewpoint");
            AsyncOperationHandle<Sprite> viewPointIconHandle = Addressables.LoadAssetAsync<Sprite>("ViewPointIcon");
            viewPointIconSprite = await viewPointIconHandle.Task;
            new_Viewpoint.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            new_Viewpoint.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            edit_ViewPoint.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            edit_ViewPoint.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            var fieldName = "heightValueTextField";

            inputLatitude = float.NaN;
            inputLongitude = float.NaN;

            foreach (var elem in new List<VisualElement>() { new_Viewpoint, edit_ViewPoint })
            {
                var heightInputField = elem.Q<TextField>(fieldName);
                heightInputField.RegisterCallback<ChangeEvent<string>>(input =>
                {
                    if (input.newValue != input.previousValue)
                    {
                        if (float.TryParse(input.newValue, out var heightValue))
                        {
                            if (setPointMarker != null)
                            {
                                var yOffsetObj = setPointMarker.transform.GetChild(0);
                                yOffsetObj.localPosition = new Vector3(0f, heightValue, 0f);
                            }
                        }
                    }
                });

                // 緯度経度の入力
                var latField = elem.Q<TextField>("latitudeValueTextField");
                var lonField = elem.Q<TextField>("longitudeValueTextField");
                latField.RegisterCallback<ChangeEvent<string>>(
                    input =>
                    {
                        if (float.TryParse(input.newValue, out var lat))
                        {
                            inputLatitude = lat;

                            if (inputLongitude != float.NaN)
                            {
                                // 両方の値が入ったので内容を更新します
                                var pos = posUtil.LatLonToVector3(inputLatitude, inputLongitude);
                                if (!float.TryParse(heightInputField.value, out var height))
                                {
                                    height = 0f;
                                }
                                UpdateMarkerPoint(pos, height);
                            }
                        }
                    }
                );

                lonField.RegisterCallback<ChangeEvent<string>>(
                    input =>
                    {
                        if (float.TryParse(input.newValue, out var lon))
                        {
                            inputLongitude = lon;
                            if (inputLatitude != float.NaN)
                            {
                                // 両方の値が入ったので内容を更新します
                                var pos = posUtil.LatLonToVector3(inputLatitude, inputLongitude);
                                if (!float.TryParse(heightInputField.value, out var height))
                                {
                                    height = 0f;
                                }
                                UpdateMarkerPoint(pos, height);
                            }
                        }

                    }
                );

            }
        }

        private void UpdateLatLonInputField(float lat, float lon)
        {
            foreach (var elem in new List<VisualElement>() { new_Viewpoint, edit_ViewPoint })
            {
                var latField = elem.Q<TextField>("latitudeValueTextField");
                var lonField = elem.Q<TextField>("longitudeValueTextField");

                latField.SetValueWithoutNotify(lat.ToString());
                inputLatitude = lat;
                lonField.SetValueWithoutNotify(lon.ToString());
                inputLongitude = lon;
            }
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            isMouseOverUI = true;
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            isMouseOverUI = false;
        }

        public GameObject GeneratePointMarker(Vector3 pos, float yOffset)
        {
            var go = new GameObject();
            go.name = "ViewPoint_Root";

            go.transform.position = pos;

            var iconAnchor = new GameObject("anchor");
            iconAnchor.transform.parent = go.transform;
            iconAnchor.transform.localPosition = new Vector3(0f, yOffset, 0f);
            iconAnchor.AddComponent<IconBillboard>();

            var marker = new GameObject("ViewPoint_Icon");

            marker.transform.parent = iconAnchor.transform;
            marker.transform.localPosition = new Vector3(0f, markerYOffset, 0f);

            // TODO: 大きくしたい場合はlocalscale掛ける
            marker.transform.localScale = new Vector3(iconScale, iconScale, 1);

            var setPointMarkerSpriteRenderer = marker.AddComponent<SpriteRenderer>();
            setPointMarkerSpriteRenderer.sprite = viewPointIconSprite;

            var boxCollider = go.AddComponent<BoxCollider>();
            if (viewPointIconSprite == null)
            {
                Debug.LogWarning("ViewPoint Icon Sprite is null");
                SpriteUtil.LoadAsset("ViewPointIcon", sprite =>
                {
                    setPointMarkerSpriteRenderer.sprite = sprite;
                    boxCollider.size = sprite.bounds.size;
                });
            }
            else
            {
                boxCollider.size = setPointMarkerSpriteRenderer.sprite.bounds.size;
            }
            return go;
        }
        
        /// <summary>
        /// クリックされた場所にポイントを配置する
        /// </summary>
        private void SetPoint()
        {
            cam = Camera.main;
            ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (isMouseOverUI)
                {
                    Debug.Log($"isMouseOverUI:{isMouseOverUI}");
                    return;
                }
                var setPoint = hit.point;
                var fieldName = "heightValueTextField";
                var heightValue = new_Viewpoint.Q<TextField>(fieldName).text;
                if (!float.TryParse(heightValue, out float height))
                {
                    Debug.LogWarning($"heightValue: {heightValue}");
                    new_Viewpoint.Q<TextField>(fieldName).value = "数値を入力してください";

                    return;
                }

                var latlon = posUtil.Vector3ToLatLon(setPoint);
                UpdateLatLonInputField((float)latlon.Item1, (float)latlon.Item2);

                UpdateMarkerPoint(setPoint, height);
            }
            else
            {
                Debug.Log($"raycast not hit ");
            }
        }

        void UpdateMarkerPoint(Vector3 point, float height)
        {
            if (setPointMarker == null)
            {
                setPointMarker = GeneratePointMarker(point, height);
            }

            setPointMarker.transform.position = point;
        }

        /// <summary>
        /// 視点場を生成する
        /// </summary>
        public string CreatePoint()
        {
            // ポイントがセットされずに作成ボタンが押された場合
            if (setPointMarker == null)
            {
                Debug.Log($"ポイントがセットされていません: setPointMarker is null");
                return "";
            }
            // ボタンの名前が設定されていないときの処理
            var registerName = new_Viewpoint.Q<TextField>("NewViewpointName").text;
            if (registerName == "")
            {
                new_Viewpoint.Q<TextField>("NewViewpointName").value = "名前を入力してください";
                GameObject.Destroy(setPointMarker);
                setPointMarker = null;
                return "";
            }

            // ゲームオブジェクトの追加
            createPointMarker = GameObject.Instantiate(setPointMarker);
            GameObject.Destroy(setPointMarker);
            setPointMarker = null;
            createPointMarker.name = registerName;
            createPointMarker.transform.parent = viewPointMarkers.transform;
            var yOffsetObj = createPointMarker.transform.GetChild(0);

            // データの追加
            var isAdded = lineOfSightDataComponent.AddPointData(
                LineOfSightType.viewPoint,
                registerName,
                new()
                {
                    pointPos = createPointMarker.transform.position,
                    yOffset = yOffsetObj.localPosition.y
                });
            if (isAdded)
            {
                return registerName;
            }
            else
            {
                new_Viewpoint.Q<TextField>("NewViewpointName").value = "同じ名前のポイントが存在します";
                GameObject.Destroy(createPointMarker);
                createPointMarker = null;
                return "";
            }
        }

        /// <summary>
        /// uiに値を入れる。
        /// ButtonAction()の後に呼び出されるのを想定
        /// </summary>
        public void InitializeEditPoint()
        {
            if (setPointMarker == null)
            {
                Debug.LogWarning($"setPointMarker is null");
                return;
            }
            edit_ViewPoint.Q<TextField>("EditViewpointName").value = setPointMarker.name;

            var yOffsetObj = setPointMarker.transform.GetChild(0);
            edit_ViewPoint.Q<TextField>("heightValueTextField").value = yOffsetObj.localPosition.y.ToString();
        }

        /// <summary>
        /// 視点場を編集する
        /// </summary>
        public (string beforeName, string afterName) EditPoint()
        {
            var registerName = edit_ViewPoint.Q<TextField>("EditViewpointName").text;
            if (registerName == "")
            {
                edit_ViewPoint.Q<TextField>("EditViewpointName").value = "名前を入力してください";
                return ("", "");
            }

            // 編集後のポイントを生成
            createPointMarker = GameObject.Instantiate(setPointMarker);
            createPointMarker.name = registerName;
            var yOffsetObj = createPointMarker.transform.GetChild(0);
            createPointMarker.transform.parent = viewPointMarkers.transform;

            // 既存のポイントを削除
            var deleteData = DeletePoint(); // setPointMerkerを削除している
            var deleteButtonName = deleteData.deleteButtonName;

            var isAdded = lineOfSightDataComponent.AddPointData(
                LineOfSightType.viewPoint,
                registerName,
                new()
                {
                    pointPos = createPointMarker.transform.position,
                    yOffset = yOffsetObj.transform.localPosition.y
                });
            if (isAdded)
            {
                return (deleteButtonName, registerName);
            }
            else
            {
                edit_ViewPoint.Q<TextField>("EditViewpointName").value = "同じ名前のポイントが存在します";
                GameObject.Destroy(createPointMarker);
                createPointMarker = null;
                return ("", "");
            }
        }

        /// <summary>
        /// 視点場を削除する
        /// </summary>
        public (string deleteButtonName, List<string> removedAnalyzeKeyNameList) DeletePoint()
        {
            // ゲームオブジェクトの削除
            var pointMakerName = setPointMarker?.name;
            if (setPointMarker != null)
            {
                GameObject.Destroy(setPointMarker);
                setPointMarker = null;
            }
            // データの削除
            var removeData = lineOfSightDataComponent.RemovePointElement(LineOfSightType.viewPoint, pointMakerName);
            var isRemoved = removeData.isRemoved;
            var removedAnalyzeKeyNameList = removeData.removedAnalyzeKeyNameList;
            if (isRemoved)
            {
                return (pointMakerName, removedAnalyzeKeyNameList);
            }
            else
            {
                return ("", removedAnalyzeKeyNameList);
            }
        }

        public (string deleteButtonName, List<string> removedAnalyzeKeyNameList) DeletePoint(string pointName)
        {
            TryDeletePoint(pointName);
            return (pointName, new List<string>() { pointName });
        }

        /// <summary>
        /// PointNameに対応するポイントを削除する
        /// </summary>
        /// <param name="pointName"></param>
        private bool TryDeletePoint(string pointName)
        {
            foreach (Transform point in viewPointMarkers.transform)
            {
                if (point.name == pointName)
                {
                    GameObject.Destroy(point.gameObject);
                    lineOfSightDataComponent.RemovePointElement(LineOfSightType.viewPoint, pointName);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 視点場のボタンが押されたときの処理
        /// </summary>
        public void ButtonAction(string pointName)
        {
            foreach (Transform child in viewPointMarkers.transform)
            {
                if (child.gameObject.name == pointName)
                {
                    setPointMarker = child.gameObject;
                    break;
                }
            }

            // キャンセルした際の為編集前座標を保持しておく
            editPointMarkerPos = setPointMarker.transform.position;
            editPointOffsetPos = setPointMarker.transform.GetChild(0).localPosition;
            editPointName = setPointMarker.name;
        }

        /// <summary>
        /// キャンセルボタンが押されたときの処理
        /// </summary>
        public void RestorePoint()
        {
            if (setPointMarker == null)
            {
                setPointMarker = GeneratePointMarker(editPointMarkerPos, editPointOffsetPos.y);
            }

            createPointMarker = GameObject.Instantiate(setPointMarker);
            createPointMarker.name = editPointName;
            createPointMarker.transform.parent = viewPointMarkers.transform;

            // TODO: setPointMarkerを削除する必要があるかも知れない

        }

        /// <summary>
        /// 編集可能か
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CanEdit(string name)
        {
            var data = lineOfSightDataComponent
                .ViewPointDatas.Find(point => point.Name == name);
            return data.IsProject(ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID);
        }

        public override void OnSelect()
        {
            SetPoint();
        }

        public override void OnDisable()
        {
            GameObject.Destroy(setPointMarker);
            setPointMarker = null;
        }
    }
}
