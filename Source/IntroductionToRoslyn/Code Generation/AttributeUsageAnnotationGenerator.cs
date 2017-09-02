using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntroductionToRoslyn.CodeGeneration
{
    public class AttributeUsageAnnotationGenerator
    {
        public static AttributeListSyntax GenerateSyntax(AttributeTargets usageTarget)
        {
            return SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName("System"),
                            SyntaxFactory.IdentifierName("AttributeUsage")))
                    .WithArgumentList(
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>(
                                SyntaxFactory.AttributeArgument(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("System"),
                                            SyntaxFactory.IdentifierName("AttributeTargets")),
                                        SyntaxFactory.IdentifierName(usageTarget.ToString()))))))));
        }
    }
}
