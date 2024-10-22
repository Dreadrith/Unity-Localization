using System;
using UnityEditor;
using UnityEngine;

namespace DreadScripts.Localization
{
	public class LocalizationHandlerBase
	{
		private static readonly Lazy<GUIContent> lazyGlobeIcon = new Lazy<GUIContent>(() => new GUIContent(EditorGUIUtility.IconContent("BuildSettings.Web.Small")){tooltip = "Language"});
		public static GUIContent globeIcon => lazyGlobeIcon.Value;
	}
}
