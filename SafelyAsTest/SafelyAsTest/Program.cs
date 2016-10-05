using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GOOAC.Lib;

[module: Constraint.Implies(typeof(SafelyAsSample.IShape), typeof(SafelyAsSample.IGeometry))]

[module: Constraint.Implies(typeof(SafelyAsSample.IShape), typeof(SafelyAsSample.IDrawing))]

[module: Constraint.Implies(typeof(SafelyAsSample.Circle), typeof(SafelyAsSample.Circle))]


// try to fool the checker by overloading the symbol
static class SafelyTest
{
    public static void Safely(int n)
    {
    }
}



namespace SafelyAsSample
{
    class Program
    {
        static void Main(string[] args)
        {
            SafelyTest.Safely(1);

            IShape s1 = new Circle(3);
            IDrawing d1 = To<IDrawing>.Safely(s1);

            Circle d2 = To<Circle>.Safely(s1);

            IDrawing d3 = To<IDrawing>.Safely(d2);

            INotImplemented n = To<INotImplemented>.Safely(s1);
        }
    }
}
