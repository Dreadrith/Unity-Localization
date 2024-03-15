using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using Type = System.Type; 


namespace DreadScripts.Localization
{
    public static class ReflectionSplitterGUILayout
    {
        private const string ORIGINAL_TYPE_FULL_NAME = "UnityEditor.SplitterGUILayout, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        private const string SPLITTER_TYPE_FULL_NAME = "UnityEditor.SplitterState, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        private static readonly Color defaultColor = new Color(0.33f, 0.33f, 0.33f);
        
        #region Reflection Properties
        public static Type _originalClassType;
        public static Type originalClassType => _originalClassType ?? (_originalClassType = Type.GetType(ORIGINAL_TYPE_FULL_NAME));
        
        private static Type _splitterStateType;
        public static Type splitterStateType => _splitterStateType ?? (_splitterStateType = Type.GetType(SPLITTER_TYPE_FULL_NAME));

        private static ConstructorInfo _splitterStateConstructor;
        public static ConstructorInfo splitterStateConstructor
        {
            get
            {
                if (_splitterStateConstructor == null)
                    _splitterStateConstructor = splitterStateType.GetConstructor(new[] {typeof(float[])});
                return _splitterStateConstructor;
            }
        }

        private static MethodInfo _splitMethod;
        public static MethodInfo splitMethod
        {
            get
            {
                if (_splitMethod == null)
                    _splitMethod = originalClassType.GetMethod("BeginSplit", new[] {splitterStateType, typeof(GUIStyle), typeof(bool), typeof(GUILayoutOption[])});
                return _splitMethod;
            }
        }
        
        private static MethodInfo _endLayoutMethod;
        public static MethodInfo endLayoutMethod
        {
            get
            {
                if (_endLayoutMethod == null)
                    _endLayoutMethod = typeof(GUILayoutUtility).GetMethod("EndLayoutGroup", BindingFlags.Static | BindingFlags.NonPublic);
                return _endLayoutMethod;
            }
        }
        #endregion

        #region Splitting Methods
        public static object CreateSplitterState(params float[] relativeSizes) => splitterStateConstructor.Invoke(new object[] {relativeSizes});
        public static void BeginHorizontalSplit(object splitterState, GUIStyle style = null, params GUILayoutOption[] options) => BeginSplit(splitterState, style, false, options);
        public static void BeginVerticalSplit(object splitterState, GUIStyle style = null, params GUILayoutOption[] options) => BeginSplit(splitterState, style, true, options);
        public static void BeginSplit(object splitterState, GUIStyle style = null, bool isVertical = true, params GUILayoutOption[] options) => splitMethod.Invoke(null, new object[] {splitterState, style ?? GUIStyle.none, isVertical, options});

        public static void EndSplit() => endLayoutMethod.Invoke(null, null);
        #endregion
        
        #region Drawing Methods

        public static void DrawTitle(string title) => DrawTitle(new GUIContent(title));
        public static void DrawTitle(GUIContent content)
        {
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            DrawHorizontalSplitter();
            GUILayout.Space(7f);
            //GUILayout.Label(GUIContent.none, GUILayout.Height(5));
        }
        public static void DrawVerticalSplitter(Rect rect = default, Color color = default)
        {
            if (color == default)
                color = defaultColor;
            
            if (rect == default) rect = GUILayoutUtility.GetLastRect();
            rect.width = 1.5f;
            rect.x -= 2f;
            EditorGUI.DrawRect(rect, color);
        }
        
        public static void DrawHorizontalSplitter(Color color = default)
        {
            if (color == default)
                color = defaultColor;
            float thickness = 1.5f;
            int padding = 2;
            
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(thickness + padding));
            r.height = thickness;
            r.y += padding/2f;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
        #endregion
        
    }

}
