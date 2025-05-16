using Drawing.Search.Core.Interfaces;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using ModelObject = Tekla.Structures.Model.ModelObject;

namespace Drawing.Search.Core;

/// <summary>
/// Extractor for <c>ModelObject</c> to get searchable data from object.
/// </summary>
public class ModelObjectExtractor : IDataExtractor
{
    /// <summary>
    /// Extracts a searchable string from object.
    /// </summary>
    /// <param name="obj"><c>ModelObject</c> object</param>
    /// <returns>Searchable string.</returns>
    public string ExtractSearchableString(object obj)
    {
        var model = obj as ModelObject;
        var prop = string.Empty;
        if (model == null) return prop;
        if (model is not Beam beam) return prop;
        model.GetReportProperty($"ASSEMBLY_POS", ref prop);

        if (prop == string.Empty)
        {
            model.GetReportProperty($"PART_POS", ref prop);
        }

        return beam.GetAssembly().GetMainObject().Equals(model)  ? prop : string.Empty;
    }
}

/// <summary>
/// Extractor for <c>TextElement</c> to get searchable data from object.
/// </summary>
public class TextElementExtractor : IDataExtractor
{
    /// <summary>
    /// Extracts a searchable string from object.
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
/// Extractor for <c>Tekla.Structures.Drawing.Text</c> to get searchable data from object.
/// </summary>
public class TextExtractor : IDataExtractor
{
    /// <summary>
    /// Extracts a searchable string from object.
    /// </summary>
    /// <param name="obj"><c>Text</c> object</param>
    /// <returns>Searchable string.</returns>
    public string ExtractSearchableString(object obj)
    {
        var text = obj as Text;
        return text.TextString;
    }
}

/// <summary>
/// Extractor for <c>Tekla.Structures.Drawing.Mark</c> to get searchable data from object.
/// </summary>
public class MarkExtractor : IDataExtractor
{
    /// <summary>
    /// Extracts a searchable string from object.
    /// </summary>
    /// <param name="obj"><c>Mark</c> object</param>
    /// <returns>Searchable string.</returns>
    public string ExtractSearchableString(object obj)
    {
        if (obj is Mark mark) return ParseUnformattedString(mark.Attributes.Content.GetUnformattedString());
        return "";
    }
    
    /// <summary>
    /// Filters an unformatted string to be more searchable.
    /// </summary>
    /// <param name="str">Unformatted string.</param>
    /// <returns>Formatted string.</returns>
    // TODO: Rename this method to FormatUnformattedString
    private static string ParseUnformattedString(string str)
    {
        return str.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "").Replace("\n", "").Trim();
    }
}
