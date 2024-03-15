using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DreadScripts.Localization
{
    public static class LocalizationHelper
    {
        public static readonly GUIContent missingContent = new GUIContent("[Missing Content]", "This content is missing from the localization file");
        public static readonly GUIContent addTranslationIcon = new GUIContent(EditorGUIUtility.IconContent("d_ol_plus")){tooltip = "Add Translation"};
        public static readonly GUIContent popoutIcon = new GUIContent(EditorGUIUtility.IconContent("ScaleTool")) {tooltip = "Popout"};
        public static readonly GUIContent helpIcon = new GUIContent(EditorGUIUtility.IconContent("_Help")) {tooltip = "Help"};
        public static readonly GUIContent _tempContent = new GUIContent();

        public static T ReadyWindow<T>(string title) where T : EditorWindow
        {
            var windows = Resources.FindObjectsOfTypeAll<T>();
            var window = windows.Length > 0 ? windows[0] : ScriptableObject.CreateInstance<T>();
            window.titleContent = new GUIContent(title);
            return window;
        }
        
        public static GUIContent TempContent(string text, string tooltip = "", Texture2D icon = null)
        {
            _tempContent.text = text;
            _tempContent.tooltip = tooltip;
            _tempContent.image = icon;
            return _tempContent;
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

        public static bool OnHoverEnter(Rect r, ref bool b)
        {
            Event e = Event.current;
            if (!r.Contains(e.mousePosition)) b = true;
            else if (b) return !(b = false);

            return false;
        }

        
        public static GUIContent ToGUIContent(this MiniContent mc) => ToGUIContent(mc, (GUIContent)null, null);
        public static GUIContent ToGUIContent(this MiniContent mc, string fallback) => ToGUIContent(mc, fallback == null ? null : new GUIContent(fallback), null);
        public static GUIContent ToGUIContent(this MiniContent mc, GUIContent fallback) => ToGUIContent(mc, fallback, null);
        public static GUIContent ToGUIContent(this MiniContent mc, Texture2D icon) => ToGUIContent(mc, (GUIContent)null, icon);
        public static GUIContent ToGUIContent(this MiniContent mc, string fallback, Texture2D icon) => ToGUIContent(mc, fallback == null ? null : new GUIContent(fallback), icon);
        public static GUIContent ToGUIContent(this MiniContent mc, GUIContent fallback, Texture2D icon)
        {
            if (mc == null) return fallback ?? missingContent;
            
            GUIContent content = mc;
            if (!ReferenceEquals(icon, null)) content.image = icon;
            return content;
        }


    }

    
}

