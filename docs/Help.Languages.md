# Languages

x360ce 4 shows its window in English, 中文 (Simplified Chinese), Українська (Ukrainian), Español, Português (Brazilian),
Deutsch, 日本語 (Japanese), Français, Polski, 한국어 (Korean) and Türkçe. The globe at the right end of the `[Game]` row
picks one; [Help.v4](Help.v4.md#language) says how. This page is for whoever corrects a translation or adds the texts
a language lacks: a person or an AI agent working in the source.

## Where the words are

- **English** is written where it has always been: in the designer files of each page and in the code. It is what every
  translation is made from, and the only place an English text is changed.
- **Each other language** is one file, `App.v4/Languages/{culture}.json` (`zh-Hans`, `uk`, `es`, `pt-BR`, `de`, `ja`,
  `fr`, `pl`, `ko`, `tr`), carried inside `x360ce.exe`. It holds a glossary and the texts, one to a line in the order of
  their keys:

  ```json
  { "Key": "MainControl.SaveAllButton.Text", "Source": "Save All", "Text": "Alle speichern" },
  ```

  `Key` names the text: the page's type, the control's field and the property (`Text`, `ToolTipText`, `HeaderText`,
  `ToolTip`) for what a designer made; the page's type and the field with `.AccessibleName` or `.AccessibleDescription`
  for the name and the explanation the window's header shows under the mouse; or the key the code gave. `Source` is the
  English it was translated from, and `Text` the translation.
- `App.v4/Languages/en.json` lists every English text with what shows it (`Role`) and what it is for (`Purpose`). The
  build writes it; it is never edited by hand.

A text with no translation, or translated from English the program no longer has, shows in English. So a missing
translation never stops anything, and a stale one never says what the program no longer says.

## After a build: what a language lacks

Every build of x360ce 4 starts the program it built with `-Languages`, which builds every page without showing a
window, reads its English, and compares it with each language file. For each language that lacks something the build
shows a warning:

```text
App.v4\Languages\de.json(1,1): warning XLANG001: 12 to add, 3 to update, 1 to remove - see App.v4\Languages\de.todo.json
```

`{culture}.todo.json` beside it says what to do, with the glossary and, beside each text, what shows it and what it is
for, so a short word such as Range is translated in the right sense:

- **Add**: a text the language does not have yet. Add it to the language file with its `Key`, its `Source` and your
  `Text`.
- **Update**: a text whose English changed. `Was` is the old English and `Text` the old translation. Put the new
  `Source` and a new `Text` in the entry.
- **Remove**: a key the program no longer has. Delete its entry.

Build again: the warning goes once nothing is left, and the to-do file is deleted. The build runs the check again on
every build while a language lacks something, and never fails because of it. `XLANG000` says the check itself could not
run, and why; `XLANG002` says a plugin's key does not start with the plugin's name. Neither stops the build, and the
check is ended after two minutes.

The check runs in its own settings folder under `App.v4\obj`, reads no device and makes no virtual controller, and shows
nothing on the screen. It costs about three seconds, and only on a build that compiled the program or changed a
language file, or while a language lacks texts.

## Translating well

**The rule:** translate with the words the language uses for programs and settings, as Windows (Microsoft's
terminology), Steam, Xbox and Logitech G HUB say them, never everyday wording, and use the glossary at the top of the
language's file. Research a term that is unclear, and add it to the glossary with its source as its `Note`:

```json
{ "Source": "dead zone", "Text": "Totzone", "Note": "Microsoft Terminology, Xbox Accessories app" },
```

- Keep each `{0}`-style placeholder, moved to where the grammar needs it.
- Keep the access key: the `&` before a letter (`&Delete`) is the keyboard shortcut. Put one before a fitting letter of
  the translation. Chinese, Japanese and Korean write the English letter after the text, as Windows does:
  `删除(&D)`. `&&` is an ampersand and stays `&&`.
- Keep the names of products and standards as they are: x360ce, XInput, DirectInput, ViGEmBus, HID Hide, Logitech,
  AutoHotkey, SDL, Raw Input, Xbox.
- Use the glossary's word for each of the program's terms (dead zone, anti-dead zone, force feedback, trigger, axis,
  rumble and others), so one term reads the same on every page. Change a term in the glossary and in every text at
  once.
- Keep it as short as Windows' own words for the same thing: labels sit on buttons and tabs. The pages lay themselves
  out by their text, so a longer word moves its neighbours rather than being cut, but a short one reads better.
- Shorten a text only to the shorter word Windows itself uses for the same command, such as Copy for Copy Preset on the
  row of preset buttons, and say why in the glossary's `Note`. Never drop meaning to make a text fit.
- `Tests/Common/LanguageLayoutTest.cs` opens every page in every language at 100 % and 150 %, at the window's first
  size and its smallest, and fails on each control that is covered, cut, past its holder's edge or over the controller
  picture, and on a controller General page that cuts its last row or leaves an empty band below it. Run it after a
  translation changes.

## Texts built in code

A text the program builds while it runs, such as a message or a status line, is written as a key with its English:

```csharp
UiLanguage.T("PadControl.ClearControllerSettings", "Clear Controller Settings")
```

The check finds every such call in the compiled program, so both must be written as literals. A plugin writes
`PluginText.T("MyPlugin.Key", "English")`, its key starting with the plugin's name, and x360ce translates it in the same
files ([Plugins](Help.Plugins.md#words-in-other-languages)).

## Adding a language

Add the language to `AppLanguages.All` in `App.v4/Common/AppLanguages.cs`: its culture, its name in its own words, and
the two-letter country code of its flag. Add the flag as `Resources/Images/v4/flags/flag_{code}_32x32.png` and its line
in `App.v4/Properties/Resources.resx`, and an empty `App.v4/Languages/{culture}.json`:

```json
{
	"Culture": "it",
	"Name": "Italiano",
	"Items": []
}
```

The next build lists every text it lacks.

## The flags

The flags are drawn once from the SVG flags of [flagcdn.com](https://flagcdn.com/), at 32 by 24 pixels on a 32-pixel
square, twice their size at 100%, and the program brings them down to the screen's size. flagcdn's flags are public
domain ([flagpedia.net](https://flagpedia.net/about): "Flag images are in the public domain").
