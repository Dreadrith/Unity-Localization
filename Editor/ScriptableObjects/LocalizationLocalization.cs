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
        public override KeyCollection[] keyCollections { get; } =
        {
            new KeyCollection("Main", typeof(LocalizationLocalizationKeys)),
            new KeyCollection("Placeholder", typeof(LocalizationPlaceholderKeys))
        };
    }
}

