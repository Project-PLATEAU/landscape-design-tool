using Landscape2.Runtime.Common;
using Landscape2.Runtime.LandscapePlanLoader;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Common;
using ProceduralToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Landscape2.Runtime.DisplayInstallationRestrictionZones
{
    /// <summary>
    /// 道路側面の設置規制範囲表示機能を提供する起点となるクラスのインターフェイス
    /// </summary>
    public interface IDisplayInstallationRestrictionZonesModule
    {
        /// <summary>
        /// 機能の有効化・無効化を行う。
        /// 失敗した場合はfalseを返す。
        /// </summary>
        /// <param name="isActive"></param>
        /// <returns></returns>
        bool Activate(bool isActive);

        ///// <summary>
        ///// エリアの生成
        ///// 再呼び出し時には元々選択していたエリアは削除して再生する
        ///// </summary>
        ///// <param name="road"></param>
        ///// <returns></returns>
        //bool GenerateArea(GameObject road);

        /// <summary>
        /// エリアの生成
        /// 再呼び出し時には元々選択していたエリアは削除して再生する
        /// </summary>
        /// <param name="roads"></param>
        /// <returns></returns>
        bool GenerateAreas(List<GameObject> roads);

        /// <summary>
        /// 制限距離の設定
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        bool SetRestrictionDistance(float distance);
    }

    /// <summary>
    /// 道路側面の設置規制範囲表示機能を提供する起点となるクラス
    /// </summary>
    public class DisplayInstallationRestrictionZonesModule : IDisplayInstallationRestrictionZonesModule, ISubComponent
    {
        // 仮　インスタンス,nullで管理するステートパターンに置き換えるかも
        bool isActive = false;

        ///// <summary>
        ///// エリアを表すクラス
        ///// </summary>
        //class Area
        //{
        //    // エリアを所持するゲームオブジェクト
        //    GameObject areaObj;

        //    // エリアの頂点群　加工前
        //    List<Vector3> originalPoints;

        //    public GameObject AreaObj => areaObj;
        //    public List<Vector3> Points => originalPoints;

        //    public Area(GameObject obj, List<Vector3> originalPoints)
        //    {
        //        this.areaObj = obj;
        //        this.originalPoints = originalPoints;
        //    }

        //    public static void Release(ref Area area)
        //    {
        //       if (area.areaObj != null)
        //        {
        //            GameObject.Destroy(area.areaObj);
        //        }

        //        area = null;
        //    }
        //    public static void Release(ref List<Area> areas)
        //    {
        //        var nArea = areas.Count;
        //        for (var i = 0; i < nArea; i++)
        //        {
        //            var area = areas[i];
        //            Area.Release(ref area);
        //            areas[i] = area;
        //        }
        //     }

        //}
        //Area area;
        List<Area> areaList = new List<Area>();

        /// <summary>
        /// パラメータを表すクラス
        /// </summary>
        class Parameter
        {
            public bool IsValid()
            {
                return wallMaterial != null && RestrictionDistance >= 0.0f; // マテリアルがnullでないことと、制限距離が負でないことを確認
            }

            float restrictionDistance = 2.0f; // 設置規制距離のデフォルト値
            public float RestrictionDistance
            {
                get => restrictionDistance;
                set
                {
                    restrictionDistance = Mathf.Max(0.0f, value); // 負の値は許容しない
                }
            }

            Material wallMaterial = null; // 壁のマテリアル
            public Material WallMaterial
            {
                get => wallMaterial;
                set => wallMaterial = value ?? throw new System.ArgumentNullException(nameof(value), "Wall material cannot be null.");
            }

            Material cellingMaterial = null;
            public Material CeilingMaterial
            {
                get => cellingMaterial;
                set => cellingMaterial = value ?? throw new System.ArgumentNullException(nameof(value), "Celling material cannot be null.");
            }
        }
        Parameter parameter = new Parameter();

        // エリアの検証を行うクラス
        AreaChecker areaChecker = new AreaChecker();

        public DisplayInstallationRestrictionZonesModule()
        {
            // デバッグ用の描画クラスを初期化
            MeshUtil.DebugDrawer.IsDebugEnabled = false;
            MeshUtil.DebugDrawer.IsVretsEnabled = true;
            MeshUtil.DebugDrawer.IsEdgesEnabled = true;

        }

        void ISubComponent.OnDisable()
        {
            Debug.Log("OnDisable called in DisplayInstallationRestrictionZonesModule");
        }

        void ISubComponent.OnEnable()
        {
            Debug.Log("OnEnable called in DisplayInstallationRestrictionZonesModule");
        }

        void ISubComponent.Start()
        {
            var wallMaterial = Resources.Load<Material>("Materials/RestrictionZoneWallMaterial");
            if (wallMaterial == null)
            {
                Debug.LogError("Wall material not found. Ensure the material is placed in Resources/Materials/RestrictionZoneWallMaterial.");
                return;
            }
            parameter.WallMaterial = wallMaterial;

            var ceilingMaterial = Resources.Load<Material>("Materials/RestrictionZoneCeilingMaterial");
            if (ceilingMaterial == null)
            {
                Debug.LogError("Ceiling material not found. Ensure the material is placed in Resources/Materials/RestrictionZoneCeilingMaterial.");
                return;
            }
            parameter.CeilingMaterial = ceilingMaterial;
        }

        void ISubComponent.Update(float deltaTime)
        {

        }

        void ISubComponent.LateUpdate(float deltaTime)
        {

        }

        bool IDisplayInstallationRestrictionZonesModule.Activate(bool isActive)
        {
            if (this.isActive == isActive)
                return false;

            this.isActive = isActive;

            if (isActive)
            {

            }
            else
            {
                ResetCurrentAreas();
            }

            return true;
        }

        private void ResetCurrentAreas()
        {
            // 既存のエリアを削除
            if (areaList.IsEmpty() == false)
            {
                Area.Release(ref areaList);
            }

            // エリアリストを初期化
            areaList.Clear();

        }

        bool IDisplayInstallationRestrictionZonesModule.GenerateAreas(List<GameObject> roads)
        {
            var roadOutlines = new List<(GameObject road, List<Vector3> points)>(roads.Count); // 各道路のアウトラインを保持する辞書
            // 道路のメッシュを取得し、アウトラインを生成する
            foreach (var road in roads)
            {
                Mesh roadMesh = GetRodaMesh(road);
                if (roadMesh == null)
                {
                    Debug.LogError("Road mesh is null. Ensure the road GameObject has a valid MeshCollider or MeshFilter.");
                    continue;
                    //return false;
                }

                // 道路のメッシュとゲームオブジェクトをタプルにまとめる　オブジェとメッシュそれぞれに引数を用意すると同一オブジェを示さないように見える可能性があるため
                var roadMeshTuple = (road, roadMesh);

                // エリアの頂点を収集する 頂点群はループを想定している
                List<Vector3> points;
                if (CollectAreaPoints(roadMeshTuple, out points) == false)
                {
                    Debug.LogError("Failed to collect area points from the road mesh. Ensure the road has a valid MeshCollider.");
                    continue;
                    //return false;
                }

                roadOutlines.Add((road, points));
            }

            // 他の道路と共有している頂点に目印をつける　目印は道路側面を判定するために利用する
            var outlines = new TupleItem2ReadOnlyList<GameObject>(roadOutlines);
            var sharedPoints = Common.MeshUtil.GetSharedVerticesApprox(outlines);
            MeshUtil.DebugDrawer.DrawVerts(sharedPoints, Color.red, size: 100.0f);

            // 共有している頂点で囲まれた頂点は削除する　アウトライン結合時には必要ないため
            //...
            // 共有している頂点でアウトラインを結合する
            //...

            // 

            // エリア生成
            var newAreas = new List<Area>(roadOutlines.Count);
            foreach (var outline in roadOutlines)
            {
                var road = outline.road;
                var points = outline.points;
                // エリアを生成
                if (GenerateArea(road.transform.name, points, out var newArea) == false)
                {
                    Debug.LogWarning($"Failed to generate area for road: {road.name}. Ensure the road mesh and material are valid.");
                    Area.Release(ref newArea);
                    continue;
                    //return false;
                }

                // エリアを追加
                newAreas.Add(newArea);
            }

            // エリア群の生成に成功したので　既存のエリア群を削除
            ResetCurrentAreas();

            if (newAreas.IsEmpty() == false)
            {
                // 生成したエリアをリストに追加
                areaList.AddRange(newAreas);
            }

            // エリア内の広告物をチェック
            CheckAdObjects();
            return true;
        }

        private void CheckAdObjects()
        {
            return; // 広告物の色変更でバグが複数あるためとりあえず無効化
            // エリアチェッカを更新
            areaChecker.SetAreas(areaList);
            areaChecker.Update();
            var containsAdInArea = areaChecker.Check();
            MeshUtil.DebugDrawer.DrawVerts(containsAdInArea.Select(ad => ad.transform.position).ToList(), Color.green, size: 100.0f);
        }

        bool IDisplayInstallationRestrictionZonesModule.SetRestrictionDistance(float distance)
        {
            if (parameter.RestrictionDistance == distance)
                return true;

            // パラメータの設定
            if (distance < 0.0f)
            {
                Debug.LogError("Restriction distance cannot be negative.");
                return false;
            }

            parameter.RestrictionDistance = distance;

            // 既存エリアを再生成
            if (areaList.IsEmpty() == false)
            {
                for (var i = 0; i < areaList.Count; i++)
                {
                    var area = areaList[i];
                    var points = area.Outline;

                    if (GenerateArea(area.AreaObj.name, points, out var newArea) == false)
                    {
                        Debug.LogError("Failed to generate area mesh. Ensure the road mesh and material are valid.");
                        return false;
                    }

                    // 新エリアを設定する
                    Area.Release(ref area);
                    areaList[i] = newArea;

                    //GenerateAreaAndUpdate(ref area);
                }
            }

            // エリア内の広告物をチェック
            CheckAdObjects();

            return true;
        }

        /// <summary>
        /// 道路のメッシュを取得する
        /// </summary>
        /// <param name="road"></param>
        /// <returns></returns>
        private static Mesh GetRodaMesh(GameObject road)
        {
            //var roadMesh = road.GetComponent<MeshFilter>()?.sharedMesh;   // Play時に他の道路とメッシュが結合されて想定するメッシュが取得できないため利用できない
            return road.GetComponent<MeshCollider>()?.sharedMesh;
        }

        /// <summary>
        /// エリアの頂点を収集する 頂点群はループを想定している
        /// </summary>
        /// <param name="road"></param>
        /// <param name="roadMesh"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        private static bool CollectAreaPoints((GameObject obj, Mesh mesh) road, out List<Vector3> points)
        {
            // アウトラインとなる頂点群を取得　（穴あきメッシュなど一部のメッシュには対応していない）
            points = Common.MeshUtil.OutlineExtractor.GetOutlineVertices(road.mesh);
            if ((points?.Count ?? 0) == 0)
            {
                Debug.LogWarning("No outline vertices found in the road mesh. Ensure the mesh is a closed loop without holes.");
                return false;
            }

            // ワールド座標に変換
            for (var i = 0; i < points.Count; i++)
            {
                points[i] = road.obj.transform.TransformPoint(points[i]);
            }

            return true;
        }

        /// <summary>
        /// エリアの生成を行う
        /// </summary>
        /// <param name="road"></param>
        /// <param name="wallMaterial"></param>
        /// <param name="originalPoints"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        private bool GenerateArea(string areaName, List<Vector3> originalPoints, out Area area)
        {
            // とりあえず無効値を設定
            area = null;

            // 頂点数0,1,2は生成出来ないのでスキップ
            if (originalPoints.Count < 3)
            {
                Debug.LogWarning("Not enough points to form an area. At least 3 points are required.");
                return false;
            }

            // パラメータが有効か確認
            if (parameter.IsValid() == false)
            {
                Debug.LogError("Parameter is not valid. Ensure the wall material and restriction distance are set correctly.");
                return false;
            }

            // アウトラインを最適化する　直線上の中点を削除、付近の頂点マージなど
            originalPoints = OptimizeOutline(originalPoints, mergeDistance:0.1f);

            // メッシュ生成可能な頂点に調整する
            AddjustmentOutlinePoints(originalPoints);

            // 制限距離を取得
            var restrictionDistance = parameter.RestrictionDistance;
            var wallMaterial = parameter.WallMaterial;


            // エリア生成
            if (GenerateArea(areaName, wallMaterial, originalPoints, out var areaObj) == false)
            {
                Debug.LogError("Failed to generate area mesh with zero restriction distance. Ensure the road mesh and material are valid.");
                return false;
            }

            // エリアオブジェクトを追加するルートのゲームオブジェクトを取得
            var areaObjRoot = GameObject.Find("RestrictionZones");
            if (areaObjRoot == null)
            {
                areaObjRoot = new GameObject("RestrictionZones");
            }

            // 親を設定する areaObjのワールド座標を変えずに
            areaObj.transform.SetParent(areaObjRoot.transform, true); // ワールド座標を維持するためにtrueを設定

            // 制限距離が0以下のため加工無しでエリアを生成する
            if (restrictionDistance <= 0.0f)
            {
                area = new Area(areaObj, originalPoints, originalPoints);
                return true;
            }

            // 変形用頂点
            var translatedNewPoints = new List<Vector3>(originalPoints);

            // 高さ情報を無くす
            for (var i = 0; i < translatedNewPoints.Count; i++)
            {
                translatedNewPoints[i] = translatedNewPoints[i].ToVector3XZ();
            }

            // 移動させた頂点を生成する
            if (GenerateNewPoints(originalPoints, restrictionDistance, out translatedNewPoints) == false)
            {
                Debug.LogWarning("Failed to generate new points for the area. Ensure the original points are valid and the restriction distance is set correctly.");
                return false;
            }

            // 移動後の頂点とエリアを構成する頂点を紐づける
            {
                // 前提として元の頂点と移動後の頂点は同じインデックスで紐づいているとする

                // todo エリアを構成を理解してラップしているクラスがほしい。専用の取得処理がいろんなところに散らばりそう。クラスに加工関数を用意してもいい。
                // エリアの天井、壁面のメッシュを取得
                var areaMeshFileters = areaObj.GetComponentsInChildren<MeshFilter>();
                foreach (var item in areaMeshFileters)
                {
                    ApplyTranslatePosXZForAreaMesh(originalPoints, translatedNewPoints, item);
                }
            }

            // 元のオブジェクトのエリア情報を保持    頂点群は加工前の頂点なので注意
            area = new Area(areaObj, originalPoints, translatedNewPoints);
            return true;

            static bool CalculateNewPoints(List<MeshUtil.EdgeWithAttribute<bool>> edges, List<Vector3> edgeShiftDir, float restrictionDistance, out List<Vector3> intersectionPoints)
            {
                // エッジの法線の方向に移動
                for (var i = 0; i < edges.Count; i++)
                {
                    edges[i].Translate(edgeShiftDir[i] * restrictionDistance);
                }

                // 法線をデバッグ描画                
                MeshUtil.DebugDrawer.DrawEdges(edges, edgeShiftDir, duration: 50.0f, normalLength: 50.0f, edgeColor: Color.cyan, normalColor: Color.red);

                // 接続していた接線同士で線が交差する位置を計算　それが新しい頂点
                intersectionPoints = Common.MeshUtil.EdgeUtility.GetAdjacentEdgeIntersections(edges);

                // 新しい頂点をデバッグ描画
                MeshUtil.DebugDrawer.DrawVerts(intersectionPoints, Color.yellow, 50.0f, 30.0f);
                return true;
            }

            bool GenerateArea(string areaName, Material wallMaterial, List<Vector3> points, out GameObject areaObj)
            {
                // エリアオブジェクトを生成
                areaObj = CreateAreaObj(areaName);
                // メッシュを生成　エリアオブジェクトにメッシュフィルターを追加
                if (GenerateMeshes(new List<List<Vector3>> { points }, wallMaterial, areaObj) == false)
                {
                    Debug.LogWarning("Failed to generate meshes for the area. Ensure the mesh data is valid and the material is set correctly.");
                    GameObject.Destroy(areaObj);
                    return false;
                }

                return true;
            }

            static bool GenerateNewPoints(List<Vector3> originalPoints, /*List<Vector3> sharedPoints, */float deltaRestrictionDistance, out List<Vector3> translatedNewPoints)
            {
                translatedNewPoints = null;

                // エッジを生成
                var edges = Common.MeshUtil.EdgeUtility.CreateEdges((List<Vector3>)originalPoints, defaultAttribute: false, true);


                // エッジの移動方向を計算
                var edgeShiftDir = Common.MeshUtil.EdgeUtility.CalculateEdgeNormalsXZ((List<Vector3>)originalPoints, true);
                if (edges.Count != edgeShiftDir.Count)
                {
                    // 実装ミス
                    Debug.LogError("Edge count does not match normal count. This is unexpected behavior.");
                    return false;
                }

                //// 他の道路と共有しているエッジは移動しないようにする
                //var comparer = new MeshUtil.Vector3EpsilonComparer(1e-4f);
                //for (var i = 0; i < edges.Count; i++)
                //{
                //    // 始点終点を他の道路と共有している場合、移動方向を無効化する
                //    var edge = edges[i];
                //    if (sharedPoints.Contains(edge.Start, comparer))
                //    {
                //        if (sharedPoints.Contains(edge.End, comparer))
                //        {
                //            edgeShiftDir[i] = Vector3.zero;
                //        }
                //    }
                //}


                // 移動方向ををデバッグ描画
                MeshUtil.DebugDrawer.DrawEdges(edges, edgeShiftDir, duration: 50.0f, normalLength: 50.0f, edgeColor: Color.green, normalColor: Color.blue);
                if (CalculateNewPoints(edges, edgeShiftDir, deltaRestrictionDistance, out translatedNewPoints) == false)
                {
                    Debug.LogError("Failed to calculate new points for the area. Ensure the edge intersection calculation is correct.");
                    return false;
                }

                // 新しい頂点の数が元の頂点数と一致するか確認
                if (originalPoints.Count != translatedNewPoints.Count)
                {
                    // 頂点数が一致しない場合は、何らかの問題がある可能性がある
                    Debug.LogError("The number of intersection points does not match the original outline vertices. This may indicate an issue with the edge intersection calculation.");
                    return false;
                }

                return true;
            }

            static void AddjustmentOutlinePoints(List<Vector3> originalPoints)
            {
                // 最低でも頂点数が4つ必要なので頂点を追加する 
                if (originalPoints.Count == 3)
                {
                    // 最も距離のある隣接する頂点のペアを探す
                    var maxSqrDistance = 0.0f;
                    int index = -1;
                    for (int i = 0; i < originalPoints.Count; i++)
                    {
                        var v = originalPoints[i] - originalPoints[(i + 1) % originalPoints.Count];
                        var m = v.sqrMagnitude;
                        if (m > maxSqrDistance)
                        {
                            maxSqrDistance = m;
                            index = i;
                        }
                    }

                    var nextIdx = (index + 1) % originalPoints.Count;
                    var p = (originalPoints[index] + originalPoints[nextIdx]) / 2.0f;
                    originalPoints.Insert(nextIdx, p);

                    //Debug.Log(areaName + " has only 3 points. Adding a point to make it valid for area generation.");
                }
            }
        }

        /// <summary>
        /// エリア生成処理で生成されたメッシュ専用
        /// 元の頂点群を元に生成したメッシュに
        /// 元の頂点群を変形した頂点群の結果を適用する
        /// </summary>
        /// <param name="originalPoints"></param>
        /// <param name="translatedNewPoints"></param>
        /// <param name="meshFileter"></param>
        private static void ApplyTranslatePosXZForAreaMesh(List<Vector3> originalPoints, List<Vector3> translatedNewPoints, MeshFilter meshFileter)
        {
            var mesh = meshFileter.sharedMesh;
            var triangles = mesh.triangles;
            var verts = mesh.vertices;

            // 元の頂点とエリアを構成する頂点を紐づける
            ApplyTranslatePosXZ(originalPoints, translatedNewPoints, triangles, verts);

            mesh.vertices = verts;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(false);
        }

        /// <summary>
        /// 元の頂点群を元に生成したメッシュを構成する頂点群に
        /// 元の頂点群を変形した頂点群の結果を適用する
        /// 
        /// 例　元の頂点群を元にエリアの天板の頂点群を作成
        /// エリアの拡大処理は元の頂点群を複製したものに行って
        /// その結果をエリアの天板の頂点群に適用する
        /// </summary>
        /// <param name="originalPoints"></param>
        /// <param name="translatedNewPoints"></param>
        /// <param name="triangles"></param>
        /// <param name="verts"></param>
        private static void ApplyTranslatePosXZ(List<Vector3> originalPoints, List<Vector3> translatedNewPoints, int[] triangles, Vector3[] verts)
        {
            var link = new Dictionary<int, int>(triangles.Length);
            for (int triIdx = 0; triIdx < triangles.Length; triIdx++)
            {
                var vertIdx = triangles[triIdx];
                if (link.ContainsKey(vertIdx))
                {
                    continue;
                }

                var vert = verts[vertIdx];
                for (var originalIdx = 0; originalIdx < originalPoints.Count; originalIdx++)
                {
                    if (ApproximatelyEqualXZ(vert, originalPoints[originalIdx]) == false)
                    {
                        continue;
                    }

                    // triIdxxからoriginalIdを検索できるように
                    link.Add(vertIdx, originalIdx);
                    break;
                }
            }

            bool ApproximatelyEqualXZ(Vector3 a, Vector3 b, float epsilon = 1e-3f)
            {
                return Mathf.Abs(a.x - b.x) < epsilon &&
                       Mathf.Abs(a.z - b.z) < epsilon;
            }

            // trianglesで同じ頂点を参照することもあるので要素数が同じとは限らないはず
            //// 適切に紐づけが出来なかった
            //if (link.Count != triangles.Length)
            //{
            //    Debug.LogError("");
            //    return false;
            //}

            // 移動後の頂点の値をエリアの頂点に適用する
            foreach (var item in link)
            {
                verts[item.Key].x = translatedNewPoints[item.Value].x;
                verts[item.Key].z = translatedNewPoints[item.Value].z;
            }
        }

        private static List<Vector3> OptimizeOutline(List<Vector3> original, float mergeDistance)
        {
            var outline = new List<Vector3>(original);

            // ほぼ直線に並んだ頂点は結合する
            while (true)
            {
                var newPoints = MeshUtil.MergeCollinear(outline, angleThresholdDeg: 5f);
                var isMerged = newPoints.Count != outline.Count;
                outline = newPoints;
                if (isMerged == false)
                {
                    break;
                }
            }

            var mergedPoints = outline;

            while (true)
            {
                var newMergedPoints = MergeCloseVertices(mergedPoints, mergeDistance, false);
                if (newMergedPoints.Count == mergedPoints.Count) // 結合される頂点が無くなった場合
                    break;
                mergedPoints = newMergedPoints;
            }

            if (mergedPoints.Count < 3)
            {
                // 3点未満の頂点ではエリアを生成しない
                Debug.LogWarning("Not enough merged points to form an area. At least 3 points are required after merging.");
                return null;
            }

            return outline;
        }

        /// <summary>
        /// エリア用の空の GameObject を生成するメソッド
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private GameObject CreateAreaObj(string name)
        {
            // 1. 空の GameObject を生成
            GameObject go = new GameObject(name);

            // 2. MeshFilter コンポーネントを追加し、空の Mesh をセット
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = new UnityEngine.Mesh();  // 空のメッシュを新規作成して設定

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();

            return go;
        }

        private bool GenerateMeshes(List<List<Vector3>> listOfVertices, Material wallMaterial, GameObject areaObj)
        {
            return ReloadMeshes(listOfVertices, wallMaterial, areaObj);
        }

        /// <summary>
        /// 既に登録された区画データのメッシュオブジェクトを頂点データから再生成するメソッド
        /// </summary>
        public bool ReloadMeshes(List<List<Vector3>> listOfVertices, Material wallMaterial, GameObject areaObj)
        {
            // 仮
            const float limitHight = 10.0f; // 高さ制限のデフォルト値
            const float wallMaxHight = 100.0f;
            string name = areaObj.name;
            string id = System.Guid.NewGuid().ToString(); /*GUID.Generate().ToString()*/;

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
                Debug.LogError($"Failed to create tessellated mesh for {name}. Ensure the vertex data is valid and the mesh can be tessellated.");
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

            //BuildMesh(verts, ceiliing, parameter.CeilingMaterial);
            //GenerateCeilingMesh(mesh, wallGenerator, walls);

            return true;
        }

            //static void GenerateCeilingMesh(Mesh mesh, WallGenerator wallGenerator, GameObject[] walls)
            //{
            //    //上面Meshのマテリアルを設定
            //    areaProperty.CeilingMaterial.color = areaProperty.Color;
            //    gisObjMeshRenderer.material = areaProperty.CeilingMaterial;
            //    gisObjMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            //    //区画のメッシュから下向きに壁を生成
            //    GameObject[] walls = wallGenerator.GenerateWall(mesh, areaProperty.WallMaxHeight, Vector3.down, areaProperty.WallMaterial);
            //    for (int j = 0; j < walls.Length; j++)
            //    {
            //        walls[j].transform.SetParent(gisObject.transform);
            //        walls[j].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            //        walls[j].name = $"AreaWall_{areaProperty.ID}_{j}";
            //    }



        /// <summary>
        /// 内部に (TKey, List<Vector3>) のコレクションを持ち、
        /// IReadOnlyList&lt;List&lt;Vector3&gt;&gt; として Item2 部分だけを読み取りで露出するラッパー。
        /// </summary>
        private class TupleItem2ReadOnlyList<TKey> : IReadOnlyList<List<Vector3>>
        {
            private readonly IList<(TKey Key, List<Vector3> Points)> source;
            
            /// <summary>
            /// ソースのタプルリストを注入。null 非許容。
            /// </summary>
            public TupleItem2ReadOnlyList(IList<(TKey, List<Vector3>)> source)
                => this.source = source ?? throw new ArgumentNullException(nameof(source));

            // IReadOnlyList<List<Vector3>> のインデクサを明示的実装
            List<Vector3> IReadOnlyList<List<Vector3>>.this[int index]
                => source[index].Points;

            // IReadOnlyCollection<List<Vector3>>.Count
            int IReadOnlyCollection<List<Vector3>>.Count
                => source.Count;

            // IEnumerable<List<Vector3>>.GetEnumerator()
            IEnumerator<List<Vector3>> IEnumerable<List<Vector3>>.GetEnumerator()
                => source.Select(tuple => tuple.Points).GetEnumerator();

            // IEnumerable.GetEnumerator()
            IEnumerator IEnumerable.GetEnumerator()
                => ((IEnumerable<List<Vector3>>)this).GetEnumerator();
        }

        public static List<Vector3> MergeCloseVertices(
            List<Vector3> vertices,
            float threshold,
            bool ignoreHeight = false
)
        {
            // 入力が少なければそのまま返す
            if (vertices == null || vertices.Count < 2)
                return vertices != null ? new List<Vector3>(vertices) : new List<Vector3>();

            // 作業用コピー
            var verts = new List<Vector3>(vertices);

            int i = 0;
            while (i < verts.Count)
            {
                // 終端は先頭と隣接
                int nextIndex = (i + 1) % verts.Count;

                // 頂点が1つになったら終了
                if (verts.Count <= 1)
                    break;

                Vector3 a = verts[i];
                Vector3 b = verts[nextIndex];

                // 距離判定
                bool shouldMerge;
                if (ignoreHeight)
                {
                    Vector2 pa = new Vector2(a.x, a.z);
                    Vector2 pb = new Vector2(b.x, b.z);
                    shouldMerge = Vector2.Distance(pa, pb) <= threshold;
                }
                else
                {
                    shouldMerge = Vector3.Distance(a, b) <= threshold;
                }

                if (shouldMerge)
                {
                    // 平均点でマージ
                    Vector3 merged = (a + b) * 0.5f;
                    verts[i] = merged;
                    verts.RemoveAt(nextIndex);

                    // nextIndex が i より前ならインデックス補正
                    if (nextIndex < i)
                        i = Mathf.Max(i - 1, 0);
                }
                else
                {
                    i++;
                }
            }

            return verts;
        }
    }
}
