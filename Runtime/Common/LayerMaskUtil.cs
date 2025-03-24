using UnityEngine;

namespace Landscape2.Runtime.Common
{
    public static class LayerMaskUtil
    {
        private static int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        
        public static void SetIgnore(GameObject target, bool isIgnore, int defaultLayer = 0)
        {
            if (defaultLayer == 0)
            {
                defaultLayer = LayerMask.NameToLayer("Default");
            }
            target.layer = isIgnore ? ignoreLayer : defaultLayer;
        }
        
        public static bool IsIgnore(GameObject target)
        {
            return target.layer == ignoreLayer;
        }
    }
}