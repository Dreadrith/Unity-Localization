using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using static DreadScripts.Localization.LocalizationStyles;
using static DreadScripts.Localization.LocalizationHelper;
using static DreadScripts.Localization.LocalizationStringUtility;

namespace DreadScripts.Localization
{
    [CustomEditor(typeof(LocalizationScriptableBase), true, isFallback = true)]
    internal class LocalizationScriptableEditor : Editor
    {
        #region Fields & Properties

        #region Fields

        private static Localization localizationLocalizer;

        private LocalizationScriptableBase targetScriptable;
        private Localization targetLocalization;
        private Localization comparisonLocalization;
        private KeyMatch[][] keyMatches2D;
        private KeyMatch[] keyMatches1D;
        private object splitState;
        private bool drawingFirstColumn;

        private static bool editorExtrasFoldout;
        private static bool showKeyNameColumn = true;
        private static bool showComparisonColumn = true;
        private static bool showDisplayColumn;

        private LocalizationKeyCategory[] keyCollections;

        private string[] toolbarOptions;
        private int toolbarIndex = 0;
        private string _search;

        #endregion

        #region Properties

        private string search
        {
            get => _search;
            set
            {
                if (_search == value) return;
                _search = value;
                OnFilterChanged();
            }
        }

        #endregion

        #endregion

        public override void OnInspectorGUI()
        {
            using (new GUILayout.HorizontalScope("in bigtitle"))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(targetScriptable.hostTitle, Styles.centeredHeader, GUILayout.ExpandWidth(false));
                GUILayout.Label(Localize(LocalizationLocalizationKeys.HelpIcon, null, helpIcon.image as Texture2D), GUIStyle.none, GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();
            }
            DrawSeparator();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (DrawFoldout(ref editorExtrasFoldout, Localize(LocalizationLocalizationKeys.ExtrasFoldout)))
                {
                    EditorGUI.indentLevel++;

                    using (new GUILayout.VerticalScope(GUI.skin.box))
                        localizationLocalizer.DrawField(Localize(LocalizationLocalizationKeys.EditorLanguageSelectionField));
                    
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        if (GUILayout.Button(Localize(LocalizationLocalizationKeys.CopyCategory), EditorStyles.toolbarButton))
                            CopyAsCSV(true);
                        
                        if (GUILayout.Button(Localize(LocalizationLocalizationKeys.PasteCategory), EditorStyles.toolbarButton))
                            PasteAsCSV(true);
                    }
                    
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        if (GUILayout.Button(Localize(LocalizationLocalizationKeys.CopyAll), EditorStyles.toolbarButton))
                            CopyAsCSV(false);
                        
                        if (GUILayout.Button(Localize(LocalizationLocalizationKeys.PasteAll), EditorStyles.toolbarButton))
                            PasteAsCSV(false);
                    }
                    
                    EditorGUILayout.Space();

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        EditorGUI.BeginChangeCheck();
                        showKeyNameColumn = GUILayout.Toggle(showKeyNameColumn, Localize(LocalizationLocalizationKeys.ShowKeyNameToggle), EditorStyles.toolbarButton);
                        showComparisonColumn = GUILayout.Toggle(showComparisonColumn, Localize(LocalizationLocalizationKeys.ShowComparisonToggle), EditorStyles.toolbarButton);
                        showDisplayColumn = GUILayout.Toggle(showDisplayColumn, Localize(LocalizationLocalizationKeys.ShowDisplayToggle), EditorStyles.toolbarButton);
                        //showIconField = GUILayout.Toggle(showIconField, Localize(LocalizationLocalizationKeys.ShowIconToggle), EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck()) OnOptionsChanged();
                    }

