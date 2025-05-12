using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using Events = Tekla.Structures.Drawing.UI.Events;
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
        private readonly QueryHandler _queryHandler = new QueryHandler("");
        private readonly Events _events = new Events();

        public SearchManager()
        {
            _events.DrawingLoaded += _queryHandler.ClearCache;
            _events.Register();
        }


        //TODO: Profile this method to see why caching seems to not work. Slow!
        public bool ExecutePartMarkSearch(string query)
        {
            var sw =  Stopwatch.StartNew();
            _queryHandler.Query = query;
            var drawingObjects = GetAllDrawingObjectsInActiveDrawing(out var drawingObjectList);

            while (drawingObjects.MoveNext())
            {
                var curr = drawingObjects.Current;
                if (curr.GetType() != typeof(Mark)) continue;
                var mark = (Mark) curr;
                var content = mark.Attributes.Content;
                drawingObjectList.Add(mark);
            }

            var doList = _queryHandler.MatchAsDrawingObjectMark(drawingObjectList);
            // var doArrayList = new ArrayList();
            // foreach (DrawingObject obj in doList)
            // {
            //     doArrayList.Add(obj);
            // }
            
            var drawingObjectSelector = _drawingHandler.GetDrawingObjectSelector();
            drawingObjectSelector.SelectObjects(doList, false);
            
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            return true;
        }

        private DrawingObjectEnumerator GetAllDrawingObjectsInActiveDrawing(out List<Mark> drawingObjectList)
        {
            var drawingObjects = _drawingHandler.GetActiveDrawing().GetSheet().GetAllObjects();
            if (Cache.TryGetValue(_drawingHandler.GetActiveDrawing(), out drawingObjectList)) return drawingObjects;
            drawingObjectList = new List<Mark>();
            Cache.Set(drawingObjects, drawingObjectList, TimeSpan.FromMinutes(15));
            return drawingObjects;
        }

        public bool ExecuteDetailSearch(string query)
        {
            var sw = new Stopwatch();
            sw.Start();
            _queryHandler.Query = query;
            var drawingObjectSelector = _drawingHandler.GetDrawingObjectSelector();
            var views = _drawingHandler.GetActiveDrawing().GetSheet().GetViews();
            var drawingObjects = _drawingHandler.GetActiveDrawing().GetSheet().GetAllObjects();
            var drawingObjectList = new List<Text>();

            while (drawingObjects.MoveNext())
            {
                var curr = drawingObjects.Current;
                if (curr.GetType() != typeof(Text)) continue;
                var text = (Text) curr;
                drawingObjectList.Add(text);
            }

            var doList = _queryHandler.MatchAsDrawingObjectDetail(drawingObjectList);
            var doArrayList = new ArrayList();
            if (doList != null)
                foreach (var obj in doList)
                {
                    doArrayList.Add(obj);
                }

            drawingObjectSelector.SelectObjects(doArrayList, false);
            
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            return true;
        }

        public bool ExecuteSearch(string query)
        {
            var sw = new Stopwatch();
            sw.Start();
            _queryHandler.Query = query;
            var drawing = GetDrawingData(out var objList);
            var modelObjects = _queryHandler.MatchAsModelObject(objList);
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
        /// Gets the current drawing's raw ModelObject data. Cached, to reduce redundant database reads.
        /// </summary>
        /// <param name="objList">list of ModelObject</param>
        /// <returns>The current active drawing</returns>
        private Tekla.Structures.Drawing.Drawing GetDrawingData(out List<ModelObject> objList)
        {
            var drawing = _drawingHandler.GetActiveDrawing();
            if (Cache.TryGetValue(drawing, out objList)) return drawing;
            
            var idList = _drawingHandler.GetModelObjectIdentifiers(_drawingHandler.GetActiveDrawing());
            objList = _model.FetchModelObjects(idList, false);
            Cache.Set(drawing, objList, TimeSpan.FromMinutes(15));
            return drawing;
        }
    }
}