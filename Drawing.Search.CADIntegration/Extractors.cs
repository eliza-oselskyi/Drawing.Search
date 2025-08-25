using System;
using System.Text;
using Drawing.Search.Common.Interfaces;
using Tekla.Structures.Drawing;
using ModelObject = Tekla.Structures.Model.ModelObject;
using Part = Tekla.Structures.Model.Part;

namespace Drawing.Search.CADIntegration;

/// <summary>
///     Extractor for <c>ModelObject</c> to get searchable data from object.
/// </summary>
public class ModelObjectExtractor : IDataExtractor
{
    /// <summary>
    ///     Extracts a searchable string from object.
    /// </summary>
    /// <param name="obj"><c>ModelObject</c> object</param>
    /// <returns>Searchable string.</returns>
    public string ExtractSearchableString(object obj)
    {
        var modelObject = obj as ModelObject;
        var prop = string.Empty;
        if (modelObject == null) return prop;
        if (modelObject is not Part part) return prop;
        modelObject.GetReportProperty($"ASSEMBLY_POS", ref prop);
        var temp = string.Empty;
        modelObject.GetReportProperty($"PART_POS", ref temp);
        prop = prop + "\n" + temp;

        //if (prop == string.Empty) model.GetReportProperty($"PART_POS", ref prop);

        return part.GetAssembly().GetMainObject().Equals(modelObject) ? prop : string.Empty;
    }
}

/// <summary>
///     Extractor for <c>TextElement</c> to get searchable data from object.
/// </summary>
public class TextElementExtractor : IDataExtractor
{
    /// <summary>
    ///     Extracts a searchable string from object.
    /// </summary>
    /// <param name="obj"><c>TextElement</c> object</param>
    /// <returns>Searchable string.</returns>
    public string ExtractSearchableString(object obj)
    {
        if (obj is TextElement textElement) return textElement.GetUnformattedString();
        return "";
    }
}

/// <summary>
///     Extractor for <c>Tekla.Structures.Drawing.Text</c> to get searchable data from object.
/// </summary>
public class TextExtractor : IDataExtractor
{
    /// <summary>
    ///     Extracts a searchable string from object.
    /// </summary>
    /// <param name="obj"><c>Text</c> object</param>
    /// <returns>Searchable string.</returns>
    public string ExtractSearchableString(object obj)
    {
        if (obj is Text text) return text.TextString;
        return "";
    }
}

public class StringExtractor : IDataExtractor
{
    public string ExtractSearchableString(object obj)
    {
        var s = obj as string;
        return s ?? string.Empty;
    }
}

/// <summary>
///     Extractor for <c>Tekla.Structures.Drawing.Mark</c> to get searchable data from object.
/// </summary>
public class MarkExtractor : IDataExtractor
{
    /// <summary>
    ///     Extracts a searchable string from object.
    /// </summary>
    /// <param name="obj"><c>Mark</c> object</param>
    /// <returns>Searchable string.</returns>
    public string ExtractSearchableString(object obj)
    {
        if (obj is not Mark mark) return "";
        var stringBuilder = new StringBuilder();
        var enumerator = mark.Attributes.Content.GetEnumerator();
        using var disposable = enumerator as IDisposable;
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            if (curr is ElementBase elm) stringBuilder.AppendLine(GetElementValue(elm));
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Gets the value of an element in a Mark.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static string GetElementValue(ElementBase element)
    {
        return element switch
        {
            PropertyElement elm => elm.Value,
            UserDefinedElement elm => elm.Value,
            TextElement elm => elm.Value,
            _ => ParseUnformattedString(element.GetUnformattedString())
        };
    }

    /// <summary>
    ///     Filters an unformatted string to be more searchable.
    /// </summary>
    /// <param name="str">Unformatted string.</param>
    /// <returns>Formatted string.</returns>
    // TODO: Rename this method to FormatUnformattedString
    private static string ParseUnformattedString(string str)
    {
        return str.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "").Replace("\n", "").Trim();
    }
}