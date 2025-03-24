namespace Landscape2.Runtime.Common
{
    /// <summary>
    /// 座標関連のユーティリティクラス
    /// </summary>
    public static class CoordinateUtils
    {
        public static bool IsValidLatitude(double latitude)
        {
            // 緯度の範囲は-90度から90度
            return latitude is >= -90 and <= 90;
        }
        
        public static bool IsValidLongitude(double longitude)
        {
            // 経度の範囲は-180度から180度
            return longitude is >= -180 and <= 180;
        }
    }
}