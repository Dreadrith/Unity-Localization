using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreadScripts.Localization
{
	public static class LocalizationMainHelper
	{
		internal static GUIContent TextToContent(string text) => text == null ? null : new GUIContent(text);
		
		private static readonly GUIContent _tempContent = new GUIContent();
		public static GUIContent TempContent(string text, string tooltip = "", Texture2D icon = null)
		{
			_tempContent.text = text;
			_tempContent.tooltip = tooltip;
			_tempContent.image = icon;
			return _tempContent;
		}
		
		///<summary>Gets the native word of 'Language' in the given language name. If it doesn't exists, returns false and outs 'Language'.</summary>
		public static bool TryGetLanguageWordTranslation(string languageName, out string translatedWord)
		{
			bool found = LocalizationConstants.LanguageWordTranslationDictionary.TryGetValue(languageName, out translatedWord);
			if (!found) translatedWord = "Language";
			return found;
		}

		public static void SetGlobalPreferredLanguage(LocalizationScriptableBase languageMap)
		{
			if (languageMap != null) 
				SetGlobalPreferredLanguage(languageMap.languageName);
		}
		public static void SetGlobalPreferredLanguage(string languageName)
		{
			EditorPrefs.SetString(LocalizationConstants.PREFERRED_LANGUAGE_KEY, languageName);
			Debug.Log($"[Localization] Preferred language set to {languageName}. This will try to be the default language if no specific language was set.");
		}
		
	}
}
