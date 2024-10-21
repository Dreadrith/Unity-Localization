using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DreadScripts.Localization
{
    public class LocalizationStyles
    {
        private static LocalizationStyles _instance;
        public static LocalizationStyles Styles => _instance ?? (_instance = new LocalizationStyles());

        public readonly GUIStyle
            header = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold, fontSize = 18, alignment = TextAnchor.MiddleLeft},
            centeredHeader = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold, fontSize = 18, alignment = TextAnchor.MiddleCenter},
            fadedLabel = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleRight, contentOffset = new Vector2(-4f, 0), normal = {textColor = new Color(1, 1, 1, 0.33f)}},
            wrappedTextArea = new GUIStyle(EditorStyles.textArea) {wordWrap = true},
            wrappedLabel = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.UpperLeft, wordWrap = true};

        public readonly Lazy<GUIContent> globeIcon = new Lazy<GUIContent>(() => new GUIContent(EditorGUIUtility.IconContent("BuildSettings.Web.Small")){tooltip = "Language"});
    }
}
