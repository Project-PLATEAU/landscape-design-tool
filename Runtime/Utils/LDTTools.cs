using System;
using EGIS.ShapeFileLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LandscapeDesignTool
{
    public static class LDTTools
    {
        public enum AreaType
        {
            CIRCLE_REGURATION,
            POLYCGON_REGURATION
        }

        public static string MaterialName = "RegurationAreaMaterial";


        public static void SetUI()
        {

            GameObject ui = GameObject.Find("PlayerPanel");
            if (ui == null)
            {
#if UNITY_EDITOR
                string absolute = Path.GetFullPath("Packages/landscape-design-tool/Prefabs/PlayerPanel.prefab");
                GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.synesthesias.landscape-design-tool/Prefabs/PlayerPanel.prefab", typeof(GameObject));
                GameObject panel = GameObject.Instantiate(prefab);
                panel.name = "PlayerPanel";
#endif
            }
        }

        public static Material MakeMaterial(Color col)
        {
            //レンダーパイプラインに応じた Unlitシェーダーを求めます。
            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            Shader shader;
            if (pipelineAsset == null)
            {
                shader = Shader.Find("Unlit/Transparent Colored");
            }
            else if (pipelineAsset.name == "UniversalRenderPipelineAsset")
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            else if (pipelineAsset.name.Contains("HighDefinition"))
            {
                shader = Shader.Find("HDRP/Unlit");
            }
            else
            {
                throw new Exception("Unknown Pipeline.");
            }
            Material material = new Material(shader);

#if UNITY_EDITOR
            if (pipelineAsset != null && pipelineAsset.name == "UniversalRenderPipelineAsset")
            {
                var originalMaterial = (Material)AssetDatabase.LoadAssetAtPath("Packages/com.synesthesias.landscape-design-tool/Materials/RegulationArea.mat", typeof(Material));
                material.CopyPropertiesFromMaterial(originalMaterial);
            }
#endif

            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            // General Transparent Material Settings
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_ZWrite", 0.0f);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.renderQueue += material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetColor("_BaseColor", col);
            material.name = MaterialName;

            return material;
        }

        public static void WriteShapeFile(string exportFilePath, string[] areatype, string[] type, Color[] col1, Color[] col2, float[] height, Vector3[] originpoint, Vector3[] targetpoint, 
            Vector2[,] specpoint, List<List<Vector2>> vertexlist/*,  List<int> instanceList*/)
        {

            int nblock = vertexlist.Count;

            Debug.Log("WriteShapeFile to : " + exportFilePath);
            DbfFieldDesc[] fields = new DbfFieldDesc[10];
            fields[0] = new DbfFieldDesc { FieldName = "ID", FieldType = DbfFieldType.Character, FieldLength = 14, RecordOffset = 0 };
            fields[1] = new DbfFieldDesc { FieldName = "AREATYPE", FieldType = DbfFieldType.Character, FieldLength = 14, RecordOffset = 0 };
            fields[2] = new DbfFieldDesc { FieldName = "TYPE", FieldType = DbfFieldType.Character, FieldLength = 14, RecordOffset = 0 };
            fields[3] = new DbfFieldDesc { FieldName = "HEIGHT", FieldType = DbfFieldType.Character, FieldLength = 14, RecordOffset = 0 };
            fields[4] = new DbfFieldDesc { FieldName = "COLOR1", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };
            fields[5] = new DbfFieldDesc { FieldName = "COLOR2", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };
            fields[6] = new DbfFieldDesc { FieldName = "POINT1", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };
            fields[7] = new DbfFieldDesc { FieldName = "POINT2", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };
            fields[8] = new DbfFieldDesc { FieldName = "ORIGIN", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };
            fields[9] = new DbfFieldDesc { FieldName = "TARGET", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };


            string exportBaseDirPath = Directory.GetParent(exportFilePath)?.FullName;
            if (string.IsNullOrEmpty(exportBaseDirPath))
            {
                Debug.LogError($"Export path is invalid. path = {exportFilePath}");
                return;
            }
            ShapeFileWriter sfw = ShapeFileWriter.CreateWriter(exportBaseDirPath, Path.GetFileNameWithoutExtension(exportFilePath), ShapeType.Polygon, fields);

            Debug.Log("nblock:" + nblock);
            for (int i = 0; i < nblock; i++)
            {
                List<Vector2> vlist = vertexlist[i];

                PointD[] vertex = new PointD[vlist.Count];

                int n = 0;

                foreach (var v in vlist)
                {
                    vertex[n++] = new PointD(v.x, v.y);
                }

                string[] fielddata = new string[10];
                fielddata[0] = i.ToString();
                fielddata[1] = areatype[i];
                fielddata[2] = type[i];
                fielddata[3] = height[i].ToString();
                fielddata[4] = col1[i].r.ToString() + "," + col1[i].g.ToString() + "," + col1[i].b.ToString() + "," + col1[i].a.ToString();
                fielddata[5] = col2[i].r.ToString() + "," + col2[i].g.ToString() + "," + col2[i].b.ToString() + "," + col2[i].a.ToString();
                fielddata[6] = specpoint[i, 0].x.ToString() + ", " + specpoint[i, 0].y.ToString();
                fielddata[7] = specpoint[i, 1].x.ToString() + ", " + specpoint[i, 1].y.ToString();
                fielddata[8] = originpoint[i].x.ToString() + ", " + originpoint[i].y.ToString() + ", " + originpoint[i].z.ToString();
                fielddata[9] = targetpoint[i].x.ToString() + ", " + targetpoint[i].y.ToString() + ", " + targetpoint[i].z.ToString();
                
                sfw.AddRecord(vertex, vertex.Length, fielddata);
            }

            sfw.Close();
        }

        static string[] layerName = { "RegulationArea" };
        static int[] layerId = { 30 };
        public static void CheckLayers()
        {
#if UNITY_EDITOR
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            //layer情報を取得
            var layersProp = tagManager.FindProperty("layers");
            var index = 0;
            foreach (var layerId in layerId)
            {
                if (layersProp.arraySize > layerId)
                {
                    var sp = layersProp.GetArrayElementAtIndex(layerId);
                    if (sp != null && sp.stringValue != layerName[index])
                    {
                        sp.stringValue = layerName[index];
                        Debug.Log("Adding layer " + layerName[index]);
                    }
                }

                index++;
            }

            tagManager.ApplyModifiedProperties();
            tagManager.Update();
#endif
        }

        public static void CheckTag(string tagname)
        {
            #if UNITY_EDITOR
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            //layer情報を取得
            var tags = tagManager.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; ++i)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagname)
                {
                    return;
                }
            }

            int index = tags.arraySize;
            tags.InsertArrayElementAtIndex(index);
            tags.GetArrayElementAtIndex(index).stringValue = tagname;
            tagManager.ApplyModifiedProperties();
            tagManager.Update();
            #endif
        }

        public static string GetNumberWithTag(string tagname, string title)
        {
            int rval = -1;

            int maxvalule = 0;
            CheckTag(tagname);
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tagname);
            foreach (GameObject obj in objects)
            {
                string nm = obj.name;
                string[] s1 = nm.Split('-');

                if (s1.Length < 2)
                    continue;

                if (!int.TryParse(s1[1], out int n))
                    continue;

                maxvalule = Mathf.Max(maxvalule, n);
            }
            rval = maxvalule + 1;

            return title + "-" + rval.ToString();
        }

    }

