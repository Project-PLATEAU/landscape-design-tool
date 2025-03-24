using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LandscapeDesignTool.Editor
{
    [CustomEditor(typeof(RegulationArea))]
    public class RegulationAreaEditor : UnityEditor.Editor
    {
        public static RegulationAreaEditor Active;

        Color _overlayColor = new Color(1, 0, 0, 0.5f);
        bool isBuildSelecting = false;
        GameObject hitTarget = null;

        private SerializedProperty heightProperty;

        public RegulationArea Target => target as RegulationArea;
        
        private void OnEnable()
        {
            heightProperty = serializedObject.FindProperty("areaHeight");
        }

        public override void OnInspectorGUI()
        {
            Active = this;

            var newHeight = EditorGUILayout.FloatField("高さ制限", heightProperty.floatValue);
            if (Math.Abs(newHeight - heightProperty.floatValue) > 0.01f)
                ((RegulationArea)target).SetHeight(newHeight);

            if (Target.IsEditMode)
            {
                GUI.color = Color.green;
                if (GUILayout.Button("頂点の編集を完了"))
                {
                    Target.IsEditMode = false;
                    SceneView.lastActiveSceneView.Repaint();
                }
                GUI.color = Color.white;
            }
            else
            {
                if (GUILayout.Button("頂点を編集する"))
                {
                    Target.IsEditMode = true;
                    SceneView.lastActiveSceneView.Repaint();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBuildingColorEditPanel()
        {
            EditorGUILayout.HelpBox("建物のカラーを変更します", MessageType.Info);
            if (isBuildSelecting == false)
            {
                GUI.color = Color.white;
                if (GUILayout.Button("建物のカラーを変更"))
                {
                    SceneView.lastActiveSceneView.Focus();
                    isBuildSelecting = true;
                }
            }
            else
            {
                GUI.color = Color.green;
                if (GUILayout.Button("カラー変更を終了"))
                {
                    isBuildSelecting = false;
                }
            }
        }

        private void OnSceneGUI()
        {
            if (!Target.IsEditMode)
                return;

            var regulationArea = target as RegulationArea;

            for (int i = 0; i < regulationArea.Vertices.Count; ++i)
            {
                Handles.color = Color.blue;
                EditorGUI.BeginChangeCheck();
                Vector3 pos = Handles.FreeMoveHandle(regulationArea.Vertices[i], Quaternion.identity, 10f, Vector3.zero, Handles.SphereHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    if (regulationArea.Vertices[i] == pos)
                        continue;

                    regulationArea.TrySetVertexOnGround(i, pos);
                    if (regulationArea.IsMeshGenerated)
                        regulationArea.GenMesh();
                }
            }

            if (isBuildSelecting)
            {
                var ev = Event.current;
                if (ev.type == EventType.KeyUp && ev.keyCode == KeyCode.LeftShift)
                {
                    Vector3 mousePosition = Event.current.mousePosition;
                    RaycastHit[] hits;
                    Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                    hits = Physics.RaycastAll(ray, Mathf.Infinity);

                    float minDistance = float.MaxValue;

                    if (hits.Length > 0)
                    {
                        bool isRegurationArea = false;

                        for (int i = 0; i < hits.Length; i++)
                        {
                            RaycastHit hit = hits[i];

                            int layer = LayerMask.NameToLayer("Building");

                            if (hit.collider.gameObject.layer == layer)
                            {
                                if (hit.distance < minDistance)
                                {
                                    hitTarget = hit.collider.gameObject;
                                    minDistance = hit.distance;
                                    Debug.Log("hit " + hit.collider.name + " " + minDistance + " " + hit.point.ToString());
                                    Bounds box = hitTarget.GetComponent<Renderer>().bounds;
                                    float x = (box.min.x + box.max.x) / 2.0f;
                                    float z = (box.min.z + box.max.z) / 2.0f;
                                    Vector3 bottomCenter = new Vector3(x, box.min.y, z);
                                    Debug.Log(bottomCenter);

                                    RaycastHit[] hits2;

                                    Physics.queriesHitBackfaces = true;
                                    hits2 = Physics.RaycastAll(bottomCenter, new Vector3(0, 1, 0), Mathf.Infinity);

                                    Debug.Log(hits2.Length);
                                    if (hits.Length > 0)
                                    {
                                        for (int j = 0; j < hits2.Length; j++)
                                        {
                                            RaycastHit hit2 = hits2[j];
                                            Debug.Log(hit2.collider.gameObject.name);
                                            int layer2 = LayerMask.NameToLayer("RegulationArea");
                                            if (hit2.collider.gameObject.layer == layer2)
                                            {
                                                isRegurationArea = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (isRegurationArea)
                        {
                            List<Material> oldmat = new List<Material>();
                            hitTarget.GetComponent<Renderer>().GetSharedMaterials(oldmat);
                            _overlayColor = new Color(1, 0, 0, 0.5f);
                            bool find = false;
                            foreach (Material mat in oldmat)
                            {
                                if (mat.name == LDTTools.MaterialName)
                                {
                                    find = true;
                                    _overlayColor = mat.GetColor("_BaseColor");
                                }
                            }

                            SelectColorPopup.Init(_overlayColor, ColorSelected, ColorRemove, find);
                        }
                    }
                }
            }
        }

        void ColorSelected(Color col)
        {
            Debug.Log("Selected " + col.ToString());
            List<Material> oldmat = new List<Material>();
            hitTarget.GetComponent<Renderer>().GetSharedMaterials(oldmat);
            _overlayColor = new Color(1, 0, 0, 0.5f);
            bool f = false;
            foreach (Material mat in oldmat)
            {
                if (mat.name == LDTTools.MaterialName)
                {
                    mat.SetColor("_BaseColor", col);
                    f = true;
                }
            }

            if (f) return;

            int nmat;
            Material material = LDTTools.MakeMaterial(col);
            nmat = oldmat.Count;
            Material[] matArray = new Material[nmat + 1];
            for (int i = 0; i < nmat; i++)
            {
                matArray[i] = oldmat[i];
            }
            matArray[nmat] = material;

            hitTarget.GetComponent<Renderer>().materials = matArray;
        }

        void ColorRemove()
        {
            List<Material> oldmat = new List<Material>();
            hitTarget.GetComponent<Renderer>().GetSharedMaterials(oldmat);
            for (int i = 0; i < oldmat.Count; i++)
            {
                if (oldmat[i].name == LDTTools.MaterialName)
                    oldmat.RemoveAt(i);
            }

            hitTarget.GetComponent<Renderer>().sharedMaterials = oldmat.ToArray();
        }
    }
}
