#if !DREADSCRIPTS_LOCALIZATION
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace [CHANGEME]
{
	public abstract class LocalizationScriptableBase : ScriptableObject
	{
		public abstract string hostTitle { get; }
		public abstract KeyCollection[] keyCollections{ get; }
		[SerializeField] public string languageName = "";
		[SerializeField] internal LocalizedContent[] localizedContent = Array.Empty<LocalizedContent>();
		[Serializable]
		internal class LocalizedContent
		{
			[SerializeField] public string keyName;
			[SerializeField] public MiniContent content;
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
		public string text;
		public string tooltip;
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
		private const string LANGUAGE_KEY_PREFIX = "DSLocalizationLanguage";
		private const string PREFERRED_LANGUAGE_KEY = "DSLocalizationPreferredLanguage";

		private static readonly Dictionary<LocalizationScriptableBase, Dictionary<string, int>> mapToLocalizationCache = new Dictionary<LocalizationScriptableBase, Dictionary<string, int>>();
		private LocalizationScriptableBase localizationMap;
		private Type localizationType;

		private LocalizationScriptableBase[] languageOptions;
		private string[] languageOptionsNames;
		private int selectedLanguageIndex;

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

		private void SetLanguage(LocalizationScriptableBase map)
		{
			localizationMap = map;
			localizationType = map != null ? map.GetType() : null;
			if (map != null)
			{
				languageOptions = (LocalizationScriptableBase[]) Resources.FindObjectsOfTypeAll(localizationType);
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
        
		public GUIContent Get(string keyName) => StringGet_Internal(keyName, null, null);
		public GUIContent Get(string keyName, string fallBack) => StringGet_Internal(keyName, TextToContent(fallBack), null);
		public GUIContent Get(string keyName, GUIContent fallBack) => StringGet_Internal(keyName, fallBack, null);
		public GUIContent Get(string keyName, Texture2D icon) => StringGet_Internal(keyName, null, icon);
		public GUIContent Get(string keyName, string fallBack, Texture2D icon) => StringGet_Internal(keyName, TextToContent(fallBack), icon);
		public GUIContent Get(string keyName, GUIContent fallBack, Texture2D icon) => StringGet_Internal(keyName, fallBack, icon);
        
		public GUIContent this[string keyName] => Get(keyName);
        
		private GUIContent StringGet_Internal(string keyName, GUIContent fallback, Texture2D icon) => Get_Internal(keyName, fallback, icon);
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
			return Get_Internal(key, fallback, icon);
		}
		#endregion

		internal GUIContent Get_Internal(string keyName, GUIContent fallBackContent = null, Texture2D icon = null)
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

			var mc = localizedContent[index].content;
			GUIContent content = mc ?? (fallBackContent != null ? new GUIContent(fallBackContent) : new GUIContent("[Missing Content]", "This content is missing from the localization file"));
			if (!ReferenceEquals(icon, null)) content.image = icon;
			return content;
		}

		public void DrawField(string keyName = "LanguageSelectionField", Action onChange = null)
		{
			if (!TryGet(keyName, out var content))
				content = new GUIContent("Language");
			DrawField(content, onChange);
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
		private static GUIContent TextToContent(string text) => text == null ? null : new GUIContent(text);
	}

	public struct KeyCollection
	{
		public KeyCollection(string categoryName, params string[] keyNames) { }
		public KeyCollection(string name, Type type) { }
	}
}
#endif