using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Landscape2.Runtime.Common
{
    
    public static class MaterialUtil
    {
        private static string MaterialName = "RegurationAreaMaterial";
        
        public static Material MakeMaterial(Color col)
        {
            //レンダーパイプラインに応じた Unlitシェーダーを求めます。
            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            Shader shader;
            if (pipelineAsset == null)
            {
                shader = Shader.Find("Unlit/Transparent Colored");
            }
            else if (pipelineAsset.name == "UniversalRenderPipelineAsset")
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            else if (pipelineAsset.name.Contains("HDRenderPipelineAsset"))
            {
                shader = Shader.Find("HDRP/Unlit");
            }
            else
            {
                throw new Exception("Unknown Pipeline.");
            }
            Material material = new Material(shader);

#if UNITY_EDITOR
            if (pipelineAsset != null && pipelineAsset.name == "UniversalRenderPipelineAsset")
            {
                var originalMaterial = (Material)AssetDatabase.LoadAssetAtPath("Packages/com.synesthesias.landscape-design-tool/Materials/RegulationArea.mat", typeof(Material));
                material.CopyPropertiesFromMaterial(originalMaterial);
            }
#endif

            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            // General Transparent Material Settings
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_ZWrite", 0.0f);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.renderQueue += material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetColor("_BaseColor", col);
            material.name = MaterialName;

            return material;
        }
    }
}