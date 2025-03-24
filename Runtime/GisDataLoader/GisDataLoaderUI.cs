using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// GISデータをロードして表示するプレゼンター
    /// </summary>
    public class GisDataLoaderUI
    {
        // UI
        private GisLoadUI gisLoadUI;
        private GisPointListUI gisPointListUI;
        private GisPointPinsUI gisPointPinsUI;
        
        // カメラが移動したか
        private bool isCameraMoved = false;

        public GisDataLoaderUI(VisualElement gisElement, SaveSystem saveSystem)
        {
            // ローダー
            var gisDataLoader = new GisLoader(); 
            
            // モデル
            var gisPointInfos = new GisPointInfos();
            
            // 各UIを初期化
            gisLoadUI = new GisLoadUI(gisDataLoader, gisPointInfos, gisElement);
            gisPointListUI = new GisPointListUI(gisPointInfos, gisElement);
            gisPointPinsUI = new GisPointPinsUI(gisPointInfos);

            // セーブシステム
            var gisSaveSystem = new GisDataSaveSystem(saveSystem, gisPointInfos);
            
            // カメラ変更監視
            CameraMoveByUserInput.OnCameraMoved.AddListener(() =>
            {
                // ピンの更新
                gisPointPinsUI?.OnUpdate();
            });
        }
    }
}