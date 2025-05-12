using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using ModelObject = Tekla.Structures.Model.ModelObject;
using Part = Tekla.Structures.Drawing.Part;
using Task = System.Threading.Tasks.Task;
using View = Tekla.Structures.Drawing.View;

namespace Drawing.Search.Core
{
    public class SearchManager
    {
        private readonly DrawingHandler  _drawingHandler = new DrawingHandler();
        private readonly Model _model = new Model();
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        public SearchManager()
        {
        }

        public bool ExecuteSearch(string query)
        {
            var sw = new Stopwatch();
            sw.Start();
            var drawing = _drawingHandler.GetActiveDrawing();
            var idList = _drawingHandler.GetModelObjectIdentifiers(_drawingHandler.GetActiveDrawing());
            var objList = _model.FetchModelObjects(idList, false);
            var modelObjects = GetMatchedModelObjects(query, objList);
            if (modelObjects is { Count: 0 }) return false;
            
            var selector = _drawingHandler.GetDrawingObjectSelector();
            var drawingObjectArrayList = new ArrayList();

            var views = drawing.GetSheet().GetViews();
            while (views.MoveNext())
            {
                if (!(views.Current is View curr) ||
                    (curr.ViewType.Equals(View.ViewTypes.UnknownViewType) &&
                     curr.ViewType.Equals(View.ViewTypes.DetailView) &&
                     curr.ViewType.Equals(View.ViewTypes.SectionView))) continue;
                if (modelObjects == null) continue;
                foreach (var x in modelObjects.Select(o => curr.GetModelObjects(o.Identifier)))
                {
                    while (x.MoveNext())
                    {
                        if (x.Current != null) drawingObjectArrayList.Add(x.Current);
                    }
                }
            }

            
            selector.SelectObjects(drawingObjectArrayList, false);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            return true;
        }

        /// <summary>
        /// Returns a list of model objects that match the query. Cached, to reduce database reads on subsequent duplicate queries
        /// </summary>
        /// <param name="query"></param>
        /// <param name="objList"></param>
        /// <returns>List of ModelObject</returns>
        private static List<ModelObject>? GetMatchedModelObjects(string query, List<ModelObject> objList)
        {
            if (Cache.TryGetValue(query, out List<ModelObject>? result))
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

                    if (!(m is Beam beam)) return prop.Equals(query);
                    return beam.GetAssembly().GetMainObject().Equals(m) && prop.Equals(query);
                }).
                Select((m) => m)
                .ToList();
            Cache.Set(query, result, TimeSpan.FromMinutes(30));
            return result;
        }
    }
}