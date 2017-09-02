using System;
using System.Collections.Immutable;
using IntroductionToRoslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IntroductionToRoslyn.Diagnostics
{
	/// <summary>
	/// The localisable string diagnostic looks for string literals that are being
	/// assigned to properties onto a class that derives from Xamarin.Forms.View.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LocalisableStringDiagnostic : DiagnosticAnalyzer
	{
		public const string DiagnosticName = "Potentially Localisable String";
		public const string DiagnosticDescription = "Inspects for string literals that are assigned to properties onto a class that derives from Xamarin.Forms.View and ";

        private readonly DiagnosticDescriptor _descriptor = new DiagnosticDescriptor("IR001", DiagnosticName, "This could be potentially localised. Consider using a resource lookup instead.", "MFractor", DiagnosticSeverity.Warning, true, DiagnosticDescription);

		private ImmutableArray<DiagnosticDescriptor> _descriptors;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get
			{
				if (_descriptors == null)
				{
					_descriptors = ImmutableArray.Create(_descriptor);
				}

				return _descriptors;
			}
		}

        public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction((SyntaxNodeAnalysisContext analysis) =>
			{
				var semanticModel = analysis.SemanticModel;
                var assignment = analysis.Node as AssignmentExpressionSyntax;

				if (assignment == null)
				{
					return;
                }

                var literalExpression = assignment.Right as LiteralExpressionSyntax;
                if (literalExpression == null || literalExpression.Token.Kind() != SyntaxKind.StringLiteralToken)
				{
					return;
				}

                var memberExpression = assignment.Left as MemberAccessExpressionSyntax;
                if (memberExpression == null)
                {
                    return;
                }


				var typeInfo = analysis.SemanticModel.GetTypeInfo(memberExpression.Expression);
                if (typeInfo.Type == null)
                {
                    return;
                }

                var viewType = analysis.Compilation.GetTypeByMetadataName("Xamarin.Forms.View");
				if (viewType == null)
				{
					return;
				}

                if (!SymbolHelper.DerivesFrom(typeInfo.Type, viewType))
                {
                    return;
                }

                var diagnostic = Diagnostic.Create(this._descriptor, assignment.GetLocation());
				analysis.ReportDiagnostic(diagnostic);

            }, new SyntaxKind[] { SyntaxKind.SimpleAssignmentExpression });
        }
    }
}
