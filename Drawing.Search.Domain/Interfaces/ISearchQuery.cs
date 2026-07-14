using System;
using System.Text.RegularExpressions;

namespace Drawing.Search.Domain.Interfaces;

public interface ISearchQuery
{
    public string Term { get; }
    public StringComparison CaseSensitive { get; }
    public bool Wildcard { get; }
    public Regex CompiledRegex { get; }
    public Regex WildcardRegex { get; }
}