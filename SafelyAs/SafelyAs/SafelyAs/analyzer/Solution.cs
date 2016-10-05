using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace SafelyAsAnalyzer
{
    class Solution
    {
        private const string DiagnosticId = "SafelyAsAnalyzer";

        private static readonly LocalizableString Rule0Title = "Implies parameters must be interfaces";
        private static readonly LocalizableString Rule0MessageFormat = "Constraint error: 'Implies' parameters must be interfaces";
        private static readonly LocalizableString Rule0Description = "Implications must be from <Interface1> to <Interface2>";
        private const string Category = "Typing";
        private static DiagnosticDescriptor Rule0 = new DiagnosticDescriptor(DiagnosticId, Rule0Title, Rule0MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Rule0Description);

        private static readonly LocalizableString Rule1Title = "'To' parameter must be an interface";
        private static readonly LocalizableString Rule1MessageFormat = "Safe cast error: 'To' type parameter must be an interface but is {0}";
        private static readonly LocalizableString Rule1Description = "Safe casts must be from <Interface1> to <Interface2>";
        private const string Rule1Category = "Typing";
        private static DiagnosticDescriptor Rule1 = new DiagnosticDescriptor(DiagnosticId, Rule1Title, Rule1MessageFormat, Rule1Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Rule1Description);

        private static readonly LocalizableString Rule2Title = "'Safely' argument must be typed as an interface";
        private static readonly LocalizableString Rule2MessageFormat = "Safe cast error: 'Safely' argument must be typed as an interface, but its type is {0}";
        private static readonly LocalizableString Rule2Description = "Safe casts must be from <Interface1> to <Interface2>";
        private const string Rule2Category = "Typing";
        private static DiagnosticDescriptor Rule2 = new DiagnosticDescriptor(DiagnosticId, Rule2Title, Rule2MessageFormat, Rule2Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Rule2Description);

        private static readonly LocalizableString Rule3Title = "Implication fails on concrete type";
        private static readonly LocalizableString Rule3MessageFormat = "Implication error: {0} implies {1} yet {2} implements {0} but not {1}";
        private static readonly LocalizableString Rule3Description = "Implications must by satisfied by all types";
        private const string Rule3Category = "Typing";
        private static DiagnosticDescriptor Rule3 = new DiagnosticDescriptor(DiagnosticId, Rule3Title, Rule3MessageFormat, Rule3Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Rule3Description);

        private static readonly LocalizableString Rule4Title = "Unsafe cast";
        private static readonly LocalizableString Rule4MessageFormat = "Safe cast error: trying to cast from {0} to {1} but {0} does not imply {1}";
        private static readonly LocalizableString Rule4Description = "Safe casts require implication";
        private const string Rule4Category = "Typing";
        private static DiagnosticDescriptor Rule4 = new DiagnosticDescriptor(DiagnosticId, Rule4Title, Rule4MessageFormat, Rule4Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Rule4Description);


        private ConstrainedTypeSystem types;

        public Solution()
        {
            types = new ConstrainedTypeSystem();
        }

        public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics()
        {
            return ImmutableArray.Create(Rule0, Rule1, Rule2, Rule3, Rule4);
        }

        public void RegisterNamedTypes(SymbolAnalysisContext context)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;
            // concrete types only; also don't waste time keeping track of types not implementing interfaces
            if (!namedType.IsAbstract && !namedType.AllInterfaces.IsEmpty)
                types.AddConcreteType(namedType);
        }

        public void AnalyzeSafelyCall(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            var memberAccessExpr = invocationExpr.Expression as MemberAccessExpressionSyntax;

            if (memberAccessExpr?.Name.ToString() != "Safely") return;

            var safelySymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;

            var safelyContainingType = safelySymbol?.ContainingType;
            var safelyTypeName = safelyContainingType?.MetadataName;
            if (safelyTypeName != "To`1") return;

            //### check also that is from gooa or nothing

            bool typeErr = false;

            var toTypeArg = safelyContainingType.TypeArguments[0];
            if (toTypeArg.TypeKind != TypeKind.Interface)
            {
                typeErr = true;
                var toExpression = (GenericNameSyntax)memberAccessExpr.Expression;
                var toTypeArgs = toExpression.TypeArgumentList;
                var diagnostic = Diagnostic.Create(Rule1, toTypeArgs.GetLocation(), toTypeArg.Name);
                context.ReportDiagnostic(diagnostic);
            }

            var safelyArgs = invocationExpr.ArgumentList;
            if ((safelyArgs?.Arguments.Count ?? 0) != 1) return;
            var safelyArgExpr = safelyArgs.Arguments[0].Expression;

            var safelyArgType = context.SemanticModel.GetTypeInfo(safelyArgExpr).Type;
            if (safelyArgType == null)
                return;
            if (safelyArgType.TypeKind != TypeKind.Interface)
            {
                typeErr = true;
                var diagnostic = Diagnostic.Create(Rule2, safelyArgExpr.GetLocation(), safelyArgType.Name);
                context.ReportDiagnostic(diagnostic);
            }

            if (!typeErr)
            {
                // got a potentially valid call - keep track for final analysis
                types.AddCastCall(invocationExpr, safelyArgType, toTypeArg);
            }
        }

        public void AnalyzeImpliesDeclaration(SyntaxNodeAnalysisContext context)
        {
            var attributeExpr = (AttributeSyntax)context.Node;
            if (attributeExpr?.Name.ToString() != "Constraint.Implies")
                return;

            //### check also that is from gooa or nothing

            var attributeArgs = attributeExpr.ArgumentList;
            if ((attributeArgs?.Arguments.Count ?? 0) != 2)
                return;

            ITypeSymbol from = CheckIsInterface(context, attributeArgs.Arguments[0].Expression);
            ITypeSymbol to = CheckIsInterface(context, attributeArgs.Arguments[1].Expression);

            if (from != null && to != null)
                types.AddImplication(from, to);
        }

        public void AnalyzeConstraintsOnTypes(CompilationAnalysisContext context)
        {
            var violations = types.CheckImplicationConstraints();
            foreach (ImpliesViolation iv in violations)
            {
                var locs = iv.failing.Locations;
                var diagnostic = Diagnostic.Create(Rule3, locs[0], locs, iv.impl.from.Name, iv.impl.to.Name, iv.failing.Name);
                context.ReportDiagnostic(diagnostic);
            }

            IEnumerable<Cast> failedCasts = types.FindUnsafeCasts();
            foreach (Cast c in failedCasts)
            {
                var diagnostic = Diagnostic.Create(Rule4, c.castExpr.GetLocation(), c.from.Name, c.to.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private ITypeSymbol CheckIsInterface(SyntaxNodeAnalysisContext context, ExpressionSyntax arg)
        {
            var typeofExpr = arg as TypeOfExpressionSyntax;
            if (typeofExpr == null)
                return null;
            var type = typeofExpr.Type;
            var typeSymbol = context.SemanticModel.GetSymbolInfo(type).Symbol as ITypeSymbol;
            if (typeSymbol.TypeKind != TypeKind.Interface)
            {
                var diagnostic = Diagnostic.Create(Rule0, type.GetLocation(), "");
                context.ReportDiagnostic(diagnostic);
                return null;
            }
            return typeSymbol;
        }

    }
}
