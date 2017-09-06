using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntroductionToRoslyn
{
    /// <summary>
    /// We can use a syntax walker to explore and find certain syntax elements within
    /// a SyntaxTree.
    /// </summary>
    public class ClassDeclarationSyntaxWalker : SyntaxWalker
    {
        public CancellationToken Token = default(CancellationToken);

        public ClassDeclarationSyntax ClassDeclaration
        {
            get;
            private set;
        }

        public override void Visit(SyntaxNode node)
        {
            if (Token.IsCancellationRequested)
            {
                return;
            }

            if (node is CompilationUnitSyntax)
			{
				base.Visit(node);
            }
            else if (node is NamespaceDeclarationSyntax)
            {
                base.Visit(node);
            }
            else if (node is ClassDeclarationSyntax)
            {
                ClassDeclaration = node as ClassDeclarationSyntax;
            }
        }
    }
}

