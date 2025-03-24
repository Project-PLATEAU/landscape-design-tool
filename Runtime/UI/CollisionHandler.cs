using System.Collections;
using System.Collections.Generic;
using LandscapeDesignTool;
using UnityEngine;

namespace LandScapeDesignTool
{

    public class CollisionHandler : MonoBehaviour
    {

        public float areaHeight = float.MaxValue;
        List<GameObject> targetbjects = new List<GameObject>();
        public bool isApply = false;

        // Start is called before the first frame update
        void Start()
        {
            isApply = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject target = other.gameObject;
            if (target.layer == LayerMask.NameToLayer("Building"))
                targetbjects.Add(target);
        }

        public void ApplyHeight(float h)
        {

            isApply = true;
            foreach (var target in targetbjects)
            {
                var mesh = target.GetComponent<MeshFilter>().mesh;
                var bounds = mesh.bounds;

                // 地面の高さ取得
                if (!RegulationArea.TryGetGroundPosition(bounds.center, out var groundPosition))
                    continue;

                float targetHeight = bounds.center.y + (bounds.size.y / 2.0f);
                if (targetHeight - groundPosition.y > h)
                {
                    if (target.GetComponent<TmpHeight>() == null)
                    {
                        target.AddComponent<TmpHeight>();
                    }
                    float dy = targetHeight - groundPosition.y - h;
                    Vector3 p = target.transform.position;
                    Vector3 np = new Vector3(p.x, p.y - dy, p.z);
                    target.transform.position = np;
                }

                areaHeight = h;
            }
        }

        public void UndoHeight()
        {
            isApply = false;
            foreach (var target in targetbjects)
            {
                var newPosition = target.transform.position;
                newPosition.y = 0f;
                target.transform.position = newPosition;
            }
        }
    }
}
