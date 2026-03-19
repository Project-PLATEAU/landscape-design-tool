using Landscape2.Runtime.DynamicTile;
using UnityEngine;

namespace Landscape2.Runtime.Common
{
    public static class RendererUtil
    {
        /// <summary>
        /// Boundsを計算
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError("指定されたオブジェクトにRendererが存在しません。");
                return new Bounds(obj.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        public static Bounds CalculateBounds(DynamicTileGameObject obj)
        {
            // 内部で参照を保持していないので CurrentGameObjectで直ぐには問題にならないはず。CurrentGameObjectの更新前後でBoundsが大きく変わらないならおそらく大丈夫
            return CalculateBounds(obj.CurrentGameObject);
        }
    }
}