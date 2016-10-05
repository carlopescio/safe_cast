using Microsoft.CodeAnalysis;

namespace SafelyAsAnalyzer
{
    class TypeImplication
    {
        public readonly ITypeSymbol from;
        public readonly ITypeSymbol to;

        public TypeImplication(ITypeSymbol from, ITypeSymbol to )
        {
            this.from = from;
            this.to = to;
        }

        public bool FailsOn(ITypeSymbol concrete)
        {
            var allIfc = concrete.AllInterfaces.CastArray<ITypeSymbol>();
            return allIfc.Contains(from) && ! allIfc.Contains(to);
        }
    }
}
