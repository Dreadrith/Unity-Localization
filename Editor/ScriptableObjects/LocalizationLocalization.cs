using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DreadScripts.Localization
{
    internal sealed class LocalizationLocalization : LocalizationScriptableBase
    {
        public override string hostTitle { get; } = "Localization Localization";
        public override KeyCollection[] keyCollections { get; } =
        {
            new KeyCollection("Main", typeof(LocalizationLocalizationKeys)),
            new KeyCollection("Logs & Errors", typeof(LocalizationLogsAndErrorsKeys)),
            new KeyCollection("Placeholder", typeof(LocalizationPlaceholderKeys))
        };
    }
    
    internal enum LocalizationLocalizationKeys
    {
        EditorLanguageSelectionField,
        LanguageNameField,
        SearchField,
        ShowKeyNameToggle,
        ShowComparisonToggle,
        ShowDisplayToggle,
        ComparisonField,
        KeyNameTitle,
        TranslationTitle,
        ComparisonTitle,
        DisplayTitle,
        TranslationTextField,
        TranslationTooltipField,
        ExtrasFoldout,
        HelpIcon,
        CopyCategory,
        PasteCategory,
        CopyAll,
        PasteAll,
        PreferredLanguageMenuItem,
        PopoutAutoClose
    }
    
    internal enum LocalizationLogsAndErrorsKeys
    {
        MissingContent,
        LineParseFailLog,
        KeyNotFoundLog,
        CSVPasteFinishLog,
        PreferredLanguageSetLog
    }

    internal enum LocalizationPlaceholderKeys
    {
        BaseLocalizationTitle,
        BaseLocalizationSelectionHelp,
        BaseLocalizationSelectionField,
    }

   
}

