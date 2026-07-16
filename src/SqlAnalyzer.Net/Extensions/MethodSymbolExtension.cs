using Microsoft.CodeAnalysis;

namespace SqlAnalyzer.Net.Extensions
{
    public static class MethodSymbolExtension
    {
        public static bool IsDapperMethod(this IMethodSymbol symbol, SemanticModel semanticModel)
        {
            var isSqlMapper = symbol.ContainingType.Name == "SqlMapper"
                && symbol.ContainingType.ContainingNamespace.Name == "Dapper";

            if (isSqlMapper)
            {
                return true;
            }

            var sqlGridReader = semanticModel.Compilation.GetTypeByMetadataName("Dapper.SqlMapper+GridReader");
            return SymbolEqualityComparer.Default.Equals(symbol.ContainingType, sqlGridReader);
        }
    }
}
