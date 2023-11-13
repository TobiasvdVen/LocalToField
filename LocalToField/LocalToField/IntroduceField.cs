using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LocalToField
{
    public class IntroduceField
    {
        private readonly Document _document;

        public IntroduceField(Document document)
        {
            _document = document;
        }

        public async Task<LocalDeclarationStatementSyntax?> FindLocalDeclarationAsync(TextSpan textSpan, CancellationToken cancellationToken = default)
        {
            SyntaxNode syntaxRoot = await GetSyntaxRoot(cancellationToken);

            var descendantNodes = syntaxRoot.DescendantNodes();
            var localDeclarations = descendantNodes.OfType<LocalDeclarationStatementSyntax>();
            var intersections = localDeclarations.Where(v => v.Span.IntersectsWith(textSpan));

            return intersections.SingleOrDefault();
        }

        public async Task<Document> FromLocalAsync(LocalDeclarationStatementSyntax local, CancellationToken cancellationToken = default)
        {
            SyntaxNode syntaxRoot = await GetSyntaxRoot(cancellationToken);

            DocumentEditor editor = await DocumentEditor.CreateAsync(_document, cancellationToken);

            ClassDeclarationSyntax classDeclaration = local.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            syntaxRoot = syntaxRoot.RemoveNode(local, SyntaxRemoveOptions.KeepNoTrivia);

            SourceText localRemoved = syntaxRoot.GetText();
            string text = localRemoved.ToString();

            LocalDeclarationParser localDeclarationParser = new();
            GenerateFieldInfo fieldInfo = localDeclarationParser.Parse(local);

            if (classDeclaration != null)
            {
                SyntaxToken classIdentifier = classDeclaration.DescendantTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken));
                string className = classIdentifier.Text;

                SyntaxToken openBrace = classDeclaration
                    .DescendantTokens()
                    .First(n => n.IsKind(SyntaxKind.OpenBraceToken));

                string indentation = string.Empty;

                if (openBrace.LeadingTrivia.Any())
                {
                    SyntaxTrivia whitespace = openBrace.LeadingTrivia.First(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

                    indentation = new string(Enumerable.Repeat(' ', whitespace.Span.Length).ToArray());
                }

                string fileUpToField = text.Substring(0, openBrace.FullSpan.End);
                string fileAfterField = text.Substring(openBrace.FullSpan.End);

                string generated = Generate(fieldInfo, indentation, className);

                text = string.Concat(fileUpToField, generated, fileAfterField);
            }

            return _document.WithText(SourceText.From(text));
        }

        private async Task<SyntaxNode> GetSyntaxRoot(CancellationToken cancellationToken)
        {
            return await _document.GetSyntaxRootAsync(cancellationToken);
        }

        private string Generate(GenerateFieldInfo info, string indentation, string className)
        {
            string fieldDeclaration = $"{indentation}    private readonly {info.Type} {info.Name};{Environment.NewLine}{Environment.NewLine}";
            string constructorPrototype = $"{indentation}    public {className}(){Environment.NewLine}";
            string openingBrace = $"{indentation}    {{{Environment.NewLine}";
            string fieldInitialization = $"{indentation}        {info.Name} = {info.Value};{Environment.NewLine}";
            string closingBrace = $"{indentation}    }}{Environment.NewLine}{Environment.NewLine}";

            return string.Concat(
                fieldDeclaration,
                constructorPrototype,
                openingBrace,
                fieldInitialization,
                closingBrace);
        }
    }
}
