using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 建物高さ表示のピン情報
    /// </summary>
    public class VisualHeightPin
    {
        public string GmlID { get; set; }
        public Bounds Bounds { get; set; }
        public float Height { get; set; }
        public VisualElement Pin { get; set; }

        public void SetDisplay(float height)
        {
            Pin.style.display = height > Height ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public bool IsVisible()
        {
            return Pin.style.display != DisplayStyle.None;
        }
    }
}