using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace JPP.Core.Autocad
{
    interface IDrawingObject
    {
        ObjectId BaseObject { get; set; }

        Point3d Location { get; set; }

        bool Erased { get; }

        double Rotation { get; set; }
    }
}
