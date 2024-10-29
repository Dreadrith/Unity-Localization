using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DreadScripts.Localization
{
	public struct MiniCultureInfo
	{
		public string displayName;
		public string englishName;
		public string nativeName;

		public MiniCultureInfo(CultureInfo info)
		{
			englishName = info.EnglishName;
			nativeName = info.TextInfo.ToTitleCase(info.NativeName);
			displayName = $"{englishName} ({nativeName})";
		}

		public MiniCultureInfo(string englishName, string nativeName)
		{
			this.englishName = englishName;
			this.nativeName = nativeName;
			displayName = $"{englishName} ({nativeName})";
		}
	}
}
