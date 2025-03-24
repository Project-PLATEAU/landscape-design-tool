using Landscape2.Runtime.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UIElements;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using UnityEngine.Animations;
namespace Landscape2.Runtime
{
    /// <summary>
    /// 眺望対象のポイントに関するクラス
    /// </summary>
    public class Landmark : LineOfSightModeClass
    {
        const float iconScale = 5f;
        const float markerYOffset = 2.5f * iconScale; // markerは上にずらす

        private Camera cam;
        private Ray ray;
        private GameObject setPointMarker;
        private GameObject createPointMarker;
        private GameObject landmarkMarkers;
        private VisualElement new_Landmark;
        private VisualElement edit_Landmark;
        private Sprite landmarkIconSprite;
        private Vector3 editPointMarkerPos;
        private Vector3 editPointOffsetPos;
        private string editPointName;
        private bool isMouseOverUI;
        private LineOfSightDataComponent lineOfSightDataComponent;

        private LineOfSightPosUtil posUtil;

        double inputLatitude;
        double inputLongitude;



        public Landmark(LineOfSightDataComponent lineOfSightDataComponentInstance)
        {
            landmarkMarkers = new GameObject("LandmarkMarkers");
            lineOfSightDataComponent = lineOfSightDataComponentInstance;

            posUtil = new();
        }

