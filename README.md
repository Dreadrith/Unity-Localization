# Unity-Localization
Unity-Localization is a Library for easy Localization for Unity Scripts. It works by creating different Language files, which you can include in your project for easily localization.
### [Get it from here](https://vpm.dreadscripts.com)
<br><img src="https://github.com/user-attachments/assets/637ad79a-b615-49d6-b025-de0a925cf805" width="400" /><br>
> An example Localization file. This localization is the German Localization for the Unity-Localization script.

## Creating the Localization Definition
To get started, create a new C# script that extends `DreadScripts.Localization.LocalizationScriptableBase` 

If your project is in Packages, you will have to add a reference to the Unity-Localization-Core assembly to your Assembly Definition as shown below. It can be found under `Packages > DreadScripts - Localization > Editor > Core > com.dreadscripts.localization.core.asmdef`.
<br><br><img src="https://github.com/user-attachments/assets/92215d0d-d90b-4fe6-ab5b-78c70c04ba81" width ="400" /><br>

In this C# script, you will have to implement two properties:

- `hostTitle`, a string which is shown at the top of the file when editing language files
- `keyCollections`, an array of `KeyCollection`, which is a way of categorizing the Localization Keys.
    - KeyCollection has two constructors:
        - `public KeyCollection(string collectionName, params string[] keyNames)`, which means that the first string you pass to the constructor is the category name, and all the other strings are keys to be Localized.
        - `public LocalizationKeyCategory(string collectionName, Type enumType)`, which means that the string you pass to the constructor is the category name, and for the keys to be Localized you can pass in the type of an Enum. This way, you can make sure that every value you want localized is always covered simply by adding more values to the Enum.

Here is an example of the script.
```csharp
using DreadScripts.Localization;
namespace DreadScripts.TestScript {
	public class ExampleLocalizationUsingStrings : LocalizationScriptableBase
	{
		/*          */ public override string hostTitle => "TestScript";
		/* OPTION 1 */ public override KeyCollection[] keyCollections => new [] { new KeyCollection("ExampleName", "TestKey1", "TestKey2", "TestKey3") };
		/* OPTION 2 */ public override KeyCollection[] keyCollections => new [] { new KeyCollection("ExampleName", typeof(LocalizationKeys)) };
	}
}
```
---
## Creating a Localization File

Now that the Localization Scriptable class has been created, we can create our first Localization File. This is done by right clicking in your Project folder and clicking `Create` → `DreadScripts` → `Localization File` as shown below.
<br><br><img src="https://github.com/user-attachments/assets/37a4fd90-11ce-4ae3-b857-3b227a4d3589" width="400" /><br>

This file will have two dropdowns, as shown below.<br>
In the top dropdown, select the language you want the Localization editor to have.<br>
In the bottom dropdown, select the type of the Localization asset you want to make.
<br><br><img src="https://github.com/user-attachments/assets/13019cb7-298c-4d8d-8406-06386a91840b" width="400" /><br>

After selecting the Localization asset type, the Inspector will transform into the Inspector shown below.<br>
In here, you can select or fill in the Language name for this language, and for every Key, you can press the circular + in the ‘Translation’ column, and edit the text. If you want to edit the tooltip, you can press the triangle to the left of the Key, and it will show the tooltip on the second row.
<br><br><img src="https://github.com/user-attachments/assets/346b0b04-0c1f-4a85-b514-8cf0a5c69723" width="400" /><br>

If you already have one or more Localization assets completed, you can compare with them by selecting them in the Comparison Language dropdown. 

If you have multiple collections defined, you can swap between them by clicking their name above the Translation table.

---
## Using the Localization Assets

### Instancing

In the editor script you want to Localize, Make a new instance of the `LocalizationHandler<T>` class by passing in your Localization Scriptable’s type, like this:

```csharp
LocalizationHandler<ExampleLocalizationUsingEnum> handler = new LocalizationHandler<ExampleLocalizationUsingEnum>();
```

Here’s the definition of the constructor:

```csharp
/// loadFromAssets: Whether to load localization files of this handler's type from the assets.
/// defaultLanguageName: The default language to load if no preferred language can be found.
/// builtinLanguages: Additional localization scriptables to load.
public LocalizationHandler(bool loadFromAssets, string defaultLanguageName, params T[] builtinLanguages)
```

### Language Selection

There’s 3 ways to offer language selection:<br>
- `handler.DrawField()` : Draws a dropdown field where the user can select a loaded language.
- `handler.DrawIconOnlyField()` : Draws a blue globe icon that upon click, shows a menu to select a loaded language from.
- `handler.DrawLanguageSelectionList()` : Draws all loaded languages names as big buttons that the user can click to select a language.

### Localizing
When getting any localized content, it will return a `GUIContent` based on the Key given and the currently selected language. If the content doesn’t exist, it will return either the `fallBack` content if available or “Missing Content” as the final fallback. To determine whether a key exists, use `TryGet`. The key may be either a `string` or `Enum`.<br>
Ways to get localized content:<br>
- `handler[key]`: The basic method of getting the localized content using the key.
- `handler.Get(key)`: Has multiple overloads to get the localized content using a key and optionally giving it fallback content or an icon.
- `handler.TryGet(key, out content)` : Returns a bool and outs the content. Use this to determine whether the localized content is missing.

