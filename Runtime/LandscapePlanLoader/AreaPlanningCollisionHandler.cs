using Landscape2.Runtime.Common;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 区画メッシュの衝突判定を管理するクラス
    /// 作成された区画メッシュにAddされる
    /// </summary>
    public class AreaPlanningCollisionHandler : MonoBehaviour
    {
        [HideInInspector]
        public UnityEvent<GameObject> OnEnter = new ();
        
        private MeshCollider collider;
        private Rigidbody rigidbody;

        private void Awake()
        {
            collider = gameObject.AddComponent<MeshCollider>();
            collider.convex = true;
            collider.isTrigger = true;
            
            rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true; // 物理シミュレーションの影響を受けないようにする
        }

        public void SetCollider(Mesh mesh)
        {
            collider.sharedMesh = mesh;
            
            // コライダーをONにして、OnTriggerEnterを発生させる
            collider.enabled = true;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (CityObjectUtil.IsBuilding(other.gameObject))
            {
                OnEnter.Invoke(other.gameObject);
                SetOffCollider();
            }
        }

        private async void SetOffCollider()
        {
            // コライダーEnter後にOFFにするため、1フレーム待つ
            await Task.Yield();
            
            // コライダーをOFFにして、何度もOnTriggerEnterを発生させない
            if (collider.enabled)
            {
                collider.enabled = false;
            }
        }
    }
}