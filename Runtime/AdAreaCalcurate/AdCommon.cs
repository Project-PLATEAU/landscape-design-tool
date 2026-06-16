using UnityEngine;

namespace Landscape2.Runtime
{
    /*
     * 広告規制に使用する共通変数
     */
    public static class AdCommon
    {
        /* 右側のUIで値を入れるもの */
        // 選択された壁面
        public static GameObject SelectedWall;
        
        // 現在広告機能にアクセスしているか？
        public static bool IsCurrentUseAdSystem = false;
        
        /* 左側のUIで値を入れるもの */
        // 現在選択さている広告物
        public static AdSettings CurrentSelected;
    }
}
