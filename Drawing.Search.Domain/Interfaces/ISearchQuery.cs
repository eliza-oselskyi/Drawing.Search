using System;

namespace Drawing.Search.Domain.Interfaces;

public interface ISearchQuery
{
    public string Term { get; set; }
    public StringComparison CaseSensitive { get; set; }
    public bool Wildcard { get; set; }
}