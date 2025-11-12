using Archipelago.MultiClient.Net.Analyzers.Generators;
using Archipelago.MultiClient.Net.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Archipelago.MultiClient.Net.Analyzers.Fixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class DataStorageFixes : CodeFixProvider
    {
        public const string FixKeyMakeInline = "MakeInline";
        public const string FixKeyMakeDataStorageProperty = "MakeDataStorageProperty";

        public override ImmutableArray<string> FixableDiagnosticIds => [
            Constants.DiagnosticPrefix + "001"
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

            // The fix we are trying to do is to take local declarations and inline them at all usages
            // That means:
            // 1. The diagnostic must be targeting a local declaration
            // 2. The local declaration may ONLY be an index access from datastorage
            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan span = diagnostic.Location.SourceSpan;
            VariableDeclaratorSyntax? declarator = root.FindToken(span.Start).Parent?
                .FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            if (declarator == null || declarator.Initializer == null || !declarator.Initializer.Value.IsKind(SyntaxKind.ElementAccessExpression))
            {
                return;
            }

            SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            SymbolInfo initializerInfo = semanticModel.GetSymbolInfo(declarator.Initializer.Value);
            if (initializerInfo.Symbol is not IPropertySymbol ips 
                || !ArchipelagoTypeUtils.IsTypeDataStorageHelper(ips.ContainingType, semanticModel.Compilation)
                || semanticModel.GetDeclaredSymbol(declarator) is not ISymbol symbol)
            {
                return;
            }

            IEnumerable<ReferencedSymbol> references = await SymbolFinder.FindReferencesAsync(symbol,
                context.TextDocument.Project.Solution,
                [context.Document],
                context.CancellationToken).ConfigureAwait(false);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make DataStorage access inline",
                    createChangedDocument: c => MakeDataStorageAccessInlineAsync(
                        document: context.Document,
                        oldRoot: root,
                        declarator: declarator,
                        references: references,
                        cancellationToken: c
                    ),
                    equivalenceKey: FixKeyMakeInline
                ),
                diagnostic
            );

            // if we're accessing data storage off a field or property, we can also offer the source-generator-based
            // data storage properties as a fix
            
            // let's re-explore the initializer a bit and try to find a session
            if (declarator.Initializer.Value is ElementAccessExpressionSyntax elem && elem.Expression is MemberAccessExpressionSyntax member)
            {
                SymbolInfo expectedSessionInfo = semanticModel.GetSymbolInfo(member.Expression);
                if (expectedSessionInfo.Symbol is IPropertySymbol { Type: ITypeSymbol ts } 
                    && ArchipelagoTypeUtils.IsTypeArchipelagoSession(ts, semanticModel.Compilation))
                {
                }
                else if (expectedSessionInfo.Symbol is IFieldSymbol { Type: ITypeSymbol ts2 }
                    && ArchipelagoTypeUtils.IsTypeArchipelagoSession(ts2, semanticModel.Compilation))
                {
                }
                else
                {
                    return;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Make DataStorage access a DataStorageProperty",
                        createChangedDocument: c => MakeDataStoragePropertyAsync(
                            document: context.Document,
                            oldRoot: root,
                            declarator: declarator,
                            references: references,
                            sessionSymbol: expectedSessionInfo.Symbol,
                            dataStorageAccessArguments: elem.ArgumentList.Arguments,
                            cancellationToken: c
                        ),
                        equivalenceKey: FixKeyMakeDataStorageProperty
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Document> MakeDataStorageAccessInlineAsync(
            Document document,
            SyntaxNode oldRoot,
            IEnumerable<ReferencedSymbol> references,
            VariableDeclaratorSyntax declarator,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            foreach (ReferenceLocation reference in references.SelectMany(r => r.Locations))
            {
                SyntaxNode node = oldRoot.FindNode(reference.Location.SourceSpan);
                if (node is ExpressionSyntax)
                {
                    editor.ReplaceNode(node, declarator.Initializer!.Value.WithTriviaFrom(node));
                }
            }
            editor.RemoveNode(declarator, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            return editor.GetChangedDocument();
        }

        private async Task<Document> MakeDataStoragePropertyAsync(
            Document document,
            SyntaxNode oldRoot,
            IEnumerable<ReferencedSymbol> references,
            VariableDeclaratorSyntax declarator,
            ISymbol sessionSymbol,
            IEnumerable<ArgumentSyntax> dataStorageAccessArguments,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);

            string localName = declarator.Identifier.Text;
            string fieldName = $"_{localName}";
            string propName = DataStoragePropertyGenerator.GeneratePropName(fieldName);

            // make the field backing the property (with annotation).
            SyntaxNode sessionNode = oldRoot.FindNode(sessionSymbol.Locations[0].SourceSpan);
            SyntaxNode newFieldNode = ArchipelagoSyntaxFactory.CreateDataStorageProperty(
                editor.Generator,
                fieldName,
                sessionSymbol.Name, 
                dataStorageAccessArguments
            ).WithAdditionalAnnotations(Formatter.Annotation);

            editor.InsertAfter(sessionNode, newFieldNode.WithTriviaFrom(sessionNode));

            // make the containing class partial, if needed
            ClassDeclarationSyntax? classDecl = sessionNode.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl != null && !classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                editor.SetModifiers(classDecl, editor.Generator.GetModifiers(classDecl).WithPartial(true));
            }

            SyntaxNode ident = editor.Generator.IdentifierName(propName);

            // replace all references with references to the property instead
            foreach (ReferenceLocation reference in references.SelectMany(r => r.Locations))
            {
                SyntaxNode node = oldRoot.FindNode(reference.Location.SourceSpan);
                if (node is ExpressionSyntax)
                {
                    editor.ReplaceNode(node, ident.WithTriviaFrom(node));
                }
            }

            // remove the declaration
            editor.RemoveNode(declarator, SyntaxRemoveOptions.KeepUnbalancedDirectives);

            Document newDoc = editor.GetChangedDocument();
            return newDoc;
        }
    }
}
