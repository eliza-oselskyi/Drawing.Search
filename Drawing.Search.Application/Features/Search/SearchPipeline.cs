using System;
using System.Collections.Generic;
using System.Linq;
using DotNext;
using Drawing.Search.Domain.Drawings;
using Drawing.Search.Domain.Effects;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Domain.Search;
using Drawing.Search.Infrastructure.CAD.Strategies;

namespace Drawing.Search.Application.Features.Search;

public static class SearchPipeline
{
    public static Result<SearchPlan> TrySearch(IEnumerable<SearchableDrawingObject> objects, SearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Term))
                return new InvalidOperationException(nameof(SearchError.EmptySearchTerm))
                    .ResultFromException<SearchPlan>();

            return Search(objects, request).ResultFromValue();
        }
        catch (ArgumentException ex)
        {
            return new InvalidOperationException(new SearchError.InvalidRegex(request.Term, ex.Message).ToString(), ex)
                .ResultFromException<SearchPlan>();
        }
        catch (Exception ex)
        {
            return new InvalidOperationException(new SearchError.Unexpected(ex.Message).ToString())
                .ResultFromException<SearchPlan>();
        }
    }
    
    public static SearchPlan Search(IEnumerable<SearchableDrawingObject> objects, SearchRequest request)
    {
        if (objects is null) throw new ArgumentNullException(nameof(objects));
        if (request is null) throw new ArgumentNullException(nameof(request));

        var query = new SearchQuery(request.Term, request.CaseSensitive, wildcard: !request.UseRegex);

        Func<string, ISearchQuery, bool> predicate = request.UseRegex
            ? SearchPredicates.Regex
            : SearchPredicates.Wildcard;

        var matches = objects
            .Select(obj => new SearchMatch<SearchableDrawingObject>(obj, obj.SearchText))
            .Where(match => predicate(match.Content, query))
            .ToList();

        var targets = matches
            .Select(match => ToSelectionTarget(match.Item, request))
            .Where(target => target is not null)
            .Cast<SelectionTarget>()
            .ToList();

        return new SearchPlan(
            new SearchSummary(matches.Count, TimeSpan.Zero, matches.Select(match => match.Content).ToList()),
            [ new SearchEffect.SelectTargets(targets), new SearchEffect.SetStatus($"Found {matches.Count} matches.")]);
    }

    private static SelectionTarget? ToSelectionTarget(SearchableDrawingObject obj, SearchRequest request) => obj switch
        {
            SearchableDrawingObject.TextObject text => new SelectionTarget.DrawingObject(text.DrawingId, text.Id),
            SearchableDrawingObject.PartMarkObject partMark => new SelectionTarget.DrawingObject(partMark.DrawingId,
                partMark.Id),
            SearchableDrawingObject.AssemblyObject assembly when request is SearchRequest.Assembly assemblyRequest =>
                new SelectionTarget.AssemblyPosition(assembly.DrawingId, assembly.AssemblyPosition,
                    assemblyRequest.IncludeAllParts),
            _ => null
        };

    public static IReadOnlyList<SearchMatch<T>> Search<T>(
        IEnumerable<T> items,
        Func<T, string> extract,
        ISearchQuery query,
        SearchPredicate predicate)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (extract is null) throw new ArgumentNullException(nameof(extract));
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));

        return items
            .Where(item => item is not null)
            .Select(item => new SearchMatch<T>(item, extract(item) ?? string.Empty))
            .Where(match => predicate(match.Content, query))
            .ToList();
    }
}