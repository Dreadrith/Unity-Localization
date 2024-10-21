﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Object = UnityEngine.Object;
using static DreadScripts.Localization.LocalizationHelper;

namespace DreadScripts.Localization
{

    public class LocalizationHandler
    {
        private static readonly Dictionary<LocalizationScriptableBase, Dictionary<string, int>> mapToLocalizationCache = new Dictionary<LocalizationScriptableBase, Dictionary<string, int>>();
        private LocalizationScriptableBase localizationMap;
        private Type localizationType;
        
        public LocalizationScriptableBase[] languageOptions;
        public string[] languageOptionsNames;
        public int selectedLanguageIndex;
        private bool shouldRefresh;
        
        #region Instancing
        public static LocalizationHandler Load(LocalizationScriptableBase map) => new LocalizationHandler(map);
        public static LocalizationHandler Load<T>(string baseLanguageName = "English") where T : LocalizationScriptableBase => new LocalizationHandler(typeof(T), baseLanguageName);
        public static LocalizationHandler Load(Type type, string baseLanguageName = "English") => new LocalizationHandler(type, baseLanguageName);
        public LocalizationHandler(LocalizationScriptableBase map) => SetLanguage(map);
        
        public LocalizationHandler(Type type, string baseLanguageName = "English")
        {
            if (!typeof(LocalizationScriptableBase).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type.Name} doesn't inherit from {nameof(LocalizationScriptableBase)}");
            
            // This works but is slow.
            // var allLanguages = AssetDatabase.FindAssets($"t:{type.Name}").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LocalizationScriptableBase>).Where(so => so != null && so.GetType() == type).ToArray();
            
            //This is faster but Resource may not be loaded yet if it's in Packages
            var allLanguages = Resources.FindObjectsOfTypeAll(type) as LocalizationScriptableBase[];
            
            if (allLanguages == null || allLanguages.Length == 0)
            {
                //Best of both worlds solution
                //If the resources aren't loaded, do a concentrated search in the package of the type's script and load them.
                try
                {
                    var tempInstance = ScriptableObject.CreateInstance(type);
                    var ms = MonoScript.FromScriptableObject(tempInstance);
                    Object.DestroyImmediate(tempInstance);
                    var msPath = AssetDatabase.GetAssetPath(ms);
                    var packagePath = msPath.Substring(0, msPath.IndexOf('/', msPath.IndexOf('/') + 1));
                    var guids = AssetDatabase.FindAssets($"t:{type.Name}", new[] {packagePath});
                    if (guids.Length > 0) allLanguages = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LocalizationScriptableBase>).Where(so => so != null && so.GetType() == type).ToArray();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to force load localization files:\n{e}");
                }
            }
            
            if (allLanguages == null || allLanguages.Length == 0)
            {
                Debug.LogError($"No localization files of type {type.Name} found");
                SetLanguage(null);
                return;
            }
            
            var prefKey = $"{LocalizationConstants.LANGUAGE_KEY_PREFIX}{type.Name}";
        
            //Tries to load product specific lannguage first, then preferred language, then base language
            
            LocalizationScriptableBase map = null;
            
            if (EditorPrefs.HasKey(prefKey)) 
                map = allLanguages.FirstOrDefault(m => m.languageName == EditorPrefs.GetString(prefKey));
            
            if (map == null && EditorPrefs.HasKey(LocalizationConstants.PREFERRED_LANGUAGE_KEY)) 
                map = allLanguages.FirstOrDefault(m => m.languageName == EditorPrefs.GetString(LocalizationConstants.PREFERRED_LANGUAGE_KEY));
            
            if (map == null) 
                map = allLanguages.FirstOrDefault(m => m.languageName == baseLanguageName);
            
            if (map == null) map = allLanguages.First();
            SetLanguage(map);
        }
        #endregion

        #region Get with KeyName
        public bool TryGet(string keyName, out GUIContent content, Texture2D icon = null)
        {
            var mc = Get_Internal(keyName);
            bool success = mc != null;
            content = mc.ToGUIContent(icon);
            return success;
        }
        
