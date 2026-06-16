using System;
using UnityEngine;
using UnityEngine.UIElements;
using Landscape2.Runtime.Common;

namespace Landscape2.Runtime
{

    public interface IAdAssetListUI
    {
        /// <summary>
        /// アセット選択時
        /// </summary>
        /// <returns></returns>
        event System.Action<GameObject> OnSelectAsset;

        /// <summary>
        /// アセット配置時
        /// </summary>
        /// <param name="asset"></param>
        event System.Action<GameObject> OnPutAsset;

        event System.Action OnSelectAssetCancel;
    }


    public class AdAssetListUI : IDisposable, ISubComponent, IAdAssetListUI
    {
        VisualElement uiRoot;
        VisualElement assetListTitleRoot;
        VisualElement cloneElement;

        VisualElement listElement;
        VisualElement buttonContainer; // ボタンコンテナを保持

        Button thumbnailButton;

        // bindingされるinterface

        // アイテムがクリックされた
        private System.Action<GameObject> onClickedItem;

        //System.Action<bool> OnCheckBoxUpdate;
        //System.Action<bool> OnCheckBoxUpdate;

        // ArrangementAssetの機能を再利用するためのフィールド
        private CreateMode createMode;
        private Camera cam;
        private Ray ray;
        private GameObject currentPrefab;
        private GameObject generatedAsset;
        private AssetPlacedDirectionComponent component;
        private bool isMouseOverUI;
        private Vector3? assetSize = null;
        private float buriedHeight = 0.0f;

        // AdAssetListへの参照を保持
        private AdAssetList adAssetList;

        private FilterView filterView;

        public event Action<GameObject> OnSelectAsset;
        public event Action<GameObject> OnPutAsset;

        public event Action OnSelectAssetCancel;

        /// <summary>
        /// アセット配置モードが有効かどうか
        /// </summary>
        public bool IsAssetPlacementMode => generatedAsset != null;

        public AdAssetListUI(VisualElement rootElement, VisualElement assetListTitleRoot, AdAssetList adAssetList)
        {
            uiRoot = rootElement;
            this.assetListTitleRoot = assetListTitleRoot;

            // CreateModeの初期化
            createMode = new CreateMode();

            // AdAssetListへの参照を保存
            this.adAssetList = adAssetList;

            filterView = new FilterView(rootElement.Q<Toggle>("FilterToggle"));

            if (this.adAssetList != null)
            {
                // AdAssetListのデータ更新イベントにサブスクライブ
                this.adAssetList.OnDataUpdated += UpdateButtons;

                // 状態の同期
                this.adAssetList.IsEnableFilter = filterView.IsEnable;

                // チェックボックスの状態が変わったときにフィルタを更新
                filterView.OnCheckBoxUpdate += isChecked =>
                {
                    // チェックボックスの状態が変わったときにフィルタを更新
                    this.adAssetList.IsEnableFilter = isChecked;
                };
            }

            buttonContainer = rootElement.Q<VisualElement>("unity-content-container");
            UpdateButtons();
        }

        /// <summary>
        /// ボタンリストを更新
        /// AdAssetListのThumbnailListが更新されたときに呼び出される
        /// </summary>
        public void UpdateButtons()
        {
            if (buttonContainer == null || adAssetList == null) return;

            // 既存のボタンをクリア
            buttonContainer.Clear();

            // アセットリストとサムネイルリストが両方存在する場合のみ処理
            if (adAssetList.AssetList != null && adAssetList.ThumbnailList != null)
            {
                foreach (var asset in adAssetList.AssetList)
                {
                    // 対応するサムネイルを検索
                    Texture2D thumbnail = null;
                    foreach (var thumb in adAssetList.ThumbnailList)
                    {
                        if (thumb.name == asset.name)
                        {
                            thumbnail = thumb;
                            break;
                        }
                    }

                    // サムネイルが見つからない場合はデフォルトのサムネイル（null）を使用
                    var button = CreateThumbnailButton(asset.name, asset, thumbnail);
                    buttonContainer.Add(button);
                }
            }
        }

        public void Show()
        {
            uiRoot.style.display = DisplayStyle.Flex;
            assetListTitleRoot.style.display = DisplayStyle.Flex;
        }

        public void HideAndCancel()
        {
            uiRoot.style.display = DisplayStyle.None;
            assetListTitleRoot.style.display = DisplayStyle.None;
            CancelAssetPlacement();
        }

