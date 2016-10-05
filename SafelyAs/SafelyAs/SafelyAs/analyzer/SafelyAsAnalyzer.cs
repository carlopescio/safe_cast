using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SafelyAsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SafelyAsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return Solution.SupportedDiagnostics();
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction( compilationContext =>
            {
                Solution wholeThing = new Solution();

                compilationContext.RegisterSyntaxNodeAction(wholeThing.AnalyzeImpliesDeclaration, SyntaxKind.Attribute);
                compilationContext.RegisterSyntaxNodeAction(wholeThing.AnalyzeSafelyCall, SyntaxKind.InvocationExpression);
                compilationContext.RegisterSymbolAction(wholeThing.RegisterNamedTypes, SymbolKind.NamedType);
                compilationContext.RegisterCompilationEndAction(wholeThing.AnalyzeConstraintsOnTypes);
            });
        }
    }
}
