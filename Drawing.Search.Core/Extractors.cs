using Drawing.Search.Core.Interfaces;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using ModelObject = Tekla.Structures.Model.ModelObject;

namespace Drawing.Search.Core;

public class ModelObjectExtractor : IDataExtractor
{
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

public class TextElementExtractor : IDataExtractor
{
    public string ExtractSearchableString(object obj)
    {
        if (obj is TextElement textElement) return textElement.GetUnformattedString();
        return "";
    }
}

public class TextExtractor : IDataExtractor
{
    public string ExtractSearchableString(object obj)
    {
        var text = obj as Text;
        return text.TextString;
    }
}

public class MarkExtractor : IDataExtractor
{
    public string ExtractSearchableString(object obj)
    {
        if (obj is Mark mark) return ParseUnformattedString(mark.Attributes.Content.GetUnformattedString());
        return "";
    }
    
    private static string ParseUnformattedString(string str)
    {
        return str.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "").Replace("\n", "").Trim();
    }
}
