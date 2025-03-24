using UnityEditor;

namespace LandscapeDesignTool.Editor
{
    [CustomEditor(typeof(LandscapeViewPoint))]
    public class LandScapeViewPointEditor : UnityEditor.Editor
    {
        public static LandScapeViewPointEditor Active;

        public LandscapeViewPoint Target => target as LandscapeViewPoint;
        
        public override void OnInspectorGUI()
        {
            Active = this;

            Target.gameObject.name = EditorGUILayout.TextField("視点場名", Target.gameObject.name);

            Target.Fov = EditorGUILayout.FloatField("視野角", Target.Fov);
            Target.Camera.fieldOfView = Target.Fov;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
