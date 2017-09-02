using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using IntroductionToRoslyn.CodeGeneration;
using IntroductionToRoslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntroductionToRoslyn.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = ActionName), Shared]
    public class AnnotateWithAttributeUsage : CodeRefactoringProvider
    {
        public const string ActionName = "Annotate With Attribute Usage";

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            SemanticModel model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            CompilationUnitSyntax root = await document.GetCSharpSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (textSpan.Start >= root.FullSpan.Length)
                return;

            var token = root.FindToken(textSpan.Start);

            if (!token.Span.Contains(textSpan))
            {
                return;
            }
            cancellationToken.ThrowIfCancellationRequested();

            var classDeclaration = token.Parent as ClassDeclarationSyntax;
            if (classDeclaration == null)
            {
                return;
            }

            var baseList = classDeclaration.BaseList;
            if (baseList == null || !baseList.Types.Any())
            {
                return;
            }

            var attributeType = model.Compilation.GetTypeByMetadataName("System.Attribute");

            bool derivesFromAttribute = false;
            foreach (var type in baseList.Types)
            {
                var baseType = type as SimpleBaseTypeSyntax;

                if (baseType != null)
                {
                    var typeInfo = model.GetTypeInfo(baseType.Type);

                    if (typeInfo.Type != null
                        && typeInfo.Type.TypeKind != TypeKind.Interface
                        && SymbolHelper.DerivesFrom(typeInfo.Type, attributeType))
                    {
                        derivesFromAttribute = true;
                        break;
                    }
                }
            }

            if (!derivesFromAttribute)
            {
                return;
            }

            AddAttributeUsageAnnotation(context, root, document, classDeclaration);

        }

        void AddAttributeUsageAnnotation(CodeRefactoringContext context, CompilationUnitSyntax root, Document document, ClassDeclarationSyntax classDeclaration)
        {
            string message = $"Annotate with AttributeUsage attribute";
            var func = new System.Func<CancellationToken, Task<Document>>((CancellationToken cancellation) =>
            {
                return Task.Run(delegate
                {
                    var attrbuteList = AttributeUsageAnnotationGenerator.GenerateSyntax(AttributeTargets.Class);

                    var newClass = classDeclaration.AddAttributeLists(attrbuteList);

                    var newRoot = root.ReplaceNode(classDeclaration, newClass);

                    return document.WithSyntaxRoot(newRoot);
                });
            });
            context.RegisterRefactoring(new DocumentChangeAction(message, func));
        }
    }
}
