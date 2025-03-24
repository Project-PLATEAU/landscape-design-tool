using System;
using UnityEditor;
using UnityEngine;

namespace LandscapeDesignTool.Editor
{
    public static class LandscapeEditorStyle
    {
        private static readonly GUIStyle styleLabelRichText = new GUIStyle(EditorStyles.label)
        {
            richText = true
        };
        
        public static void Header(string text)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField($"<size=15>{text}</size>", styleLabelRichText);
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// 「表示する」「非表示にする」の2つのボタンを表示します。
        /// 前者が押された時、 <paramref name="onButtonPushed"/>(true) を実行し、
        /// 後者が押された時、 <paramref name="onButtonPushed"/>(false) を実行します。
        /// </summary>
        public static void ButtonSwitchDisplay(Action<bool> onButtonPushed)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("表示する"))
                {
                    onButtonPushed(true);
                }

                if (GUILayout.Button("非表示にする"))
                {
                    onButtonPushed(false);
                }
            }
        }
    }
}