using UnityEngine.UIElements;

namespace Landscape2.Runtime.WalkerMode
{
    /// <summary>
    /// 歩行者モード時の方位の表示プレゼンター
    /// </summary>
    public class WalkerModeOrientationUI
    {
        private VisualElement root;
        private WalkerMode walkerMode;
        
        public WalkerModeOrientationUI(VisualElement parent, WalkerMode walkerMode)
        {
            this.walkerMode = walkerMode;
            root = parent.Q<VisualElement>("AngleController");
            RegisterEvents();
        }
        
        private void RegisterEvents()
        {
            var upButton = root.Q<Button>("Rotate_Up");
            upButton.clicked += () => walkerMode.RotateWalker(WalkerOrientationType.Up);
            
            var leftButton = root.Q<Button>("Rotate_Left");
            leftButton.clicked += () => walkerMode.RotateWalker(WalkerOrientationType.Left);
            
            var rightButton = root.Q<Button>("Rotate_Right");
            rightButton.clicked += () => walkerMode.RotateWalker(WalkerOrientationType.Right);
            
            var downButton = root.Q<Button>("Rotate_Down");
            downButton.clicked += () => walkerMode.RotateWalker(WalkerOrientationType.Down);
        }

        public void Show(bool isShow)
        {
            root.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}