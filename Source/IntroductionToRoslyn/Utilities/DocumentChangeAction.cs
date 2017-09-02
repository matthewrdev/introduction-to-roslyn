using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace IntroductionToRoslyn.Utilities
{
    public class DocumentChangeAction : Microsoft.CodeAnalysis.CodeActions.CodeAction
    {
        private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;

        public DocumentChangeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
        {
            Title = title;
            _createChangedDocument = createChangedDocument;
        }

        public override string Title { get; }

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            return _createChangedDocument(cancellationToken);
        }
    }
}
