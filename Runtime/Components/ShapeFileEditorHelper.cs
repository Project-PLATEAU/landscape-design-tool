using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EGIS.ShapeFileLib;
using PLATEAU.CityInfo;
using PLATEAU.Geometries;
using UnityEditor.Experimental.GraphView;

namespace LandscapeDesignTool
{
    public class ShapeFileEditorHelper
    {
        private float _areaHeight = 10;
        private string _shapefileLoadPath;
        private string _generateGameObjName = "LoadedShapeFile";
        private const string ObjNameLineOfSight = "LineOfSight";
        private const string ObjNameCoveringMesh = "CoveringMesh";

        private List<List<Vector2>> _contours;
        List<Color> _colors1 = new List<Color>();
        List<Color> _colors2 = new List<Color>();
        List<float> _heights = new List<float>();
        List<string> _areatype = new List<string>();
        List<Vector3> _origin = new List<Vector3>();
        List<Vector3> _target = new List<Vector3>();
        List<Vector2> _size = new List<Vector2>();

        private PLATEAUInstancedCityModel _cityModel;

#if UNITY_EDITOR

        public void DrawGui()
        {
            _cityModel =
                (PLATEAUInstancedCityModel)EditorGUILayout.ObjectField("対象都市", _cityModel,
                    typeof(PLATEAUInstancedCityModel), true);
            EditorGUILayout.LabelField("読込ファイル:");
            string displayPath = string.IsNullOrEmpty(_shapefileLoadPath) ? "未選択" : _shapefileLoadPath;

            EditorGUILayout.LabelField(displayPath);
            if (GUILayout.Button("ファイル選択"))
            {
                string selectedPath = EditorUtility.OpenFilePanel("ShapeFile選択", "", "shp");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _shapefileLoadPath = selectedPath;
                }
            }

            if (string.IsNullOrEmpty(_shapefileLoadPath)) return;

            _generateGameObjName = EditorGUILayout.TextField("ゲームオブジェクト名: ", _generateGameObjName);

