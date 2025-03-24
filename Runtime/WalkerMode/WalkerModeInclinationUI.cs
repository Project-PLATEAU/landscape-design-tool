using UnityEngine.UIElements;

namespace Landscape2.Runtime.WalkerMode
{
    /// <summary>
    ///  歩行者モード時の仰角の表示プレゼンター
    /// </summary>
    public class WalkerModeInclinationUI
    {
        private Label angleValue;

        private WalkerMode walkerMode;
        private VisualElement root;
        
        public WalkerModeInclinationUI(VisualElement parent, WalkerMode walkerMode)
        {
            this.walkerMode = walkerMode;
            root = parent.Q<VisualElement>("container_inclination");
            angleValue = root.Q<Label>("angle");
        }
        
        public void Show(bool isShow)
        {
            root.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Update(float deltaTime)
        {
            if (!walkerMode.IsWalkerMode())
            {
                return;
            }
            angleValue.text = walkerMode.GetInclination().ToString("F1");
        }
    }
}