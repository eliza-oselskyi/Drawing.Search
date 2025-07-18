# Search in Drawing

Search in Drawing allows you to search for different types of objects in an active drawing in the TeklaStructures Drawing Editor.

## Features

* **Multiple Search Types**:
  * **Part Marks** - Search for associative notes and part marks
  * **Text Objects** - Search through any text elements in the drawing
  * **Assemblies** - Search for assembly representations with options to show all parts
  
* **Advanced Search Options**:
  * **Regular Expression (RegEx)** support for complex search patterns
  * **Wildcard** search mode for simpler queries
  * **Dark/Light** theme support

* **Performance Features**:
  * Parallel processing for faster searches
  * Smart caching system for improved performance
  * Real-time search status updates
  * Previous search suggestions

## Usage

1. Run the macro from Applications and Components:

    ```txt
    Search_Drawings
    ```

2. Choose your search type (Part Marks, Text, or Assembly)

3. Enter your search query:

    For example with RegEx mode:

    ```regex
    80Z-[1-9]
    ```

    This finds any match that starts with `80Z-` followed by a single digit from 1 through 9.

4. Use additional options as needed:
   * Switch between RegEx and Wildcard search modes
   * Enable "Show all assembly parts" for assembly searches
   * Switch between dark/light themes

<details>
    <summary>
    <b>Search Tips</b>
    </summary>

### Regex Quickstart Guide

Here are some common regex patterns to get you started:

| Pattern | Description | Example | Matches |
|---------|-------------|---------|---------|
| `^` | Start of line | `^80` | "80Z-1", "80-A2" |
| `$` | End of line | `1$` | "A-1", "80Z-1" |
| `\d` | Any digit | `A\d` | "A1", "A5" |
| `\d+` | One or more digits | `A\d+` | "A1", "A123" |
| `[abc]` | Any character in brackets | `[ABC]1` | "A1", "B1", "C1" |
| `[a-z]` | Any character in range | `[A-C]1` | "A1", "B1", "C1" |
| `.` | Any single character | `A.1` | "AA1", "AB1" |
| `*` | Zero or more of previous | `A\d*` | "A", "A1", "A123" |
| `+` | One or more of previous | `A\d+` | "A1", "A123" |
| `?` | Zero or one of previous | `A\d?` | "A", "A1" |

#### Common Examples:

1. Match any mark starting with 80Z:
   ```regex
   ^80Z
   ```

2. Match assembly numbers of an RCB assembly, 1–99:
   ```regex
   ^RCB-[1-9]\d?$
   ```

3. Match part marks with optional revision (A-Z):
   ```regex
   \d+[A-Z]?$
   ```

To learn more about regular expressions, check out these resources:
- [Interactive RegEx Guide](https://www.regexone.com/)
- [Learn RegEx](https://github.com/ziishaned/learn-regex)
- [Quick Reference](https://gist.github.com/Vinoshan/8355823d4c09ec611569025f1d346e28)
- [.NET RegEx Reference](https://github.com/dotnet/docs/blob/main/docs/standard/base-types/regular-expression-language-quick-reference.md)

</details>

## Limitations

<details><summary><b>Searching for CED bubbles</b></summary>

#### Searching for CED bubbles
---

 Searching for CED bubbles is still inconsistent. This has to do with the way NBG has decided to represent them internally. Currently, there are four representations:

- [Search As Text] Text objects: this one, obviously, works, since this is a Native Tekla drawing object. If you insert them in manually, you are most likely using this version.

- [Unsupported] TypicalDetail plugin: This is a new plugin that doesn't expose any of its internal data. Effectively, the program is blind to this type of object as a result.

- [Unsupported] Custom.Detail associative note: This one loads a custom template that also doesn't expose any data to plugin developers. Instead, simply the plain text `Custom.Detail` is displayed internally. Interestingly, if you search verbatim `Custom.Detail` as a part mark, it will match. Not entirely useful for now, unless you want to mass select all of these to delete them, in order to insert them in yourself manually.

- [Search As Part Mark] Detail associative note: This one, thankfully, does have a searchable detail string, but the inconsistency here is that you have to search for it as a different object type.

</details>

<details> <summary> <b> Searching in unexploded charts and notes and other NBG plugins: </b> </summary>

#### Searching in unexploded charts and notes and other NBG plugins:
---

As mentioned in the previous item, the NBG plugins do not give much, if any, information that is contained within them. This means that searching in any of these is impossible.

The good news is, I have been communicating with NBGGS about this issue, and they are willing to work with me to begin implementing this feature.

For now, unfortunately, you would have to explode the plugin first to be able to search for the text contained within it.

</details>