            /*
            _areaHeight =
                EditorGUILayout.FloatField("高さ",
                    _areaHeight);
            */
            if (_cityModel == null)
            {
                EditorGUILayout.HelpBox("対象都市を指定してください。", MessageType.Error);
            }
            else if (GUILayout.Button("メッシュデータの作成"))
            {
                var parentObj = new GameObject(_generateGameObjName);
                BuildMesh(_shapefileLoadPath, parentObj.transform, _cityModel.GeoReference);
            }
        }
#endif

#if UNITY_EDITOR
        void BuildMesh(string shapefilePath, Transform parentTransform, GeoReference geoRef)
        {

            Debug.Log(shapefilePath);
            var shp = new ShapeFile(shapefilePath);
            ShapeFileEnumerator sfEnum = shp.GetShapeFileEnumerator();

            int keynoColor1 = -1;
            int keynoColor2 = -1;
            int keynoHeight = -1;
            int keyAreaTpe = -1;
            int keyOrigin = -1;
            int keyTarget = -1;
            int keySize = -1;
            string[] keys = shp.GetAttributeFieldNames();
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == "COLOR1")
                {
                    keynoColor1 = i;
                }
                if (keys[i] == "COLOR2")
                {
                    keynoColor2 = i;
                }
                if (keys[i] == "HEIGHT")
                {
                    keynoHeight = i;
                }
                if (keys[i] == "AREATYPE")
                {
                    keyAreaTpe = i;
                }
                if (keys[i] == "ORIGIN")
                {
                    keyOrigin = i;
                }
                if (keys[i] == "TARGET")
                {
                    keyTarget = i;
                }
                if (keys[i] == "POINT1")
                {
                    keySize = i;
                }
            }

            Debug.Log("keys");

            for (int i = 0; i < shp.RecordCount; i++)
            {
                string[] valus = shp.GetAttributeFieldValues(i);
                if (keyAreaTpe > -1)
                {
                    _areatype.Add(valus[keyAreaTpe]);
                }

                if (keynoColor1 > -1)
                {
                    string cols = valus[keynoColor1];
                    string[] carr = cols.Split(',');
                    float r = float.Parse(carr[0]);
                    float g = float.Parse(carr[1]);
                    float b = float.Parse(carr[2]);
                    float a = float.Parse(carr[3]);
                    Color col = new Color(r, g, b, a);
                    _colors1.Add(col);
                }
                else
                {

                    Color col = new Color(1, 1, 1, 0.5f);
                    _colors1.Add(col);
                }
                if (keynoColor2 > -1)
                {
                    string cols = valus[keynoColor2];
                    string[] carr = cols.Split(',');
                    float r = float.Parse(carr[0]);
                    float g = float.Parse(carr[1]);
                    float b = float.Parse(carr[2]);
                    float a = float.Parse(carr[3]);
                    Color col = new Color(r, g, b, a);
                    _colors2.Add(col);
                }
                else
                {

                    Color col = new Color(1, 1, 1, 0.5f);
                    _colors2.Add(col);
                }

                if (keynoHeight > -1)
                {
                    float h = float.Parse(valus[keynoHeight]);
                    _heights.Add(h);
                }
                else
                {
                    float h = 10.0f;
                    _heights.Add(h);

                }

                if (keyOrigin > -1)
                {
                    string cols = valus[keyOrigin];
                    string[] carr = cols.Split(',');
                    float x = float.Parse(carr[0]);
                    float y = float.Parse(carr[1]);
                    float z = float.Parse(carr[2]);
                    Vector3 origin = new Vector3(x, y, z);
                    _origin.Add(origin);
                }
                else
                {
                    Vector3 origin = Vector3.zero;
                    _origin.Add(origin);
                }
                if (keyTarget > -1)
                {
                    string cols = valus[keyTarget];
                    string[] carr = cols.Split(',');
                    float x = float.Parse(carr[0]);
                    float y = float.Parse(carr[1]);
                    float z = float.Parse(carr[2]);
                    Vector3 target = new Vector3(x, y, z);
                    _target.Add(target);
                }
                else
                {
                    Vector3 target = Vector3.zero;
                    _origin.Add(target);
                }

                if (keySize > -1)
                {
                    string cols = valus[keySize];
                    string[] carr = cols.Split(',');
                    float w = float.Parse(carr[0]);
                    float h = float.Parse(carr[1]);
                    Vector2 size = new Vector2(w, h);
                    _size.Add(size);
                }
                else
                {
                    Vector2 size = Vector2.zero;
                    _size.Add(size);
                }
            }

            _contours = new List<List<Vector2>>();

            while (sfEnum.MoveNext())
            {
                System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> pointRecords = sfEnum.Current;


                int i = 0;
                foreach (PointD[] pts in pointRecords)
                {
                    if (pts.Length < 3) continue;
                    List<Vector2> contour = new List<Vector2>();

                    // Debug.Log(string.Format("[NumPoints:{0}]", pts.Length));


                    for (int n = 0; n < pts.Length; ++n)
                    {
                        var refP = geoRef.ReferencePoint;
                        Vector2 p = new Vector2((float)(pts[n].X - refP.X), (float)(pts[n].Y - refP.Z));
                        contour.Add(p);
                    }
                    _contours.Add(contour);

                    i++;
                }

            }

            int count = 0;
            foreach (var contour in _contours)
            {
                ;
                Debug.Log("areatype " + _areatype[count]);


                if (string.Compare(_areatype[count], "RegulationArea") == 0)
                {
                    var regulationArea = RegulationArea.Create(parentTransform);

                    for (int i = 0; i < contour.Count; i++)
                    {
                        var point = contour[i];
                        var pos = new Vector3(point.x, 0, point.y);
                        regulationArea.TryAddVertexOnGround(pos);
                    }

                    Color col = _colors1[count];
                    float h = _heights[count];
                    regulationArea.SetHeight(h);
                    regulationArea.SetAreaColor(col);
                    regulationArea.GenMesh();
                }
                else if (string.Compare(_areatype[count], "ViewRegulation") == 0)
                {
                    GameObject go = new GameObject();
                    go.name = "RegulationArea";
                    go.tag = "ViewRegulationArea";
                    go.layer = LayerMask.NameToLayer("RegulationArea");
                    go.transform.parent = parentTransform;
                    ViewRegulation viewregulation = go.AddComponent<ViewRegulation>();
                    viewregulation.StartPos = _origin[count];
                    viewregulation.EndPos = _target[count];
                    viewregulation.ScreenWidth = _size[count].x;
                    viewregulation.ScreenHeight = _size[count].y;
                    viewregulation.LineColorValid = _colors1[count];
                    viewregulation.LineColorInvalid = _colors2[count];
                    CreateOrUpdateViewRegulation(viewregulation);
                }
                else if (string.Compare(_areatype[count], "HeightRestrict") == 0)
                {
                    Debug.Log("AAAA");
                    var diameter = (_target[count] - _origin[count]).magnitude * 2;
                    var height = _heights[count];
                    var color = _colors1[count];

                    var heightRegulation = HeightRegulationAreaHandler.CreateRegulationArea(
                        diameter, color, height, _origin[count]
                        );
                    heightRegulation.transform.parent = parentTransform;
                }

                count++;
            }

        }
