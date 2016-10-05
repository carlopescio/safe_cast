using Microsoft.CodeAnalysis;

namespace SafelyAsAnalyzer
{
    class ImpliesViolation
    {
        public readonly ITypeSymbol failing;
        public readonly TypeImplication impl;

        public ImpliesViolation(ITypeSymbol failing, TypeImplication impl)
        {
            this.failing = failing;
            this.impl = impl;
        }
    }
}
