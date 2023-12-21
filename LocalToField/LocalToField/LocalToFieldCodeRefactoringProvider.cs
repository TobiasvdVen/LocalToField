using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Composition;
using System.Threading.Tasks;

namespace LocalToField
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(LocalToFieldCodeRefactoringProvider)), Shared]
    internal class LocalToFieldCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            IDebugLog debugLog = new NullDebugLog();

            IntroduceField introduceField = new(context.Document, debugLog);

            LocalDeclarationStatementSyntax? localDeclaration = await introduceField.FindLocalDeclarationAsync(context.Span);

            if (localDeclaration is null)
            {
                return;
            }

            CodeAction action = CodeAction.Create("Introduce field", c => introduceField.FromLocalAsync(localDeclaration));
            context.RegisterRefactoring(action);
        }
    }
}
