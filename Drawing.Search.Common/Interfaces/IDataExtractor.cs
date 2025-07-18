namespace Drawing.Search.Common.Interfaces;

/// <summary>
///     Declares methods for data extraction strategies on different objects.
/// </summary>
public interface IDataExtractor
{
    string ExtractSearchableString(object obj);
}