        /// <summary>
        /// サムネイルボタンを作成
        /// ArrangementAssetUI.CreateButton()の内容をcopyしてきた。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefab"></param>
        /// <param name="thumbnail"></param>
        /// <returns></returns>
        Button CreateThumbnailButton(string name, GameObject prefab, Texture2D thumbnail)
        {
            Button newButton = new Button()
            {
                name = "Thumbnail_Asset" // ussにスタイルが指定してある
            };

            newButton.style.width = Length.Percent(30f);

            newButton.style.backgroundImage = new StyleBackground(thumbnail);
            newButton.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            newButton.style.backgroundColor = Color.clear;

            newButton.AddToClassList("AssetButton");
            newButton.clicked += () =>
            {
                onClickedItem?.Invoke(prefab);
                // アセットがクリックされた際に配置モードを開始
                StartAssetPlacement(prefab);
            };

            return newButton;
        }

        /// <summary>
        /// アセット配置モードを開始
        /// CreateModeの機能を再利用
        /// </summary>
        /// <param name="assetPrefab"></param>
        private void StartAssetPlacement(GameObject assetPrefab)
        {
            // 既存のアセットがあれば削除
            if (generatedAsset != null)
            {
                GameObject.Destroy(generatedAsset);
                generatedAsset = null;
            }

            // アセットを生成
            GenerateAsset(assetPrefab);
        }

        /// <summary>
        /// アセットを生成（CreateMode.generateAssets()の機能を再利用）
        /// </summary>
        /// <param name="obj"></param>
        private void GenerateAsset(GameObject obj)
        {
            cam = Camera.main;
            ray = cam.ScreenPointToRay(Input.mousePosition);

            if (isMouseOverUI && generatedAsset != null)
            {
                // UI上にマウスがある場合はアセットを削除
                GameObject.Destroy(generatedAsset);
                assetSize = null;
            }

            if (Common.LandscapeRaycast.Raycast(ray, out RaycastHit hit))
            {
                GameObject parent = GameObject.Find("CreatedAssets");
                if (parent == null)
                {
                    parent = new GameObject("CreatedAssets");
                }
                generatedAsset = GameObject.Instantiate(obj, hit.point, Quaternion.identity, parent.transform) as GameObject;
                currentPrefab = obj;
            }
            else
            {
                GameObject parent = GameObject.Find("CreatedAssets");
                if (parent == null)
                {
                    parent = new GameObject("CreatedAssets");
                }
                generatedAsset = GameObject.Instantiate(obj, Vector3.zero, Quaternion.identity, parent.transform) as GameObject;
                currentPrefab = obj;
            }

            // LODGroupによってカリングされないないように
            LandscapeToolAssetUtil.DisableLodGroup(generatedAsset);

            assetSize = GetGameObjectSize(generatedAsset);

            generatedAsset.name = obj.name;
            int generateLayer = LayerMask.NameToLayer("Ignore Raycast");
            SetLayerRecursively(generatedAsset, generateLayer);

            // アセット生成時に必要コンポーネント付与
            AssetPlacedDirectionComponent.TryAdd(generatedAsset);
            component = generatedAsset.GetComponent<AssetPlacedDirectionComponent>();

            OnSelectAsset?.Invoke(generatedAsset);
        }

        /// <summary>
        /// アセット配置の更新処理
        /// </summary>
        public void UpdateAssetPlacement()
        {
            if (generatedAsset != null)
            {
                cam = Camera.main;
                ray = cam.ScreenPointToRay(Input.mousePosition);
                int layerMask = LayerMask.GetMask("Ignore Raycast");
                layerMask = ~layerMask;
                if (LandscapeRaycast.Raycast(ray, out RaycastHit hit, layerMask))
                {
                    var point = hit.point;
                    point.y += buriedHeight;
                    generatedAsset.transform.position = point;
                    if (component != null)
                    {
                        component.setPlaceNormal = hit.normal;
                    }
                }
            }
        }

