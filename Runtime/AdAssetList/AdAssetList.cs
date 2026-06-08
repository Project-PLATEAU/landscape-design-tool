using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Landscape2.Runtime
{
    public class AdAssetList : ArrangementAssetData, ISubComponent
    {
        private IList<GameObject> assetList;
        private IList<Texture2D> thumbnailList;

        private Dictionary<string, Texture2D> thumbnailData = new();


        public class ListFilter
        {
            public bool IsEnable { get; set; } = false;
            public ListFilter()
            {
            }

            public IReadOnlyList<GameObject> Get(IList<GameObject> list)
            {
                // IsEnable が false の場合は、フィルタリングせずにそのまま返す
                if (!IsEnable)
                {
                    return list?.ToList().AsReadOnly();
                }

                // PlateauSandboxAdvertisementScaled コンポーネントを持つ GameObject のみをフィルタリング
                string[] table = new[] { "Advertisement_WallSignboard", "Advertisement_Billboard", "Advertisement_Billboard_Single" };
                return list?.Where(go => table.Contains(go.name))
                        .ToList().AsReadOnly();
                // return list?
                //     .Where(go => go.TryGetComponent<PlateauSandboxAdvertisementScaled>(out _))
                //     .ToList().AsReadOnly(); 
            }
        }

        public IReadOnlyList<GameObject> AssetList
        {
            get
            {
                return listFilter?.Get(assetList);
            }
        }

        public IReadOnlyList<Texture2D> ThumbnailList => thumbnailList?.ToList().AsReadOnly();

        // データが更新されたときに発行されるイベント
        public event System.Action OnDataUpdated;

        public bool IsEnableFilter
        {
            get => listFilter?.IsEnable ?? false;
            set
            {
                if (listFilter == null)
                {
                    return;
                }
                if (listFilter.IsEnable == value)
                {
                    return; // 変更がない場合は何もしない
                }

                listFilter.IsEnable = value;
                // フィルタが変更されたときにデータ更新イベントを発行
                OnDataUpdated?.Invoke();
            }
        }

        private AdAssetListUI ui;

        private ListFilter listFilter;

        public AdAssetList()
        {
            listFilter = new ListFilter();
        }

        public void LateUpdate(float deltaTime)
        {
        }

        public void OnDisable()
        {
        }

        public async void OnEnable()
        {
            // アセットのロード
            var result = await LoadPlateauAssets(ArrangementAssetType.Advertisement.GetKeyName());
            assetList = result.Item1;
            foreach (var thumb in result.Item2)
            {
                if (!thumbnailData.TryAdd(thumb.name, thumb))
                {
                    Debug.LogWarning($"[AdAssetList] Duplicate thumbnail key: {thumb.name}");
                }
            }
            thumbnailList = result.Item2;

            // データが更新されたことを通知
            OnDataUpdated?.Invoke();
        }

        public void Start()
        {
        }

        public void Update(float deltaTime)
        {
        }
    }
}
