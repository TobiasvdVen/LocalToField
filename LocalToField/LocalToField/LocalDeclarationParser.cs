using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace LocalToField
{
    public class LocalDeclarationParser
    {
        public GenerateFieldInfo Parse(LocalDeclarationStatementSyntax localDeclaration)
        {
            VariableDeclaratorSyntax variableDeclarator = localDeclaration.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();

            TypeSyntax typeDeclarator = localDeclaration.DescendantNodes().OfType<TypeSyntax>().First();
            string typeName = typeDeclarator.GetFirstToken().Text;

            ExpressionSyntax valueExpression = variableDeclarator.DescendantNodes().OfType<ExpressionSyntax>().First();
            string value = valueExpression.ToFullString();

            if (ValueIsImplicitNew(value))
            {
                value = TransformImplicitNewToExplicitValue(value, typeName);
            }

            SyntaxToken variableIdentifier = variableDeclarator.DescendantTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken));
            string localName = variableIdentifier.Text;
            string fieldName = $"_{localName}";

            return new GenerateFieldInfo(fieldName, typeName, value);
        }

        private bool ValueIsImplicitNew(string value)
        {
            string withoutWhitespace = value.Replace(" ", "");
            int w = withoutWhitespace.IndexOf('w');
            int parenthesis = withoutWhitespace.IndexOf('(');

            return parenthesis - w == 1;
        }

        private string TransformImplicitNewToExplicitValue(string implicitNewValue, string typeName)
        {
            int open = implicitNewValue.IndexOf('(') + 1;
            int close = implicitNewValue.LastIndexOf(')');

            string arguments = implicitNewValue.Substring(open, close - open);
            return $"new {typeName}({arguments})";
        }
    }
}
