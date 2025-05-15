using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using ModelObject = Tekla.Structures.Model.ModelObject;

namespace Drawing.Search.Core;

public class TeklaWrapper
{
    public static void DrawingObjectListToSelection(List<DrawingObject> drawingObjects, Tekla.Structures.Drawing.Drawing drawing)
    {
        var dh = new DrawingHandler();
        var doArrayList = new ArrayList();
        var selector = dh.GetDrawingObjectSelector();

        foreach (var o in drawingObjects)
        {
            doArrayList.Add(o);
        }
        
        selector.SelectObjects(doArrayList, false);
    }
    public static void ModelObjectListToSelection(List<ModelObject> objList, Tekla.Structures.Drawing.Drawing drawing)
    {
        var dh = new DrawingHandler();
        var doArrayList = new ArrayList();
        var selector = dh.GetDrawingObjectSelector();
        
            var views = drawing.GetSheet().GetViews();
            while (views.MoveNext())
            {
                if (!(views.Current is View curr) ||
                    (curr.ViewType.Equals(View.ViewTypes.UnknownViewType) &&
                     curr.ViewType.Equals(View.ViewTypes.DetailView) &&
                     curr.ViewType.Equals(View.ViewTypes.SectionView))) continue;
                if (objList == null) continue;
                foreach (var x in objList.Select(o => curr.GetModelObjects(o.Identifier)))
                {
                    while (x.MoveNext())
                    {
                        if (x.Current != null) doArrayList.Add(x.Current);
                    }
                }
            }
            
            selector.SelectObjects(doArrayList, false);
        
    }
}