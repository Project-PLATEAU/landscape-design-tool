using System.Runtime.InteropServices;
using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// AdAreaCalcを有効化するためのダミーUI処理
    /// </summary>
    public class AdAreaActionEnabler : MonoBehaviour
    {
        private static AdAreaActionEnabler instance = null;

        public static AdAreaActionEnabler Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject
                    {
                        name = nameof(AdAreaActionEnabler)
                    };
                    instance = go.AddComponent<AdAreaActionEnabler>();
                }
                return instance;
            }
        }

        private bool isActive = false;
        public bool IsActive => isActive;

        public System.Action OnEnableCallback;
        public System.Action OnDisableCallback;

        public System.Action OnEnableOneWallAreaSelectCallback;
        public System.Action OnEnableAllWallAreaSelectCallback;

        public System.Action<float> OnWallAreaValueChangeCallback;

        public float WallArea { get; set; }

        /// <summary>
        /// 総壁面面積
        /// </summary>
        float allWallAdPanelArea;

#if false

        float wallAdPanelSize;

        string inputText = "";
        void OnGUI()
        {
            if (GUILayout.Button($"AdAreaCalc : {!isActive}"))
            {
                isActive = !isActive;
                if (isActive)
                {
                    OnEnableCallback?.Invoke();
                }
                else if (!isActive)
                {
                    OnDisableCallback?.Invoke();
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1壁面"))
            {
                OnEnableOneWallAreaSelectCallback?.Invoke();
            }
            if (GUILayout.Button("総壁面"))
            {
                OnEnableAllWallAreaSelectCallback?.Invoke();
            }
            GUILayout.EndHorizontal();


            GUILayout.Label($"壁面面積 : {WallArea:F2}m^2");

            GUILayout.BeginHorizontal();
            GUILayout.Label("総壁面広告サイズ: ");
            var text = GUILayout.TextField(inputText, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            if (inputText != text)
            {
                if (float.TryParse(text, out wallAdPanelSize))
                {
                    inputText = text;
                }
                else
                {
                    inputText = "";
                }
            }

        }
#endif
    }
}
