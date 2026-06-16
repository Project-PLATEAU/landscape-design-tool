using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Landscape2.Runtime.AdAreaCalcurateSub
{
    /// <summary>
    /// GetTriangleArea()で使用するためのデータ
    /// </summary>
    public class TriangleAreaCalcInfo
    {
        public TriangleAreaCalcInfo(Mesh mesh)
        {
            verts = (Vector3[])mesh.vertices.Clone();
            Bounds = mesh.bounds;
        }

        private readonly Vector3[] verts;
        // Vertsの値は更新しないでください。配列を引数とする箇所があるのでこうしてます。
        public Vector3[] Verts { get => verts; }    
        public Bounds Bounds { get; private set; }
    }

    public class Wall
    {
        /// <summary>
        ///  この壁面が属する建物
        /// </summary>
        private string buildingGmlID;
        public string BuildingGmlID { get => buildingGmlID; }

        private readonly TriangleAreaCalcInfo triangleAreaCalcInfo;
        // Verticesの値は更新しないでください。配列を引数とする箇所があるのでこうしてます。
        public Vector3[] Vertices { get => triangleAreaCalcInfo.Verts; }
        public Bounds Bounds { get => triangleAreaCalcInfo.Bounds; }

        /// <summary>
        /// 壁を形成する三角形のインデックスを保持するリスト
        /// </summary>
        List<int[]> wallTris = new();
        public List<int[]> WallTris { get => wallTris; }

        // 面積
        public float AreaSize { get; private set; } = 0f;


        /// <summary>
        /// 自身から指定された壁面に含まれるポリゴンを取り除く
        /// </summary>
        /// <param name="wall"></param>
        public void Remove(Wall wall)
        {
            foreach (var tri in wall.wallTris)
            {
                Remove(tri, wall.buildingGmlID);
            }
        }

        /// <summary>
        /// 自身から指定されたポリゴンを取り除く
        /// </summary>
        /// <param name="tri"></param>
        /// <param name="buildingGmlID"></param>
        /// <param name="hint"></param>
        public void Remove(int[] tri, string buildingGmlID, int hint = -1)
        {
            // 違う建物
            if (this.buildingGmlID != buildingGmlID)
                return;

            if (hint < 0)
            {
                // hint無し

                var r = wallTris.FindIndex((v) =>
                {
                    return Compare(v, tri);
                });
                if (r >= 0)
                {
                    wallTris.RemoveAt(r);
                    //hints.RemoveAt(r); // 実際には一度作成したらそれを保持して利用するので削除が必要
                }
            }
            else
            {
                // hintあり

                // hintリストを作成
                var hints = CreateTriangleIndexHashList(wallTris);  // 実際には一度作成したらそれを保持して利用する
                var r = hints.FindIndex((v) =>
                {
                    return hint == v;
                });

                if (r >= 0)
                {
                    wallTris.RemoveAt(r);
                    //hints.RemoveAt(r); // 実際には一度作成したらそれを保持して利用するので削除が必要
                }
            }


        }
        public static int TriangleIndexHash(int[] tri)
        {
            // tri.Length == 3 前提
            int a = tri[0], b = tri[1], c = tri[2];
            // 並び順を無視するためソート 昇順ソート
            if (a > b) (a, b) = (b, a);
            if (b > c) (b, c) = (c, b);
            if (a > b) (a, b) = (b, a);

            // 65536未満のインデックスなら衝突しにくい
            return (a * 65536 * 65536) + (b * 65536) + c;
        }

        private static List<int> CreateTriangleIndexHashList(List<int[]> tris)
        {
            var hashList = new List<int>(tris.Count);
            foreach (var tri in tris)
            {
                hashList.Add(TriangleIndexHash(tri));
            }
            return hashList;
        }

        /// <summary>
        /// 三角形の比較
        /// memo 三角形のソート、三角形リストのソートを行う場合　もっと効率的な方法はありそう
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool Compare(int[] a, int[] b)
        {
            var cnt = 0;
            foreach (var item1 in a)
            {
                foreach (var item2 in b)
                {
                    if (item1 == item2)
                    {
                        cnt++;
                        break;
                    }
                }
            }
            return cnt == 3;
        }

        /// <summary>
        /// データの内容で比較
        /// 重い
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Compare(Wall a, Wall b)
        {
            if (a == null || b == null) return false;
            if (a.wallTris.Count != b.wallTris.Count) return false;
            if (a.buildingGmlID != b.buildingGmlID) return false;

            // ハッシュ値で高速比較しつつ、衝突時は実配列比較
            var aHashes = CreateTriangleIndexHashList(a.wallTris);
            var bHashes = CreateTriangleIndexHashList(b.wallTris);

            // ハッシュ値ごとの三角形リストを作成
            var aHashToTris = new Dictionary<int, List<int[]>>();
            for (int i = 0; i < aHashes.Count; i++)
            {
                if (!aHashToTris.TryGetValue(aHashes[i], out var list))
                {
                    list = new List<int[]>();
                    aHashToTris[aHashes[i]] = list;
                }
                list.Add(a.wallTris[i]);
            }

            var bHashToTris = new Dictionary<int, List<int[]>>();
            for (int i = 0; i < bHashes.Count; i++)
            {
                if (!bHashToTris.TryGetValue(bHashes[i], out var list))
                {
                    list = new List<int[]>();
                    bHashToTris[bHashes[i]] = list;
                }
                list.Add(b.wallTris[i]);
            }

            // aの全三角形がbに存在するかチェック
            foreach (var kv in aHashToTris)
            {
                if (!bHashToTris.TryGetValue(kv.Key, out var bList))
                    return false;

                foreach (var aTri in kv.Value)
                {
                    bool found = false;
                    foreach (var bTri in bList)
                    {
                        if (Compare(aTri, bTri))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
            }

            // bの全三角形がaに存在するかチェック（念のため対称性も保証）
            foreach (var kv in bHashToTris)
            {
                if (!aHashToTris.TryGetValue(kv.Key, out var aList))
                    return false;

                foreach (var bTri in kv.Value)
                {
                    bool found = false;
                    foreach (var aTri in aList)
                    {
                        if (Compare(bTri, aTri))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wallTris"></param>
        /// <param name="building"></param>
        /// <returns></returns>
        public static Wall Create(List<int[]> wallTris, Mesh building, string gmlID)
        {
            var wallData = new Wall(wallTris, building, gmlID);
            return wallData;
        }

        /// <summary>
        /// 三角形が含まれているか
        /// </summary>
        /// <param name="tri"></param>
        /// <returns></returns>
        public bool IsContains(Wall tri)
        {
            if (buildingGmlID != tri.buildingGmlID)
                return false;

            foreach (var wallTri in wallTris)
            {
                if (IsContains(wallTri, tri.buildingGmlID))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 三角形が含まれているか
        /// </summary>
        /// <param name="tri"></param>
        /// <returns></returns>
        public bool IsContains(int[] tri, string buildingGmlID)
        {
            if (this.buildingGmlID != buildingGmlID)
                return false;

            foreach (var item in wallTris)
            {
                if (Wall.Compare(item, tri))
                    return true;
            }

            return false;
        }

        
        private Wall(List<int[]> wallTris, Mesh building, string gmlId)
        {
            this.wallTris = wallTris;
            this.triangleAreaCalcInfo = new TriangleAreaCalcInfo(building);
            this.buildingGmlID = gmlId;
        }

        /// <summary>
        /// 面積計算
        /// </summary>
        /// <param name="meshFilter"></param>
        /// <returns></returns>
        public void CalculateArea()
        {
            AreaSize = CalculateArea(wallTris, triangleAreaCalcInfo);
        }

        /// <summary>
        /// todo:delete
        /// 面積計算
        /// </summary>
        /// <param name="meshFilter"></param>
        /// <returns></returns>
        public static float CalculateArea(List<int[]> currentSelectWallTris, TriangleAreaCalcInfo triangleAreaCalcInfo)
        {
            if (0 < currentSelectWallTris.Count)
            {
                System.Text.StringBuilder sb = new();
                sb.AppendLine($"area: {currentSelectWallTris.Count}");
                var triAreas = 0f;
                foreach (var t in currentSelectWallTris)
                {
                    var mag = GetTriangleArea(triangleAreaCalcInfo, t);
                    sb.AppendLine($"mag: {mag}");
                    triAreas += mag;
                }
                sb.AppendLine($"total: {triAreas}");
                return triAreas;
            }

            return 0f;
        }

        /// <summary>
        /// triで指定された3角形の面積を出す
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="triIndex"></param>
        /// <returns></returns>
        private static float GetTriangleArea(TriangleAreaCalcInfo calcInfo, int[] triIndex)
        {
            var v0 = calcInfo.Verts[triIndex[0]];
            var v1 = calcInfo.Verts[triIndex[1]];
            var v2 = calcInfo.Verts[triIndex[2]];
            var v01 = v1 - v0;
            var v02 = v2 - v0;
            return 0.5f * Vector3.Cross(v01, v02).magnitude;
        }

        /// <summary>
        /// Wallデータを破棄する
        /// </summary>
        /// <param name="wallData"></param>
        public static void Release(Wall wallData)
        {
            wallData.wallTris.Clear();
            wallData.AreaSize = 0f;
            wallData.buildingGmlID = null;
        }

    }


    /// <summary>
    /// 壁面群を管理する溜めのクラス
    /// </summary>
    public class AdAreaCalcurateWallCollection
    {

        // 壁面群
        private List<Wall> wallDataList = new List<Wall>();

        // 壁面群の面積の合計
        public float AllWllArea { get; private set; } = 0f;


        public ReadOnlyArray<Wall> WallDataList => new ReadOnlyArray<Wall>(wallDataList.ToArray());

        /// <summary>
        /// 壁面の追加
        /// </summary>
        /// <param name="wallTris"></param>
        /// <param name="building"></param>
        /// <returns></returns>
        public Wall Add(List<int[]> wallTris, Mesh building, string buildingGmlID)
        {
            var wallData = Wall.Create(wallTris, building, buildingGmlID);
            return Add(wallData);
        }

        /// <summary>
        /// 壁面の追加
        /// </summary>
        /// <param name="wallData"></param>
        /// <returns></returns>
        private Wall Add(Wall wallData)
        {
            RemoveDuplicateTris(wallData);

            if (wallData.WallTris.Count <= 0)
            {
                // 三角形が無い場合は追加しない
                return wallData;
            }
            wallDataList.Add(wallData);
            return wallData;
        }

        /// <summary>
        /// 壁面の削除
        /// </summary>
        /// <param name="wallData"></param>
        /// <returns></returns>
        public bool Remove(Wall wallData)
        {
            var r = wallDataList.FindIndex((v) =>
            {
                return Wall.Compare(v, wallData);
            });

            if (r >= 0)
            {
                wallDataList.RemoveAt(r);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 壁面を持っているか
        /// </summary>
        /// <param name="selectTri"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public bool HasTriangleWall(int[] selectTri, string buildingGmlID)
        {
            var wallData = FindSelectWallData(selectTri, buildingGmlID);
            if (wallData == null)
            {
                return false;
            }

            var r = wallDataList.FindIndex((v) =>
            {
                return Wall.Compare(v, wallData);
            });

            return 0 <= r;
        }

        /// <summary>
        /// 新規なら追加
        /// 既に登録済みなら削除する
        /// </summary>
        /// <param name="pointingTris">選択されたポリゴン</param>
        /// <param name="wallTris"></param>
        public Wall AddOrRemove(int[] pointingTris, List<int[]> wallTris, Mesh building, string buildingGmlID)
        {
            var pointingWallData = FindSelectWallData(pointingTris, buildingGmlID);

            if (pointingWallData != null)
            {
                Remove(pointingWallData);
                return pointingWallData;
            }

            var wallData = Wall.Create(wallTris, building, buildingGmlID);
            return Add(wallData);
        }

        /// <summary>
        /// 壁面群のデータをクリア
        /// </summary>
        public void Clear()
        {
            // todo: wallDataListのMeshを破棄する処理を入れる
            foreach (var item in wallDataList)
            {
                Wall.Release(item);
            }
            wallDataList.Clear();

            AllWllArea = 0f;
        }

        /// <summary>
        /// 壁面群から面積を計算する
        /// AllWllAreaが更新される
        /// </summary>
        public void CalculateAllWallArea()
        {
            AllWllArea = 0f;
            foreach (var wallData in wallDataList)
            {
                // todo 計算済みか、計算する必要があるかを確認出来れば最適化できる
                wallData.CalculateArea();
                if (wallData.AreaSize >= 0f)
                {
                    AllWllArea += wallData.AreaSize;
                }
            }
        }

        public List<int[]> MergeTris()
        {
            if (wallDataList.Count == 0)
                return new List<int[]>();

            string buildingGmlID = wallDataList.First().BuildingGmlID;
            List<int[]> tris = new List<int[]>();
            foreach (var wallData in wallDataList)
            {
                Debug.Assert(buildingGmlID == wallData.BuildingGmlID, "複数建物には対応していない。MergeTris2()を利用すること。");
                foreach (var tri in wallData.WallTris)
                {
                    tris.Add(tri);
                }

            }
            return tris;
        }

        /// <summary>
        /// wallDataにwallDataListに含まれている三角形があれば削除する
        /// 重複削除
        /// </summary>
        /// <param name="wallData"></param>
        private void RemoveDuplicateTris(Wall wallData)
        {
            foreach (var d in wallDataList)
            {
                wallData.Remove(d);
            }
        }

        /// <summary>
        /// 選択した1ポリゴン（三角形）を含む壁面データを取得します。
        /// </summary>
        /// <param name="selectTri">検索対象となる三角形のインデックス配列</param>
        /// <returns>該当するWallオブジェクト。見つからない場合はnullを返します。</returns>
        public Wall FindSelectWallData(int[] selectTri, string buildingGmlID)
        {
            foreach (var wallData in wallDataList)
            {
                if (wallData.IsContains(selectTri, buildingGmlID))
                {
                    return wallData;
                }
            }
            return null;
        }


    }

}
