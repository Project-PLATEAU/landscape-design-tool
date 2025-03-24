using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{
    [Serializable]
    public class BIMImportSaveData
    {
        [SerializeField]
        private string id;
        public string ID => id;
        
        [SerializeField]
        Vector3 position;
        [SerializeField]
        Vector3 angle;
        [SerializeField]
        Vector3 scale;

        [SerializeField]
        string name;

        [SerializeField]
        byte[] glbArray;

        public Vector3 Position => position;
        public Vector3 Angle => angle;
        public Vector3 Scale => scale;

        public string Name => name;
        public byte[] GlbArray => glbArray;

        public BIMImportSaveData(GameObject obj, byte[] data)
        {
            id = obj.GetInstanceID().ToString();
            position = obj.transform.position;
            angle = obj.transform.eulerAngles;
            scale = obj.transform.localScale;

            name = obj.name;
            glbArray = data;
        }
        
        public void SetID(string id)
        {
            this.id = id;
        }
        
        public void SetTransform(Vector3 position, Vector3 angle, Vector3 scale)
        {
            this.position = position;
            this.angle = angle;
            this.scale = scale;
        }
    }
}
