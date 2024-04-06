using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Object = UnityEngine.Object;

namespace DreadScripts.Localization
{

    public class LocalizationHandler
    {
        //This is a prefix preference key for the language of a specific type. 1st in language setting priority.
        private const string LANGUAGE_KEY_PREFIX = "DSLocalizationLanguage";
        
        //This is a general preference key for the preferred language. 2nd in language setting priority.
        private const string PREFERRED_LANGUAGE_KEY = "DSLocalizationPreferredLanguage";
        
        internal static readonly Dictionary<Type, EnumToKeyHandler> typeToKeyHandlers = new Dictionary<Type, EnumToKeyHandler>();
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
                
                if (allLanguages == null || allLanguages.Length == 0)
                {
                    Debug.LogError($"No localization files of type {type.Name} found");
                    SetLanguage(null);
                    return;
                }
            }
            
            var prefKey = $"{LANGUAGE_KEY_PREFIX}{type.Name}";
        
            //Tries to load product specific lannguage first, then preferred language, then base language
            
            LocalizationScriptableBase map = null;
            
            if (EditorPrefs.HasKey(prefKey)) 
                map = allLanguages.FirstOrDefault(m => m.languageName == EditorPrefs.GetString(prefKey));
            
            if (map == null && EditorPrefs.HasKey(PREFERRED_LANGUAGE_KEY)) 
                map = allLanguages.FirstOrDefault(m => m.languageName == EditorPrefs.GetString(PREFERRED_LANGUAGE_KEY));
            
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
        
        public GUIContent Get(string keyName) => Get_Internal(keyName).ToGUIContent();
        public GUIContent Get(string keyName, string fallBack) => Get_Internal(keyName).ToGUIContent(fallBack);
        public GUIContent Get(string keyName, GUIContent fallBack) => Get_Internal(keyName).ToGUIContent(fallBack);
        public GUIContent Get(string keyName, Texture2D icon) => Get_Internal(keyName).ToGUIContent(icon);
        public GUIContent Get(string keyName, string fallBack, Texture2D icon) => Get_Internal(keyName).ToGUIContent(fallBack, icon);
        public GUIContent Get(string keyName, GUIContent fallBack, Texture2D icon) => Get_Internal(keyName).ToGUIContent(fallBack, icon);
        
        public GUIContent this[string keyName] => Get(keyName);

        #endregion
        
        #region Get with EnumKey
        public bool TryGet<T2>(T2 key, out GUIContent content, Texture2D icon = null) where T2 : Enum => TryGet(GetKeyhandler(key.GetType()).ETK(key), out content, icon);
        public GUIContent Get<T2>(T2 enumKey) where T2 : Enum => Get(GetKeyhandler(enumKey.GetType()).ETK(enumKey));

        public GUIContent Get<T2>(T2 enumKey, string fallBack) where T2 : Enum => Get(GetKeyhandler(enumKey.GetType()).ETK(enumKey), fallBack);
        public GUIContent Get<T2>(T2 enumKey, GUIContent fallBack) where T2 : Enum => Get(GetKeyhandler(enumKey.GetType()).ETK(enumKey), fallBack);
        public GUIContent Get<T2>(T2 enumKey, Texture2D icon) where T2 : Enum => Get(GetKeyhandler(enumKey.GetType()).ETK(enumKey), icon);
        public GUIContent Get<T2>(T2 enumKey, string fallBack, Texture2D icon) where T2 : Enum => Get(GetKeyhandler(enumKey.GetType()).ETK(enumKey), fallBack, icon);
        public GUIContent Get<T2>(T2 enumKey, GUIContent fallBack, Texture2D icon) where T2 : Enum => Get(GetKeyhandler(enumKey.GetType()).ETK(enumKey), fallBack, icon);
        public GUIContent this[Enum key] => Get(key);
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
        
        public void SetLanguage(LocalizationScriptableBase map)
        {
            localizationMap = map;
            localizationType = map != null ? map.GetType() : null;
            RefreshLanguages();
            if (map != null) selectedLanguageIndex = Array.FindIndex(languageOptions, l => l == map);
        }

        ///<summary>Refreshes the options for the language selection dropdown</summary>
        public void RefreshLanguages()
        {
            if (localizationType == null)
            {
                languageOptions = Array.Empty<LocalizationScriptableBase>();
                languageOptionsNames = Array.Empty<string>();
                return;
            }
            
            languageOptions = (LocalizationScriptableBase[])Resources.FindObjectsOfTypeAll(localizationType);
            languageOptionsNames = languageOptions.Select(l => string.IsNullOrWhiteSpace(l.languageName) ? "Unnamed" : l.languageName).ToArray();
            shouldRefresh = false;
        }

        ///<summary>Draws the language selection field.</summary>
        /// <param name="keyName">The key to use for localizing the label of the dropdown. Falls back to 'Language' if not found.</param>
        /// <param name="onChange">Action to call on language change.</param>
        public void DrawField(string keyName = "LanguageSelectionField", Action onChange = null)
        {
            if (!TryGet(keyName, out var content))
                content = LocalizationHelper.TempContent("Language");
            DrawField(content, onChange);
        }
        
        ///<summary>Draws the language selection field.</summary>
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
                EditorPrefs.SetString(PREFERRED_LANGUAGE_KEY, localizationMap.languageName);
                EditorPrefs.SetString($"{LANGUAGE_KEY_PREFIX}{localizationType.Name}", localizationMap.languageName);
                onChange?.Invoke();
            }
      
            //Refresh the languages when the dropdown for languages gets hovered over.
            if (LocalizationHelper.OnHoverEnter(GUILayoutUtility.GetLastRect(), ref shouldRefresh))
                RefreshLanguages();
        }

        /*public static void ClearCache()
        {
            typeToKeyHandlers.Clear();
            mapToLocalizationCache.Clear();
        }*/

        private static EnumToKeyHandler GetKeyhandler(Type type)
        {
            if (typeToKeyHandlers.TryGetValue(type, out var keyHandler)) return keyHandler;
            
            keyHandler = new EnumToKeyHandler(Enum.GetNames(type));
            typeToKeyHandlers.Add(type, keyHandler);
            return keyHandler;
        }
    }
    
    internal readonly struct EnumToKeyHandler
    {
        private readonly string[] indexToKey;
        internal EnumToKeyHandler(string[] indexToKey)
        {
            this.indexToKey = indexToKey;
        }
        internal string ETK(Enum key) => indexToKey[(int)(object)key];
    }
}
