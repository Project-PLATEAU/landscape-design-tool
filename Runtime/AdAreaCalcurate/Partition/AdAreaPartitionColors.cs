using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 高さインデックスに対応する広告帯可視化用の共通カラーパレット。
    /// 固定配列。必要に応じて ScriptableObject 化 / 差し替え API 追加を検討する。
    /// </summary>
    public static class AdAreaPartitionColors
    {
        /// <summary>高さインデックス順の基本色配列。インデックスが範囲外の場合は modulo で循環。</summary>
        public static readonly Color[] HeightColors =
        {
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.red,
            Color.green,
            Color.blue,
            Color.gray,
            Color.white
        };

        /// <summary>
        /// インデックスに対応する色を取得 (HeightColors が空の場合は white)。
        /// アルファは呼び出し側で設定する。
        /// </summary>
        public static Color Get(int index)
        {
            var arr = HeightColors;
            if (arr == null || arr.Length == 0) return Color.white;
            if (index < 0) index = 0; // 最低 0
            return arr[index % arr.Length];
        }
    }
}
