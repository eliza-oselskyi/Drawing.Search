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

            foreach (var x in modelObjects.Select(o => drawing.GetSheet().GetModelObjects(o.Identifier)))
            {
                while (x.MoveNext())
                {
                    if (x.Current != null) doArrayList.Add(x.Current);
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
    }
}