All the overloads of `handler.Get`:<br>
```csharp
// Final fallback content is GUIContent with text "[Missing Content]". This will be abbreviated with [MS]. [MS] is returned if the Content is missing and the fallBack is not set or null.
GUIContent Get(string/enum key); // Returns [MS] if missing.
GUIContent Get(string/enum key, string fallBack); // If missing, returns fallBack as GUIContent.
GUIContent Get(string/enum key, GUIContent fallBack); // If missing, returns fallBack.
GUIContent Get(string/enum key, Texture2D icon); // If not missing, gives it an icon, otherwise returns [MS].
GUIContent Get(string/enum key, string fallBack, Texture2D icon); // If not missing, gives it an icon, otherwise returns fallBack as GUIContent.
GUIContent Get(string/enum key, GUIContent fallBack, Texture2D icon) => StringGet_Internal(keyName, fallBack, icon); // if not missing, gives it an icon, otherwise returns fallBack.
```

An example script using this is shown below:<br>
```csharp
using DreadScripts.Localization;
using UnityEditor;
using UnityEngine;

namespace DreadScripts.TestScript
{
	public class ExampleLanguageScript : EditorWindow
	{
		[MenuItem("Example/Show Example Script")]
		private static void ShowWindow() => GetWindow<ExampleLanguageScript>();
		
		private LocalizationHandler<ExampleLocalizationUsingEnum> handler = new LocalizationHandler<ExampleLocalizationUsingEnum>();

		private void OnGUI()
		{
			handler.DrawField();
			GUILayout.Label(handler[ExampleLocalizationUsingEnums.LocalizationKeys.TestKey1]);
			GUILayout.Label(handler[ExampleLocalizationUsingEnums.LocalizationKeys.TestKey2]);
			GUILayout.Label(handler[ExampleLocalizationUsingEnums.LocalizationKeys.TestKey3]);
		} 
	}
}
```
<br><br>
<img src="https://github.com/user-attachments/assets/cc18d25e-0925-4a2b-88d6-5bb367bcaafb" width="300" />
<img src="https://github.com/user-attachments/assets/663e5e0e-0a3d-4ffb-b972-603e1e704855" width="300" />
<br>
And that’s it! Your script can now be read bymore people! But, it does mean the script depends on Unity-Localization to work…

---
## Dependency Management

Whether to keep the dependency or not is up to you. Either way should not have any errors or conflicts but has its pros and cons.
**Keeping the dependency:**<br>
- Pro: Less clutter in your package.
- Pro: Polished editor to edit the localization files.
- Pro: Certain package managers can easily import the localization package along with it
- Con: Has to rely on the Unity-Localization package and doesn’t work without it.

**Removing the dependency:**<br>
- Pro: Can work as an independent package.
- Con: More clutter in your package.
- Con: Difficult to edit the localization files, but if the original package is included, then the editor will be used.
- Con: More fuss to set up.

### Keeping the dependency

If you’re keeping the dependency, open your package’s .json in your text editor of choice, and add `com.dreadscripts.localization` as one of the `vpmDependencies` like so.

![image](https://github.com/user-attachments/assets/47beaab2-bb84-4729-bf46-343e9e7d4e8c)

### Removing the dependency

This one’s a bit more complicated but we tried making it as easy as possible. If you’re curious about the technicality, read below. Here are the steps to remove the dependency:

1. Copy the “Core” folder in `Packages > DreadScripts - Localization > Editor > Core` and paste it anywhere in your project’s folder. 
> [!WARNING]
> Make sure that the GUIDs in the .meta files of the copy are not the same as the original!
2. Click on the asmdef `com.dreadscripts.localization.core` included in the copy folder. You can optionally change the file’s name (Recommended).
3. Through the inspector, change the `Name` field to be unique like so

![image](https://github.com/user-attachments/assets/8f160f27-95cb-4fce-9b26-d04825d64d8b)

4. Scroll down and clear up the Expression field from its original ‘9.9.9’ value:

![image](https://github.com/user-attachments/assets/162b7603-cfc2-481d-b48c-f742b305f5ca)

5. Press `Apply` in the bottom right to confirm changes.
6. Go to your package’s asmdef and add the Copy Core’s asmdef as a reference

![image](https://github.com/user-attachments/assets/fc075b43-5839-4766-a7ed-948641f1eabf)

7. Done! This makes it so that if Unity-Localization is not imported, then your package will use the Core copy included in your package.

<br>
<details>
  <summary>How the dependency exclusion works</summary>
  <blockquote>
    <ul>
      <li>We copy over the Core folder because that’s the part responsible for the logic and most things that your script may user. The Inspector folder is for the Custom Editor that Unity-Localization provides to make it easier to make Localization files.</li>
      <li>The GUIDs MUST be different because otherwise, scriptable objects may reference the wrong script between the original and your package and may cause your package to overwrite the original files of Unity-Localization when importing.</li>
      <li>You can guarantee that GUIDs are different by NOT including them when copying over the Core folder. If Unity-Localization already exists in the project, Unity will likely automatically regenerate the GUIDs for the copied Core folder. Please be wary of GUIDs not being different if neither of those criteria were met.</li>
      <li>Asmdef name change is to make it different from the original. An asmdef generates an assembly with that name and no two assemblies may have the same name.</li>
      <li>The Core asmdef is prepped to exclude itself if the definition `DREADSCRIPTS_LOCALIZATION` exists. This definition exists if Unity-Localization package is imported. However, to prevent the original package from its own core by existing, we make the definition have an “always false” condition, hence the `Expression = 9.9.9` field. By clearing this field, the definition may exist if Unity-Localization is imported and exclude the copy from compilation.</li>
    </ul>
</blockquote>
</details>

### Credits
Thanks to [jellejurre](https://github.com/jellejurre) for helping with modifying, testing and documenting Unity-Localization

### Thank you
If you enjoy Unity-Localization, please consider [supporting me ♡](https://ko-fi.com/Dreadrith)!
