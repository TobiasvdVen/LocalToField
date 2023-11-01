﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LocalToField
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(LocalToFieldCodeRefactoringProvider)), Shared]
    internal class LocalToFieldCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // TODO: Replace the following code with your own analysis, generating a CodeAction for each refactoring to offer

            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            SyntaxNode node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a type declaration node.
            TypeDeclarationSyntax typeDecl = node as TypeDeclarationSyntax;
            if (typeDecl == null)
            {
                return;
            }

            // For any type declaration node, create a code action to reverse the identifier text.
            CodeAction action = CodeAction.Create("Reverse type name", c => ReverseTypeNameAsync(context.Document, typeDecl, c));

            // Register this code action.
            context.RegisterRefactoring(action);
        }

        private async Task<Solution> ReverseTypeNameAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Produce a reversed version of the type declaration's identifier token.
            SyntaxToken identifierToken = typeDecl.Identifier;
            string newName = new string(identifierToken.Text.ToCharArray().Reverse().ToArray());

            // Get the symbol representing the type to be renamed.
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            INamedTypeSymbol typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            Solution originalSolution = document.Project.Solution;
            OptionSet optionSet = originalSolution.Workspace.Options;
            Solution newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}