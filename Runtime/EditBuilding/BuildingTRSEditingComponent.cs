using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 建物のTRSを編集用コンポーネント
    /// </summary>
    public class BuildingTRSEditingComponent : MonoBehaviour
    {
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
        
        private MeshRenderer meshRenderer;
        
        private bool isShow = true;
        
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