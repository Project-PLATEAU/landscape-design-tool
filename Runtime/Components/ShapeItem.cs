using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LandscapeDesignTool
{
    public class ShapeItem : MonoBehaviour
    {
        public Material material;
        public float height;

        public float oldHeight;

        public List<Vector2> Contours;

        [SerializeField]
        public LDTShapeFileHandler fields;
        
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetVertex( List<Vector2> org)
        {
            Contours = new List<Vector2>();
            foreach(var v in org)
            {
                Contours.Add(v);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ShapeItem))]
        public class ShapeItemEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var shapeItem = (ShapeItem)target;
                shapeItem.material =
                    (Material)EditorGUILayout.ObjectField("マテリアル",
                        shapeItem.material, typeof(Material), false);

                shapeItem.height =
                    EditorGUILayout.FloatField("高さ",
                        shapeItem.height);
                if (GUILayout.Button("変更する"))
                {
                    shapeItem.ReConstruct();
                }
                EditorGUILayout.LabelField($"頂点数: {shapeItem.Contours?.Count ?? 0}");
            }
        }
#endif

        void ReConstruct()
        {
            GameObject upper = gameObject.transform.Find("Upper").gameObject;

            MeshRenderer mr = upper.GetComponent<MeshRenderer>();
            MeshFilter mf = upper.GetComponent<MeshFilter>();
            // Vector3[] ov = mf.mesh.vertices;

            Vector3[] nv = new Vector3[mf.sharedMesh.vertices.Length];

            for (int i = 0; i < mf.sharedMesh.vertices.Length; i++)
            {
                Vector3 ov = mf.sharedMesh.vertices[i];

                nv[i] = new Vector3(ov.x, (ov.y - oldHeight) + height, ov.z);

            }
            mf.sharedMesh.vertices = nv;
            mr.sharedMaterial = material;


            GameObject side = gameObject.transform.Find("Side").gameObject;

            mr = side.GetComponent<MeshRenderer>();
            mf = side.GetComponent<MeshFilter>();

            nv = new Vector3[mf.sharedMesh.vertices.Length];
            mf.sharedMesh.vertices.CopyTo(nv, 0);

            for (int i = 1; i < mf.sharedMesh.vertices.Length; i += 2)
            {
                Vector3 ov = nv[i];
                nv[i] = new Vector3(ov.x, (ov.y - oldHeight) + height, ov.z);

            }

            mf.sharedMesh.vertices = nv;
            mr.sharedMaterial = material;

            oldHeight = height;

        }

    }
}
