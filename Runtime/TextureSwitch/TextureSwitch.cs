using Landscape2.Runtime.Common;
using Landscape2.Runtime.DynamicTile;
using PLATEAU.CityInfo;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 建物のテクスチャを切り替える機能
    /// </summary>
    public class TextureSwitch : ISubComponent
    {
        private bool isTextureNull = false;
        
        // テクスチャを保存するリスト
        private readonly List<TextureSwitchBuilding> dataList = new List<TextureSwitchBuilding>();
        
        public TextureSwitch(VisualElement uiRoot, DynamicTileRefDataUpdater  dynamicTileRefDataUpdater)
        {
            var switchToggle = uiRoot.Q<Toggle>("Toggle_Material");
            switchToggle.RegisterValueChangedCallback((evt) =>
            {
                isTextureNull = evt.newValue;
                foreach (var textureSwitchData in dataList)
                {
                    if (isTextureNull != textureSwitchData.IsTextureHidden)
                    {
                        textureSwitchData.SetTextureVisibility(!isTextureNull);
                    }
                }
            });

            // イベント登録
            RegisterDynamicTileEvent(dynamicTileRefDataUpdater);
        }

        /// <summary>
        /// 動的タイル用のイベント登録
        /// </summary>
        private void RegisterDynamicTileEvent(INotifyUpdated notifyUpdated)
        {

            // unloadされた建物の参照を削除
            var beforeUnloadFileter = notifyUpdated.FindFromBeforeUnloadFileter<BuildingGroupFileter>();
            beforeUnloadFileter.EvUpdated += (obj) =>
            {
                // 参照切れを検索して削除
                var idx = dataList.FindIndex((d) => d.IsSameBuilding(obj.gameObject));
                if (idx < 0)
                {
                    Debug.LogError("TextureSwitch: 該当する建物データが見つかりませんでした。");
                    return;
                }
                dataList.RemoveAt(idx);
            };

            // instantiateされた建物を登録
            var instantiatedFileter = notifyUpdated.FindFromInstantiatedFileter<BuildingGroupFileter>();
            instantiatedFileter.EvUpdated += (obj) =>
            {
                RegisterBuilding(obj);
            };
        }

        private void RegisterBuilding(GameObject building)
        {
            var cityObjectGroup = building.GetComponent<PLATEAUCityObjectGroup>();
            if (cityObjectGroup == null)
            {
                Debug.LogError("PLATEAUCityObjectGroup component is missing on the building GameObject.");
                return;
            }
            RegisterBuilding(cityObjectGroup);
        }

        /// <summary>
        /// 建物をテクスチャスイッチデータとして登録する
        /// </summary>
        /// <param name="buildings">登録する建物</param>
        private void RegisterBuilding(PLATEAUCityObjectGroup building)
        {
            var meshRenderer = building.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogError("建物にMeshRendererがアタッチされていない");
                return;
            }
            var switchBuilding = new TextureSwitchBuilding(meshRenderer);
            dataList.Add(switchBuilding);

            // トグルがONの場合のみテクスチャを非表示に
            if (isTextureNull)
            {
                switchBuilding.SetTextureVisibility(false);
            }
        }

        public void Start()
        {
        }
        public void Update(float deltaTime)
        {
        }
        public void OnEnable()
        {
        }
        public void OnDisable()
        {
        }
        public void LateUpdate(float deltaTime)
        {
        }
    }
}
