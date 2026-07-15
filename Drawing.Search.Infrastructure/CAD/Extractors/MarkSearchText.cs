using System;
using System.Text;
using Drawing.Search.Domain.Interfaces;
using Tekla.Structures.Drawing;

namespace Drawing.Search.Infrastructure.CAD.Extractors;

public static class MarkSearchText
{
    /// <summary>
    ///     Extracts a searchable string from object.
    /// </summary>
    /// <returns>Searchable string.</returns>
    public static string Extract(Mark? mark)
    {
        if (mark is null) return string.Empty;
        
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
    private static string GetElementValue(ElementBase element) =>
        element switch
        {
            PropertyElement elm => elm.Value,
            UserDefinedElement elm => elm.Value,
            TextElement elm => GetTextElementValue(elm),
            _ => FormatUnformattedString(element.GetUnformattedString())
        };

    private static string GetTextElementValue(TextElement elm) =>
        !string.IsNullOrWhiteSpace(elm.Value)
            ? elm.Value
            : FormatUnformattedString(elm.GetUnformattedString());

    /// <summary>
    /// Filters an unformatted string to be more searchable.
    /// </summary>
    /// <param name="str">Unformatted string.</param>
    /// <returns>Formatted string.</returns>
    private static string FormatUnformattedString(string str)
    {
        return str.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "").Replace("\n", "").Trim();
    }
}