        public GUIContent Get(string keyName) => StringGet_Internal(keyName, null, null);
        public GUIContent Get(string keyName, string fallBack) => StringGet_Internal(keyName, TextToContent(fallBack), null);
        public GUIContent Get(string keyName, GUIContent fallBack) => StringGet_Internal(keyName, fallBack, null);
        public GUIContent Get(string keyName, Texture2D icon) => StringGet_Internal(keyName, null, icon);
        public GUIContent Get(string keyName, string fallBack, Texture2D icon) => StringGet_Internal(keyName, TextToContent(fallBack), icon);
        public GUIContent Get(string keyName, GUIContent fallBack, Texture2D icon) => StringGet_Internal(keyName, fallBack, icon);
        
        public GUIContent this[string keyName] => Get(keyName);
        
        private GUIContent StringGet_Internal(string keyName, GUIContent fallback, Texture2D icon) => Get_Internal(keyName).ToGUIContent(fallback, icon);
        #endregion
        
        #region Get with EnumKey

        public bool TryGet(Enum enumKey, out GUIContent content, Texture2D icon = null)
        {
            if (!_globalEnumDictionary.TryGetValue(enumKey, out var key))
            {
                key = enumKey.ToString();
                _globalEnumDictionary.Add(enumKey, key);
            }
            return TryGet(key, out content, icon);
        }

        public GUIContent Get(Enum enumKey) => EnumGet_Internal(enumKey, null, null);
        public GUIContent Get(Enum enumKey, string fallBack) => EnumGet_Internal(enumKey, TextToContent(fallBack), null);
        public GUIContent Get(Enum enumKey, GUIContent fallBack) => EnumGet_Internal(enumKey, fallBack, null);
        public GUIContent Get(Enum enumKey, Texture2D icon) => EnumGet_Internal(enumKey, null, icon);
        public GUIContent Get(Enum enumKey, string fallBack, Texture2D icon) =>  EnumGet_Internal(enumKey, TextToContent(fallBack), icon);
        public GUIContent Get(Enum enumKey, GUIContent fallBack, Texture2D icon) => EnumGet_Internal(enumKey, fallBack, icon);
        public GUIContent this[Enum key] => Get(key);
        
        private static readonly Dictionary<Enum, string> _globalEnumDictionary = new Dictionary<Enum, string>();
        private GUIContent EnumGet_Internal(Enum value, GUIContent fallback, Texture2D icon)
        {
            if (!_globalEnumDictionary.TryGetValue(value, out var key))
            {
                key = value.ToString();
                _globalEnumDictionary.Add(value, key);
            }
            return Get_Internal(key).ToGUIContent(fallback, icon);
        }
        #endregion
        
        internal MiniContent Get_Internal(string keyName)
        {
            if (localizationMap == null) return null;
            if (!mapToLocalizationCache.TryGetValue(localizationMap, out var localizationCache))
            {
                localizationCache = new Dictionary<string, int>();
                mapToLocalizationCache.Add(localizationMap, localizationCache);
            }
            var localizedContent = localizationMap.localizedContent;
            if (!(localizationCache.TryGetValue(keyName, out var index)))
            {
                index = Array.FindIndex(localizedContent, c => c.keyName == keyName);
                if (index == -1) return null;
                localizationCache.Add(keyName, index);
            }

            return localizedContent[index].content;
        }

        #region Language
        internal void SetLanguage(object userData)
        {
            LocalizationScriptableBase map = userData as LocalizationScriptableBase;
            if (map != null) SetLanguage(map);
        }
        
        public void SetLanguage(LocalizationScriptableBase map)
        {
            localizationMap = map;
            localizationType = map != null ? map.GetType() : null;
            RefreshLanguages();
            if (map != null) selectedLanguageIndex = Array.FindIndex(languageOptions, l => l == map);
        }

        /// <summary>Refreshes the options for the language selection dropdown</summary>
        public void RefreshLanguages()
        {
            if (localizationType == null)
            {
                languageOptions = Array.Empty<LocalizationScriptableBase>();
                languageOptionsNames = Array.Empty<string>();
                return;
            }
            
            languageOptions = Resources.FindObjectsOfTypeAll(localizationType).Cast<LocalizationScriptableBase>().OrderBy(sb => sb.languageName).ToArray();
            languageOptionsNames = languageOptions.Select(l => string.IsNullOrWhiteSpace(l.languageName) ? "Unnamed" : l.languageName).ToArray();
            shouldRefresh = false;
        }
        #endregion

