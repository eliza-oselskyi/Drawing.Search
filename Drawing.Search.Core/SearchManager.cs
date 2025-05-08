using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public SearchManager()
        {
        }

        public async Task<bool>  ExecuteSearch(string query)
        {
            var drawing = _drawingHandler.GetActiveDrawing();
            var idList = _drawingHandler.GetModelObjectIdentifiers(_drawingHandler.GetActiveDrawing());
            var objList = _model.FetchModelObjects(idList, false);
            var modelObjects = await Task.Run(() => GetMatchedModelObjects(query, objList));
            if (modelObjects.Count == 0) return false;
            
            var selector = _drawingHandler.GetDrawingObjectSelector();
            var doArrayList = new ArrayList();

            var views = drawing.GetSheet().GetViews();
            while (views.MoveNext())
            {
                if (!(views.Current is View curr) ||
                    (curr.ViewType.Equals(View.ViewTypes.UnknownViewType) &&
                     curr.ViewType.Equals(View.ViewTypes.DetailView) &&
                     curr.ViewType.Equals(View.ViewTypes.SectionView))) continue;
                foreach (var x in modelObjects.Select(o => curr.GetModelObjects(o.Identifier)))
                {
                    while (x.MoveNext())
                    {
                        if (x.Current != null) doArrayList.Add(x.Current);
                    }
                }
            }

            
            selector.SelectObjects(doArrayList, false);
            return true;
        }

        private static List<ModelObject> GetMatchedModelObjects(string query, List<ModelObject> objList)
        {
            return (objList.FindAll((m) =>
            {
                var prop = string.Empty;
                m.GetReportProperty($"ASSEMBLY_POS", ref prop);

                if (prop == string.Empty)
                {
                    m.GetReportProperty($"PART_POS", ref prop);
                }

                if (!(m is Beam beam)) return prop.Equals(query);
                return beam.GetAssembly().GetMainObject().Equals(m) && prop.Equals(query);
            }));
        }

        private static void GetViewType(View view)
        {
            
        }
    }
}