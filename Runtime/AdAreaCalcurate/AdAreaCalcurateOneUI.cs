using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 1壁面広告領域計算用UI
    /// </summary>
    public class AdAreaCalcurateOneUI
    {
        enum HeightOffset
        {
            Top,
            Bottom
        }

        // マンセル表表示ボタン

        // 設置位置高さInputField / IN,OUT
        private float adYPosition
        {
            get => adPosition.y;
            set => adPosition = new Vector3(adPosition.x, value, adPosition.z);
        }

 
        // 壁面広告の位置
        private Vector3 adPosition;

        // 1壁面情報
        private Bounds wallBounds;






        public AdAreaCalcurateOneUI(VisualElement rightContainer)
        {


        }

        public void Bind(VisualElement element)
        {

        }

#if UNITY_EDITOR
        public static VisualElement UIFactory()
        {
            var vte = Resources.Load<VisualTreeAsset>("AdRegulation");

            var rootElement = vte.CloneTree();

            var mainContainer = rootElement.Q<VisualElement>("MainContainer");
            var leftContainer = rootElement.Q<VisualElement>("LeftContainer");

            var rightContainer = rootElement.Q<VisualElement>("RightContainer");

            return rightContainer;
        }
#endif

    }
}
