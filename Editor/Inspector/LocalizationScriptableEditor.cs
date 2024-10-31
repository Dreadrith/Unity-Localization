using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using static DreadScripts.Localization.LocalizationStyles;
using static DreadScripts.Localization.LocalizationInspectorHelper;
using static DreadScripts.Localization.LocalizationStringUtility;

namespace DreadScripts.Localization
{
    [CustomEditor(typeof(LocalizationScriptableBase), true, isFallback = true)]
    internal class LocalizationScriptableEditor : Editor
    {
        #region Fields & Properties

        #region Fields
        
        private LocalizationScriptableBase targetScriptable;
        private LocalizationHandler<LocalizationScriptableBase> _targetLocalizationHandler;
        private LocalizationHandler<LocalizationScriptableBase> _comparisonLocalizationHandler;
        private KeyMatch[][] keyMatches2D;
        private KeyMatch[] keyMatches1D;
        private object splitState;
        private bool drawingFirstColumn;

        private static bool editorExtrasFoldout;
        private static bool showKeyNameColumn = true;
        private static bool showComparisonColumn = true;
        private static bool showDisplayColumn;

        private KeyCollection[] keyCollections;

        private string[] toolbarOptions;
        private int toolbarIndex = 0;
        private string _search;

        private static string[] languageOptions;
        private static string[] languageIdentifiers;
        private int languageOptionIndex;

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
                        localizationLocalizationHandler.DrawField(Localize(LocalizationLocalizationKeys.EditorLanguageSelectionField));
                    
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

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        if (GUILayout.Button(Localize(LocalizationLocalizationKeys.CleanUpKeys), EditorStyles.toolbarButton))
                            CleanUpKeys();
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
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                EditorGUI.BeginChangeCheck();
                languageOptionIndex = EditorGUILayout.Popup(Localize(LocalizationLocalizationKeys.LanguageNameField), languageOptionIndex, languageOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    if (languageOptionIndex != 0)
                    {
                        targetScriptable.languageName = languageIdentifiers[languageOptionIndex-1];
                        EditorUtility.SetDirty(targetScriptable);
                    }
                }

                if (languageOptionIndex == 0)
                {
                    EditorGUI.BeginChangeCheck();
                    string dummy = EditorGUILayout.DelayedTextField(targetScriptable.languageName, GUILayout.Width(200));
                    if (EditorGUI.EndChangeCheck())
                    {
                        targetScriptable.languageName = dummy;
                        EditorUtility.SetDirty(targetScriptable);
                    }
                }
            }
            
            if (showComparisonColumn)
                using (new GUILayout.VerticalScope(GUI.skin.box))
                    _comparisonLocalizationHandler.DrawField(Localize(LocalizationLocalizationKeys.ComparisonField));
            
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
            targetScriptable = (LocalizationScriptableBase) target;
            _targetLocalizationHandler = LocalizationHandler<LocalizationScriptableBase>.CreateFromLanguages(targetScriptable);
            
            if (languageOptions == null)
            {
                var cultures = 
                    CultureInfo.GetCultures(CultureTypes.NeutralCultures).Skip(1)
                               .Where(c => !c.EnglishName.StartsWith("Chinese"))
                               .Select(c => new MiniCultureInfo(c))
                               .Union(new MiniCultureInfo[]
                               {
                                   new MiniCultureInfo("Chinese-Simplified","简体中文"),
                                   new MiniCultureInfo("Chinese-Traditional","繁體中文")
                               })
                               .OrderBy(c => c.englishName).ToArray();
                
                languageOptions = cultures.Select(c => c.displayName).Prepend("[Custom]").ToArray();
                languageIdentifiers = cultures.Select(c => c.nativeName).ToArray();
            }
            languageOptionIndex = Array.IndexOf(languageIdentifiers, targetScriptable.languageName);
            if (languageOptionIndex == -1) languageOptionIndex = 0;
            else languageOptionIndex++;
            
