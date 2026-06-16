using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    public interface IAdRegulationView
    {
        public event System.Action<bool> OnDisplay;
    }

    public class AdRegulationView : IAdRegulationView
    {
        // この機能の一番上の親　タブ変更による表示非表示の切り替えはこのエレメントに行われる
        VisualElement origialRoot;

        // AdRegulationと名前が付いている要素
        VisualElement root;

        AdControllerView adController;

        // タブが開かれた、閉じた時のイベント IAdRegulationView
        public event System.Action<bool> OnDisplay;

        public AdRegulationView(VisualElement origialRoot, VisualElement root, AdControllerView adController)
        {
            this.origialRoot = origialRoot;
            this.root = root;
            this.adController = adController;
            
            root.RegisterCallback<GeometryChangedEvent>((e) => UICallBack.OnDisplay(this, e));
        }

        private static class UICallBack
        {
            /// <summary>
            /// 交通規制タブが開かれた時の処理
            /// </summary>
            /// <param name="e"></param>
            public static void OnDisplay(AdRegulationView self, GeometryChangedEvent e)
            {
                // 表示される このタブに移行した
                if (self.origialRoot.resolvedStyle.display == DisplayStyle.Flex)
                {
                    //Debug.Log("DisplayStyle.Flex AdRegulationView");
                    self.OnDisplay?.Invoke(true);
                    self.adController.CallbackOnDisplay(true);
                }
                else // 非表示 別のタブに移行した
                {
                    //Debug.Log("DisplayStyle.None AdRegulationView");
                    self.OnDisplay?.Invoke(false);
                    self.adController.CallbackOnDisplay(false);
                }
            }
        }
    }
}
