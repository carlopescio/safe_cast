using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SafelyAsAnalyzer
{
    class Cast
    {
        public readonly ITypeSymbol from;
        public readonly ITypeSymbol to;
        public InvocationExpressionSyntax castExpr;

        public Cast(InvocationExpressionSyntax castExpr, ITypeSymbol from, ITypeSymbol to )
        {
            this.castExpr = castExpr;
            this.from = from;
            this.to = to;
        }
    }
}
