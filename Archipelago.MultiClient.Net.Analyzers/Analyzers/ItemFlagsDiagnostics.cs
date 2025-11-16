using Archipelago.MultiClient.Net.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Archipelago.MultiClient.Net.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ItemFlagsDiagnostics : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor NoDirectComparisonToItemFlagsConstants = new(
        id: Constants.DiagnosticPrefix + "002",
        title: "Use HasFlag when comparing ItemFlags",
        messageFormat: "Use HasFlag when comparing ItemFlags",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/BadMagic100/Archipelago.MultiClient.Net.Analyzers#multiclient002---use-hasflag-when-comparing-itemflags"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        NoDirectComparisonToItemFlagsConstants
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeComparison, SyntaxKind.EqualsExpression);
    }

    private static bool IsBitwiseAndExpression(ExpressionSyntax expr)
    {
        // Unwrap parentheses first
        while (expr is ParenthesizedExpressionSyntax pes)
        {
            expr = pes.Expression;
        }
        return expr is BinaryExpressionSyntax { OperatorToken.RawKind: (int)SyntaxKind.AmpersandToken };
    }

    private void AnalyzeComparison(SyntaxNodeAnalysisContext context)
    {
        BinaryExpressionSyntax syntax = (BinaryExpressionSyntax)context.Node;
        ExpressionSyntax lhs = syntax.Left;
        ExpressionSyntax rhs = syntax.Right;

        // Skip if either side is a bitwise & expression
        if (IsBitwiseAndExpression(lhs) || IsBitwiseAndExpression(rhs))
        {
            return;
        }

        // we apply the diagnostic if we are comparing 2 itemflags.
        TypeInfo leftHandType = context.SemanticModel.GetTypeInfo(lhs, context.CancellationToken);
        TypeInfo rightHandType = context.SemanticModel.GetTypeInfo(rhs, context.CancellationToken);

        if (!ArchipelagoTypeUtils.IsTypeItemFlags(leftHandType.Type, context.Compilation)
            || !ArchipelagoTypeUtils.IsTypeItemFlags(rightHandType.Type, context.Compilation))
        {
            return;
        }

        // if either them is a constant None/Filler, that's fine because HasFlag(0) is not quite what you probably meant
        // there
        Optional<object?> leftValue = context.SemanticModel.GetConstantValue(lhs, context.CancellationToken);
        Optional<object?> rightValue = context.SemanticModel.GetConstantValue(rhs, context.CancellationToken);
        if (leftValue.HasValue && leftValue.Value is 0 || rightValue.HasValue && rightValue.Value is 0)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(NoDirectComparisonToItemFlagsConstants, syntax.GetLocation()));
    }
}
