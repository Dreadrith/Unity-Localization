﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

namespace DreadScripts.Localization
{
    public abstract class LocalizationScriptableBase : ScriptableObject
    {
        public abstract string hostTitle { get; }
        public abstract KeyCollection[] keyCollections{ get; }

        [SerializeField] public string languageName = "";
        [SerializeField] internal LocalizedContent[] localizedContent = Array.Empty<LocalizedContent>();

        public void PopulateContent(bool canUndo = true)
        {
            if (canUndo) Undo.RecordObject(this, "Populate Localization");
            var keys = keyCollections.SelectMany(kc => kc.keyNames).ToArray();
            foreach(var k in keys.Except(localizedContent.Select(lc => lc.keyName)))
                localizedContent = localizedContent.Append(new LocalizedContent(k, new MiniContent("Untranslated Text"))).ToArray();
            EditorUtility.SetDirty(this);
        }
    }

    [Serializable]
    internal class LocalizedContent
    {
        public string keyName;
        public MiniContent content;

        internal LocalizedContent(string keyName, MiniContent content)
        {
            this.keyName = keyName;
            this.content = content;
        }
    }
    
    [Serializable]
    public class MiniContent
    {
        public string text = "";
        public string tooltip = "";
        /*[SerializeField] internal string _iconName = "";
        
        internal string iconName
        {
            get => _iconName;
            set
            {
                if (_iconName == value) return;
                _iconName = value;
                iconRequiresLoad = true;
            }
        }

        private bool iconRequiresLoad = true;
        private Texture2D _icon;
        public Texture2D icon
        {
            get
            {
                if (!iconRequiresLoad) return _icon;
                
                iconRequiresLoad = false;
                _icon = string.IsNullOrWhiteSpace(iconName) ? null : EditorGUIUtility.IconContent(iconName).image as Texture2D;
                return _icon;
            }
            set => _icon = value;
        }*/

        public MiniContent(string text)
        {
            this.text = text;
            tooltip = "";
            //iconName = "";
        }
            
        public MiniContent(string text, string tooltip)
        {
            this.text = text;
            this.tooltip = tooltip;
            //iconName = "";
        }
        
        public static implicit operator GUIContent(MiniContent content) => LocalizationHelper.TempContent(content.text, content.tooltip/*, content.icon*/);
        
    }

    public struct KeyCollection
    {
        public string collectionName;
        public string[] keyNames;
        public KeyCollection(string collectionName, params string[] keyNames)
        {
            this.collectionName = collectionName;
            this.keyNames = keyNames;
        }
        
        public KeyCollection(string collectionName, Type enumType)
        {
            this.collectionName = collectionName;
            keyNames = Enum.GetNames(enumType);
        }
    }
}


