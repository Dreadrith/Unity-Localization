using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DreadScripts.Localization
{
    [CreateAssetMenu(fileName = "New Localization File", menuName = "DreadScripts/Localization File")]
    internal sealed class LocalizationScriptablePlaceholder : LocalizationScriptableBase
    {
        public override string hostTitle { get; } = "Placeholder";
        public override KeyCollection[] keyCollections { get; } = { };
    }
}
