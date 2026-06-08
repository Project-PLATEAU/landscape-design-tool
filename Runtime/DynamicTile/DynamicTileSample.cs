using UnityEngine;
using PLATEAU.CityInfo;

namespace Landscape2.Runtime.DynamicTile
{
    public class DynamicTileSample
    {
        public void Test(DynamicTileRefDataUpdater updater)
        {
            var notifyUpdated = updater as INotifyUpdated;
            Debug.Assert(notifyUpdated != null);

            // 実行サンプル
            notifyUpdated.FindFromInstantiatedFileter<RootGroupFilter>().EvUpdated += (o) =>
            {
                Debug.Log($"Instantiated : {o.name}");
            };

            notifyUpdated.FindFromBeforeUnloadFileter<RootGroupFilter>().EvUpdated += (o) =>
            {
                Debug.Log($"BeforeUnload : {o.name}");
            };

            // テスト実行
            var testUpdater = updater as IDynamicTileUpdater;
            var t = GameObject.FindFirstObjectByType<PLATEAUCityObjectGroup>();
            if (t != null)
            {
                testUpdater.OnTileInstantiated(t.gameObject);
                testUpdater.OnBeforeTileUnload(t.gameObject);
            }

        }

    }
}
