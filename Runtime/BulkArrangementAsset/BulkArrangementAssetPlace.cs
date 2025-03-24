using Landscape2.Runtime.Common;
using PlateauToolkit.Sandbox.Runtime;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Landscape2.Runtime
{
    public class BulkArrangementAssetPlace
    {
        private CancellationTokenSource cancellation;
        private PlateauSandboxPrefabPlacement prefabPlacement;
        
        public async Task<string> PlaceAll(BulkArrangementAsset bulkArrangementAsset)
        {
            prefabPlacement = new PlateauSandboxPrefabPlacement();
            if (!prefabPlacement.IsValidCityModel())
            {
                return "配置範囲内に3D都市モデルが存在しません";
            }
            
            if (bulkArrangementAsset.AssetTypes.All(n => n.PrefabConstantID < 0))
            {
                return "プレハブを設定してください";
            }
            
            // 作成するAssetの親オブジェクトを更新しておく
            prefabPlacement.SetParentObjectName("CreatedAssets");
            
            foreach (var placeData in bulkArrangementAsset.GetParsedData())
            {
                var assetItem = bulkArrangementAsset.AssetTypes.FirstOrDefault(n => n.CategoryName == placeData.AssetType);
                if (assetItem == null || assetItem.PrefabConstantID < 0)
                {
                    continue;
                }

                var prefab = ArrangementAssetLoader.GetAsset(assetItem.PrefabConstantID);
                if (prefab == null)
                {
                    continue;
                }

                try
                {
                    bool isIgnoreHeight = placeData.IsIgnoreHeight | bulkArrangementAsset.IsIgnoreHeight;
                    var context = new PlateauSandboxPrefabPlacement.PlacementContext()
                    {
                        m_Latitude = double.Parse(placeData.Latitude),
                        m_Longitude = double.Parse(placeData.Longitude),
                        m_Height = isIgnoreHeight ? 0 : float.Parse(placeData.Height),
                        m_Prefab = prefab,
                        m_IsIgnoreHeight = isIgnoreHeight,
                        m_IsPlaced = false,
                        // 判別できるようにプレハブ名とGUIDを結合して名前をつける
                        m_ObjectName = prefab.name + "_" + Guid.NewGuid().ToString("N"),
                    };
                    prefabPlacement.AddContext(context);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("配置情報の取得に失敗しました: " + e.Message);
                }
            }

            if (prefabPlacement.PlacingCount == 0)
            {
                return "配置情報の取得に失敗しました";
            }

            if (prefabPlacement.PlacementContexts.Any(context => 
                    !CoordinateUtils.IsValidLatitude(context.m_Latitude) ||
                    !CoordinateUtils.IsValidLongitude(context.m_Longitude)))
            {
                return "データの座標系が異なっています。\n緯度経度座標を持ったデータを入力してください。";
            }

            Debug.Log($"アセットの一括配置を開始しました。{prefabPlacement.PlacingCount}個の配置");

            if (cancellation == null)
            {
                cancellation = new CancellationTokenSource();
            }
            await prefabPlacement.PlaceAllAsync(cancellation.Token);

            // 空文字で返す
            return string.Empty;
        }

        public void Stop()
        {
            if (cancellation == null || prefabPlacement == null)
            {
                return;
            }
            
            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = null;
            prefabPlacement.StopPlace();
        }
        
        public bool IsAllPlaceSuccess()
        {
            return prefabPlacement.PlacementContexts.All(context => context.m_IsPlaced);
        }

        public bool IsAllPlaceFailed()
        {
            return prefabPlacement.PlacementContexts.All(context => !context.m_IsPlaced);
        }

        public void TrySetAssetList()
        {
            foreach (var context in prefabPlacement.PlacementContexts)
            {
                // 成功した分だけリストに追加
                if (!context.m_IsPlaced)
                {
                    continue;
                }

                var target = GameObject.Find(context.m_ObjectName);
                if (target == null)
                {
                    Debug.LogWarning($"配置したアセットが見つかりませんでした。{context.m_ObjectName}");
                    continue;
                }
                // リストに通知
                ArrangementAssetListUI.OnCreatedAsset.Invoke(target);
            }
        }
    }
}