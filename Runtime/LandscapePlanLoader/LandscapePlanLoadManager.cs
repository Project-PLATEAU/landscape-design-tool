using System.Collections.Generic;
using UnityEngine;
using SFB;
using PlateauToolkit.Maps;
using UnityEngine.Rendering.HighDefinition;
using System.IO;
using System.Text;
using System;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// GISデータの読み込みとメッシュの生成を管理するクラス
    /// </summary>
    public sealed class LandscapePlanLoadManager
    {
        private readonly Material wallMaterial;
        private readonly Material ceilingMaterial;

        public LandscapePlanLoadManager()
        {
            wallMaterial = Resources.Load<Material>("Materials/PlanAreaWallMaterial");
            ceilingMaterial = Resources.Load<Material>("Materials/PlanAreaCeilingMaterial");
            ceilingMaterial.SetFloat("_Alpha", 0.2f);
            ceilingMaterial.SetFloat("_Intensity", 1.5f);
        }

        /// <summary>
        /// 指定されたフォルダパスからGISデータを読み込み、メッシュオブジェクトを生成するメソッド
        /// </summary>
        /// <param name="gisTargetFolderPath"> .shp、.dbfファイルを含むフォルダのパス </param>
        public List<AreaProperty> LoadShapefile(string gisTargetFolderPath)
        {
            List<GameObject> listOfGISObjects;
            List<List<List<Vector3>>> listOfAreaPointDatas;
            var loadedAreaProperties = new List<AreaProperty>();

            // エンコーディングの判定
            var encoding = DetectEncoding(gisTargetFolderPath);

            // GISデータの読み込みとメッシュオブジェクトの生成
            using (ShapefileRenderManager shapefileRenderManager = new ShapefileRenderManager(gisTargetFolderPath, 0 /*RenderMode:Mesh*/, 0, false, false, encoding, null))
            {
                if (shapefileRenderManager.Read(0, out listOfGISObjects, out listOfAreaPointDatas))
                {
                    Debug.Log("Loading GIS data completed");
                }
                else
                {
                    Debug.LogError("Loading GIS data failed.");
                    return loadedAreaProperties;
                }
            }

            if (listOfGISObjects == null || listOfGISObjects.Count == 0)
            {
                Debug.LogError("No GIS data was included");
                return loadedAreaProperties;
            }

            LandscapePlanMeshModifier landscapePlanMeshModifier = new LandscapePlanMeshModifier();
            WallGenerator wallGenerator = new WallGenerator();

            // 区画の制限高さに合わせてメッシュデータを変形し、周囲に壁を生成する
            for (int i = 0; i < listOfGISObjects.Count; i++)
            {
                GameObject gisObject = listOfGISObjects[i];
                List<List<Vector3>> areaPointData = listOfAreaPointDatas[i];

                //GISデータのプロパティを取得
                DbfComponent dbf = gisObject.GetComponent<DbfComponent>();
                if (dbf == null)
                {
                    Debug.LogError("GisObject have no DbfComponent");
                    return loadedAreaProperties;
                }

                MeshFilter gisObjMeshFilter = gisObject.GetComponent<MeshFilter>();
                MeshRenderer gisObjMeshRenderer = gisObject.GetComponent<MeshRenderer>();
                if (gisObjMeshFilter == null)
                {
                    Debug.LogError($"{gisObject.name} have no MeshFilter Component");
                    return loadedAreaProperties;
                }
                if (gisObjMeshRenderer == null)
                {
                    Debug.LogError($"{gisObject.name} have no MeshRenderer Component");
                    return loadedAreaProperties;
                }

                Mesh mesh = gisObjMeshFilter.sharedMesh;
                if (mesh == null)
                {
                    Debug.LogError($"Mesh in MeshFilter of {gisObject.name} is null");
                    continue;
                }


                //メッシュを変形
                if (!landscapePlanMeshModifier.TryModifyMeshToTargetHeight(mesh, 0, gisObject.transform.position))
                {
                    Debug.LogError($"{gisObject.name} is out of range of the loaded map");
                    continue;
                }

                // コライダー判定用のMeshColliderを追加
                if (!gisObject.GetComponent<AreaPlanningCollisionHandler>())
                {
                    gisObject.AddComponent<AreaPlanningCollisionHandler>();
                }

                //新規のAreaPropertyを生成し初期化
                float initLimitHeight = float.TryParse(GetPropertyValueOf("HEIGHT", dbf), out float heightValue) ? heightValue : 50; // 区画の制限高さを取得
                int id = int.TryParse(GetPropertyValueOf("ID", dbf), out int idValue) ? idValue : 0;
                string colorString = GetPropertyValueOf("COLOR", dbf);
                var color = colorString != "" ? DbfStringToColor(colorString) : Color.red;

                AreaProperty areaProperty = new AreaProperty(
                    id,
                    GetPropertyValueOf("AREANAME", dbf),
                    initLimitHeight,
                    10,
                    color,
                    new Material(wallMaterial),
                    new Material(ceilingMaterial),
                    Mathf.Max(300, initLimitHeight + 50),
                    gisObject.transform.position + mesh.bounds.center,
                    gisObject.transform,
                    areaPointData
                    );


                //上面Meshのマテリアルを設定
                areaProperty.CeilingMaterial.color = areaProperty.Color;
                gisObjMeshRenderer.material = areaProperty.CeilingMaterial;
                gisObjMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                //区画のメッシュから下向きに壁を生成
                GameObject[] walls = wallGenerator.GenerateWall(mesh, areaProperty.WallMaxHeight, Vector3.down, areaProperty.WallMaterial);
                for (int j = 0; j < walls.Length; j++)
                {
                    walls[j].transform.SetParent(gisObject.transform);
                    walls[j].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    walls[j].name = $"AreaWall_{areaProperty.ID}_{j}";
                }
                areaProperty.WallMaterial.color = areaProperty.Color;
                areaProperty.WallMaterial.SetFloat("_DisplayRate", areaProperty.LimitHeight / areaProperty.WallMaxHeight);
                areaProperty.WallMaterial.SetFloat("_LineCount", areaProperty.LimitHeight / areaProperty.LineOffset);
                areaProperty.SetLocalPosition(new Vector3(
                    areaProperty.Transform.localPosition.x,
                    areaProperty.LimitHeight,
                    areaProperty.Transform.localPosition.z
                    ));
                gisObject.name = $"Area_{areaProperty.ID}";

                //区画データリストにAreaPropertyを追加登録
                AreasDataComponent.AddNewProperty(areaProperty);

                loadedAreaProperties.Add(areaProperty);
            }
            Debug.Log("Mesh modification and wall generation completed");

            foreach (var loadedProperty in loadedAreaProperties)
            {
                // プロジェクトへ保存
                ProjectSaveDataManager.Add(ProjectSaveDataType.LandscapePlan, loadedProperty.ID.ToString());
            }

            return loadedAreaProperties;
        }

        /// <summary>
        /// セーブデータからGISデータを読み込み、メッシュオブジェクトを生成するメソッド
        /// </summary>
        /// <param name="saveDatas"> ロードした区画セーブデータ </param>
        public List<AreaProperty> LoadFromSaveData(List<PlanAreaSaveData> saveDatas)
        {
            List<GameObject> listOfGISObjects;
            List<List<List<Vector3>>> listOfAreaPointDatas;

            var loadedAreaProperties = new List<AreaProperty>();

            // 景観区画の頂点データを取得
            listOfAreaPointDatas = new List<List<List<Vector3>>>();
            foreach (PlanAreaSaveData saveData in saveDatas)
            {
                listOfAreaPointDatas.Add(saveData.PointData);
            }

            // メッシュオブジェクトの生成
            PointDataRenderManager m_PointDataRenderManager = new PointDataRenderManager();
            if (m_PointDataRenderManager.DrawShapes("LoadedPlanArea", listOfAreaPointDatas, out listOfGISObjects))
            {
                Debug.Log("Loading GIS data completed");
            }
            else
            {
                Debug.LogError("Loading GIS data failed.");
                return loadedAreaProperties;
            }

            if (listOfGISObjects == null || listOfGISObjects.Count == 0)
            {
                Debug.LogError("No GIS data was saved");
                return loadedAreaProperties;
            }

            LandscapePlanMeshModifier landscapePlanMeshModifier = new LandscapePlanMeshModifier();
            WallGenerator wallGenerator = new WallGenerator();

            // 区画の制限高さに合わせてメッシュデータを変形し、周囲に壁を生成する
            for (int i = 0; i < listOfGISObjects.Count; i++)
            {
                GameObject gisObject = listOfGISObjects[i];
                List<List<Vector3>> areaPointData = listOfAreaPointDatas[i];
                PlanAreaSaveData saveData = saveDatas[i];

                MeshFilter gisObjMeshFilter = gisObject.GetComponent<MeshFilter>();
                MeshRenderer gisObjMeshRenderer = gisObject.GetComponent<MeshRenderer>();
                if (gisObjMeshFilter == null)
                {
                    Debug.LogError($"{gisObject.name} have no MeshFilter Component");
                    return loadedAreaProperties;
                }
                if (gisObjMeshRenderer == null)
                {
                    Debug.LogError($"{gisObject.name} have no MeshRenderer Component");
                    return loadedAreaProperties;
                }

                Mesh mesh = gisObjMeshFilter.sharedMesh;
                if (mesh == null)
                {
                    Debug.LogError($"Mesh in MeshFilter of {gisObject.name} is null");
                    return loadedAreaProperties;
                }

                // Meshを変形
                if (!landscapePlanMeshModifier.TryModifyMeshToTargetHeight(mesh, 0, gisObject.transform.position))
                {
                    Debug.LogError($"{gisObject.name} is out of range of the loaded map");
                    return loadedAreaProperties;
                }

                // コライダー判定用のMeshColliderを追加
                if (!gisObject.GetComponent<AreaPlanningCollisionHandler>())
                {
                    gisObject.AddComponent<AreaPlanningCollisionHandler>();
                }

                // 新規のAreaPropertyを生成し初期化
                float initLimitHeight = saveData.LimitHeight;
                AreaProperty areaProperty = new AreaProperty(
                    saveData.Id,
                    saveData.Name,
                    initLimitHeight,
                    saveData.LineOffset,
                    saveData.Color,
                    new Material(wallMaterial),
                    new Material(ceilingMaterial),
                    Mathf.Max(300, initLimitHeight + 50),
                    gisObject.transform.position + mesh.bounds.center,
                    gisObject.transform,
                    areaPointData
                    );

                // 上面Meshのマテリアルを設定
                areaProperty.CeilingMaterial.color = areaProperty.Color;
                gisObjMeshRenderer.material = areaProperty.CeilingMaterial;

                // 区画のメッシュから下向きに壁を生成
                GameObject[] walls = wallGenerator.GenerateWall(mesh, areaProperty.WallMaxHeight, Vector3.down, areaProperty.WallMaterial);
                for (int j = 0; j < walls.Length; j++)
                {
                    walls[j].transform.SetParent(gisObject.transform);
                    walls[j].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    walls[j].name = $"AreaWall_{areaProperty.ID}_{j}";
                }
                areaProperty.WallMaterial.color = areaProperty.Color;
                areaProperty.WallMaterial.SetFloat("_DisplayRate", areaProperty.LimitHeight / areaProperty.WallMaxHeight);
                areaProperty.WallMaterial.SetFloat("_LineCount", areaProperty.LimitHeight / areaProperty.LineOffset);
                areaProperty.SetLocalPosition(new Vector3(
                    areaProperty.Transform.localPosition.x,
                    areaProperty.LimitHeight,
                    areaProperty.Transform.localPosition.z
                    ));
                gisObject.name = $"Area_{areaProperty.ID}";

                // 高さを適用
                areaProperty.ApplyBuildingHeight(true);

                // 区画データリストにAreaPropertyを追加登録
                AreasDataComponent.AddNewProperty(areaProperty);

                // ロードしたAreaPropertyをリストに追加
                loadedAreaProperties.Add(areaProperty);
            }
            Debug.Log("Mesh modification and wall generation completed");

            return loadedAreaProperties;
        }

        /// <summary>
        /// 既に登録された区画データのメッシュオブジェクトを頂点データから再生成するメソッド
        /// </summary>
        public void ReloadMeshes(List<List<Vector3>> listOfVertices, int index)
        {
            // テッセレーション処理を行ったメッシュを生成
            AreaProperty areaProperty = AreasDataComponent.GetProperty(index);
            GameObject gisObject = areaProperty.Transform.gameObject;
            if (gisObject == null)
            {
                Debug.LogError("GIS object is not found");
                return;
            }

            MeshFilter gisObjMeshFilter = gisObject.GetComponent<MeshFilter>();
            TessellatedMeshCreator tessellatedMeshCreator = new TessellatedMeshCreator();
            tessellatedMeshCreator.CreateTessellatedMesh(listOfVertices, gisObjMeshFilter, 30, 40);
            Mesh mesh = gisObjMeshFilter.sharedMesh;

            // Meshを変形
            LandscapePlanMeshModifier landscapePlanMeshModifier = new LandscapePlanMeshModifier();
            if (!landscapePlanMeshModifier.TryModifyMeshToTargetHeight(mesh, areaProperty.LimitHeight, gisObject.transform.position))
            {
                Debug.LogError($"{gisObject.name} is out of range of the loaded map");
                return;
            }

            //GISオブジェクトの壁オブジェクトを削除
            for (int i = 0; i < gisObject.transform.childCount; i++)
            {
                GameObject wallObject = gisObject.transform.GetChild(i).gameObject;
                if (wallObject.name.Contains("AreaWall"))
                {
                    GameObject.Destroy(wallObject);
                }
            }
            WallGenerator wallGenerator = new WallGenerator();
            // 区画のメッシュから下向きに壁を再生成
            GameObject[] walls = wallGenerator.GenerateWall(mesh, areaProperty.WallMaxHeight, Vector3.down, areaProperty.WallMaterial);
            for (int j = 0; j < walls.Length; j++)
            {
                //GameObject wallObject = GameObject.Find($"AreaWall_{areaEditManager.GetAreaID()}_{j}");
                // 存在する壁オブジェクトを削除
                //if (wallObject != null) GameObject.Destroy(wallObject);

                walls[j].transform.SetParent(gisObject.transform);
                walls[j].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                walls[j].name = $"AreaWall_{areaProperty.ID}_{j}";


                areaProperty.SetLocalPosition(new Vector3(
                    areaProperty.Transform.localPosition.x,
                    areaProperty.LimitHeight,
                    areaProperty.Transform.localPosition.z
                    ));
            }
        }

        /// <summary>
        /// フォルダ選択用のダイアログを開き、パスを取得するメソッド
        /// </summary>
        /// <returns>フォルダパス</returns>
        public string BrowseFolder()
        {
            var paths = StandaloneFileBrowser.OpenFolderPanel("Open Folder", "", false);

            if (paths.Length > 0) return paths[0];
            return null;
        }

        /// <summary>
        /// 色のstirngデータをColorに変換するメソッド
        /// </summary>
        /// <param name="colorString"> "r,g,b"のフォーマットで記述された色のstringデータ </param>
        Color DbfStringToColor(string colorString)
        {
            string[] colorValues = colorString.Split(',');

            if (colorValues.Length >= 3 &&
                float.TryParse(colorValues[0], out float r) &&
                float.TryParse(colorValues[1], out float g) &&
                float.TryParse(colorValues[2], out float b))
            {
                return new Color(r, g, b, 1);
            }

            Debug.LogError("Invalid color string format. Color data must be in the form 'R,G,B' with values ranging from 0 to 1. For example, '0.2,0.2,0.2'.");
            return Color.white;
        }

        /// <summary>
        /// 区画オブジェクトのDbfComponentから指定された名前の属性値を取得するメソッド
        /// </summary>
        /// <param name="propertyName">取得したいdbfの属性名</param>
        /// <param name="dbf">取得対象の区画オブジェクトにアタッチされているDbfComponentクラス</param>
        string GetPropertyValueOf(string propertyName, DbfComponent dbf)
        {
            int index = dbf.PropertyNames.IndexOf(propertyName);
            if (index != -1) return dbf.Properties[index];

            Debug.LogError($"Attribute name '{propertyName}' was not found.");
            return "";
        }

        /// <summary>
        /// 指定された.cpgファイルのテキストからエンコーディングがUTF-8かを判定するメソッド
        /// </summary>
        /// <param name="filePath">判定したい.dbfファイルのパス</param>
        /// <returns>判定結果：UTF-8 または Shift-JIS のいずれか。判定できない場合はShift-JISを返す</returns>
        public SupportedEncoding DetectEncoding(string folderPath)
        {
            try
            {
                // 指定されたディレクトリ内で ".cpg" 拡張子のファイルを検索
                string[] cpgFiles = Directory.GetFiles(folderPath, "*.cpg");

                if (cpgFiles.Length == 0)
                {
                    // 見つからなかった場合はワーニングを表示し、Shift-JISを返す
                    Debug.LogWarning($"'.cpg' ファイルが見つかりませんでした。");
                    return SupportedEncoding.ShiftJIS;
                }

                // .cpgファイルが見つかった場合、テキストの内容からエンコーディングを判定
                foreach (var cpgFile in cpgFiles)
                {
                    // ファイル内容を読み込み
                    string content = File.ReadAllText(cpgFile);

                    // "utf-8" が含まれているかを確認
                    if (content.Contains("utf-8") || content.Contains("UTF-8"))
                    {
                        return SupportedEncoding.UTF8;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"エンコーディングの判定に失敗しました。Shift-JISを返します。");
            }
            return SupportedEncoding.ShiftJIS;
        }
    }
}