#endif

        public void CreateOrUpdateViewRegulation(ViewRegulation targetViewRegulation)
        {
            var originPoint = targetViewRegulation.StartPos;
            var targetPoint = targetViewRegulation.EndPos;
            CreateCoveringMesh(targetViewRegulation, originPoint, targetPoint);
            CreateLineOfSight(targetViewRegulation, originPoint, targetPoint);
        }

        /// <summary>
        /// 選択判定のためのメッシュを生成します。
        /// </summary>
        private void CreateCoveringMesh(ViewRegulation target, Vector3 originPoint, Vector3 targetPoint)
        {
            // 以前の CoveringMesh があれば削除します。
            var trans = target.transform;
            for (int i = 0; i < trans.childCount; i++)
            {
                var child = trans.GetChild(i);
                if (child.name == ObjNameCoveringMesh)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var wSize = target.ScreenWidth;
            var hSize = target.ScreenHeight;
            float length = (targetPoint - originPoint).magnitude;
            Vector3[] vertex = new Vector3[6];
            vertex[0] = new Vector3(0, 0, 0);
            vertex[1] = new Vector3(-wSize / 2.0f, -hSize / 2.0f, length);
            vertex[2] = new Vector3(-wSize / 2.0f, hSize / 2.0f, length);
            vertex[3] = new Vector3(wSize / 2.0f, hSize / 2.0f, length);
            vertex[4] = new Vector3(wSize / 2.0f, -hSize / 2.0f, length);
            vertex[5] = new Vector3(0, 0, length);

            int[] idx = {
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 1,
                5, 2, 1,
                5, 3, 2,
                5, 4, 3,
                5, 1, 4 };

            GameObject go = new GameObject(ObjNameCoveringMesh);
            go.layer = LayerMask.NameToLayer("RegulationArea");

            var mf = go.GetComponent<MeshFilter>();
            if (mf == null)
                mf = go.AddComponent<MeshFilter>();

            var mesh = new Mesh();
            mesh.vertices = vertex;
            mesh.triangles = idx;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null)
                mr = go.AddComponent<MeshRenderer>();

            Material material = LDTTools.MakeMaterial(target.LineColorValid);

            mr.sharedMaterial = material;
            mf.mesh = mesh;

            go.transform.position = originPoint;
            go.transform.LookAt(targetPoint, Vector3.up);
            go.transform.parent = (target).transform;
            mr.enabled = false;
        }

        private static void ClearLineOfSight(ViewRegulation target)
        {
            var root = (target).transform;
            for (int i = 0; i < root.childCount; ++i)
            {
                var trans = root.GetChild(i);
                string childName = trans.name;
                if (childName == ObjNameLineOfSight)
                {
                    Object.DestroyImmediate(trans.gameObject);
                }
            }
        }

        float CreateLineOfSight(ViewRegulation target, Vector3 origin, Vector3 destination)
        {
            ClearLineOfSight(target);

            var obj = new GameObject(ObjNameLineOfSight);
            obj.transform.parent = ((ViewRegulation)target).transform;

            float result = -1;
            float interval = target.LineInterval;
            int divx = (int)(target.ScreenWidth / interval);
            int divy = (int)(target.ScreenHeight / interval);

            for (int i = 0; i < divx + 1; i++)
            {
                for (int j = 0; j < divy + 1; j++)
                {
                    float x = destination.x - (target.ScreenWidth / 2.0f) + interval * i;
                    float y = destination.y - (target.ScreenHeight / 2.0f) + interval * j;
                    Vector3 d = new Vector3(x, y, destination.z);
                    RaycastHit hit;

                    if (RaycastBuildings(target, origin, d, out hit))
                    {
                        DrawLine(origin, hit.point, obj, target.LineColorValid);
                        DrawLine(hit.point, d, obj, target.LineColorInvalid);
                    }
                    else
                    {
                        DrawLine(origin, d, obj, target.LineColorValid);
                    }
                }
            }

            return result;
        }

        void DrawLine(Vector3 origin, Vector3 distination, GameObject parent, Color col)
        {
            Vector3[] point = new Vector3[2];
            point[0] = origin;
            point[1] = distination;

            GameObject go = new GameObject("ViewRegurationAreaByLine");
            go.layer = LayerMask.NameToLayer("RegulationArea");

            LineRenderer lineRenderer = go.AddComponent<LineRenderer>();

            lineRenderer.SetPositions(point);
            lineRenderer.positionCount = point.Length;
            lineRenderer.startWidth = 1.0f;
            lineRenderer.endWidth = 1.0f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            lineRenderer.startColor = col;
            lineRenderer.endColor = col;

            go.transform.parent = parent.transform;
        }

        bool RaycastBuildings(ViewRegulation target, Vector3 origin, Vector3 destination, out RaycastHit hitInfo)
        {
            bool result = false;

            hitInfo = new RaycastHit();

            Vector3 direction = (destination - origin).normalized;

            RaycastHit[] hits;
            hits = Physics.RaycastAll(origin, direction, 10000);

            float minDistance = float.MaxValue;
            if (hits.Length <= 0)
                return result;

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.name == target.name)
                    continue;

                int layerIgnoreRaycast = LayerMask.NameToLayer("RegulationArea");

                if (hit.collider.gameObject.layer == layerIgnoreRaycast)
                    continue;

                result = true;

                if (hit.distance >= minDistance)
                    continue;

                hitInfo = hit;
                minDistance = hit.distance;
            }

            return result;
        }

    }
}
