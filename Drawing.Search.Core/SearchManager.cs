using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures;
using Tekla.Structures.Drawing;
using Tekla.Structures.Drawing.UI;
using Tekla.Structures.DrawingInternal;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using Events = Tekla.Structures.Drawing.UI.Events;
using ModelObject = Tekla.Structures.Model.ModelObject;
using Part = Tekla.Structures.Drawing.Part;
using Task = System.Threading.Tasks.Task;
using View = Tekla.Structures.Drawing.View;
using System.Windows;

namespace Drawing.Search.Core
{
    public class SearchManager
    {
        private readonly DrawingHandler  _drawingHandler = new();
        private readonly Model _model = new();
        private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
        private readonly Events _events = new();

        public SearchManager()
        {
            if (!_model.GetConnectionStatus()) throw new ApplicationException("Connection not established");
            _events.DrawingLoaded += QueryHandler.ClearCache;
            _events.Register();
        }

        public bool ExecutePartMarkSearch(string query)
        {
            var sw =  Stopwatch.StartNew();
            var drawingObjects = GetAllDrawingObjectsInActiveDrawing(out var drawingObjectList);

            while (drawingObjects.MoveNext())
            {
                var curr = drawingObjects.Current;
                if (curr.GetType() != typeof(Mark)) continue;
                var mark = (Mark) curr;
                var content = mark.Attributes.Content;
                drawingObjectList.Add(mark);
            }

            var searcher = new ObservableSearch<Mark>([new RegexMatchStrategy<Mark>()], new MarkExtractor());
            var searchObserver = new SearchResultObserver();
            searcher.Subscribe(searchObserver);
            

            var res = searcher.Search(drawingObjectList, new SearchQuery(query));
            var dolist = res.Select(DrawingObject (mark) => mark).ToList();
            
            TeklaWrapper.DrawingObjectListToSelection(dolist, _drawingHandler.GetActiveDrawing());
            
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
            var drawingObjectSelector = _drawingHandler.GetDrawingObjectSelector();
            var views = _drawingHandler.GetActiveDrawing().GetSheet().GetViews();
            var drawingObjects = _drawingHandler.GetActiveDrawing().GetSheet().GetAllObjects();
            var drawingObjectList = new List<Text>();
            if (drawingObjectList == null) throw new ArgumentNullException(nameof(drawingObjectList));

            while (drawingObjects.MoveNext())
            {
                var curr = drawingObjects.Current;
                if (curr.GetType() != typeof(Text)) continue;
                var text = (Text) curr;
                drawingObjectList.Add(text);
            }

            var searcher = new ObservableSearch<Text>([new RegexMatchStrategy<Text>()], new TextExtractor());
            var searchObserver = new SearchResultObserver();
            
            searcher.Subscribe(searchObserver);
            var result = searcher.Search(drawingObjectList, new SearchQuery(query));
            var x = result.Select(DrawingObject (text) => text).ToList();


            TeklaWrapper.DrawingObjectListToSelection(x, _drawingHandler.GetActiveDrawing());
            
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            return true;
        }
        
        public void ExecuteAssemblySearch(string query)
        {
            var sw = new Stopwatch();
            sw.Start();
            
            var dh = new DrawingHandler();
            var model = new Model();
            var allObjects = dh.GetModelObjectIdentifiers(dh.GetActiveDrawing());
            //var objList = model.FetchModelObjects(allObjects, false);
            var drawing = GetDrawingData(out var objList);

            var searcher =
                new ObservableSearch<ModelObject>([new RegexMatchStrategy<ModelObject>()], new ModelObjectExtractor());
            var searchObserver = new SearchResultObserver();
            searcher.Subscribe(searchObserver);
            
            var results = searcher.Search(objList, new SearchQuery(query));
            
            TeklaWrapper.ModelObjectListToSelection(results.ToList(), dh.GetActiveDrawing());
            
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
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