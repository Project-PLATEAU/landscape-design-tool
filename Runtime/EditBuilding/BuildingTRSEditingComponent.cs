using Landscape2.Runtime.DynamicTile;
using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 建物のTRSを編集用コンポーネント
    /// </summary>
    public class BuildingTRSEditingComponent : MonoBehaviour
    {
        private class Inherit : DynamicTileGameObject.IInherit
        {
            public bool IsShow { get; set; }
            void DynamicTileGameObject.IInherit.Init(GameObject newGameObject)
            {
                var trs = BuildingTRSEditingComponent.TryGetOrCreate(newGameObject);
                if (trs != null)
                {
                    trs.ShowBuilding(IsShow);
                }
                
            }
        }

        public static BuildingTRSEditingComponent TryGetOrCreate(DynamicTileGameObject target)
        {
            if (target.TryGetRawComponent<BuildingTRSEditingComponent>(out var component))
            {
                return component;
            }
            return target.AddRawComponent<BuildingTRSEditingComponent>();
        }

        public static BuildingTRSEditingComponent TryGetOrCreate(GameObject target)
        {
            if (target.TryGetComponent<BuildingTRSEditingComponent>(out var component))
            {
                return component;
            }
            return target.AddComponent<BuildingTRSEditingComponent>();
        }
        
        // オリジナルのTransformを保持
        private Vector3 originalPosition;
        private Vector3 originalRotation;
        private Vector3 originalScale;
        
        // 編集中のGameObject
        private GameObject editingObject;

        Inherit dynamicTileInherit;
        private MeshRenderer meshRenderer;
        
        private bool isShow = true;
        public bool IsShow => isShow;


        private void Awake()
        {
            // オリジナルのTransformを保持
            originalPosition = transform.position;
            originalRotation = transform.eulerAngles;
            originalScale = transform.localScale;

            // MeshColliderからオリジナルのメッシュを取得
            // デフォルトがCombine Meshのため、Transformの変更ができないため
            var meshCollider = GetComponent<MeshCollider>();
            Mesh originalMesh = null;
            if (meshCollider != null)
            {
                originalMesh = meshCollider.sharedMesh;
            }

            // 動的タイルGameObjectに継承される設定を行う
            var dGameObject = DynamicTileGameObjectUpdater.CreateOrGet(gameObject);
            if (dGameObject.GetOrAddInit<Inherit>(nameof(BuildingTRSEditingComponent), out dynamicTileInherit))
            {
                // 初めて追加するので初期化する
                dynamicTileInherit.IsShow = isShow;
            }

            meshRenderer = GetComponent<MeshRenderer>();

            CreateEditingObject(originalMesh);
        }
        
        private void CreateEditingObject(Mesh originalMesh)
        {
            editingObject = new GameObject("EditingObject");
            editingObject.transform.SetParent(transform);

            // 編集可能なオリジナルのメッシュを付与
            editingObject.AddComponent<MeshFilter>().sharedMesh = originalMesh;
            var editingRenderer = editingObject.AddComponent<MeshRenderer>();
            editingRenderer.sharedMaterials =  GetComponent<MeshRenderer>().sharedMaterials;
            
            // defaultでは非表示
            editingObject.SetActive(false);
        }

        public void ShowBuilding(bool isShow)
        {
            this.isShow = isShow;
            
            // 両方とも表示/非表示
            meshRenderer.enabled = isShow;
            dynamicTileInherit.IsShow = meshRenderer.enabled;
            editingObject.SetActive(isShow);

            if (isShow)
            {
                // 表示時は編集中かどうかチェック
                TrySetEditingMode();
            }
        }

        public void SetPosition(Vector3 position)
        {
            if (!isShow)
            {
                return;
            }

            transform.position = position;
            TrySetEditingMode();
        }
        
        public void SetRotation(Vector3 rotation)
        {
            if (!isShow)
            {
                return;
            }
            
            transform.eulerAngles = rotation;
            TrySetEditingMode();
        }
        
        public void SetScale(Vector3 scale)
        {
            if (!isShow)
            {
                return;
            }
            
            transform.localScale = scale;
            TrySetEditingMode();
        }

        private void TrySetEditingMode()
        {
            if (!IsEditing())
            {
                // 戻す
                EnableEditing(false);
                return;
            }
            
            EnableEditing(true);
        }

        private void EnableEditing(bool isEnable)
        {
            editingObject.SetActive(isEnable);

            // オリジナルのメッシュは非表示
            if (meshRenderer != null)
            {
                meshRenderer.enabled = !isEnable;
                dynamicTileInherit.IsShow = meshRenderer.enabled;
            }
        }
        
        private bool IsEditing()
        {
            return transform.position != originalPosition ||
                   transform.eulerAngles != originalRotation ||
                   transform.localScale != originalScale;
        }
        
        public void Reset()
        {
            transform.position = originalPosition;
            transform.eulerAngles = originalRotation;
            transform.localScale = originalScale;
            
            EnableEditing(false);
        }
    }
}