                    EditorGUI.indentLevel--;


                }
            }

            DrawSeparator();
            using (new GUILayout.VerticalScope(GUI.skin.box))
                targetScriptable.languageName = EditorGUILayout.TextField(Localize(LocalizationLocalizationKeys.LanguageNameField), targetScriptable.languageName);
            
            if (showComparisonColumn)
                using (new GUILayout.VerticalScope(GUI.skin.box))
                    comparisonLocalization.DrawField(Localize(LocalizationLocalizationKeys.ComparisonField), RefreshKeyMatches);
            
            using (new GUILayout.VerticalScope(GUI.skin.box))
                search = EditorGUILayout.TextField(Localize(LocalizationLocalizationKeys.SearchField), search, EditorStyles.toolbarSearchField);
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (keyCollections.Length > 1)
                    toolbarIndex = GUILayout.Toolbar(toolbarIndex, toolbarOptions, EditorStyles.toolbarButton);

                var selectedKeyCollection = keyMatches2D[toolbarIndex];
                ReflectionSplitterGUILayout.BeginHorizontalSplit(splitState, GUIStyle.none);

                drawingFirstColumn = true;
                if (showKeyNameColumn)
                {
                    using (new GUILayout.VerticalScope())
                    {

                        ReflectionSplitterGUILayout.DrawTitle(Localize(LocalizationLocalizationKeys.KeyNameTitle));
                        foreach (var km in selectedKeyCollection)
                            DrawKeyName(km);
                    }

                    drawingFirstColumn = false;
                }


                using (new GUILayout.VerticalScope())
                {
                    ReflectionSplitterGUILayout.DrawTitle(Localize(LocalizationLocalizationKeys.TranslationTitle));
                    foreach (var km in selectedKeyCollection)
                        DrawTranslationContent(km);
                }
                
                if (showKeyNameColumn) ReflectionSplitterGUILayout.DrawVerticalSplitter();

                if (showComparisonColumn)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        ReflectionSplitterGUILayout.DrawTitle(Localize(LocalizationLocalizationKeys.ComparisonTitle));
                        foreach (var km in selectedKeyCollection)
                            DrawComparisonContent(km);
                    }

                    ReflectionSplitterGUILayout.DrawVerticalSplitter();
                }

                if (showDisplayColumn)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        ReflectionSplitterGUILayout.DrawTitle(Localize(LocalizationLocalizationKeys.DisplayTitle));
                        foreach (var km in selectedKeyCollection)
                            DrawDisplayContent(km);
                    }

                    ReflectionSplitterGUILayout.DrawVerticalSplitter();
                }
                
                
                ReflectionSplitterGUILayout.EndSplit();
            }
        }

        #region Automated Methods

        private void OnEnable()
        {
            localizationLocalizer = Localization.Load<LocalizationLocalization>();

            targetScriptable = (LocalizationScriptableBase) target;
            targetLocalization = Localization.Load(targetScriptable);
            
            OnOptionsChanged();
            comparisonLocalization = Localization.Load(target.GetType());
            keyCollections = targetScriptable.LocalizationKeyCollections;
            toolbarOptions = keyCollections.Select(kc => kc.categoryName).ToArray();
            RefreshKeyMatches();
        }

        private void OnFilterChanged()
        {
            if (string.IsNullOrEmpty(search))
            {
                foreach (var km in keyMatches1D)
                    km.hidden = false;
                return;
            }

            foreach (var km in keyMatches1D)
                km.hidden = !MatchesSearch(km);
        }

        private void OnOptionsChanged()
        {
            List<float> sizes = new List<float>{2};
            if (showKeyNameColumn) sizes.Insert(0, 1);
            if (showComparisonColumn) sizes.Add(2);
            if (showDisplayColumn) sizes.Add(1);

            splitState = ReflectionSplitterGUILayout.CreateSplitterState(sizes.ToArray());
        }

        private void RefreshKeyMatches()
        {
            keyMatches2D = new KeyMatch[keyCollections.Length][];
            int currentIndex = 0;
            for (int i = 0; i < keyMatches2D.Length; i++)
            {
                var kma = keyMatches2D[i] = new KeyMatch[keyCollections[i].keyNames.Length];
                for (int j = 0; j < kma.Length; j++)
                {
                    var k = keyCollections[i].keyNames[j];
                    var comparisonContent = comparisonLocalization.Get_Internal(k);
                    var targetContent = targetLocalization.Get_Internal(k);
                    kma[j] = new KeyMatch(k, comparisonContent, targetContent, currentIndex++);
                }
            }

            keyMatches1D = keyMatches2D.SelectMany(km => km).ToArray();
        }

        #endregion

        #region GUI Methods

        private void DrawKeyName(KeyMatch km)
        {
            if (km.hidden) return;
            var baseRect = GetRect(km);
            DrawBackground(baseRect, km.index);
            HandleFirstColumn(km, ref baseRect);
            GUI.Label(new Rect(baseRect){height = EditorGUIUtility.singleLineHeight}, km.keyName);
        }

        private void DrawComparisonContent(KeyMatch km)
        {
            if (km.hidden) return;
            var baseRect = GetRect(km);
            DrawBackground(baseRect, km.index);
            
            Rect textFieldRect = new Rect(baseRect) {height = EditorGUIUtility.singleLineHeight};
            bool hasContent = km.comparisonContent != null;
            if (hasContent) GUI.Label(textFieldRect, EscapeNewLines(km.comparisonContent.text));
            else GUI.Label(textFieldRect, missingContent);

            if (hasContent && km.foldout)
            {
                textFieldRect.y += EditorGUIUtility.singleLineHeight;
                GUI.Label(textFieldRect, km.comparisonContent.tooltip);

            }
        }

        private void DrawTranslationContent(KeyMatch km)
        {
            if (km.hidden) return;
            var baseRect = GetRect(km);
            DrawBackground(baseRect, km.index);
            HandleFirstColumn(km, ref baseRect);
            MiniContent miniContent = km.targetContent;
            Rect textFieldRect = new Rect(baseRect) {height = EditorGUIUtility.singleLineHeight};
            bool hasContent = miniContent != null;
            if (!hasContent)
            {
                Rect addTranslationRect = new Rect(textFieldRect) {width = 12, height = 12, y = textFieldRect.y + 3};
                textFieldRect.x += 14;
                textFieldRect.width -= 14;


                if (GUI.Button(addTranslationRect, addTranslationIcon, GUIStyle.none))
                    ReadyKeyContent(km.keyName);
                

                GUI.Label(textFieldRect, missingContent);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                string text = miniContent.text;
                string tooltip = miniContent.tooltip;
                
                text = UnescapeNewLines(EditorGUI.DelayedTextField(textFieldRect, EscapeNewLines(miniContent.text)));
                if (km.foldout)
                {
                    Rect tooltipRect = new Rect(textFieldRect) {y = textFieldRect.y + EditorGUIUtility.singleLineHeight};
                    tooltip = UnescapeNewLines(EditorGUI.DelayedTextField(tooltipRect, EscapeNewLines(miniContent.tooltip)));
                    
                    GUI.Label(textFieldRect, Localize(LocalizationLocalizationKeys.TranslationTextField), Styles.fadedLabel);
                    GUI.Label(tooltipRect, Localize(LocalizationLocalizationKeys.TranslationTooltipField),Styles.fadedLabel);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetScriptable, "Translation Change");
                    miniContent.text = text;
                    miniContent.tooltip = tooltip;
                    EditorUtility.SetDirty(targetScriptable);
                }
            }
        }

        private void DrawDisplayContent(KeyMatch km)
        {
            if (km.hidden) return;
            var baseRect = GetRect(km);
            DrawBackground(baseRect, km.index);
            GUI.Label(baseRect, km.targetContent ?? missingContent);
        }

        private void HandleFirstColumn(KeyMatch km, ref Rect r)
        {
            if (!drawingFirstColumn) return;

            Color ogColor = GUI.contentColor;
            try
            {
               
                GUI.contentColor = Color.grey;
                Rect popoutRect = new Rect(r) {width = 14, height = 14, x = r.x - 16};
                if (GUI.Button(popoutRect, popoutIcon, GUIStyle.none))
                {
                    km = ReadyKeyContent(km.keyName);
                    var arr = keyMatches1D.Where(km2 => km2.hasTranslation).ToArray();
                    var index = ArrayUtility.IndexOf(arr, km);
                    LocalizationPopout.ShowWindow(popoutRect, localizationLocalizer, targetScriptable, arr, index);
                }
            }
            finally
            {
                GUI.contentColor = ogColor;
            }
            
            r.x += 14;
            r.width -= 14;
            Rect foldoutRect = new Rect(r) {height = EditorGUIUtility.singleLineHeight};
            km.foldout = EditorGUI.Foldout(foldoutRect, km.foldout, GUIContent.none, true);
        }

        private static readonly Color oddColor = new Color(0, 0, 0, 0);
        private static readonly Color evenColor = new Color(0, 0, 0, 0.14f);

        private void DrawBackground(Rect rect, int index) => EditorGUI.DrawRect(rect, index % 2 == 0 ? evenColor : oddColor);
        private float GetHeight(KeyMatch km) => km.foldout ? EditorGUIUtility.singleLineHeight * 2 /* * (showIconField ? 3 : 2)*/ : EditorGUIUtility.singleLineHeight;
        private Rect GetRect(KeyMatch km) => EditorGUILayout.GetControlRect(false, GetHeight(km), GUILayout.ExpandWidth(true));

        private bool DrawFoldout(ref bool b, GUIContent label)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(14);
                b = EditorGUILayout.Foldout(b, label);
            }

            return b;
        }
        #endregion

        #region Helper Methods
        
        private KeyMatch ReadyKeyContent(string keyName)
        {
            KeyMatch km = keyMatches1D.FirstOrDefault(k => k.keyName == keyName);
            if (km == null) throw new Exception($"Key {keyName} does not exist in {targetScriptable.GetType().Name}");
            if (!km.hasTranslation) km = AddKey(keyName);
            return km;
        }
        private KeyMatch AddKey(string keyName)
        {
            var mc = new MiniContent("Untranslated Text");
            targetScriptable.localizedContent = targetScriptable.localizedContent.Append(new LocalizedContent(keyName, mc)).ToArray();
            EditorUtility.SetDirty(targetScriptable);
            RefreshKeyMatches();
            return keyMatches1D.First(km => km.keyName == keyName);
        }
        
        private bool MatchesSearch(KeyMatch km) => (showKeyNameColumn && ICContains(km.keyName, search)) ||
                                                   MatchesSearch(km.targetContent) ||
                                                   (showComparisonColumn && MatchesSearch(km.comparisonContent));

        private bool MatchesSearch(MiniContent mc)
        {
            if (mc == null) return false;
            var text = mc.text;
            var tooltip = mc.tooltip;
            //var iconName = mc.iconName;

            return ICContains(text, search) || ICContains(tooltip, search) /*|| ICContains(iconName, search)*/;
        }

        private void CopyAsCSV(bool categoryOnly)
        {
            StringBuilder builder = new StringBuilder();
            IEnumerable<LocalizedContent> targetContent = targetScriptable.localizedContent; 
            if (categoryOnly) targetContent = targetContent.Where(lc => keyMatches2D[toolbarIndex].Any(km => km.keyName == lc.keyName));
            foreach (var lc in targetContent)
            {
                var mc = lc.content;
                if (mc == null) continue;
                builder.AppendLine($"{EscapeAndQuote(lc.keyName)},{EscapeAndQuote(mc.text)},{EscapeAndQuote(mc.tooltip)}");
            }

            EditorGUIUtility.systemCopyBuffer = builder.ToString();
        }

        private void PasteAsCSV(bool categoryOnly)
        {
            Undo.RecordObject(targetScriptable, "Paste Localization CSV");
            var lines = EditorGUIUtility.systemCopyBuffer.Split('\n');
            string parsePattern = @"("".*""),("".*""),("".*"")";
            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i];
                if (string.IsNullOrWhiteSpace(l)) continue;
                Match m = Regex.Match(l, parsePattern);
                if (!m.Success)
                {
                    Debug.LogError(string.Format(Localize(LocalizationLocalizationKeys.LineParseFailLog).text, i));
                    continue;
                }

                var key = UnquoteAndUnescape(m.Groups[1].Value);
                var text = UnquoteAndUnescape(m.Groups[2].Value);
                var tooltip = UnquoteAndUnescape(m.Groups[3].Value);

                IEnumerable<LocalizedContent> targetContent = targetScriptable.localizedContent; 
                if (categoryOnly) targetContent = targetContent.Where(lc2 => keyMatches2D[toolbarIndex].Any(km => km.keyName == lc2.keyName));
                var lc = targetContent.FirstOrDefault(c => c.keyName == key);
                if (lc == null)
                {
                    if (
                        (!categoryOnly && !targetScriptable.LocalizationKeyCollections.Any(kc => kc.keyNames.Any(k => k == key))) ||
                        (categoryOnly && !targetScriptable.LocalizationKeyCollections[toolbarIndex].keyNames.Any(k => k == key))
                        )
                    {
                        Debug.LogError(string.Format(Localize(LocalizationLocalizationKeys.KeyNotFoundLog).text, key));
                        continue;
                    }

                    lc = new LocalizedContent(key, new MiniContent(string.Empty));
                    targetScriptable.localizedContent = targetScriptable.localizedContent.Append(lc).ToArray();
                }

                lc.content.text = text;
                lc.content.tooltip = tooltip;
            }

            EditorUtility.SetDirty(targetScriptable);

            //Finished pasting to Localization file.
            Debug.Log($"[Localization] {Localize(LocalizationLocalizationKeys.CSVPasteFinishLog).text}");
            RefreshKeyMatches();
            Repaint();
        }

        private static GUIContent Localize(LocalizationLocalizationKeys value, GUIContent fallbackContent = null, Texture2D icon = null) => localizationLocalizer.Get(value, fallbackContent, icon);

        #endregion

        
    }

    [CustomEditor(typeof(LocalizationScriptablePlaceholder))]
    internal class LocalizationPlaceholderEditor : Editor
    {
        private static Localization localizationLocalizer;
        
        private static readonly Type[] dropdownIgnoredTypes =
        {
            typeof(LocalizationScriptablePlaceholder),
            typeof(LocalizationLocalization)
        };

        private static Type[] validLocalizationTypes;
        private static string[] validLocalizationTypeNames;
        
        [InitializeOnLoadMethod]
        private static void GetValidLocalizationTypes()
        {
            validLocalizationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !dropdownIgnoredTypes.Contains(t) && t.IsSubclassOf(typeof(LocalizationScriptableBase)) && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .ToArray();
            
            validLocalizationTypeNames = validLocalizationTypes.Select(t => t.Name).ToArray();
            ArrayUtility.Insert(ref validLocalizationTypeNames, 0, "[None]");
        }
        public override void OnInspectorGUI()
        {
            GUILayout.Label(localizationLocalizer[LocalizationPlaceholderKeys.BaseLocalizationTitle], Styles.centeredHeader);
            localizationLocalizer.DrawField(localizationLocalizer[LocalizationLocalizationKeys.EditorLanguageSelectionField]);
            EditorGUILayout.HelpBox(localizationLocalizer[LocalizationPlaceholderKeys.BaseLocalizationSelectionHelp].text, MessageType.Info);
            EditorGUILayout.Space();
            var dummy = 0;
            EditorGUI.BeginChangeCheck();
            dummy = EditorGUILayout.Popup(localizationLocalizer[LocalizationPlaceholderKeys.BaseLocalizationSelectionField], dummy, validLocalizationTypeNames);
            if (EditorGUI.EndChangeCheck() && dummy-- > 0)
            {
                var localizationType = validLocalizationTypes[dummy];
                var path = AssetDatabase.GetAssetPath(target);
                var newFile = (LocalizationScriptableBase)CreateInstance(localizationType);
                //newFile.PopulateContent(false);
                
                AssetDatabase.CreateAsset(newFile,path);
                AssetDatabase.ImportAsset(path);
                Selection.activeObject = newFile;
                DestroyImmediate(target, true);
            }
        }
        
        private void OnEnable()
        {
            localizationLocalizer = Localization.Load<LocalizationLocalization>();
        }
    }
    
    internal class KeyMatch
    {
        internal readonly string keyName;
        internal readonly MiniContent comparisonContent;
        internal readonly MiniContent targetContent;
        internal readonly int index;
        
        internal readonly bool hasTranslation;
        internal readonly bool hasComparison;
        internal bool hidden;
        internal bool foldout;
        

        internal KeyMatch(string kn, MiniContent comparison, MiniContent target, int i)
        {
            keyName = kn;
            comparisonContent = comparison;
            targetContent = target;
            index = i;
            
            hasTranslation = targetContent != null;
            hasComparison = comparisonContent != null;
        }
    }
    
    internal enum LocalizationLocalizationKeys
    {
        EditorLanguageSelectionField,
        LanguageNameField,
        SearchField,
        HowToUseFoldout,
        ShowKeyNameToggle,
        ShowComparisonToggle,
        ShowDisplayToggle,
        ShowIconToggle,
        HowToUse,
        ComparisonField,
        KeyNameTitle,
        TranslationTitle,
        ComparisonTitle,
        DisplayTitle,
        TranslationTextField,
        TranslationTooltipField,
        TranslationIconField,
        ExtrasFoldout,
        HelpIcon,
        CopyCategory,
        PasteCategory,
        CopyAll,
        PasteAll,
        LineParseFailLog,
        KeyNotFoundLog,
        CSVPasteFinishLog
    }

    internal enum LocalizationPlaceholderKeys
    {
        BaseLocalizationTitle,
        BaseLocalizationSelectionHelp,
        BaseLocalizationSelectionField,
    }
}


