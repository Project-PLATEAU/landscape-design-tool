using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 配置したアセットの一覧のモデル
    /// </summary>
    public class ArrangementAssetListItem
    {
        public int PrefabID { get; private set; }
        public VisualElement Element { get; private set; }
        public ArrangementAssetType Type { get; private set; }
        
        public ArrangementAssetListItem(
            int prefabID,
            VisualElement element,
            ArrangementAssetType type)
        {
            PrefabID = prefabID;
            Element = element;
            Type = type;
        }
    }
}