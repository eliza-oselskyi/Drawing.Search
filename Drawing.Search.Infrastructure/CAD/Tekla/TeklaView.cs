using System;
using Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;

namespace Drawing.Search.Infrastructure.CAD.Tekla;

public class TeklaView
{
    public TeklaView(View view)
    {
        View = view;
        BoundingBox = View.GetAxisAlignedBoundingBox();
    }

    public View View { get; internal set; }
    public AABB BoundingBox { get; internal set; }

    public Tuple<double, double> GetDimensions()
    {
        return new Tuple<double, double>(View.ExtremaCenter.X, View.ExtremaCenter.Y);
    }
}