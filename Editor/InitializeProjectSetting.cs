using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

namespace Landscape2.Editor
{
    [InitializeOnLoad]
    public class InitializeProjectSetting
    {
        static InitializeProjectSetting()
        {
            ConfigureHDRPSettings();
        }

        private static void ConfigureHDRPSettings()
        {
            // HDRPアセットを取得
            var hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdrpAsset == null)
            {
                Debug.LogError("HDRP Asset is not assigned in Graphics Settings.");
                return;
            }

            // Lit Shader ModeをBothに設定
            SerializedObject serializedHDRPAsset = new SerializedObject(hdrpAsset);

            SerializedProperty litShaderModeProperty = serializedHDRPAsset.FindProperty("m_RenderPipelineSettings.supportedLitShaderMode");
            litShaderModeProperty.enumValueIndex = 2;
            // litShaderModeProperty.enumValueIndex = (int)RenderPipelineSettings.SupportedLitShaderMode.Both; == 3 -> enum index is out of range

            // CustomPassにチェックが入っているか確認し、設定
            SerializedProperty customPassProperty = serializedHDRPAsset.FindProperty("m_RenderPipelineSettings.supportCustomPass");
            customPassProperty.boolValue = true;

            // Custom Buffer FormatをR16G16B16に設定
            SerializedProperty customBufferFormatProperty = serializedHDRPAsset.FindProperty("m_RenderPipelineSettings.customBufferFormat");
            customBufferFormatProperty.enumValueIndex = 2;
            // customBufferFormatProperty.entryValueIndex = (int)RenderPipelineSettings.CustomBufferFormat.R16G16B16A16; == 48 -> enum index is out of range

            serializedHDRPAsset.ApplyModifiedProperties();
        }

        private static void SetApiCompatibilityLevelForTarget(BuildTargetGroup targetGroup, int targetIndex)
        {
            ApiCompatibilityLevel[] levels = (ApiCompatibilityLevel[])System.Enum.GetValues(typeof(ApiCompatibilityLevel));
            if (targetIndex >= 0 && targetIndex < levels.Length)
            {
                PlayerSettings.SetApiCompatibilityLevel(targetGroup, levels[targetIndex]);
                Debug.Log($"{targetGroup} API Compatibility Level set to {levels[targetIndex]}");
            }
            else
            {
                Debug.LogError("Invalid index for API Compatibility Level");
            }
        }
    }
}