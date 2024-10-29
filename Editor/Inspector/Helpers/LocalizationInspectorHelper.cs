using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using static DreadScripts.Localization.LocalizationMainHelper;

namespace DreadScripts.Localization
{
    public static class LocalizationInspectorHelper
    {
        private static LocalizationHandler<LocalizationLocalization> _localizationLocalizationHandler;
        internal static LocalizationHandler<LocalizationLocalization> localizationLocalizationHandler => _localizationLocalizationHandler ?? (_localizationLocalizationHandler = LocalizationHandler<LocalizationLocalization>.LoadLanguagesFromAssets());
        public static readonly GUIContent addTranslationIcon = new GUIContent(EditorGUIUtility.IconContent("d_ol_plus")){tooltip = "Add Translation"};
        public static readonly GUIContent popoutIcon = new GUIContent(EditorGUIUtility.IconContent("ScaleTool")) {tooltip = "Popout"};
        public static readonly GUIContent helpIcon = new GUIContent(EditorGUIUtility.IconContent("_Help")) {tooltip = "Help"};
        
        private static readonly GUIContent fallbackMissingContent = new GUIContent("[Missing Content]", "This content is missing from the localization file");

        public static T ReadyWindow<T>(string title) where T : EditorWindow
        {
            var windows = Resources.FindObjectsOfTypeAll<T>();
            var window = windows.Length > 0 ? windows[0] : ScriptableObject.CreateInstance<T>();
            window.titleContent = new GUIContent(title);
            return window;
        }
        
        public static void DrawSeparator()
        {
            int thickness = 2;
            int padding = 10;
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(thickness + padding));
            r.height = thickness;
            r.y += padding / 2f;
            r.x -= 2;
            r.width += 6;
            ColorUtility.TryParseHtmlString(EditorGUIUtility.isProSkin ? "#595959" : "#858585", out Color lineColor);
            EditorGUI.DrawRect(r, lineColor);
        }
        
        public static GUIContent ToGUIContent(this MiniContent mc) => ToGUIContent(mc, (GUIContent)null, null);
        public static GUIContent ToGUIContent(this MiniContent mc, string fallback) => ToGUIContent(mc, TextToContent(fallback), null);
        public static GUIContent ToGUIContent(this MiniContent mc, GUIContent fallback) => ToGUIContent(mc, fallback, null);
        public static GUIContent ToGUIContent(this MiniContent mc, Texture2D icon) => ToGUIContent(mc, (GUIContent)null, icon);
        public static GUIContent ToGUIContent(this MiniContent mc, string fallback, Texture2D icon) => ToGUIContent(mc, TextToContent(fallback), icon);
        public static GUIContent ToGUIContent(this MiniContent mc, GUIContent fallback, Texture2D icon)
        {
            if (mc == null) return fallback ?? GetMissingContent();
            
            GUIContent content = mc;
            if (!ReferenceEquals(icon, null)) content.image = icon;
            return content;
        }
        
        public static GUIContent GetMissingContent() => Localize(LocalizationLogsAndErrorsKeys.MissingContent, fallbackMissingContent);
        internal static GUIContent Localize(LocalizationLocalizationKeys value, GUIContent fallbackContent = null, Texture2D icon = null) => localizationLocalizationHandler.Get(value, fallbackContent, icon);
        internal static GUIContent Localize(LocalizationLogsAndErrorsKeys value, GUIContent fallbackContent = null, Texture2D icon = null) => localizationLocalizationHandler.Get(value, fallbackContent, icon);
        
    }

    
}