        public override async void OnEnable(VisualElement element)
        {
            new_Landmark = element.Q<VisualElement>("New_Landmark");
            edit_Landmark = element.Q<VisualElement>("Edit_Landmark");
            AsyncOperationHandle<Sprite> landmarkIconHandle = Addressables.LoadAssetAsync<Sprite>("LandmarkIcon");
            landmarkIconSprite = await landmarkIconHandle.Task;
            new_Landmark.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            new_Landmark.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            edit_Landmark.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            edit_Landmark.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            var fieldName = "heightValueTextField";

            inputLatitude = float.NaN;
            inputLongitude = float.NaN;

            foreach (var elem in new List<VisualElement>() { new_Landmark, edit_Landmark })
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
            foreach (var elem in new List<VisualElement>() { new_Landmark, edit_Landmark })
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

        public void InitializeEditPoint()
        {
            edit_Landmark.Q<TextField>("EditLandmarkName").value = setPointMarker.name;

            var yOffsetObj = setPointMarker.transform.GetChild(0);
            edit_Landmark.Q<TextField>("heightValueTextField").value = (yOffsetObj.localPosition.y).ToString();
        }

        public GameObject GeneratePointMarker(Vector3 pos, float yOffset)
        {
            var go = new GameObject();
            go.name = "Landmark_Root";

            go.transform.position = pos;

            var iconAnchor = new GameObject("anchor");
            iconAnchor.transform.parent = go.transform;
            iconAnchor.transform.localPosition = new Vector3(0f, yOffset, 0f);
            iconAnchor.AddComponent<IconBillboard>();

            var marker = new GameObject("Landmark_Icon");

            marker.transform.parent = iconAnchor.transform;
            marker.transform.localPosition = new Vector3(0f, markerYOffset, 0f);

            marker.transform.localScale = new Vector3(iconScale, iconScale, 1);

            var setPointMarkerSpriteRenderer = marker.AddComponent<SpriteRenderer>();
            setPointMarkerSpriteRenderer.sprite = landmarkIconSprite;
            var boxCollider = go.AddComponent<BoxCollider>();
            
            if (setPointMarkerSpriteRenderer.sprite == null)
            {
                Debug.LogWarning("Landmark Icon Sprite is null");
                SpriteUtil.LoadAsset("LandmarkIcon", sprite =>
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
                    return;
                }
                var setPoint = hit.point;
                var fieldName = "heightValueTextField";
                var heightValue = new_Landmark.Q<TextField>(fieldName).text;
                if (!float.TryParse(heightValue, out float height))
                {
                    new_Landmark.Q<TextField>(fieldName).value = "数値を入力してください";
                    return;
                }

                var latlon = posUtil.Vector3ToLatLon(setPoint);
                UpdateLatLonInputField((float)latlon.Item1, (float)latlon.Item2);

                UpdateMarkerPoint(setPoint, height);
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
        /// 眺望対象を生成する
        /// </summary>
        public string CreatePoint()
        {
            // ポイントがセットされずに作成ボタンが押された時の処理
            if (setPointMarker == null)
            {
                return "";
            }
            // ボタンの名前が設定されていないときの処理
            var registerName = new_Landmark.Q<TextField>("NewLandmarkName").text;
            if (registerName == "")
            {
                new_Landmark.Q<TextField>("NewLandmarkName").value = "名前を入力してください";
                GameObject.Destroy(setPointMarker);
                setPointMarker = null;
                return "";
            }
            // ゲームオブジェクトの追加
            createPointMarker = GameObject.Instantiate(setPointMarker);
            GameObject.Destroy(setPointMarker);
            setPointMarker = null;
            createPointMarker.name = registerName;
            createPointMarker.transform.parent = landmarkMarkers.transform;
            var yOffsetObj = createPointMarker.transform.GetChild(0);

            // データの追加
            var isAdded = lineOfSightDataComponent.AddPointData(
                LineOfSightType.landmark,
                registerName,
                new()
                {
                    pointPos = createPointMarker.transform.position,
                    yOffset = yOffsetObj.localPosition.y,
                });
            if (isAdded)
            {
                return registerName;
            }
            else
            {
                new_Landmark.Q<TextField>("NewLandmarkName").value = "同じ名前のポイントが存在します";
                GameObject.Destroy(createPointMarker);
                createPointMarker = null;
                return "";
            }
        }

        /// <summary>
        /// 眺望対象を編集する
        /// </summary>
        public (string beforeName, string afterName) EditPoint()
        {
            var registerName = edit_Landmark.Q<TextField>("EditLandmarkName").text;
            if (registerName == "")
            {
                edit_Landmark.Q<TextField>("EditLandmarkName").value = "名前を入力してください";
                return ("", "");
            }
            // 編集後のポイントを追加
            createPointMarker = GameObject.Instantiate(setPointMarker);
            createPointMarker.name = registerName;
            createPointMarker.transform.parent = landmarkMarkers.transform;
            var yOffsetObj = createPointMarker.transform.GetChild(0);
            // 既存のボタンの削除
            var deleteData = DeletePoint();
            var deleteButtonName = deleteData.deleteButtonName;
            // FIXME: yOffsetの値はダミーなので修正する事
            var isAdded = lineOfSightDataComponent.AddPointData(
                LineOfSightType.landmark,
                registerName,
                new()
                {
                    pointPos = createPointMarker.transform.position,
                    yOffset = yOffsetObj.localPosition.y
                }
                );
            if (isAdded)
            {
                return (deleteButtonName, registerName);
            }
            else
            {
                edit_Landmark.Q<TextField>("EditLandmarkName").value = "同じ名前のポイントが存在します";
                GameObject.Destroy(createPointMarker);
                createPointMarker = null;
                return ("", "");
            }
        }

        /// <summary>
        /// 眺望対象を削除する
        /// </summary>
        public (string deleteButtonName, List<string> removedAnalyzeKeyNameList) DeletePoint()
        {
            var pointMakerName = setPointMarker?.name;
            // ゲームオブジェクトの削除
            if (setPointMarker != null)
            {
                GameObject.Destroy(setPointMarker);
                setPointMarker = null;
            }
            // データの削除
            var removeData = lineOfSightDataComponent.RemovePointElement(LineOfSightType.landmark, pointMakerName);
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
            return (pointName, new List<string>()
                       {
                           pointName
                       });
        }

        private bool TryDeletePoint(string pointName)
        {
            foreach (Transform child in landmarkMarkers.transform)
            {
                if (child.gameObject.name == pointName)
                {
                    GameObject.Destroy(child.gameObject);
                    lineOfSightDataComponent.RemovePointElement(LineOfSightType.landmark, pointName);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 眺望対象のボタンが押されたときの処理
        /// </summary>
        public void ButtonAction(string pointName)
        {
            foreach (Transform child in landmarkMarkers.transform)
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
            createPointMarker.transform.parent = landmarkMarkers.transform;
        }
        
        /// <summary>
        /// 編集可能か
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CanEdit(string name)
        {
            var data = lineOfSightDataComponent
                .LandmarkDatas.Find(point => point.Name == name);
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
