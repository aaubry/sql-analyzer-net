using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SqlAnalyzer.Net.Extensions
{
    public static class ArgumentSyntaxExtensions
    {
        /// <summary>
        /// Returns the parameter to which this argument is passed. If <paramref name="allowParams" />
        /// is true, the last parameter will be returned if it is params parameter and the index of
        /// the specified argument is greater than the number of parameters.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="allowParams">if set to <c>true</c> [allow parameters].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The parameter symbol.</returns>
        public static IParameterSymbol? DetermineParameter(
            this ArgumentSyntax argument,
            SemanticModel semanticModel,
            bool allowParams = false,
            CancellationToken cancellationToken = default)
        {
            if (argument.Parent is not BaseArgumentListSyntax argumentList)
            {
                return null;
            }

            if (argumentList.Parent is not ExpressionSyntax invocableExpression)
            {
                return null;
            }

            if (semanticModel.GetSymbolInfo(invocableExpression, cancellationToken).Symbol is not IMethodSymbol symbol)
            {
                return null;
            }

            var parameters = symbol.Parameters;

            // Handle named argument
            if (argument.NameColon != null && !argument.NameColon.IsMissing)
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                return parameters.FirstOrDefault(p => p.Name == name);
            }

            // Handle positional argument
            var index = argumentList.Arguments.IndexOf(argument);
            if (index < 0)
            {
                return null;
            }

            if (index < parameters.Length)
            {
                return parameters[index];
            }

            if (allowParams)
            {
                var lastParameter = parameters.LastOrDefault();
                if (lastParameter == null)
                {
                    return null;
                }

                if (lastParameter.IsParams)
                {
                    return lastParameter;
                }
            }

            return null;
        }

        public static string? TryGetArgumentStringValue(this ArgumentSyntax argument, SemanticModel semanticModel)
        {
            return argument.Expression.TryGetArgumentStringValue(semanticModel);
        }

        public static string? TryGetArgumentStringValue(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var visitor = new ExpressionStringValueVisitor(semanticModel, new StringBuilder());
            expression.Accept(visitor);
            return visitor.Value.Length > 0
                ? visitor.Value.ToString()
                : null;
        }

        private sealed class ExpressionStringValueVisitor(SemanticModel semanticModel, StringBuilder value) : CSharpSyntaxVisitor
        {
            public StringBuilder Value { get; } = value;

            public override void VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                switch (node.Token.Kind())
                {
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.MultiLineRawStringLiteralToken:
                    case SyntaxKind.SingleLineRawStringLiteralToken:
                    case SyntaxKind.Utf8StringLiteralExpression:
                    case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                    case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                        Value.Append(node.Token.ValueText);
                        break;

                    default:
                        // Insert some whitespace to avoid concatenating unrelated literals
                        Value.Append(' ');
                        break;
                }
            }

            public override void VisitInterpolatedStringText(InterpolatedStringTextSyntax node)
            {
                Value.Append(node.TextToken.Text);
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                if (node is ExpressionSyntax expression)
                {
                    var symbolVariable = semanticModel.GetConstantValue(expression);
                    if (symbolVariable.HasValue)
                    {
                        Value.Append(symbolVariable.Value?.ToString() ?? string.Empty);
                        return;
                    }
                }

                foreach (var child in node.ChildNodes())
                {
                    if (child is CSharpSyntaxNode syntaxNode)
                    {
                        syntaxNode.Accept(this);
                    }
                }
            }
        }
    }
}
