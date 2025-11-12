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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Archipelago.MultiClient.Net.Analyzers.Fixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class ItemFlagsInSwitchFixes : CodeFixProvider
    {
        public const string FixKeyConvertItemFlagsSwitch = "ConvertItemFlagsSwitch";

        public override ImmutableArray<string> FixableDiagnosticIds => [
            Constants.DiagnosticPrefix + "003"
        ];

        // explicit optout from fix all - not safe because of fallthrough cases
        public override FixAllProvider? GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            Diagnostic diagnostic = context.Diagnostics.First();
            if (!TryRewriteCaseLabel(context, root, diagnostic))
            {
                TryRewriteConstPatternArm(context, root, diagnostic);
            }
        }

        private bool TryRewriteCaseLabel(CodeFixContext context, SyntaxNode root, Diagnostic diagnostic)
        {
            TextSpan span = diagnostic.Location.SourceSpan;
            CaseSwitchLabelSyntax? caseLabel = root.FindToken(span.Start).Parent?
                .FirstAncestorOrSelf<CaseSwitchLabelSyntax>();
            if (caseLabel == null)
            {
                return false;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Convert case to use flag comparison",
                    createChangedDocument: c => ConvertCaseToFlagComparison(
                        document: context.Document,
                        caseLabel: caseLabel,
                        cancellationToken: c
                    ),
                    equivalenceKey: FixKeyConvertItemFlagsSwitch
                ),
                diagnostic
            );
            return true;
        }

        private bool TryRewriteConstPatternArm(CodeFixContext context, SyntaxNode root, Diagnostic diagnostic)
        {
            TextSpan span = diagnostic.Location.SourceSpan;
            SwitchExpressionArmSyntax? arm = root.FindToken(span.Start).Parent?
                .FirstAncestorOrSelf<SwitchExpressionArmSyntax>();
            if (arm is not { Pattern: ConstantPatternSyntax constPattern })
            {
                return false;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Convert pattern to use flag comparison",
                    createChangedDocument: c => ConvertConstantPatternArmToFlagComparison(
                        document: context.Document,
                        arm: arm,
                        constPattern: constPattern,
                        cancellationToken: c
                    ),
                    equivalenceKey: FixKeyConvertItemFlagsSwitch
                ),
                diagnostic
            );
            return true;
        }

        private async Task<Document> ConvertCaseToFlagComparison(
            Document document,
            CaseSwitchLabelSyntax caseLabel,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            string variableName = NameGenerator.GetUniqueVariableName("f", editor.SemanticModel, caseLabel.SpanStart);

            var (pattern, whenClause) = await RewriteConstantMemberAccessToPatternMatch(document, caseLabel.Value, variableName, cancellationToken);
            CasePatternSwitchLabelSyntax newCaseLabel = SyntaxFactory.CasePatternSwitchLabel(
                pattern, whenClause, SyntaxFactory.Token(SyntaxKind.ColonToken));

            editor.ReplaceNode(caseLabel, newCaseLabel);

            Document newDoc = editor.GetChangedDocument();
            return newDoc;
        }

        private async Task<Document> ConvertConstantPatternArmToFlagComparison(
            Document document,
            SwitchExpressionArmSyntax arm,
            ConstantPatternSyntax constPattern,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            string variableName = NameGenerator.GetUniqueVariableName("f", editor.SemanticModel, constPattern.SpanStart);

            var (pattern, whenClause) = await RewriteConstantMemberAccessToPatternMatch(document, constPattern.Expression, variableName, cancellationToken);

            SwitchExpressionArmSyntax newArm = SyntaxFactory.SwitchExpressionArm(pattern, whenClause, arm.Expression);
            editor.ReplaceNode(arm, newArm);

            Document newDoc = editor.GetChangedDocument();
            return newDoc;
        }

        private async Task<(PatternSyntax, WhenClauseSyntax)> RewriteConstantMemberAccessToPatternMatch(
            Document document,
            ExpressionSyntax expr, 
            string newVarName,
            CancellationToken cancellationToken)
        {
            SingleVariableDesignationSyntax variableDesignation = SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(newVarName));
            DeclarationPatternSyntax declPattern = SyntaxFactory.DeclarationPattern(
                SyntaxFactory.IdentifierName("ItemFlags"), 
                variableDesignation
            );

            Compilation? compilation = await document.Project.GetCompilationAsync(cancellationToken);
            ExpressionSyntax comparison = ArchipelagoSyntaxFactory.CreateFlagComparison(
                compilation,
                SyntaxFactory.IdentifierName(newVarName),
                expr);

            return (declPattern, SyntaxFactory.WhenClause(comparison));
        }
    }
}
