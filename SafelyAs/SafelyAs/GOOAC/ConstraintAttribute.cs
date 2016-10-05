using System;

namespace GOOAC.Lib
{
    public class Constraint
    {
        [AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
        public class ImpliesAttribute : Attribute
        {
            public ImpliesAttribute(Type source, Type target)
            {
            }
        }
    }
}
