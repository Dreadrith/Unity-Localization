#if !LOCALIZATION_FOUND
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace [CHANGEME]
{
	public abstract class LocalizationScriptableBase : ScriptableObject
	{
		public abstract string hostTitle { get; }
		public abstract LocalizationKeyCategory[] LocalizationKeyCollections{ get; }
		[SerializeField] public string languageName = "";
		[SerializeField] internal LocalizedContent[] localizedContent = Array.Empty<LocalizedContent>();
		[Serializable]
		internal class LocalizedContent
		{
			[SerializeField]
			public string keyName;
			[SerializeField]
			public MiniContent content;

			internal LocalizedContent(string keyName, MiniContent content)
			{
				this.keyName = keyName;
				this.content = content;
			}
		}
	}

	[Serializable]
	public class MiniContent
	{
		public string text = "";
		public string tooltip = "";

		public MiniContent(string text)
		{
			this.text = text;
			tooltip = "";
		}
	            
		public MiniContent(string text, string tooltip)
		{
			this.text = text;
			this.tooltip = tooltip;
		}
	        
		public static implicit operator GUIContent(MiniContent content) => new GUIContent(content.text, content.tooltip);
	        
	}


	public class LocalizationHandler
	{

	        private static readonly Dictionary<LocalizationScriptableBase, Dictionary<string, int>> mapToLocalizationCache = new Dictionary<LocalizationScriptableBase, Dictionary<string, int>>();
	        private LocalizationScriptableBase localizationMap;
	        private Type localizationType;
	        
	        private LocalizationScriptableBase[] languageOptions;
	        private string[] languageOptionsNames;
	        private int selectedLanguageIndex;
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
	            
	            var allLanguages = Resources.FindObjectsOfTypeAll(type) as LocalizationScriptableBase[];
	            
	            if (allLanguages == null || allLanguages.Length == 0)
	            {
	                Debug.LogError($"No localization files of type {type.Name} found");
	                SetLanguage(null);
	                return;
	            }
	            
	            LocalizationScriptableBase map = null;
	            
	            if (map == null) 
	                map = allLanguages.FirstOrDefault(m => m.languageName == baseLanguageName);
	            
	            if (map == null) map = allLanguages.First();
	            SetLanguage(map);
	        }

	        private void SetLanguage(LocalizationScriptableBase map)
	        {
		        localizationMap = map;
		        localizationType = map != null ? map.GetType() : null;
		        if (map != null)
		        {
			        languageOptions = (LocalizationScriptableBase[])Resources.FindObjectsOfTypeAll(localizationType);
			        languageOptionsNames = languageOptions.Select(l => string.IsNullOrWhiteSpace(l.languageName) ? "Unnamed" : l.languageName).ToArray();
			        selectedLanguageIndex = Array.FindIndex(languageOptions, l => l == map);
		        }
	        }
	        
	        #endregion

	        #region Get with KeyName
	        public bool TryGet(string keyName, out GUIContent content, Texture2D icon = null)
	        {
	            var mc = Get_Internal(keyName);
	            bool success = mc != null;
	            if (success)
	            {
		            content = new GUIContent(mc);
		            content.image = icon;
	            }
	            else
	            {
		            content = null;
	            }
	            return success;
	        }
	        
	        public GUIContent Get(string keyName) => Get_Internal(keyName);
	        public GUIContent Get(string keyName, string fallBack) => Get_Internal(keyName, fallBack: fallBack);
	        public GUIContent Get(string keyName, GUIContent fallBack) => Get_Internal(keyName, fallBackContent: fallBack);
	        public GUIContent Get(string keyName, Texture2D icon) => Get_Internal(keyName, icon: icon);
	        public GUIContent Get(string keyName, string fallBack, Texture2D icon) => Get_Internal(keyName, fallBack: fallBack, icon: icon);
	        public GUIContent Get(string keyName, GUIContent fallBack, Texture2D icon) => Get_Internal(keyName, fallBackContent: fallBack, icon: icon);
	        
	        public GUIContent this[string keyName] => Get(keyName);

	        #endregion
	        
	        #region Get with EnumKey
	        public bool TryGet<T2>(T2 key, out GUIContent content, Texture2D icon = null) where T2 : Enum => TryGet(Enum.GetNames(typeof(T2))[(int)(object)key], out content, icon);
	        public GUIContent Get<T2>(T2 enumKey) where T2 : Enum => Get(Enum.GetNames(typeof(T2))[(int)(object)enumKey]);

	        public GUIContent Get<T2>(T2 enumKey, string fallBack) where T2 : Enum => Get(Enum.GetNames(typeof(T2))[(int)(object)enumKey], fallBack);
	        public GUIContent Get<T2>(T2 enumKey, GUIContent fallBack) where T2 : Enum => Get(Enum.GetNames(typeof(T2))[(int)(object)enumKey], fallBack);
	        public GUIContent Get<T2>(T2 enumKey, Texture2D icon) where T2 : Enum => Get(Enum.GetNames(typeof(T2))[(int)(object)enumKey], icon);
	        public GUIContent Get<T2>(T2 enumKey, string fallBack, Texture2D icon) where T2 : Enum => Get(Enum.GetNames(typeof(T2))[(int)(object)enumKey], fallBack, icon);
	        public GUIContent Get<T2>(T2 enumKey, GUIContent fallBack, Texture2D icon) where T2 : Enum => Get(Enum.GetNames(typeof(T2))[(int)(object)enumKey], fallBack, icon);
	        public GUIContent this[Enum key] => Get(key);
	        #endregion
	        
	        internal GUIContent Get_Internal(string keyName, string fallBack = "", GUIContent fallBackContent = null, Texture2D icon = null)
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

		        GUIContent content = new GUIContent(localizedContent[index].content == null ? fallBackContent : localizedContent[index].content);
		        if (content.text == "") content.text = fallBack;
		        if (content.image == null) content.image = icon;
		        return content;
	        }
	        
		public void DrawField(string keyName = "LanguageSelectionField", Action onChange = null)
		{
			if (!TryGet(keyName, out var content))
				content = new GUIContent("Language");
			DrawField(content);
		}

		public void DrawField(GUIContent content, Action onChange = null)
		{
			EditorGUI.BeginChangeCheck();
			selectedLanguageIndex = EditorGUILayout.Popup(content, selectedLanguageIndex, languageOptionsNames);
			if (EditorGUI.EndChangeCheck())
			{
				localizationMap = languageOptions[selectedLanguageIndex];
				localizationType = localizationMap.GetType();
				onChange?.Invoke();
			}
		}
	}

	public class LocalizationKeyCategory
	{
		public LocalizationKeyCategory(string name, Type type)
		{
			
		}
	}
}
#endif