            OnOptionsChanged();
            _comparisonLocalizationHandler =  LocalizationHandler<LocalizationScriptableBase>.CreateFromLanguages((LocalizationScriptableBase[])Resources.FindObjectsOfTypeAll(target.GetType()));
            _comparisonLocalizationHandler.onLanguageChanged = RefreshKeyMatches;
            keyCollections = targetScriptable.keyCollections;
            toolbarOptions = keyCollections.Select(kc => kc.collectionName).ToArray();
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
                    var comparisonContent = _comparisonLocalizationHandler.Get_Internal(k);
                    var targetContent = _targetLocalizationHandler.Get_Internal(k);
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
            else GUI.Label(textFieldRect, GetMissingContent());

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
                

                GUI.Label(textFieldRect, GetMissingContent());
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
            GUI.Label(baseRect, km.targetContent?.ToGUIContent() ?? GetMissingContent());
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
                    LocalizationPopout.ShowWindow(popoutRect, targetScriptable, arr, index);
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

        private static Rect GetRect(KeyMatch km) => EditorGUILayout.GetControlRect(false, GetHeight(km), GUILayout.ExpandWidth(true));
        private static void DrawBackground(Rect rect, int index) => EditorGUI.DrawRect(rect, index % 2 == 0 ? evenColor : oddColor);
        private static float GetHeight(KeyMatch km) => km.foldout ? EditorGUIUtility.singleLineHeight * 2 /* * (showIconField ? 3 : 2)*/ : EditorGUIUtility.singleLineHeight;
        private static bool DrawFoldout(ref bool b, GUIContent label)
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
                    Debug.LogError(string.Format(Localize(LocalizationLogsAndErrorsKeys.LineParseFailLog).text, i));
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
                        (!categoryOnly && !targetScriptable.keyCollections.Any(kc => kc.keyNames.Any(k => k == key))) ||
                        (categoryOnly && !targetScriptable.keyCollections[toolbarIndex].keyNames.Any(k => k == key))
                        )
                    {
                        Debug.LogError(string.Format(Localize(LocalizationLogsAndErrorsKeys.KeyNotFoundLog).text, key));
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
            Debug.Log($"[Localization] {Localize(LocalizationLogsAndErrorsKeys.CSVPasteFinishLog).text}");
            RefreshKeyMatches();
            Repaint();
        }
        
        private void CleanUpKeys()
        {
            HashSet<string> keys = new HashSet<string>(keyMatches1D.Select(km => km.keyName));
            Undo.RecordObject(targetScriptable, "Clean Up Keys");
            targetScriptable.localizedContent = targetScriptable.localizedContent.Where(lc => keys.Contains(lc.keyName)).ToArray();
            localizationLocalizationHandler.ClearCache();
            _targetLocalizationHandler.ClearCache();
            EditorUtility.SetDirty(targetScriptable);
            RefreshKeyMatches();
            Repaint();
        }
        #endregion

        
    }

    [CustomEditor(typeof(LocalizationScriptablePlaceholder))]
    internal class LocalizationPlaceholderEditor : Editor
    {
        private static LocalizationHandler<LocalizationLocalization> _localizationHandlerLocalizer;
        
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
            GUILayout.Label(_localizationHandlerLocalizer[LocalizationPlaceholderKeys.BaseLocalizationTitle], Styles.centeredHeader);
            _localizationHandlerLocalizer.DrawField(_localizationHandlerLocalizer[LocalizationLocalizationKeys.EditorLanguageSelectionField]);
            EditorGUILayout.HelpBox(_localizationHandlerLocalizer[LocalizationPlaceholderKeys.BaseLocalizationSelectionHelp].text, MessageType.Info);
            EditorGUILayout.Space();
            var dummy = 0;
            EditorGUI.BeginChangeCheck();
            dummy = EditorGUILayout.Popup(_localizationHandlerLocalizer[LocalizationPlaceholderKeys.BaseLocalizationSelectionField], dummy, validLocalizationTypeNames);
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
            _localizationHandlerLocalizer = LocalizationHandler<LocalizationLocalization>.LoadLanguagesFromAssets();
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
}


