using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using ModelObject = Tekla.Structures.Model.ModelObject;
using Weld = Tekla.Structures.Drawing.Weld;

namespace Drawing.Search.Core;

public class QueryHandler(string query)
{
    public string Query
    {
        get => _query;
        set => _query = value.ToLower();
    }

    private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
    private string _query = query;

    /// <summary>
    /// Returns a list of model objects that match the query. Cached, to reduce database reads on subsequent duplicate queries
    /// </summary>
    /// <param name="objList">ModelObject list</param>
    /// <returns>List of ModelObject</returns>
    public List<ModelObject> MatchAsModelObject(List<ModelObject> objList)
    {
        var queryRegex = new Regex(Query, RegexOptions.IgnoreCase);
        if (Cache.TryGetValue(Query, out List<ModelObject>? result))
            if (result != null)
                return result;
        result = objList
            .AsParallel().Where((m) =>
            {
                var prop = string.Empty;
                m.GetReportProperty($"ASSEMBLY_POS", ref prop);

                if (prop == string.Empty)
                {
                    m.GetReportProperty($"PART_POS", ref prop);
                }

                if (!(m is Beam beam)) return queryRegex.IsMatch(prop);
                return beam.GetAssembly().GetMainObject().Equals(m) && queryRegex.IsMatch(prop);
            }).
            Select((m) => m)
            .ToList();
        Cache.Set(Query, result, TimeSpan.FromMinutes(30));
        return result;
    }

    public List<DrawingObject>? MatchAsDrawingObjectDetail(List<Text> objList)
    {
        var queryRegex = new Regex(Query, RegexOptions.IgnoreCase);
        
        if (Cache.TryGetValue(Query, out List<DrawingObject>? result))
            if (result != null)
                return result;
        result = objList
            .AsParallel()
            .Where((obj) => queryRegex.IsMatch(obj.TextString))
            .Select(DrawingObject (o) => o)
            .ToList();
        Cache.Set(Query, result, TimeSpan.FromMinutes(30));
        return result;
    }

    public ArrayList MatchAsDrawingObjectMark(List<Mark> objList)
    {
        var queryRegex = new Regex(Query, RegexOptions.IgnoreCase);
        if (Cache.TryGetValue(Query, out ArrayList? arrayList))
            if (arrayList != null)
                return arrayList;
        var result = objList
            .AsParallel()
            .Where((obj) =>
            {
                var content = obj.Attributes.Content.GetUnformattedString();
                var parsedContent = ParseUnformattedString(content);
                return queryRegex.IsMatch(parsedContent);
            })
            .Select(DrawingObject (m) => m)
            .ToList();
        var arrList = new ArrayList();
        foreach (var i in result)
        {
            arrList.Add(i);
        }
        Cache.Set(Query, arrList, TimeSpan.FromMinutes(30));
        return arrList;
    }
    
    private static string WildcardToRegex(string wildcard)
    {
        return "^" + Regex.Escape(wildcard).Replace("\\?", ".").Replace("\\*", ".*") + "$";
    }

    private static string ParseUnformattedString(string str)
    {
        return str.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "").Replace("\n", "").Trim();
    }

    public static void ClearCache()
    {
        Cache.Clear();
    }
}