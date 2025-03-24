using UnityEngine.UIElements;

namespace Landscape2.Runtime.GisDataLoader
{
    public class GisPointPin
    {
        // ピンのIndex
        public int PointID { get; private set; }

        // 属性のID
        public string AttributeID { get; private set; }
        
        // ピンの要素
        public VisualElement Element { get; private set; }
        
        public GisPointPin(int pointID, string attributeID, VisualElement element)
        {
            PointID = pointID;
            AttributeID = attributeID;
            Element = element;
        }
    }
}