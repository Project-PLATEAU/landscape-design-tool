using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandScapeDesignTool
{
    public class TmpHeight : MonoBehaviour
    {
        Vector3 orgPosition;
        // Start is called before the first frame update
        void Start()
        {
        }

        public void StoreOrg()
        {
            orgPosition = gameObject.transform.position;

        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void Restore()
        {
            gameObject.transform.position = orgPosition;
        }
    }
}
