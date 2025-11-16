using Archipelago.MultiClient.Net.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Archipelago.MultiClient.Net.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ItemFlagsComparisonInSwitchCaseDiagnostics : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor NoItemFlagsComparisonsInSwitchCaseConstants = new(
        id: Constants.DiagnosticPrefix + "003",
        title: "Avoid value comparisons for ItemFlags objects in switch cases",
        messageFormat: "Avoid value comparisons for ItemFlags objects in switch cases",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/BadMagic100/Archipelago.MultiClient.Net.Analyzers#multiclient003---avoid-value-comparisons-for-itemflags-objects-in-switch-cases"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        NoItemFlagsComparisonsInSwitchCaseConstants
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
        context.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
    }

    private void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
    {
        // analyze the switch statement for any cases which compare directly to the ItemFlags enum
        SwitchStatementSyntax syntax = (SwitchStatementSyntax)context.Node;
        foreach (SwitchSectionSyntax section in syntax.Sections)
        {
            foreach (CaseSwitchLabelSyntax label in section.Labels.OfType<CaseSwitchLabelSyntax>())
            {
                if (label.Value is MemberAccessExpressionSyntax identifier)
                {
                    AnalyzeConstantMemberComparison(context, label.GetLocation(), identifier);
                }
            }
        }
    }

    private void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
    {
        SwitchExpressionSyntax syntax = (SwitchExpressionSyntax)context.Node;
        foreach (SwitchExpressionArmSyntax arm in syntax.Arms)
        {
            if (arm.Pattern is ConstantPatternSyntax { Expression: MemberAccessExpressionSyntax ma } cp)
            {
                AnalyzeConstantMemberComparison(context, cp.GetLocation(), ma);
            }
        }
    }

    private void AnalyzeConstantMemberComparison(
        SyntaxNodeAnalysisContext context,
        Location warningScope,
        MemberAccessExpressionSyntax identifier)
    {
        TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(identifier);
        if (ArchipelagoTypeUtils.IsTypeItemFlags(typeInfo.Type, context.Compilation))
        {
            // Get value of enum identifier. Value cannot be null if we're in this `if` block.
            int identifierValue = (int)context.SemanticModel.GetConstantValue(identifier).Value!;

            // If value equals 0 then it is `ItemFlags.None`
            if (identifierValue != 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(NoItemFlagsComparisonsInSwitchCaseConstants, warningScope));
            }
        }
    }
}