        #region GUI

        /// <summary>Draws the language selection field. </summary>
        /// <param name="onChange">Action to call on language change.</param>
        public void DrawField(Action onChange = null) => DrawField(false, onChange);
        
        /// <summary>Draws the language selection field. </summary>
        /// <param name="drawWithIcon">Draw the blue globe icon next to the text</param>
        /// <param name="onChange">Action to call on language change.</param>
        public void DrawField(bool drawWithIcon, Action onChange)
        {
            string label = GetLanguageWordTranslation(localizationMap.languageName ?? "English");
            GUIContent content = new GUIContent(label);
            if (drawWithIcon) content.image = LocalizationStyles.Styles.globeIcon.Value?.image;
            DrawField(content, onChange);
        }
        
        /// <summary>Draws the language selection field.</summary>
        /// <param name="content">The content to use for the label of the dropdown.</param>
        /// <param name="onChange">Action to call on language change.</param>
        public void DrawField(GUIContent content, Action onChange = null)
        {
            EditorGUI.BeginChangeCheck();
            selectedLanguageIndex = EditorGUILayout.Popup(content, selectedLanguageIndex, languageOptionsNames);
            if (EditorGUI.EndChangeCheck())
            {
                localizationMap = languageOptions[selectedLanguageIndex];
                localizationType = localizationMap.GetType();
                
                if (!EditorPrefs.HasKey(LocalizationConstants.PREFERRED_LANGUAGE_KEY)) 
                    EditorPrefs.SetString(LocalizationConstants.PREFERRED_LANGUAGE_KEY, localizationMap.languageName);
                
                EditorPrefs.SetString($"{LocalizationConstants.LANGUAGE_KEY_PREFIX}{localizationType.Name}", localizationMap.languageName);
                onChange?.Invoke();
            }

            var dropdownRect = GUILayoutUtility.GetLastRect();
            DoLanguageContextEvent(dropdownRect);
        }

        public void DrawIconOnlyField() => DrawIconOnlyField(EditorGUILayout.GetControlRect(false, 20, GUIStyle.none, GUILayout.Width(20)));
        public void DrawIconOnlyField(Rect rect)
        {
            Color ogColor = GUI.color;
            try
            {
                GUI.color = new Color(0.2f,0.9f,1);
                
                GUI.Label(rect, LocalizationStyles.Styles.globeIcon.Value);
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                DoLanguageClickEvent(rect);
                DoLanguageContextEvent(rect);
            }
            finally
            {
                GUI.color = ogColor;
            }
        }

        internal void DoLanguageClickEvent(Rect rect)
        {
            if (OnLeftClick(rect)) 
                ShowLanguageOptionsMenu();
            
        }
        internal void DoLanguageContextEvent(Rect rect)
        {
            //Refresh the languages when the dropdown for languages gets hovered over.
            if (OnHoverEnter(rect, ref shouldRefresh))
                RefreshLanguages();
            
            if (localizationMap != null && OnContextClick(rect))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent(string.Format(Localize(LocalizationLocalizationKeys.PreferredLanguageMenuItem).text, localizationMap.languageName)), false, () =>
                {
                    SetPreferredLanguage(localizationMap.languageName);
                });
                menu.ShowAsContext();
            }
        }
        #endregion
        
        #region Helper
        public string GetLanguageWordTranslation(string languageName)
        {
            if (!TryGet("LanguageWordTranslation", out var content))
            {
                TryGetLanguageWordTranslation(languageName, out string translatedWord);
                content = TempContent(translatedWord);
            }

            return content.text;
        }

        public void ShowLanguageOptionsMenu()
        {
            GenericMenu menu = new GenericMenu();
            for (var i = 0; i < languageOptions.Length; i++)
            {
                var l = languageOptions[i];
                menu.AddItem(new GUIContent(languageOptionsNames[i]), selectedLanguageIndex == i, SetLanguage, l);
            }

            menu.ShowAsContext();
        }
        #endregion
    }
}