#if UNITY_EDITOR
    public class SelectColorPopup : EditorWindow
    {
        public delegate void ColorChangeDelegate(Color col);
        public static ColorChangeDelegate colorChange;
        public delegate void ColorRemoveDelegate();
        public static ColorRemoveDelegate colorRemove;
        static Color _col;
        static Color orgColor;
        static bool hasMaterial = false;
        public static void Init(Color ccol, ColorChangeDelegate dg, ColorRemoveDelegate rm, bool f)
        {

            colorChange = dg;
            colorRemove = rm;
            orgColor = ccol;
            _col = ccol;
            hasMaterial = f;
            SelectColorPopup window = ScriptableObject.CreateInstance<SelectColorPopup>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 350);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("設定する色を選択", EditorStyles.wordWrappedLabel);
            GUILayout.Space(70);

            colorChange.Invoke(_col);

            _col = EditorGUILayout.ColorField("色の設定", _col);
            GUILayout.Space(10);
            if (GUILayout.Button("色を設定"))
            {
                colorChange.Invoke(_col);
                this.Close();
            }
            if (GUILayout.Button("色を削除"))
            {
                colorRemove.Invoke();
                this.Close();
            }
            if (GUILayout.Button("キャンセル"))
            {
                if (!hasMaterial)
                {
                    colorRemove.Invoke();

                }
                else
                {
                    colorChange.Invoke(orgColor);

                }
                this.Close();
            }
        }
    }
#endif


}