        /// <summary>
        /// アセット配置の確定
        /// </summary>
        public void ConfirmAssetPlacement()
        {
            if (generatedAsset != null)
            {
                // アセット作成通知
                ArrangementAssetListUI.OnCreatedAsset?.Invoke(generatedAsset);
                OnPutAsset?.Invoke(generatedAsset);

                if (generatedAsset.TryGetComponent(out component))
                {
                    // 配置完了
                    component.SetPlaced();
                }

                // プロジェクトに通知
                ProjectSaveDataManager.Add(ProjectSaveDataType.Asset, generatedAsset.gameObject.GetInstanceID().ToString());

                SetLayerRecursively(generatedAsset, 0);

                // 配置モードを終了
                generatedAsset = null;
                CancelAssetPlacement(false);

                // 次のアセットを配置モードにする
                onClickedItem?.Invoke(currentPrefab);
                StartAssetPlacement(currentPrefab);
            }
        }

        /// <summary>
        /// アセット配置のキャンセル
        /// </summary>
        public void CancelAssetPlacement(bool callInvokeEvent = true)
        {
            if (generatedAsset != null)
            {
                GameObject.Destroy(generatedAsset);
                generatedAsset = null;
            }
            assetSize = null;

            if (callInvokeEvent)
            {
                OnSelectAssetCancel?.Invoke();
            }
        }

        /// <summary>
        /// レイヤーを再帰的に設定
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="newLayer"></param>
        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null)
                return;

            obj.layer = newLayer;

            foreach (Transform child in obj.transform)
            {
                if (child == null)
                    continue;

                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        /// <summary>
        /// GameObjectのサイズを取得
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private Vector3 GetGameObjectSize(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("GameObject が null です。");
                return Vector3.zero;
            }

            // GameObject 内のすべての Renderer を取得
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogWarning("Renderer が見つかりません。");
                return Vector3.zero;
            }

            // 初期化：最初の Renderer の Bounds を基準にする
            Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool initialized = false;

            foreach (var renderer in renderers)
            {
                // 各 Renderer の Bounds を取得
                Bounds localBounds = renderer.bounds;

                // ワールド座標系でのスケールを適用
                Vector3 worldMin = renderer.transform.TransformPoint(localBounds.min);
                Vector3 worldMax = renderer.transform.TransformPoint(localBounds.max);

                // スケール適用後の Bounds を作成
                Bounds worldBounds = new Bounds();
                worldBounds.SetMinMax(worldMin, worldMax);

                // 統合
                if (!initialized)
                {
                    combinedBounds = worldBounds;
                    initialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(worldBounds);
                }
            }

            // 統合された Bounds のサイズを返す
            return combinedBounds.size;
        }

        /// <summary>
        /// アセット配置の入力処理
        /// </summary>
        private void HandleAssetPlacementInput()
        {
            if (!IsAssetPlacementMode) return;

            // マウス左クリックでアセット配置を確定
            if (Input.GetMouseButtonDown(0) && CameraMoveByUserInput.IsMouseActive)
            {
                ConfirmAssetPlacement();
            }

            // 右クリックでアセット配置をキャンセル
            if (Input.GetMouseButtonDown(1))
            {
                CancelAssetPlacement();
            }
        }

        public void Dispose()
        {
            // 配置中のアセットがあれば削除
            CancelAssetPlacement();

            // イベントの購読を解除
            if (adAssetList != null)
            {
                adAssetList.OnDataUpdated -= UpdateButtons;
            }

            cloneElement.RemoveFromHierarchy();
            cloneElement = null;
        }

        public void Update(float deltaTime)
        {
            adAssetList?.Update(deltaTime);

            UpdateAssetPlacement();

            HandleAssetPlacementInput();
        }

        public void LateUpdate(float deltaTime)
        {
            adAssetList?.LateUpdate(deltaTime);
        }

        public void OnEnable()
        {
            adAssetList?.OnEnable();
        }

        public void OnDisable()
        {
            adAssetList?.OnDisable();
        }

        public void Start()
        {
            adAssetList?.Start();
        }

        private class FilterView
        {
            // 絞り込みチェックボックスが更新された
            public event Action<bool> OnCheckBoxUpdate;

            // チェックボックスの状態を取得
            public bool IsEnable { get => filterToggle.value; }

            private Toggle filterToggle;

            public FilterView(VisualElement root)
            {
                filterToggle = root.Q<Toggle>("FilterToggle");
                filterToggle.RegisterValueChangedCallback(evt =>
                {
                    // チェックボックスの状態が変わったときにイベントを発行
                    OnCheckBoxUpdate?.Invoke(evt.newValue);
                });
            }
        }
    }
}
