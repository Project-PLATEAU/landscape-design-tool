using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace Landscape2.Runtime.Common
{
    public static class UIUtil
    {
        /// <summary>
        /// UIDocumentの要素上での操作かどうかを判定する
        /// 注意　要素の形状によって判定が変化することがある。例えばテクスチャで色が付いていない部分も含めて判定されることに注意
        /// PickModeがignoreの要素は無視する
        /// </summary>
        /// <param name="mousePos"></param>
        /// <returns></returns>
        public static bool IsPointerOverUI(Vector2 mousePos, VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("Root element is not set.");
                return false;
            }


            var uiPos = ConvertToUIPosition(mousePos);

            var picked = root?.panel.Pick(uiPos);
            return picked != null;

        }

        public static Vector2 ConvertToUIPosition(Vector2 mousePos)
        {
            // UnityのUI座標系は左下原点なので、Y座標を反転
            mousePos.y = Screen.height - mousePos.y;
            return mousePos;
        }


    }
}
