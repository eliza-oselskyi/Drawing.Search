# Drawing Peeker

<!--<img src="https://ai.github.io/size-limit/logo.svg" align="right"-->
 <!--alt="Size Limit logo by Anton Lovchikov" width="120" height="178">-->

Drawing Peeker allows you to search for different types of objects in an active drawing in the TeklaStructures Drawing Editor.

* **Wildcard** and **Regular Expression (RegEx)** support.
* Search for **Assemblies**, **Text objects** and **Part Marks**
* Caches queries temporarily to provide quicker look-ups for duplicate searches.

<!--<p align="center">-->
  <!--<img src="./img/example.png" alt="Size Limit CLI" width="738">-->
<!--</p>-->


<!--## How It Works-->
<!--1.-->

## Installation

<details><summary><b>Show instructions</b></summary>

1. Go to the [ latest release ](https://github.com/eliza-oselskyi/Drawing.Search/releases/tag/v1.0.1) and download the `.zip` file.

2. Extract the `.zip` file to any location on your computer.

3. Run the `install_script` with powershell.

    ```txt
    Right Click on install script > Click "Run with Powershell"
    ```
4. In Tekla, go to Applications and Components and click on the "hamburger" menu.

5. Under `Catalog Management`, click `Reload Catalog`.

That should be it! It automatically installs everything where it needs to be.

</details>

## Usage

Starting your first search is very trivial:

<!--<details><summary><b>Show instructions</b></summary>-->

1. Run the macro from Applications and Components:

    ```txt
    Search_Drawings
    ```

---

2. By default, **Part Marks** are selected. Type in a search query:

    For example:

    ```regex
    80Z-[1-9]
    ```

    This states to find any match that starts with `80Z-` and the following character can be a range between 1 through 9.

    Currently, the search query types are matched using regular expressions. Don't worry, it works very similarly to how the model view search box searches for things, but with much more flexibility in terms of composing search queries.*

    There will be an option to turn on the same kind of searching style as the model view (called wildcards) in future versions.

<details>
    <summary>
    <b>
        <u>
            Notes
        </u>
    </b>
    </summary>

To learn more about more advanced search queries (regular expressions), see this [interactive guide](https://www.regexone.com/) and [this](https://github.com/ziishaned/learn-regex), or check out this [quick reference](https://gist.github.com/Vinoshan/8355823d4c09ec611569025f1d346e28) and [this one](https://github.com/dotnet/docs/blob/main/docs/standard/base-types/regular-expression-language-quick-reference.md) to get started quickly.

</details>

---

3. The search function works the exact same way for different objects.
Searching assemblies will select the main part of the actual assembly representation in the drawing, while part mark search will select the part mark (usually an associative note) of the related part. Searching text will search for any `Text` element in the drawing, including some CED bubbles. Please see [ this section ](#searching-for-ced-bubbles) for a list of current limitations.

<!--</details>-->


## Limitations

<!--<details><summary><b>Show</b></summary>-->

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

The good news is, I have been communicating with NBGGS about this issue and they are willing to work with me to begin implementing this feature.

For now, unfortunately, you would have to explode the plugin first to be able to search for the text contained within it.

</details>
