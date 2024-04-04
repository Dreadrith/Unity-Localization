using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DreadScripts.Localization
{
    public class LocalizationLocalization : LocalizationScriptableBase
    {
        public override string hostTitle { get; } = "Localization Localization";
        public override LocalizationKeyCategory[] LocalizationKeyCollections { get; } =
        {
            new LocalizationKeyCategory("Main", typeof(LocalizationLocalizationKeys)),
            new LocalizationKeyCategory("Placeholder", typeof(LocalizationPlaceholderKeys))
        };
    }
}

