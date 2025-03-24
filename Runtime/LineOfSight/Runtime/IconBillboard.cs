
using UnityEngine;

namespace Landscape2.Runtime
{
    public class IconBillboard : MonoBehaviour
    {
        const float baseSize = 1f;

        Vector3 baseScale;

        void Awake()
        {
            this.baseScale = this.transform.localScale / GetCameraDistance();
        }


        // Update is called once per frame
        void LateUpdate()
        {
            var cam = Camera.main;
            var lookAtVec = -cam.transform.forward;

            transform.LookAt(transform.position + lookAtVec);
        }

        float GetCameraDistance()
        {
            var cam = Camera.main;
            return Vector3.Distance(transform.position, cam.transform.position);
        }
    }
}
