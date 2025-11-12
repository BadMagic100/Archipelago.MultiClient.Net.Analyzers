using Archipelago.MultiClient.Net.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Archipelago.MultiClient.Net.Analyzers.Fixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class ItemFlagsFixes : CodeFixProvider
    {
        public const string FixKeyUseHasFlag = "UseHasFlag";

        public override ImmutableArray<string> FixableDiagnosticIds => [
            Constants.DiagnosticPrefix + "002"
        ];

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan span = diagnostic.Location.SourceSpan;
            BinaryExpressionSyntax? comparison = root.FindToken(span.Start).Parent?
                .FirstAncestorOrSelf<BinaryExpressionSyntax>();
            if (comparison == null)
            {
                return;
            }

            SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            ExpressionSyntax lhs = comparison.Left;
            ExpressionSyntax rhs = comparison.Right;

            // this is only fixable if exactly one side is a constant (const local or field, or enum member access).
            // all of these are named symbols
            SymbolInfo leftSymbol = semanticModel.GetSymbolInfo(lhs);
            SymbolInfo rightSymbol = semanticModel.GetSymbolInfo(rhs);
            if (IsNamedConstant(leftSymbol.Symbol) && !IsNamedConstant(rightSymbol.Symbol))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Use flag comparison",
                        createChangedDocument: c => UseFlagComparisonAsync(
                            document: context.Document,
                            comparison: comparison,
                            dynamicPart: rhs,
                            constPart: lhs,
                            cancellationToken: c
                        ),
                        equivalenceKey: FixKeyUseHasFlag
                    ),
                    diagnostic
                );
            }
            else if (IsNamedConstant(rightSymbol.Symbol) && !IsNamedConstant(leftSymbol.Symbol))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Use flag comparison",
                        createChangedDocument: c => UseFlagComparisonAsync(
                            document: context.Document,
                            comparison: comparison,
                            dynamicPart: lhs,
                            constPart: rhs,
                            cancellationToken: c
                        ),
                        equivalenceKey: FixKeyUseHasFlag
                    ),
                    diagnostic
                );
            }
        }

        private bool IsNamedConstant([NotNullWhen(true)] ISymbol? symbol)
        {
            if (symbol != null && symbol.CanBeReferencedByName)
            {
                return symbol is IFieldSymbol field && field.IsConst || symbol is ILocalSymbol local && local.IsConst;
            }
            return false;
        }

        private async Task<Document> UseFlagComparisonAsync(
            Document document,
            BinaryExpressionSyntax comparison,
            ExpressionSyntax dynamicPart,
            ExpressionSyntax constPart,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            Compilation? compilation = await document.Project.GetCompilationAsync(cancellationToken);

            ExpressionSyntax newExpression = ArchipelagoSyntaxFactory.CreateFlagComparison(
                compilation,
                dynamicPart,
                constPart);

            editor.ReplaceNode(comparison, newExpression.WithTriviaFrom(comparison));
            return editor.GetChangedDocument();
        }
    }
}
