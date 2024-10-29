using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;using DreadScripts.Localization;
using UnityEngine;
using UnityEditor;
using static DreadScripts.Localization.LocalizationStyles;
using static DreadScripts.Localization.LocalizationInspectorHelper;
using static DreadScripts.Localization.LocalizationStringUtility;

namespace DreadScripts.Localization
{
    public class LocalizationPopout : EditorWindow
    {
        private const string MISSING_COMPARISON_TEXT = "Comparison content is missing.";
        private static bool closeOnUnfocus = true;
        //private static bool showIconField;
        
        private string[] keyNames;
        private object splitState;

        private ScriptableObject targetScriptable;
        private KeyMatch[] keyMatches;
        private int selectedKeyIndex;

        internal static void ShowWindow(Rect r, ScriptableObject target, KeyMatch[] matches, int index = 0)
        {
            var instance = ReadyWindow<LocalizationPopout>("Localization Popout");
            instance.targetScriptable = target;
            instance.keyMatches = matches;
            instance.keyNames = matches.Select(k => k.keyName).ToArray();
            instance.selectedKeyIndex = index;
            instance.splitState = ReflectionSplitterGUILayout.CreateSplitterState(1, 1);
            
            r = GUIUtility.GUIToScreenRect(r);
            instance.Show();
            float currentWidth = 400;
            float estimatedHeight = 200;
            int n = 0;
            
            while (n++ < 10)
            {
                estimatedHeight = 220;
                var km = matches[index];
                var mc = km.targetContent;
                var cmc = km.comparisonContent;
                
                bool hasComparison = cmc != null;
                
                float textHeight = EditorStyles.textArea.CalcHeight(new GUIContent(mc.text), currentWidth / 2f);
                float comparisonTextHeight = hasComparison ? EditorStyles.textArea.CalcHeight(new GUIContent(cmc.text), currentWidth / 2f) : 0;
                float firstHeight = Mathf.Max(textHeight, comparisonTextHeight);
                
                float tooltipHeight = EditorStyles.textArea.CalcHeight(new GUIContent(mc.tooltip), currentWidth / 2f);
                float comparisonTooltipHeight = hasComparison ? EditorStyles.textArea.CalcHeight(new GUIContent(cmc.tooltip), currentWidth / 2f) : 0;
                float secondHeight = Mathf.Max(tooltipHeight, comparisonTooltipHeight);

                estimatedHeight += firstHeight + secondHeight;
                //if (showIconField) estimatedHeight += 100;
                
                
                if (estimatedHeight > currentWidth)
                {
                    currentWidth += (estimatedHeight - currentWidth) / 2;
                    continue;
                }

                break;
            }

            r.width = currentWidth;
            r.height = estimatedHeight;
            r.y -= r.height / 2;
            instance.position = r;
        }

        public void OnGUI()
        {
            if (keyMatches == null) Close();
            selectedKeyIndex = EditorGUILayout.Popup("Selected Content", selectedKeyIndex, keyNames);
            var km = keyMatches[selectedKeyIndex];

            DrawSeparator();

            km.targetContent.text = DrawSplit(Localize(LocalizationLocalizationKeys.TranslationTextField).text, km.targetContent.text, km.targetContent.text ?? MISSING_COMPARISON_TEXT);
            DrawSeparator();

            km.targetContent.tooltip = DrawSplit(Localize(LocalizationLocalizationKeys.TranslationTooltipField).text, km.targetContent.tooltip, km.targetContent.tooltip ?? MISSING_COMPARISON_TEXT);

            /*if (showIconField)
            {
                DrawSeparator();
                km.targetContent.iconName = DrawSplit("Icon Name", km.targetContent.iconName, km.targetContent.iconName ?? MISSING_COMPARISON_TEXT);
            }*/

            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                    closeOnUnfocus = EditorGUILayout.Toggle(Localize(LocalizationLocalizationKeys.PopoutAutoClose), closeOnUnfocus);
                /*using (new GUILayout.VerticalScope(GUI.skin.box))
                    showIconField = EditorGUILayout.Toggle("Show Icon Field", showIconField);*/
            }
        }
        
        private string DrawSplit(string title, string text, string comparisonText)
        {
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(title, Styles.header);

                ReflectionSplitterGUILayout.BeginHorizontalSplit(splitState);
                using (new GUILayout.VerticalScope(GUI.skin.box))
                    text = UnescapeNewLines(EditorGUILayout.DelayedTextField(EscapeNewLines(text), Styles.wrappedTextArea, GUILayout.ExpandHeight(true)));

                Rect r = GUILayoutUtility.GetLastRect();
                r.x += r.width + 2;
                
                using (new GUILayout.VerticalScope(GUI.skin.box))
                    GUILayout.TextArea(comparisonText, Styles.wrappedLabel, GUILayout.ExpandHeight(true));
                
                ReflectionSplitterGUILayout.DrawVerticalSplitter();
                ReflectionSplitterGUILayout.EndSplit();
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetScriptable, "Translation Change");
                EditorUtility.SetDirty(targetScriptable);
            }

            return text;
        }

        private void OnLostFocus()
        {
            if (closeOnUnfocus)
            {
                Close();
                DestroyImmediate(this);
            }
        }
    }
    
}
