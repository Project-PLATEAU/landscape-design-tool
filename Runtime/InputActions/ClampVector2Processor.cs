using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Landscape2.Runtime
{
    [System.Serializable]
    public class ClampVector2Processor : InputProcessor<Vector2>
    {
        public float minX;
        public float minY;
        public float maxX;
        public float maxY;
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return new Vector2(
                Mathf.Clamp(value.x, minX, maxX),
                Mathf.Clamp(value.y, minY, maxY)
            );
        }

#if UNITY_EDITOR
        // Unity Editor上でプロセッサを登録
        [InitializeOnLoadMethod]
        static void RegisterInEditor()
        {
            Debug.Log("RegisterInEditor");
            InputSystem.RegisterProcessor<ClampVector2Processor>("ClampVector2Processor");
        }
#else
    // ゲーム実行時にプロセッサを登録
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterInRuntime()
        {
            Debug.Log($"RegisterInRuntime");
            InputSystem.RegisterProcessor<ClampVector2Processor>("ClampVector2Processor");
        }    
#endif
    }


}
