using Landscape2.Runtime.Common;
using Landscape2.Runtime.LandscapePlanLoader;
using PLATEAU.CityInfo;
using PlateauToolkit.Sandbox;
using PlateauToolkit.Sandbox.Runtime;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Common;
using ProceduralToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Landscape2.Runtime.AdvertisingPlacementRestrictions
{
    public interface IAdvertisingPlacementRestrictionsModule
    {
        // ISubComponentのUpdateで更新しているため今は必要ない。
        //// 更新　設定しているゲームオブジェクトが変更された場合に利用するつもりで作成
        //void Update();

        void EnableUpdate(bool isEnable);

        void Reset();

        bool UpdateAdObjects(bool isForce);

        bool SetRestrictionDistance(float distance);

        void DisplayArea(bool isDisplay);

        event System.Action<float> OnChangedDistance;
    }

    public class AdvertisingPlacementRestrictionsModule : IAdvertisingPlacementRestrictionsModule, ISubComponent
    {
        // エリアオブジェクトの親
        private GameObject areaRoot;
        private GameObject AreaRoot
        {
            get
            {
                if (areaRoot == null)
                {
                    areaRoot = new GameObject();
                    areaRoot.name = "AdAreaRoot";
                }
                return areaRoot;
            }
        }

        // 都市モデルのバウンディングボックス
        private Bounds cityInstanceBounds;

        private struct AreaHeight
        {
            public AreaHeight(float min = 50.0f)
            {
                this.minHeight = min;
                this.height = min;
            }

            // エリア表示の高さの最小値
            private float minHeight;
            // エリア表示の高さ
            private float height;
            public float Height
            {
                get
                {
                    return height;
                }
                set
                {
                    // 常に最大値で更新する　低い値を適用しない
                    var v = MathF.Max(value, minHeight);
                    height = MathF.Max(v, height);
                }
            }

        }
        AreaHeight areaHeight = new AreaHeight(50.0f);

        // 対象の広告物
        private List<PlateauSandboxPlaceableHandler> targetAdObjs = new List<PlateauSandboxPlaceableHandler>();

        // Advertisement_AFrameSign_01
        // Advertisement_WallAdPanel_01
        // Advertisement_Vertical_04
        // Advertisement_RooftopAdBoard_01
        // Advertisement_Vertical_05
        // Advertisement_DigitalSignage_01
        // Advertisement_AdBoard_01
        // Advertisement_AdTower_01
        // Advertisement_RooftopAdTower_01
        // Advertisement_SidewalkSign_01
        // Advertisement_Billboard_01
        // Advertisement_WallSignboard
        // Advertisement_Billboard

        private readonly string[] advertisingPlacementRestrictionTypes = new string[]
        {
            "Advertisement_AFrameSign",
            "Advertisement_DigitalSignage",
            "Advertisement_AdBoard",
            "Advertisement_AdTower",
            "Advertisement_SidewalkSign",
            "Advertisement_Billboard",
            //"Advertisement_WallSignboard",  // 仮
        };

        private class Parameter
        {
            public static float DefaultDistance { get => 5.0f; }
            private float distance = DefaultDistance;
            public float Distance
            {
                get => distance;
                set
                {
                    var v = Mathf.Max(1e-3f, value);
                    distance = v;
                }
            }

            public Material WallMaterial { get; internal set; }

            public Material CeilingMaterial { get; internal set; }

            internal bool IsValid()
            {
                // パラメータが有効か確認
                if (WallMaterial == null)
                {
                    Debug.LogError("Wall material is not set.");
                    return false;
                }

                if (Distance <= 0)
                {
                    Debug.LogError("Distance must be greater than zero.");
                    return false;
                }

                return true;
            }
        }
        Parameter parameter;

        /// <summary>
        /// エリアを表すクラス
        /// </summary>
        class Area
        {
            public struct TargetInfo
            {
                public TargetInfo(float radius, float height, Vector3 center)
                {
                    this.Radius = radius;
                    this.Height = height;
                    this.Center = center;
                }
                public float Radius { get; private set; }
                public float Height { get; private set; }
                public Vector3 Center { get; private set; }
            }

            // 対応しているオブジェクトのTransform
            TargetInfo target;


            // エリアを所持するゲームオブジェクト
            GameObject areaObj;

            // エリアの頂点群　加工前
            List<Vector3> originalPoints;

            float height;

            public TargetInfo Target => target;

            public GameObject AreaObj => areaObj;
            public List<Vector3> Points => originalPoints;

            public float Height => height;

            public Area(GameObject obj, List<Vector3> originalPoints, float height, TargetInfo target)
            {
                this.areaObj = obj;
                this.originalPoints = originalPoints;
                this.height = height;
                this.target = target;
            }

            public static void Release(ref Area area)
            {
                if (area == null)
                    return;

                if (area.areaObj != null)
                {
                    GameObject.Destroy(area.areaObj);
                }

                area = null;
            }
            public static void Release(ref List<Area> areas)
            {
                var nArea = areas.Count;
                for (var i = 0; i < nArea; i++)
                {
                    var area = areas[i];
                    Area.Release(ref area);
                    areas[i] = area;
                }
            }

        }
        private Dictionary<GameObject, Area> areaMap = new Dictionary<GameObject, Area>();

        // 機能の有効無効　無効時に‘UpdateAdObjects()’類は動作しない
        bool isEnable = false;

        private bool isDisplay = true;

        // セーブロードを扱うための補助
        AdAreaSaveLoadHelper saveLoadHelper;

        // エリア表示対象の広告物群を更新する必要があるか示すフラグ　セーブロードのイベントの際にtrueにして遅延実行用に作成
        bool isNeedUpdateAdObjects = false;

        public AdvertisingPlacementRestrictionsModule(SaveSystem saveSystem)
        {
            parameter = new Parameter();
            this.saveLoadHelper = new AdAreaSaveLoadHelper(ProjectSaveDataType.AdArea, saveSystem);
            saveLoadHelper.LoadCallback += SaveLoadSystem_LoadCallback;
            saveLoadHelper.DeleteCallback += SaveLoadSystem_DeleteCallback;
            saveLoadHelper.ProjectChangeCallback += SaveLoadSystem_ProjectChangeCallback;

        }


        private void SaveLoadSystem_ProjectChangeCallback(SampleData d0)
        {
            // 読み込みまれたデータを設定する
            isNeedUpdateAdObjects = true;
            Reset();
            SetRestrictionsDistance(d0?.distance ?? Parameter.DefaultDistance);
            onChangedDistance?.Invoke(parameter.Distance);

        }

        private void SaveLoadSystem_DeleteCallback(SampleData data)
        {
            isNeedUpdateAdObjects = true;
            Reset();
        }

        private void SaveLoadSystem_LoadCallback(SampleData d)
        {
            // 読み込みまれたデータを設定する
            isNeedUpdateAdObjects = true;
            Reset();
            SetRestrictionsDistance(d?.distance ?? Parameter.DefaultDistance);
            onChangedDistance?.Invoke(parameter.Distance);
        }

        private Action<float> onChangedDistance;
        event Action<float> IAdvertisingPlacementRestrictionsModule.OnChangedDistance
        {
            add
            {
                onChangedDistance += value;
            }

            remove
            {
                onChangedDistance -= value;
            }
        }

        void ISubComponent.LateUpdate(float deltaTime)
        {
            if (isNeedUpdateAdObjects)
            {
                UpdateAdObjects();
                isNeedUpdateAdObjects = false;
            }
        }

        void ISubComponent.OnDisable()
        {
        }

        void ISubComponent.OnEnable()
        {
        }

        void ISubComponent.Start()
        {
            var materialPath = "Materials/ADPlacementRestrictionZoneWallMaterial";
            var wallMaterial = Resources.Load<Material>(materialPath);
            if (wallMaterial == null)
            {
                Debug.LogError("Failed to load wall material from Resources. Ensure the material exists at the specified path: " + materialPath);
                return;
            }
            parameter.WallMaterial = wallMaterial;

            var ceilingMaterial = Resources.Load<Material>("Materials/ADPlacementRestrictionZoneCeilingMaterial");
            if (ceilingMaterial == null)
            {
                Debug.LogError("Ceiling material not found. Ensure the material is placed in Resources/Materials/RestrictionZoneCeilingMaterial.");
                return;
            }
            parameter.CeilingMaterial = ceilingMaterial;
        }

        void ISubComponent.Update(float deltaTime)
        {
            if (isEnable == false)
                return;

            foreach (var item in this.targetAdObjs)
            {
                UpdateAdObjecctByTransform(item.gameObject);
            }
        }

        private void UpdateAdObjecctByTransform(GameObject adObject, bool isForce = false)
        {
            // todo adutilに判定処理は移植する　transform変更、通知はどこかのクラスでまとめて行う
            // イメージ　ADObjectDetector.Subscribe(adObj, method)

            if (IsChanged(adObject) || isForce)
            {
                // トランスフォームが変更された場合は、広告物オブジェクトの位置を更新
                if (UpdateAdObject(adObject) == false)
                {
                    Debug.LogError("Failed to set ad object. Ensure the ad object is valid and has the required components.");
                    return;
                }
            }

            return;

            static bool IsChanged(GameObject obj)
            {
                if (obj == null)
                {
                    // 広告物オブジェクトが設定されていない場合は変更なし
                    return false;
                }

                // 無理やり　広告物オブジェクトのトランスフォーム更新に合わせる
                bool isChanged = false;
                var hasScaled = obj.TryGetComponent<PlateauSandboxAdvertisementScaled>(out var scaled);

                while (true)
                {
                    if (obj.transform.hasChanged)
                    {
                        isChanged = true;
                        break;
                    }

                    if (scaled?.TransformChanged ?? false)
                    {
                        isChanged = true;
                        break;
                    }

                    break;
                }

                // hasChangedをリセット
                obj.transform.hasChanged = false;
                scaled?.ResetTransformChangedStatus();
                return isChanged;
            }
        }

        private bool UpdateAdObject(GameObject adObject)
        {
            if (adObject == null)
            {
                // 広告物オブジェクトが設定されていない場合は何もしない
                return true;
            }

            if (adObject.TryGetComponent<PlateauSandboxPlaceableHandler>(out var adComp) == false)
            {
                return false;
            }

            if (AdvertisementObjectUtil.GetBoundsFromPlateauSandboxAdvertisement(adComp, out var bounds) == false)
            {
                return false;
            }

            // 広告物が急な傾斜に配置されている時、エリア生成を行わない
            // 角度チェック: 横倒し・ひっくり返り判定
            // 上方向ベクトルとワールド上方向のなす角度で判定
            var up = adObject.transform.up;
            float angle = Vector3.Angle(up, Vector3.up);

            const float angleLimit = 80f;
            if (angle > angleLimit)
            {
                //Debug.Log($"AdObject '{adObject.name}' is tilted too much (angle: {angle}°). Skipping area generation.");
                return false;
            }

            var corners = Common.MeshUtil.GetWorldCorners(bounds, adObject.transform);

            // boundsからワールド空間上に配置する面を計算、中心座標をcornersから算出

            // cornersから下側にある4点を取得する
            var sortedCorners = corners.OrderBy(c => c.y).Take(4).ToList(); // 下側4点取得

            // XY平面上の中心点を計算
            var center = new Vector3(
                sortedCorners.Average(c => c.x),
                sortedCorners.Average(c => c.y),
                sortedCorners.Average(c => c.z)
            );

            // 左回り（反時計回り）にソート
            var bottomCorners = sortedCorners
                .OrderByDescending(c => Mathf.Atan2(c.z - center.z, c.x - center.x))
                .ToList();

            // todo 角度にとわず元の底面を使用する

            var adBoundsRadius = adObject.transform.TransformVector(bounds.extents).magnitude;    // バウンディングボックスの外接球の半径
            var cylinderHeight = bounds.size.y * adObject.transform.lossyScale.y;
            var demBoundsSizeY = GetDemBoundsSizeY(adBoundsRadius, cylinderHeight, center);
            areaHeight.Height = demBoundsSizeY;

            //// バウンディングボックスの４すみを取得
            //var bottomCorners = Common.AdvertisementObjectUtil.CalcBoundBottomCorners(bounds, adComp.transform);

            // エリアを生成
            var targetInfo = new Area.TargetInfo(adBoundsRadius, cylinderHeight, center);
            if (GenerateArea(adObject.name + "_area", bottomCorners, areaHeight.Height, targetInfo, out var newArea) == false)
            {
                Debug.LogError($"Failed to generate area for road: {adObject.name}. Ensure the road mesh and material are valid.");
                return false;
            }

            // エリアの生成に成功
            if (newArea != null)
            {
                areaMap.TryGetValue(adObject, out var area);
                if (area != null)
                {
                    // 既存のエリア削除
                    Area.Release(ref area);
                    areaMap.Remove(adObject);
                }

                areaMap.Add(adObject, newArea);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="boundsRadius">boundsを完全に包み込む球の半径</param>
        /// <param name="cylinderHeight">地形判定をとる際の縦方向への範囲</param>
        /// <param name="center"></param>
        /// <returns></returns>
        private float GetDemBoundsSizeY(float boundsRadius, float cylinderHeight, Vector3 center)
        {
            /// <summary>
            /// 指定座標から垂直（Y軸）方向に円柱状でレイキャストし、ヒットした全オブジェクトを返す
            /// </summary>
            RaycastHit[] CylinderVerticalCastAll(Vector3 center, float radius, float height, int layerMask = Physics.DefaultRaycastLayers)
            {
                // カプセルの両端点を計算（Y軸方向にheight分）
                Vector3 point1 = center + Vector3.up * (height * 0.5f + radius);
                Vector3 dir = Vector3.down;
                var hits = Physics.SphereCastAll(point1, radius, dir);
                return hits;
            }

            var hits = CylinderVerticalCastAll(center, parameter.Distance + boundsRadius, cylinderHeight);
            Bounds demBounds = new Bounds();
            foreach (var hit in hits)
            {
                if (CityObjectUtil.IsGround(hit.collider.gameObject))
                {
                    demBounds.Encapsulate(hit.collider.bounds);
                }
            }

            return demBounds.size.y;
        }

        private List<Vector3> GenerateCornerCurve(List<Vector3> bottomCorners, float radius)
        {
            var numCorners = bottomCorners.Count;
            Debug.Assert(numCorners == 4);  // すべてのコーナは直角90度を想定

            // 規制エリアのアウトラインを形成する頂点群を作成
            var curveSegements = 10; // 90度を8分割      距離によって増やしてもいいかも
            var numPoints = curveSegements * numCorners; // 90度*4=360度
            var points = new List<Vector3>(numPoints);
            for (var i = 0; i < numCorners; i++)
            {
                var start = bottomCorners[i];
                var end = bottomCorners[(i + 1) % numCorners];
                var direction = (start - end).normalized;    // 処理中の角に向けたベクトル　end-startではない
                var rotationAxis = Vector3.up; // Y軸を中心に回転

                // 90度分の点を生成
                var segmentPoints = GenerateByQuaternion(start, direction * radius, rotationAxis, curveSegements);
                points.AddRange(segmentPoints);
            }

            //// 左回りにする　処理負荷軽減のため生成段階で左回りにしてもいい
            //points.Reverse();

            for (int i = 0; i < points.Count; i++)
            {
                Debug.DrawRay(points[i], Vector3.up * (3f + (i * 0.5f)), Color.blue, 10f);
            }

            return points;
        }

        bool IAdvertisingPlacementRestrictionsModule.SetRestrictionDistance(float distance)
        {
            return SetRestrictionsDistance(distance);
        }

        private bool SetRestrictionsDistance(float distance)
        {
            if (parameter.Distance == distance)
                return true;

            // パラメータの設定
            if (distance < 0.0f)
            {
                Debug.LogError("Restriction distance cannot be negative.");
                return false;
            }

            parameter.Distance = distance;

            // セーブデータの更新
            saveLoadHelper?.TryAddDataOrUpdate(distance);

            // 既存エリアを再生成
            if (areaMap.Count > 0)
            {

                var updateList = new Dictionary<GameObject, (Area, Area)>();
                foreach (var item in areaMap)
                {
                    var area = item.Value;
                    if (area == null)
                        continue;
                    var points = area.Points;


                    var demBoundsSizeY = GetDemBoundsSizeY(area.Target.Radius, area.Target.Height, area.Target.Center);
                    areaHeight.Height = MathF.Max(demBoundsSizeY, area.Height);  // 大きいエリアが小さくなると見失う可能性があるので高い方を優先

                    if (GenerateArea(area.AreaObj.name, points, areaHeight.Height, area.Target, out var newArea) == false)
                    {
                        Debug.LogError("Failed to generate area mesh. Ensure the road mesh and material are valid.");
                        return false;
                    }

                    if (newArea != null)
                    {
                        updateList.Add(item.Key, (item.Value, newArea));
                    }

                }

                foreach (var item in updateList)
                {
                    // 新エリアを設定する
                    var oldArea = item.Value.Item1;
                    var newArea = item.Value.Item2;
                    if (oldArea != null)
                    {
                        Area.Release(ref oldArea);
                    }

                    areaMap.Remove(item.Key);
                    areaMap.Add(item.Key, newArea);
                }

            }

            return true;
        }

        /// <summary>
        /// 中心centerを起点に、vectorAを開始ベクトルとして90度分の点を生成します。
        /// </summary>
        private static List<Vector3> GenerateByQuaternion(
            Vector3 center,
            Vector3 vectorA,
            Vector3 rotationAxis,
            int segments  // 分割数。点の個数はsegments+1
        )
        {
            var result = new List<Vector3>(segments + 1);
            float arcAngle = 90f;
            float stepAngle = arcAngle / segments;

            for (int i = 0; i <= segments; i++)
            {
                float angle = stepAngle * i;
                // 回転
                Vector3 rotated = Quaternion.AngleAxis(angle, rotationAxis.normalized) * vectorA;
                result.Add(center + rotated);
            }

            return result;
        }

        private bool GenerateArea(string areaName, List<Vector3> originalPoints, float height, in Area.TargetInfo target, out Area area)
        {
            // とりあえず無効値を設定
            area = null;

            // 頂点数が3以下の場合はエリアを生成しない
            if (originalPoints.Count <= 3)
            {
                // 他のプラグインで頂点が4点以上必要なため
                Debug.LogWarning("Not enough points to form an area. At least 4 points are required.");
                return false;
            }

            // パラメータが有効か確認
            if (parameter.IsValid() == false)
            {
                Debug.LogError("Parameter is not valid. Ensure the wall material and restriction distance are set correctly.");
                return false;
            }

            // 制限距離を取得
            var wallMaterial = parameter.WallMaterial;
            var distance = parameter.Distance;

            // コナーのカーブを生成する
            var outline = GenerateCornerCurve(originalPoints, distance);

            // エリア生成
            if (GenerateArea(areaName, wallMaterial, outline, height, out var areaObj) == false)
            {
                Debug.LogError("Failed to generate area mesh. Ensure the road mesh and material are valid.");
                return false;
            }

            area = new Area(areaObj, originalPoints, height, target);
            return true;
        }

        private bool GenerateArea(string areaName, Material wallMaterial, List<Vector3> points, float height, out GameObject areaObj)
        {
            // エリアオブジェクトを生成
            areaObj = CreateAreaObj(areaName);

            // メッシュを生成　エリアオブジェクトにメッシュフィルターを追加
            if (GenerateMeshes(new List<List<Vector3>> { points }, wallMaterial, areaObj, height) == false)
            {
                Debug.LogError("Failed to generate meshes for the area. Ensure the mesh data is valid and the material is set correctly.");
                GameObject.Destroy(areaObj);
                return false;
            }

            return true;
        }

        private bool GenerateMeshes(List<List<Vector3>> list, Material wallMaterial, GameObject areaObj, float height)
        {
            return ReloadMeshes(list, wallMaterial, areaObj, height);
        }

        private GameObject CreateAreaObj(string name)
        {
            GameObject go = new GameObject(name);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = new UnityEngine.Mesh();  // 空のメッシュを新規作成して設定

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            go.transform.SetParent(AreaRoot.transform, true);

            return go;
        }

        /// <summary>
        /// 既に登録された区画データのメッシュオブジェクトを頂点データから再生成するメソッド
        /// </summary>
        public bool ReloadMeshes(List<List<Vector3>> listOfVertices, Material wallMaterial, GameObject areaObj, float height)
        {

            // 仮
            float limitHight = height; // 高さ制限のデフォルト値
            float wallMaxHight = limitHight * 1.5f;
            string name = areaObj.name;
            string id = System.Guid.NewGuid().ToString();

            if (listOfVertices.Count > 1)
            {
                // 穴あきメッシュの処理は未実装
                Debug.LogError("Hole meshes are not supported yet. Please provide a single closed loop of vertices.");
                return false;
            }

            var verts = listOfVertices[0];
            var maxEdge = Common.MeshUtil.GetMaxDistance(verts);

            // 隣接する頂点間の最大距離を計算
            var maxEdgeBetween0To1 = Common.MeshUtil.GetMaxAdjacentDistance(verts) + 1.0f;

            // 三角ポリゴンの最大面積の計算　頂点間の最大距離を辺の長さとする正三角形が最大面積のはず
            var maxArea = Common.MeshUtil.GetEquilateralTriangleArea(maxEdge) + 1.0f;

            Renderer renderer = areaObj.GetComponent<Renderer>();
            renderer.material = parameter.CeilingMaterial; // 天井のマテリアルを設定
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 影のキャストをオフにする

            MeshFilter meshFileter = areaObj.GetComponent<MeshFilter>();
            TessellatedMeshCreator tessellatedMeshCreator = new TessellatedMeshCreator();
            if (!tessellatedMeshCreator.CreateTessellatedMesh(listOfVertices, meshFileter, maxEdgeBetween0To1, maxArea))
            {
                Debug.LogWarning($"Failed to create tessellated mesh for {name}. Ensure the vertex data is valid and the mesh can be tessellated.");
                return false;
            }
            Mesh mesh = meshFileter.sharedMesh;

            // Meshを変形
            LandscapePlanMeshModifier landscapePlanMeshModifier = new LandscapePlanMeshModifier();
            if (!landscapePlanMeshModifier.TryModifyMeshToTargetHeight(mesh, limitHight, areaObj.transform.position, true))
            {
                Debug.LogError($"{name} is out of range of the loaded map");
                return false;
            }

            WallGenerator wallGenerator = new WallGenerator();
            // 区画のメッシュから下向きに壁を再生成
            GameObject[] walls = wallGenerator.GenerateWall(mesh, wallMaxHight, Vector3.down, wallMaterial);
            for (int j = 0; j < walls.Length; j++)
            {
                //GameObject wallObject = GameObject.Find($"AreaWall_{areaEditManager.GetAreaID()}_{j}");
                // 存在する壁オブジェクトを削除
                //if (wallObject != null) GameObject.Destroy(wallObject);

                walls[j].transform.SetParent(areaObj.transform);
                walls[j].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                walls[j].name = $"AreaWall_{id}_{j}";


            }

            return true;
        }

        bool IAdvertisingPlacementRestrictionsModule.UpdateAdObjects(bool isForce)
        {
            return UpdateAdObjects(isForce);
        }

        private bool UpdateAdObjects(bool isForce = false)
        {
            if (isEnable == false)
                return true;

            // 広告物オブジェクトの取得
            var adObjs = Common.AdvertisementObjectUtil.GetPlateauSandboxAdvertisements(advertisingPlacementRestrictionTypes.ToList());
            this.targetAdObjs = adObjs;

            foreach (var item in targetAdObjs)
            {
                UpdateAdObjecctByTransform(item.gameObject, isForce);
            }

            return true;
        }

        void IAdvertisingPlacementRestrictionsModule.DisplayArea(bool isDisplay)
        {
            this.isDisplay = isDisplay;
            SetDisplayStatus(isDisplay);
        }

        private void SetDisplayStatus(bool isDisplay)
        {
            var root = AreaRoot;
            root.SetActive(isDisplay);
        }

        void IAdvertisingPlacementRestrictionsModule.Reset()
        {
            Reset();

        }

        private void Reset()
        {
            targetAdObjs.Clear();
            foreach (var item in areaMap)
            {
                var area = item.Value;
                Area.Release(ref area);
            }
            areaMap.Clear();
            //isDisplay = true;             // 
            //parameter.Distance = Parameter.DefaultDistance;    // リセットしない方が使用感がよさそうなのでコメントアウト
        }

        void IAdvertisingPlacementRestrictionsModule.EnableUpdate(bool isEnable)
        {
            this.isEnable = isEnable;
        }

        //void IAdvertisingPlacementRestrictionsModule.Update()
        //{
        //    UpdateAdObj();
        //}

        /// <summary>
        /// 広告物周囲規制でのセーブロードシステムの仕様に合わせて補助するクラス
        /// SaveLoadSystemを扱う処理はここで実装する
        /// 機能側でプロジェクトIDとかを見ないように
        /// </summary>
        private class AdAreaSaveLoadHelper
        {
            // 
            ISaveLoadSystem<SampleData> saveLoadSystem;

            // データは一つだけ扱う想定なのでそれ用のイベントを用意
            public event Action<SampleData> LoadCallback;
            public event Action<SampleData> DeleteCallback;
            public event Action<SampleData> ProjectChangeCallback;

            public AdAreaSaveLoadHelper(ProjectSaveDataType type, SaveSystem saveSystem)
            {
                saveLoadSystem = new SaveLoadSystem<SampleData>(type, saveSystem) as ISaveLoadSystem<SampleData>;
                Debug.Assert(saveLoadSystem != null);
                if (saveLoadSystem == null)
                    throw new InvalidOperationException("SaveLoadSystem<SampleData> does not implement ISaveLoadSystem<SampleData>.");

                saveLoadSystem.LoadCallback += (v0, v1) =>
                {
                    LoadCallback?.Invoke(!v1.Any() ? null : v1.First());
                };
                saveLoadSystem.DeleteCallback += (v0, v1) =>
                {
                    DeleteCallback?.Invoke(!v1.Any() ? null : v1.First());
                };
                saveLoadSystem.ProjectChangeCallback += (v0, v1, v2) =>
                {
                    ProjectChangeCallback?.Invoke(
                        !v1.Any() ? null : v1.First());
                };
            }

            /// <summary>
            /// データ新規追加か既存データの更新を行う
            /// </summary>
            /// <param name="distance"></param>
            /// <returns></returns>
            public bool TryAddDataOrUpdate(float distance)
            {
                var dataKey = ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID;
                if (saveLoadSystem?.TryAddData(new SampleData(dataKey, Vector3.zero, distance)) == false)
                {
                    saveLoadSystem?.Update(new SampleData(dataKey, Vector3.zero, distance));
                }

                return true;
            }


        }

        public static class DebugBoundsDrawer
        {
            /// <summary>
            /// 指定したBoundsのワイヤーフレームをDebug.DrawLineで描画します。
            /// </summary>
            /// <param name="bounds">ワールド空間のBounds</param>
            /// <param name="color">線の色</param>
            /// <param name="duration">描画持続時間（秒）。0なら1フレームのみ描画</param>
            /// <param name="depthTest">
            /// true: 他オブジェクトに隠れると見えなくなる  
            /// false: 常に最前面に描画
            /// </param>
            public static void DrawBoundingBox(
                Bounds bounds,
                Color color,
                float duration = 0f,
                bool depthTest = true)
            {
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

                DrawBounds(pts, color, duration, depthTest);
            }

            public static void DrawBounds(Vector3[] pts, Color color, float duration, bool depthTest)
            {
                // 天面・底面を描画
                DrawQuad(pts[0], pts[1], pts[2], pts[3], color, duration, depthTest);
                DrawQuad(pts[4], pts[5], pts[6], pts[7], color, duration, depthTest);

                // 側面を描画
                for (int i = 0; i < 4; i++)
                {
                    Debug.DrawLine(pts[i], pts[i + 4], color, duration, depthTest);
                }
            }

            // 4頂点を順番に結んで四角形を描くヘルパー
            private static void DrawQuad(
                Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                Color color,
                float duration,
                bool depthTest)
            {
                Debug.DrawLine(a, b, color, duration, depthTest);
                Debug.DrawLine(b, c, color, duration, depthTest);
                Debug.DrawLine(c, d, color, duration, depthTest);
                Debug.DrawLine(d, a, color, duration, depthTest);
            }
        }
    }
}
