using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

namespace Landscape2.Editor
{
    /// <summary>
    /// アセット配置のサムネアイコンを生成します
    /// Prefabをドラッグアンドドロップで登録し、出力先フォルダを指定してGenerateボタンを押すと、サムネイルが生成されます
    /// </summary>
    public class CreateAssetIcon : EditorWindow
    {
        [MenuItem("Window/景観ツール/CreateAssetIcon")]
        static void OpenWindow()
        {
            var _ = EditorWindow.GetWindow<CreateAssetIcon>();
        }

        private Vector2 scrollPos;
        private string dstPath = "";
        private List<GameObject> prefabs = new();

        private bool IsInnerCameraFrustum(Camera cam, Renderer renderer)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

            Bounds bounds = renderer.bounds;
            Vector3 size = bounds.size;
            Vector3 min = bounds.min;

            //Calculate the 8 points on the corners of the bounding box
            List<Vector3> boundsCorners = new List<Vector3>(8) {
                min,
                min + new Vector3(0, 0, size.z),
                min + new Vector3(size.x, 0, size.z),
                min + new Vector3(size.x, 0, 0),
            };
            for (int i = 0; i < 4; i++)
            {
                boundsCorners.Add(boundsCorners[i] + size.y * Vector3.up);
            }
            //Check each plane on every one of the 8 bounds' corners*
            for (int p = 0; p < planes.Length; p++)
            {
                for (int i = 0; i < boundsCorners.Count; i++)
                {
                    if (!planes[p].GetSide(boundsCorners[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private Texture2D CreateRenderObjectPreview(GameObject prefab)
        {
            var pru = new PreviewRenderUtility();

            var obj = pru.InstantiatePrefabInScene(prefab);
            var r = obj.GetComponentInChildren<Renderer>();
            if (r == null)
            {
                if (obj.TryGetComponent<LODGroup>(out var lod))
                {
                    var lods = lod.GetLODs();
                    foreach (var l in lods)
                    {
                        if (l.renderers == null || l.renderers.Length == 0 || l.renderers[0] == null)
                        {
                            continue;
                        }
                        r = (Renderer)l.renderers[0];
                        break;
                    }

                }
            }

            if (r == null)
            {
                Debug.Log($"{obj.name} : renderer is not found");
                pru.Cleanup();
                return null;
            }
            obj.transform.position = new Vector3(0f, 0, 0f);
            obj.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            pru.AddSingleGO(obj);

            int i = 0;
            
            int checkCount = 2;
            do
            {
                pru.camera.farClipPlane = 1000;
                Vector3 camPos = new Vector3(0, 0.4f, -1) * i;
                pru.camera.transform.position = camPos;
                pru.camera.transform.LookAt(obj.transform.position + r.bounds.center);
                i++;

                if (IsInnerCameraFrustum(pru.camera, r))
                {
                    checkCount--;
                }

            } while (0 < checkCount);

            Debug.Log($"{prefab.name}.cameraPos: {pru.camera.transform.position}");

            pru.BeginStaticPreview(new Rect(0, 0, 512, 512));
            pru.Render(true);

            var tex = pru.EndStaticPreview();

            pru.Cleanup();
            return tex;
        }


        private Texture2D GenerateToWritableTex2D(Texture2D texture2d)
        {
            texture2d.Apply();
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                        texture2d.width,
                        texture2d.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(texture2d, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D readableTextur2D = new Texture2D(texture2d.width, texture2d.height);
            readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTextur2D.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return readableTextur2D;
        }

        private void CreateThumbnailAll(string dstDir, List<GameObject> prefabs)
        {
            var dstDirFull = Path.GetFullPath(dstDir);
            foreach (var obj in prefabs)
            {
                var f = AssetDatabase.GetAssetPath(obj.GetInstanceID());
                var objName = Path.GetFileNameWithoutExtension(f);
                var tex2D = CreateRenderObjectPreview(obj);
                if (tex2D != null)
                {
                    var bytes = tex2D.EncodeToPNG();
                    var fullPath = Path.Combine(dstDirFull, $"{objName}.png");

                    File.WriteAllBytes(fullPath, bytes);
                }
                else
                {
                    Debug.Log($"{objName} is skip");
                }
            }
        }

        private bool TryDragAndDropAccept(Rect rect, Event e, out string[] dndObjects)
        {
            bool result = false;
            dndObjects = new string[0];
            // 範囲外
            if (!rect.Contains(e.mousePosition))
            {
                return false;
            }

            switch (e.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                    break;
                case EventType.DragPerform:
                    result = true;
                    // objのpathを返す
                    var refs = DragAndDrop.objectReferences.Select(o => AssetDatabase.GetAssetPath(o.GetInstanceID())).ToList();

                    dndObjects = refs.ToArray();

                    DragAndDrop.activeControlID = 0;
                    DragAndDrop.AcceptDrag();
                    Event.current.Use();

                    break;
            }


            return result;
        }

        private void OnGUI()
        {
            dstPath = EditorGUILayout.TextField(dstPath);
            var dstPathRect = GUILayoutUtility.GetLastRect();
            if (TryDragAndDropAccept(dstPathRect, Event.current, out var dplist))
            {
                dstPath = dplist[0];
            }

            EditorGUI.BeginDisabledGroup(dstPath == null || prefabs.Count < 1);
            if (GUILayout.Button("Generate"))
            {
                CreateThumbnailAll(dstPath, prefabs);
            }
            EditorGUI.EndDisabledGroup();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var obj in prefabs)
            {
                EditorGUILayout.ObjectField(obj, typeof(GameObject), false);
            }
            EditorGUILayout.EndScrollView();

            var r = GUILayoutUtility.GetLastRect();
            if (TryDragAndDropAccept(r, Event.current, out var assets))
            {
                foreach (var a in assets)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<GameObject>(a);
                    if (!prefabs.Contains(obj))
                    {
                        prefabs.Add(obj);
                    }
                    Debug.Log($"{a}");
                }
            }

            if (GUILayout.Button("clear"))
            {
                prefabs = new();
                Resources.UnloadUnusedAssets();
            }

        }
    }
}
