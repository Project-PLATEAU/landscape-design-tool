using UnityEditor;
using UnityEngine;
using Material = UnityEngine.Material;
using Object = UnityEngine.Object;

namespace LandscapeDesignTool.Editor
{
    public class ViewRegulationGUI
    {
        int selectIndex = 0;
        bool selectingTarget = false;
        GameObject vpgroup;
        private Vector3 _prevPos;
        private const string ObjNameLineOfSight = "LineOfSight";
        private const string ObjNameCoveringMesh = "CoveringMesh";

        public ViewRegulationGUI(ViewRegulation target)
        {
            _prevPos = target.transform.position;
        }

        /// <summary>
        /// 視線規制の設定GUIを描画します。
        /// </summary>
        /// <returns>ユーザーがGUIで何らかの設定変更をしたときにtrueを返します。</returns>
        public bool Draw(ViewRegulation target)
        {
            var style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            SceneView sceneView = SceneView.lastActiveSceneView;

            bool isGuiChanged = false;
            using (var checkChange = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.HelpBox("視点場を選択して眺望対象をシーン内で選択してください", MessageType.Info);
                target.ScreenWidth = EditorGUILayout.FloatField("眺望対象での横サイズ(m)", target.ScreenWidth);
                target.ScreenHeight = EditorGUILayout.FloatField("眺望対象での縦サイズ(m)", target.ScreenHeight);
                target.LineColorValid = EditorGUILayout.ColorField("色の設定", target.LineColorValid);
                target.LineColorInvalid = EditorGUILayout.ColorField("規制色の設定", target.LineColorInvalid);
                target.LineInterval = EditorGUILayout.FloatField("障害物の判定間隔(m)", target.LineInterval);
                isGuiChanged |= checkChange.changed;
            }
            

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<size=12>視点場</size>", style);

            var vpGroupComponent = Object.FindObjectOfType<LandscapeViewPointGroup>();
            if (vpGroupComponent == null || vpGroupComponent.transform.childCount == 0)
            {
                EditorGUILayout.HelpBox("視点場を作成してください", MessageType.Error);
                return false;
            }
            vpgroup = vpGroupComponent.gameObject;
            

            string[] options = new string[vpgroup.transform.childCount];
            for (int i = 0; i < vpgroup.transform.childCount; i++)
            {
                LandscapeViewPoint vp = vpgroup.transform.GetChild(i).GetComponent<LandscapeViewPoint>();
                options[i] = vp.Name;
            }
            selectIndex = EditorGUILayout.Popup(selectIndex, options);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<size=12>眺望対象</size>", style);
            GUI.color = selectingTarget == false
                ? Color.white
                : Color.green;
            if (GUILayout.Button("眺望対象の選択"))
            {
                sceneView.Focus();
                selectingTarget = true;
            }

            return isGuiChanged;
        }

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }


        public void OnSceneGUI(ViewRegulation target)
        {
            var trans = target.transform;

            bool posStartChanged = false;
            bool posEndChanged = false;
            
            // 視線の始点ハンドルを描画します。
            // ゲームオブジェクトが選択されているときは、Unity標準の矢印ハンドルで始点を移動するので、ここでは始点ハンドルは描画しません。
            // 選択されていないときは、始点の矢印ハンドルを描画します。
            // どちらにしても、 transform.position の差分を見て移動があったかどうかを監視します。
            Handles.Label(trans.position, "視線 始点");
            if (!Selection.Contains(target.gameObject))
            {
                trans.position = Handles.PositionHandle(trans.position, Quaternion.identity);
            }
            if ((trans.position - _prevPos).magnitude > 0.0001)
            {
                posStartChanged = true;
            }

            // 視線の終点ハンドルを描画します。
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                Handles.Label(target.EndPos, "視線 終点");
                target.EndPos = Handles.PositionHandle(target.EndPos, Quaternion.identity);
                if (check.changed)
                {
                    posEndChanged = true;
                }
            }

            // 移動があったら視線を生成しなおします。
            if (posStartChanged || posEndChanged)
            { 
                target.StartPos = trans.position;
                CreateOrUpdateViewRegulation(target);
            }

            _prevPos = trans.position;

            if(!selectingTarget) return;

            var ev = Event.current;

            RaycastHit hit;
            if (ev.type == EventType.MouseDown)
            {

                Transform origin = vpgroup.transform.GetChild(selectIndex);
                Vector3 originPoint = origin.position;
                Vector3 mousePosition = Event.current.mousePosition;


                Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    Debug.Log(hit.collider.name);
                    Debug.Log(hit.collider.bounds.center);

                    Vector3 targetPoint = hit.point;

                    selectingTarget = false;
                    target.EndPos = targetPoint;
                    CreateOrUpdateViewRegulation(target);
                    
                }
                ev.Use();
            }
            
        }

        /// <summary>
        /// 視線を表示するためのゲームオブジェクトを生成します。
        /// すでにあれば、生成の代わりに更新します。
        /// どこに線を出すかは、引数 <paramref name="targetViewRegulation"/> の position, endPos, lineInterval が利用されます。
        /// </summary>
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