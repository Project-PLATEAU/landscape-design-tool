using System;
using UnityEngine;

namespace Landscape2.Runtime.Common
{
    public class MansellColorUtil
    {
        // 色相帯の順序
        private static readonly string[] HueBands =
            { "R", "YR", "Y", "GY", "G", "BG", "B", "PB", "P", "RP" };

        /// <summary>
        /// Munsellの各要素（Hue数値, 色相帯, Value, Chroma）を受け取りColorを返す
        /// </summary>
        /// <param name="hueNumber">Hueの数値部分 (0-10)</param>
        /// <param name="hueBand">色相帯 ("R","YR","Y","GY","G","BG","B","PB","P","RP")</param>
        /// <param name="value">Value (明度, 1-10)</param>
        /// <param name="chroma">Chroma (彩度, 通常0-14)</param>
        /// <param name="maxChroma">S化の最大Chroma (デフォルト14f)</param>
        /// <returns>対応するUnityEngine.Color</returns>
        public static Color MunsellToColor(
            float hueNumber,
            string hueBand,
            float value,
            float chroma,
            float maxChroma = 14f)
        {
            // 色相帯インデックス
            int bandIndex = Array.IndexOf(HueBands, hueBand.ToUpper());
            if (bandIndex < 0)
            {
                Debug.LogError("不明な色相帯");
                return Color.white;
            }

            // Hue → 360度にマッピング (100ステップ × 3.6度)
            float totalSteps = bandIndex * 10f + hueNumber;
            float hueDeg = (totalSteps % 100f) * 3.6f;
            float h = Mathf.Repeat(hueDeg / 360f, 1f);

            // Value → V (0-1)
            float v = Mathf.Clamp01(value / 10f);

            // Chroma → S (0-1)
            float s = Mathf.Clamp01(chroma / maxChroma);

            // HSV → RGB
            return Color.HSVToRGB(h, s, v);
        }


        /// <summary>
        /// 対象のGameObject（子含む）にアタッチされているマテリアルやレンダラーのカラーを一括変更する。
        /// </summary>
        /// <param name="target">カラーを変更したいGameObject</param>
        /// <param name="color">設定する色</param>
        /// <param name="includeChildren">子オブジェクトも含めるか</param>
        public static void SetBaseColor(GameObject target, Color color, bool includeChildren = true)
        {
            if (target == null)
                return;

            // 3DオブジェクトのRenderer
            var renderers = includeChildren
                ? target.GetComponentsInChildren<Renderer>()
                : target.GetComponents<Renderer>();

            foreach (var rend in renderers)
            {
                // materialを経由するとランタイムでインスタンスが生成される
                var mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    // URP/Lit ShaderやHDRPのベースカラー
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", color);
                    }
                    // Standard Shaderのカラー
                    else if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", color);
                    }
                }
                rend.materials = mats;
            }

        }

    }
}
