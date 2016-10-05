using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SafelyAsAnalyzer
{
    class ConstrainedTypeSystem
    {
        private HashSet<TypeImplication> allImplications;
        private HashSet<ITypeSymbol> allTypes;
        private List<Cast> allCasts;

        public ConstrainedTypeSystem()
        {
            allImplications = new HashSet<TypeImplication>();
            allTypes = new HashSet<ITypeSymbol>();
            allCasts = new List<Cast>();
        }

        public void AddImplication(ITypeSymbol from, ITypeSymbol to)
        {
            allImplications.Add(new TypeImplication(from, to));
        }

        public void AddConcreteType(ITypeSymbol t)
        {
            allTypes.Add(t);
        }

        public void AddCastCall(InvocationExpressionSyntax castExpr, ITypeSymbol from, ITypeSymbol to)
        {
            allCasts.Add(new Cast(castExpr, from, to));
        }

        public List<ImpliesViolation> CheckImplicationConstraints()
        {
            List<ITypeSymbol> allTypesImplementingFrom = AllTypesImplementingSomeFromInterface();
            return CheckAllImplicationsOn(allTypesImplementingFrom);
        }

        public IEnumerable<Cast> FindUnsafeCasts()
        {
            return allCasts.Where(c => !IsSafe(c));
        }

        private bool IsSafe(Cast c)
        {
            // covariance-contravariance check
            return allImplications.Any(ti => DerivesFrom(c.from, ti.from) && DerivesFrom(ti.to, c.to));
            /*
            foreach (TypeImplication ti in allImplications)
            {
                // covariance-contravariance check
                if( DerivesFrom(c.from,ti.from) && DerivesFrom(ti.to,c.to) )
                    return true;
            }
            return false;*/
        }

        private bool DerivesFrom(ITypeSymbol candidateDerived, ITypeSymbol candidateBase)
        {
            if (candidateDerived.Equals(candidateBase))
                return true;
            var allBaseIfc = candidateDerived.AllInterfaces.CastArray<ITypeSymbol>();
            return allBaseIfc.Contains(candidateBase);
        }

        private List<ITypeSymbol> AllTypesImplementingSomeFromInterface()
        {
            List<ITypeSymbol> res = new List<ITypeSymbol>();
            HashSet<ITypeSymbol> allFromIfc = new HashSet<ITypeSymbol>(allImplications.Select(impl => impl.from));

            foreach (ITypeSymbol concrete in allTypes)
            {
                var allImplemented = concrete.AllInterfaces.CastArray<ITypeSymbol>();
                if (allFromIfc.Overlaps(allImplemented))
                    res.Add(concrete);
            }

            return res;
        }

        private List<ImpliesViolation> CheckAllImplicationsOn(List<ITypeSymbol> allTypesImplementingFrom)
        {
            List<ImpliesViolation> res = new List<ImpliesViolation>();

            foreach (ITypeSymbol concrete in allTypesImplementingFrom)
                foreach (TypeImplication impl in allImplications)
                    if (impl.FailsOn(concrete))
                    {
                        res.Add(new ImpliesViolation(concrete, impl));
                    }

            return res;
        }
    }
}
