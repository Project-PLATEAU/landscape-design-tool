using Landscape2.Runtime.Common;
using PLATEAU.CityInfo;
using ProceduralToolkit;
using System;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UIElements;

using Landscape2.Runtime.AdAreaCalcurateSub;
using System.Threading.Tasks;
using UnityEngine.InputSystem.Utilities;
using System.Linq;
using static Landscape2.Runtime.IAdAreaCalculateModel;
using Landscape2.Runtime.DynamicTile;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 壁面広告領域計算
    /// </summary>
    public class AdAreaCalcurate : ISubComponent, IAdAreaCalculateBuildingModel, IAdAreaCalculateModel
    {
        private enum ExecState
        {
            Idle,

            SelectBuilding,
            SelectWall,

            AfterWallSelect,

        }
        public const string BuildingAdAreaCalcLayerName = "BuildingAdAreaCalc";

        readonly Color mouseOverWallColor = new(1f, 0f, 0f, 0.2f);

        readonly Color focusedWallColor = new(1f, 0f, 0.8f, 0.5f);


        const float focusDistanceMultiplyer = 8f;
        GameObjectFocus assetFocus;

        /// <summary>
        /// カメラのフォーカス位置を決定するgameobject。
        /// </summary>
        GameObject focusTargetObject;

        /// <summary>
        /// 現在実行中の処理
        /// </summary>
        ExecState currentState = ExecState.Idle;

        /// <summary>
        /// 壁面選択、1壁面 / 総壁面
        /// </summary>
        SelectMode currentMode = SelectMode.AreaWall;

        /// <summary>
        /// 選択している建物
        /// </summary>
        string currentSelectBuildingGmlID = null;

        /// <summary>
        /// 壁面エリア表示用gameobject
        /// </summary>
        GameObject wallDisplayObject;

        OrientedBounds? selectedWallBounds = null;

        /// <summary>
        /// 選択した壁面表示用GameObject
        /// </summary>
        GameObject focusWallDisplayObject;

        OrientedBounds? focusWallBounds = null;

        /// <summary>
        /// raycast排除チェック用UIElement
        /// </summary>
        VisualElement rootElement;


        //// 選択した建物の面積
        //float currentSelectWallArea = 0f;

        List<int[]> currentSelectWallTris = new();

        AdAreaCalcurateWallCollection selectedWallCollection = new();

        bool isAssetSelect = false;

        public event Action<float> OnWallAreaValueChanged;
        public event Action<float, float> OnWallAreaWHChanged;
        public event Action<SelectMode, ReadOnlyArray<Wall>, ReadOnlyArray<OrientedBounds>> OnWallSelected;

        private readonly IAdAreaCalculateWallPartition wallPartitionCalculator; // null の場合は機能無効

        public AdAreaCalcurate(LandscapeCamera landscapeCamera, VisualElement uiRoot, IAdAreaCalculateWallPartition wallPartitionCalculator, INotifyUpdated notifyUpdated)
        {
            assetFocus = new GameObjectFocus(landscapeCamera);
            assetFocus.focusFinishCallback += _ => assetFocus.FocusFinish();

            rootElement = uiRoot;

            GameObject fto = new()
            {
                name = "TargetViewObject"
            };
            focusTargetObject = fto;

            // AdAreaActionEnabler.Instance.OnEnableCallback += () => OnEnable();  //state = ExecState.SelectBuilding;
            // AdAreaActionEnabler.Instance.OnDisableCallback += () => OnDisable();

            // AdAreaActionEnabler.Instance.OnEnableAllWallAreaSelectCallback += () => currentMode = SelectMode.BuidingWall;
            // AdAreaActionEnabler.Instance.OnEnableOneWallAreaSelectCallback += () => currentMode = SelectMode.AreaWall;

            // TODO:しばらく無効に。実際に使う時にコメントアウトを戻す
            // adAssetListUI = new AdAssetListUI(AdAssetListUI.UIFactory(adRegulationElement), adAssetsList);

            this.wallPartitionCalculator = wallPartitionCalculator; // null なら後から setter で注入も可

        }

        private readonly Dictionary<string, string> buildingNamesWithGmlIDDict = new Dictionary<string, string>();
        private string GetNameFromGmlID(string id)
        {
            if (buildingNamesWithGmlIDDict.TryGetValue(id, out var v))
                return v;
            return null;
        }

        private string GetGmlID(RaycastHit hit, GameObject go)
        {
            var group = go.GetComponent<PLATEAUCityObjectGroup>();
            Debug.Assert(group != null);
            var cityObj = group.GetPrimaryCityObject(hit);
            var id = cityObj?.GmlID;
            if (id == null)
            {
                id = go.name;   // GmlIDが取れない場合はGameObject名をIDとして扱う。通常インポートで発生する。
            }

            var isSuc = buildingNamesWithGmlIDDict.TryAdd(id, go.name);
            if (isSuc == false)
            {
                var old = buildingNamesWithGmlIDDict[id];
                Debug.Assert(old == go.name);
            }
            return id;
        }

        void DisposeWallDisplayObject()
        {
            if (wallDisplayObject != null)
            {
                GameObject.DestroyImmediate(wallDisplayObject);
            }
            wallDisplayObject = null;
        }

        void DisposeFocusWallDisplayObject()
        {
            if (focusWallDisplayObject != null)
            {
                GameObject.DestroyImmediate(focusWallDisplayObject);
            }
            focusWallDisplayObject = null;

            focusWallBounds = null;
        }


        public void OnDisable()
        {
            Debug.Log($"ondisable");

            currentSelectBuildingGmlID = null;
            //currentSelectWallArea = 0f;

            DisposeWallDisplayObject();
            DisposeFocusWallDisplayObject();

            selectedWallCollection.Clear();

            // ここは強制力のあるstate変更
            currentState = ExecState.Idle;

            isAssetSelect = false;
        }

        public void OnEnable()
        {
            Debug.Log($"onenable");
            currentSelectBuildingGmlID = null;
            //currentSelectWallArea = 0f;

            DisposeWallDisplayObject();
            DisposeFocusWallDisplayObject();
            selectedWallCollection.Clear();

            isAssetSelect = false;
        }

        public void Start()
        {
        }

        /// <summary>
        /// ガード付き状態変更
        /// </summary>
        /// <param name="state"></param>
        void ChangeState(ExecState state)
        {
            if (currentState != ExecState.Idle)
            {
                currentState = state;
            }
        }

        ExecState StateSelectBuilding()
        {
            ExecState next = ExecState.SelectBuilding;
            // マウス左クリック
            if (!Input.GetMouseButtonDown(0) || !CameraMoveByUserInput.IsMouseActive)
            {
                return next;
            }

            if (UIUtil.IsPointerOverUI(Input.mousePosition, rootElement))
            {
                return next;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Debug.DrawRay(ray.origin, ray.direction * 100, Color.green, 15, true);

            var result = Physics.RaycastAll(ray.origin, ray.direction, float.MaxValue);


            if (0 < result.Length)
            {
                foreach (var hit in result)
                {
                    var go = hit.collider.gameObject;
                    // 選択可能な建物かどうか
                    if (CityObjectUtil.IsSelectableBuilding(go) == false)
                    {
                        continue;
                    }

                    var hitObjGmlID = GetGmlID(hit, go);
                    if (currentSelectBuildingGmlID != null)
                    {
                        if (currentSelectBuildingGmlID == hitObjGmlID)
                        {
                            Debug.Log($"select building same:{go.name}", go);
                            break;
                        }
                    }
                    // ビルにfocusする
                    Debug.Log($"select new Building : {go.name}", go);
                    var bounds = RendererUtil.CalculateBounds(go);
                    focusTargetObject.transform.position = bounds.center;
                    assetFocus.Focus(focusTargetObject.transform, focusDistanceMultiplyer);
                    currentSelectBuildingGmlID = hitObjGmlID;

                    DisposeWallDisplayObject();
                    DisposeFocusWallDisplayObject();
                    selectedWallCollection.Clear();
                    next = ExecState.SelectWall;
                    break;
                }
            }

            //currentSelectWallArea = 0f;
            Debug.Log($"SelectBuilding next: {next}");
            return next;
        }

        Mesh GetMeshDataFromCollider(GameObject go)
        {
            var mesh = go.GetComponent<MeshCollider>().sharedMesh;
            return mesh;
        }

        // 1壁面
        ExecState StateSelectSingleWall(bool forceMultiWallSelect = false)
        {
            ExecState next = ExecState.SelectWall;
            var isKeyDownCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (forceMultiWallSelect)
            {
                isKeyDownCtrl = true;
            }

            if (!isKeyDownCtrl)
            {
                var selectBuilding = StateSelectBuilding();
                if (selectBuilding == ExecState.SelectWall)
                {
                    // 他のビルを選択した
                    Debug.Log($"select other building : {GetNameFromGmlID(currentSelectBuildingGmlID)}");

                    // 壁面表示オブジェクトを破棄はselectbuildingでやられてる気がする。が念を押す
                    DisposeWallDisplayObject();

                    return next;
                }
            }

            if (UIUtil.IsPointerOverUI(Input.mousePosition, rootElement))
            {
                return next;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var result = Physics.RaycastAll(ray.origin, ray.direction, float.MaxValue);

            if (result.Length <= 0)
            {
                return next;
            }

            result = result.OrderBy(x => x.distance).ToArray();


            foreach (var hit in result)
            {
                var go = hit.collider.gameObject;

                if (currentSelectBuildingGmlID == null)
                {
                    Debug.LogWarning($"currentSelectBuilding is null: {currentSelectBuildingGmlID}");
                    break;
                }

                // 選択可能な建物かどうか
                if (Common.CityObjectUtil.IsSelectableBuilding(go) == false)
                {
                    continue;
                }

                // 選択したmeshの取得
                var meshFilter = GetMeshDataFromCollider(go);
                if (meshFilter == null)
                {
                    Debug.Log($"no meshfilter: {go.name}");
                    continue;
                }

                // hitから一番近いmeshの三角形を取得する
                List<int[]> meshTris = new();
                var tris = GetNearestMeshVertices(meshFilter, hit.point, ray.direction);

                var triNormal = GetTriangleNormal(meshFilter, tris);

                var changed = SelectOneWall(meshFilter, tris, selectedWalls: selectedWallCollection);
                if (changed)
                {
                    // 変更されたので削除して再生成を促す
                    DisposeWallDisplayObject();
                }

                // 面積の算出
                // var areaSize = Wall.CalculateArea(currentSelectWallTris, meshFilter);
                //if (0 < currentSelectWallTris.Count)
                //{
                //    currentSelectWallArea = GetAllTriangleArea(meshFilter);
                //}

                meshTris.AddRange(currentSelectWallTris);

                var enableBounds = CreateSelectWall(meshFilter, meshTris, go, mouseOverWallColor,
                    out var bounds, ref wallDisplayObject);
                selectedWallBounds = enableBounds ? bounds : null;

                // 選択を確定させた
                if (Input.GetMouseButtonUp(0))
                {
                    // ctrlキーが押されていない場合はリストをクリア
                    if (!isKeyDownCtrl)
                    {
                        selectedWallCollection.Clear();
                    }
                    var pointingWallTris = GetPointingTris();
                    if (pointingWallTris == null)
                    {
                        break;
                    }

                    var hitObjGmlID = GetGmlID(hit, go);
                    var hasTriangleWall = selectedWallCollection.HasTriangleWall(pointingWallTris, hitObjGmlID);

                    // removeしてほしくないとき
                    // 1. forceMultiWallSelect == true && hasTriangleWall == true => none
                    // forceMultiWallSelect == true && hasTriangleWall == false  => add
                    // forceMultiWallSelect == false => add / remove

                    if ((!forceMultiWallSelect) || (forceMultiWallSelect && !hasTriangleWall))
                    {
                        var addedWall = selectedWallCollection.AddOrRemove(pointingWallTris, currentSelectWallTris, meshFilter, hitObjGmlID);
                        selectedWallCollection.CalculateAllWallArea();

                        var wallDataList = selectedWallCollection.WallDataList;
                        Debug.Log($"wallDataList count: {wallDataList.Count}");

                        DisposeFocusWallDisplayObject();

                        float wallWidth = 0f;
                        float wallHeight = 0f;
                        float wallSize = 0f;

                        List<OrientedBounds> wallBoundsList = new();
                        foreach (var wallData in wallDataList)
                        {
                            var wallBounds = BoundsUtil.GetOrientedBoundsFromMesh(wallData.Vertices, wallData.Bounds, wallData.WallTris);
                            //var triNorm = TriangleUtil.ComputeNormalizedSumNormal(wallData.Vertices, wallData.WallTris);

                            if (Vector3.Dot(triNormal, wallBounds.Forward) < 0f)
                            {
                                wallBounds = BoundsUtil.AlignForwardToNormal(wallBounds, triNormal);
                            }

                            wallWidth += wallBounds.Size.x;
                            wallHeight = Mathf.Max(wallHeight, wallBounds.Size.y);

                            wallBoundsList.Add(wallBounds);
                        }

                        OnWallSelected?.Invoke(currentMode, wallDataList, wallBoundsList.ToArray());

                        wallSize = selectedWallCollection.AllWllArea;
                        OnWallAreaValueChanged?.Invoke(wallSize);
                        OnWallAreaWHChanged?.Invoke(wallWidth, wallHeight);

                        CreateWallDisplayGameObject(wallDataList, focusedWallColor, out focusWallDisplayObject);

                        // FIXME: 一番最後に選択した面のboundsをfocusWallBoundsに代入する
                        // 出来ればwallWidth,wallHeightを計算しているforeach内でaddedWallをチェックして算出しておきたいが、一旦個別でboundsを作成する
                        {
                            var trinorm = TriangleUtil.ComputeNormalizedSumNormal(addedWall.Vertices, addedWall.WallTris);
                            var fwb = BoundsUtil.GetOrientedBoundsFromMesh(addedWall.Vertices, addedWall.Bounds, addedWall.WallTris);
                            if (Vector3.Dot(trinorm, fwb.Forward) < 0f)
                            {
                                fwb = BoundsUtil.AlignForwardToNormal(fwb, trinorm);
                            }

                            focusWallBounds = fwb;
                        }
                    }

                    next = ExecState.AfterWallSelect;
                }

                break;
            }



            return next;
        }

        // 総壁面
        ExecState StateSelectBuildingWall()
        {
            ExecState next = ExecState.SelectWall;

            var selectBuilding = StateSelectBuilding();
            if (selectBuilding == ExecState.SelectWall)
            {
                // 他のビルを選択した
                Debug.Log($"select other building : {GetNameFromGmlID(currentSelectBuildingGmlID)}");

                // 壁面表示オブジェクトを破棄はselectbuildingでやられてる気がする。が念を押す
                DisposeWallDisplayObject();

                return next;
            }

            if (UIUtil.IsPointerOverUI(Input.mousePosition, rootElement))
            {
                return next;
            }

            if (!Input.GetMouseButtonUp(0))
            {
                return next;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var result = Physics.RaycastAll(ray.origin, ray.direction, float.MaxValue);

            if (result.Length <= 0)
            {
                return next;
            }
            foreach (var hit in result)
            {
                var go = hit.collider.gameObject;
                // 選択可能な建物かどうか
                if (CityObjectUtil.IsSelectableBuilding(go) == false)
                {
                    continue;
                }

                var hitObjGmlID = GetGmlID(hit, go);
                // 選択しているビルはhitしたobjectでなかったらスキップ
                if (currentSelectBuildingGmlID != hitObjGmlID)
                {
                    continue;
                }

                // 選択したmeshの取得
                var meshFilter = GetMeshDataFromCollider(go);
                if (meshFilter == null)
                {
                    Debug.Log($"no meshfilter: {go.name}");
                    continue;
                }

                // hitから一番近いmeshの三角形を取得する
                List<int[]> meshTris = new();
                var tris = GetNearestMeshVertices(meshFilter, hit.point, ray.direction);

                var triNormal = GetTriangleNormal(meshFilter, tris);

                SelectAllWall(meshFilter, tris);

                //// 面積の算出
                //var areaSize = Wall.CalculateArea(currentSelectWallTris, new TriangleAreaCalcInfo(meshFilter));

                meshTris.AddRange(currentSelectWallTris);

                var enableBounds = CreateSelectWall(meshFilter, meshTris, go, mouseOverWallColor,
                    out var bounds, ref wallDisplayObject);

                // 選択を確定させた
                selectedWallCollection.Clear();

                selectedWallCollection.Add(currentSelectWallTris, meshFilter, hitObjGmlID);  // クリアしているので必ず新規追加
                selectedWallCollection.CalculateAllWallArea();

                DisposeFocusWallDisplayObject();
                SelectWallDisplayAction(
                    meshFilter, selectedWallCollection.MergeTris(), selectedWallCollection.AllWllArea, go, focusedWallColor,
                    ref focusWallDisplayObject, "FocusWallDisplayObject");

                next = ExecState.AfterWallSelect;

                break;
            }

            return next;
        }

        void CreateWallDisplayGameObject(ReadOnlyArray<Wall> wallDatas, Color color, out GameObject wallDisplayObject)
        {
            wallDisplayObject = null;

            List<Vector3[]> triVertices = new();
            foreach (var wall in wallDatas)
            {
                foreach (var tri in wall.WallTris)
                {
                    var triVert = new Vector3[] {
                        wall.Vertices[tri[0]],
                        wall.Vertices[tri[1]],
                        wall.Vertices[tri[2]]
                    };
                    triVertices.Add(triVert);
                }
            }

            CreateDisplayObjectFromTriVertices(triVertices, color, out wallDisplayObject);
        }

        void CreateDisplayObjectFromTriVertices(List<Vector3[]> triVertices, Color color, out GameObject wallDisplayObject)
        {
            MeshDraft meshDraft = new MeshDraft();
            foreach (var triVert in triVertices)
            {
                meshDraft.AddTriangle(
                    triVert[0],
                    triVert[1],
                    triVert[2],
                    true
                );
            }

            var mesh = meshDraft.ToMesh();

            wallDisplayObject = new GameObject();
            wallDisplayObject.name = "FocusWallDisplayObject";
            wallDisplayObject.AddComponent<MeshFilter>().mesh = mesh;
            wallDisplayObject.AddComponent<MeshRenderer>().material = CreateWallMaterial(color);

        }

        /// <summary>
        /// 選択した壁面を確定色で表示させる為のGameObjectを作る
        /// </summary>
        /// <param name="enableBounds"></param>
        /// <param name="bounds"></param>
        void SelectWallDisplayAction(
            Mesh meshFilter, List<int[]> meshTris, float areaSize, GameObject selectBuilding, Color selectingColor,
            ref GameObject wallDisplayObject, string name)
        {
            //wallObj = CreateFocusWallDisplayObject(wallDisplayObject);
            bool enableBounds = CreateSelectWall(meshFilter, meshTris, selectBuilding, selectingColor, out var bounds, ref wallDisplayObject);
            wallDisplayObject.name = name;

            var wdmr = wallDisplayObject.GetComponent<MeshRenderer>();
            wdmr.enabled = true;

            OnWallAreaValueChanged?.Invoke(/*currentSelectWallArea*/areaSize);
            Debug.Log($"enableBounds: {enableBounds} / {bounds.Size.x} / {bounds.Size.y}");
            if (enableBounds)
            {
                OnWallAreaWHChanged?.Invoke(bounds.Size.x, bounds.Size.y);
                focusWallBounds = bounds;
            }
            else
            {
                focusWallBounds = null;
            }
        }

        /// <summary>
        /// 選択した三角形ポリゴンを返す
        /// </summary>
        /// <returns></returns>
        private int[] GetPointingTris()
        {
            // ポイントした箇所が最初に追加されているぽかったので
            if (currentSelectWallTris?.Count > 0)
            {
                return currentSelectWallTris?[0];
            }
            return null;
        }

        /// <summary>
        /// 壁面を選択して、計算する壁面を確定させたstate
        /// </summary>
        /// <returns></returns>
        ExecState StateAfterWallSelect()
        {
            var next = ExecState.AfterWallSelect;

            // FIXME: 広告物配置中に壁面をクリックされた場合は壁面選択処理を行わない様にする

            // var selectWallResult = currentMode == SelectMode.BuildingWall ? StateSelectBuildingWall() : StateSelectSingleWall(isAssetSelect);
            var selectWallResult = currentMode switch
            {
                SelectMode.BuildingWall => StateSelectBuildingWall(),
                SelectMode.AreaWall => StateSelectSingleWall(isAssetSelect),
                SelectMode.PartitionWall => StateSelectSingleWall(isAssetSelect),
                _ => ExecState.AfterWallSelect,
            };
            // if (selectWallResult == ExecState.SelectWall)
            // {
            //     return selectWallResult;
            // }

            return next;
        }

        public static void DrawOrientedBounds(OrientedBounds bounds, Color color, float duration = 0f, bool depthTest = true)
        {
#if UNITY_EDITOR
            var corners = bounds.GetCorners();

            // 天面
            Debug.DrawLine(corners[0], corners[1], color, duration, depthTest);
            Debug.DrawLine(corners[1], corners[2], color, duration, depthTest);
            Debug.DrawLine(corners[2], corners[3], color, duration, depthTest);
            Debug.DrawLine(corners[3], corners[0], color, duration, depthTest);

            // 底面
            Debug.DrawLine(corners[4], corners[5], color, duration, depthTest);
            Debug.DrawLine(corners[5], corners[6], color, duration, depthTest);
            Debug.DrawLine(corners[6], corners[7], color, duration, depthTest);
            Debug.DrawLine(corners[7], corners[4], color, duration, depthTest);

            // 側面
            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(corners[i], corners[i + 4], color, duration, depthTest);
            }
#endif
        }


        public static void DrawBounds(Bounds bounds, Color color, float duration = 0f, bool depthTest = true)
        {
#if UNITY_EDITOR
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;

            // 8頂点を算出
            Vector3[] pts = new Vector3[8]
            {
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3(-e.x, -e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z)
            };

            // 天面
            Debug.DrawLine(pts[0], pts[1], color, duration, depthTest);
            Debug.DrawLine(pts[1], pts[2], color, duration, depthTest);
            Debug.DrawLine(pts[2], pts[3], color, duration, depthTest);
            Debug.DrawLine(pts[3], pts[0], color, duration, depthTest);

            // 底面
            Debug.DrawLine(pts[4], pts[5], color, duration, depthTest);
            Debug.DrawLine(pts[5], pts[6], color, duration, depthTest);
            Debug.DrawLine(pts[6], pts[7], color, duration, depthTest);
            Debug.DrawLine(pts[7], pts[4], color, duration, depthTest);

            // 側面
            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(pts[i], pts[i + 4], color, duration, depthTest);
            }
#endif
        }


        /// <summary>
        /// 壁面を作る
        /// </summary>
        /// <param name="meshFilter"></param>
        /// <param name="meshTris"></param>
        /// <param name="selectBuilding"></param>
        static bool CreateSelectWall(
            Mesh meshFilter, List<int[]> meshTris, GameObject selectBuilding, Color selectingColor,
            out OrientedBounds bounds, ref GameObject wallDisplayObject)
        {
            if (wallDisplayObject != null)
            {
                bounds = new();
                return false;
            }

            var meshDraft = CreateAllWallAreaDraft(meshFilter, meshTris);
            var triangle = meshDraft.ToMesh();
            var tri = new GameObject();
            tri.transform.SetPositionAndRotation(selectBuilding.transform.position, selectBuilding.transform.rotation);

            tri.AddComponent<MeshFilter>().mesh = triangle;
            tri.AddComponent<MeshRenderer>().material = CreateWallMaterial(selectingColor);

            bounds = BoundsUtil.GetLocalOBBFromMesh(triangle);
            var triNormal = TriangleUtil.ComputeNormalizedSumNormal(meshFilter, meshTris);

            if (Vector3.Dot(triNormal, bounds.Forward) < 0f)
            {
                bounds = BoundsUtil.AlignForwardToNormal(bounds, triNormal);
            }

            Debug.DrawRay(bounds.center, bounds.Forward * 10f, Color.yellow, 120, true);
            Debug.DrawRay(bounds.center + Vector3.up, triNormal * 10f, Color.red, 120, true);

            DrawOrientedBounds(bounds, Color.cyan);
            DrawBounds(tri.GetComponent<MeshFilter>().sharedMesh.bounds, Color.red);

            wallDisplayObject = tri;
            return true;
        }

        /// <summary>
        /// 1壁面mesh作成
        /// </summary>
        /// <param name="meshFilter"></param>
        /// <param name="tris"></param>
        /// <returns></returns>
        bool SelectOneWall(Mesh meshFilter, int[] tris, AdAreaCalcurateWallCollection selectedWalls)
        {
            bool ischangeselect = false;
            var triNormal = GetTriangleNormal(meshFilter, tris);
#if UNITY_EDITOR
            Debug.DrawLine(meshFilter.vertices[tris[0]], meshFilter.vertices[tris[1]], Color.blue, 0, true);
            Debug.DrawLine(meshFilter.vertices[tris[1]], meshFilter.vertices[tris[2]], Color.blue, 0, true);
            Debug.DrawLine(meshFilter.vertices[tris[2]], meshFilter.vertices[tris[0]], Color.blue, 0, true);
#endif
            // 選択しているmeshの3角形が、現在選択している面に含まれている場合は生成しない
            if (currentSelectWallTris.Contains(tris))
            {
                return ischangeselect;
            }
            //if (selectedWalls.Contains(tris))
            //{
            //    Debug.Log($"already selected wall: {tris[0]}, {tris[1]}, {tris[2]}");
            //    return ischangeselect;
            //}

            // ここから壁面算出

            // 壁となる3角形抽出
            var wallVerticesIndexes = GetTrianglesWithSameNormal(meshFilter, Vector3.up, 0.05f);
            var intrisResult = new List<int[]>();

            System.Text.StringBuilder sb = new();

            // triと同じ方向(0.8f < dot)の法線を持つtriを取得
            foreach (var t in wallVerticesIndexes)
            {
                var n = Vector3.Cross(
                    meshFilter.vertices[t[1]] - meshFilter.vertices[t[0]],
                    meshFilter.vertices[t[2]] - meshFilter.vertices[t[1]]).normalized;

                var dot = Vector3.Dot(n, triNormal);
                sb.AppendLine($"dot: {dot} : {triNormal},{n}");
                if (dot <= 0.8f) // 
                {
                    sb.AppendLine("dot <= 0 : continue");
                    continue;
                }

                sb.AppendLine($"add : ({t[0]},{t[1]}, {t[2]})");
                intrisResult.Add(t);
            }


            if (0 < intrisResult.Count)
            {
                var resultTris = TriangleUtil.GetConnectedSurfaceByPosition(meshFilter, intrisResult, tris, 0.01f);
                sb.AppendLine($"finish: {resultTris.Count}");

                currentSelectWallTris = resultTris;  // intrisResult;
                Debug.Assert(GetPointingTris() == tris, "選択された三角形ポリゴンは最初の要素として追加されている必要がある");
                ischangeselect = true;
            }

            return ischangeselect;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="meshFilter"></param>
        /// <param name="tris"></param>
        /// <returns></returns>
        bool SelectAllWall(Mesh meshFilter, int[] tris)
        {
            var wallVerticesIndexes = GetTrianglesWithSameNormal(meshFilter, Vector3.up, 0.2f);
            Debug.Log($"wallVerticesIndexes: {wallVerticesIndexes.Count}");

            currentSelectWallTris = wallVerticesIndexes;
            return false;
        }
        /// <summary>
        /// 法線取得
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="tris"></param>
        /// <returns></returns>
        Vector3 GetTriangleNormal(Mesh mesh, int[] tris)
        {
            var v1 = mesh.vertices[tris[1]] - mesh.vertices[tris[0]];
            var v2 = mesh.vertices[tris[2]] - mesh.vertices[tris[0]];
            return Vector3.Cross(v1, v2).normalized;
        }

        static MeshDraft CreateAllWallAreaDraft(Mesh mesh, List<int[]> meshTris)
        {
            var meshDraft = new MeshDraft();
            foreach (var t in meshTris)
            {
                meshDraft.AddTriangle(
                    mesh.vertices[t[0]],
                    mesh.vertices[t[1]],
                    mesh.vertices[t[2]],
                    true);
            }
            return meshDraft;
        }

        /// <summary>
        /// 壁面マテリアル作成
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        static Material CreateWallMaterial(Color color)
        {
            var mat = MaterialUtil.MakeMaterial(color);

            mat.SetFloat("_SurfaceType", 1f); // Transparent
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // TODO: Runtimeでもイケるか不明なので確認する事
            mat.SetColor("_UnlitColor", color);

            return mat;
        }

        /// <summary>
        /// 法線に垂直に近い3角形を返す
        /// </summary>
        /// <param name="mesh">Mesh</param>
        /// <param name="normal">法線</param>
        /// <param name="normRange">法線の許容範囲</param>
        /// <returns>verticesのindexの配列を返す</returns>
        private List<int[]> GetTrianglesWithSameNormal(Mesh mesh, Vector3 normal, float normRange = 0f)
        {
            var result = new List<int[]>();

            var tri = mesh.triangles;
            for (int i = 0; i < tri.Length; i += 3)
            {
                var n = Vector3.Cross(mesh.vertices[tri[i + 1]] - mesh.vertices[tri[i + 0]], mesh.vertices[tri[i + 2]] - mesh.vertices[tri[i + 1]]).normalized;

                var dot = Vector3.Dot(n, normal);
                if (Mathf.Abs(dot) < normRange)
                {
                    result.Add(new int[] { tri[i + 0], tri[i + 1], tri[i + 2] });
                }
            }

            return result;
        }

        /// <summary>
        /// posに一番近いmesh内の3角形の頂点を返す
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="pos"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public int[] GetNearestMeshVertices(Mesh mesh, Vector3 pos, Vector3 normal)
        {
            var triangles = mesh.triangles;
            var minDistSq = float.MaxValue;
            var minTri = new int[3];

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v0 = mesh.vertices[triangles[i]];
                var v1 = mesh.vertices[triangles[i + 1]];
                var v2 = mesh.vertices[triangles[i + 2]];
                var tpos = TriangleUtil.ClosestPointOnTriangle(pos, v0, v1, v2);
                var distSq = Vector3.SqrMagnitude(pos - tpos);
                var n = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                // normalと nの逆ベクトルを内積に掛けて同じ向きかどうかを判定
                if (0 < Vector3.Dot(normal, -n) && distSq < minDistSq)
                {
                    minDistSq = distSq;
                    minTri[0] = triangles[i];
                    minTri[1] = triangles[i + 1];
                    minTri[2] = triangles[i + 2];
                }
            }

            return minTri;
        }

        public void Update(float deltaTime)
        {
            ExecState nextState = currentState;
            switch (currentState)
            {
                case ExecState.SelectBuilding:
                    nextState = StateSelectBuilding();
                    break;
                case ExecState.SelectWall:
                    // nextState = currentMode == SelectMode.BuildingWall ? StateSelectBuildingWall() : StateSelectSingleWall();
                    nextState = currentMode switch
                    {
                        SelectMode.BuildingWall => StateSelectBuildingWall(),
                        SelectMode.AreaWall => StateSelectSingleWall(),
                        SelectMode.PartitionWall => StateSelectSingleWall(),
                        _ => ExecState.SelectWall,
                    };
                    break;
                case ExecState.AfterWallSelect:
                    nextState = StateAfterWallSelect();
                    break;
                default:
                    break;

            }

            ChangeState(nextState);
        }


        public void LateUpdate(float deltaTime)
        {
        }

        public void SetBuidingWallSelectMode()
        {
            currentMode = SelectMode.BuildingWall;
            if (currentSelectBuildingGmlID != null)
            {
                DisposeFocusWallDisplayObject();
                selectedWallCollection.Clear();
                DisposeWallDisplayObject();

                // TODO: 今のビルの総壁面を再生成
            }
        }

        public void SetWallSelectMode()
        {
            currentMode = SelectMode.AreaWall;
            if (currentSelectBuildingGmlID != null)
            {
                DisposeFocusWallDisplayObject();
                selectedWallCollection.Clear();
                DisposeWallDisplayObject();
            }
        }

        public void SetPartitionWallSelectMode()
        {
            currentMode = SelectMode.PartitionWall;
            if (currentSelectBuildingGmlID != null)
            {
                DisposeFocusWallDisplayObject();
                selectedWallCollection.Clear();
                DisposeWallDisplayObject();
            }
        }

        public void Activate()
        {
            OnEnable();

            // ここは強制力のあるstate変更
            currentState = ExecState.SelectBuilding;
        }

        public void Deactivate()
        {
            OnDisable();
        }

        public bool TryGetSelectWallBounds(out OrientedBounds bounds)
        {
            //if (state != ExecState.SelectAdAsset)
            //{
            //    bounds = new();
            //    return false;
            //}

            if (selectedWallBounds == null)
            {
                bounds = new();
                return false;
            }
            bounds = selectedWallBounds ?? new();
            return true;
#if false
            if (focusWallBounds == null)
            {
                bounds = new();
                return false;
            }

            bounds = focusWallBounds ?? new();
            return true;
#endif
        }

        public bool TryGetFocusWallBounds(out OrientedBounds bounds)
        {
            if (focusWallBounds == null)
            {
                bounds = new();
                return false;
            }

            bounds = focusWallBounds ?? new();
            return true;
        }

        public void OnAssetSelect(GameObject asset)
        {
            isAssetSelect = true;
        }

        public async void OnAssetPut(GameObject asset)
        {
            await Task.Delay(200); // 16msあればいい筈
            isAssetSelect = false;

        }

        public void OnAssetSelectCancel()
        {
            isAssetSelect = false;
        }
    }
}
