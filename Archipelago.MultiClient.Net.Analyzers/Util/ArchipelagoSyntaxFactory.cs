using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Archipelago.MultiClient.Net.Analyzers.Util;

internal static class ArchipelagoSyntaxFactory
{
    public static FieldDeclarationSyntax CreateDataStorageProperty(
        SyntaxGenerator generator,
        string fieldName, 
        string sessionName,
        IEnumerable<ArgumentSyntax> dataStorageAccessArgs)
    {
        IEnumerable<AttributeArgumentSyntax> convertedAttributeArgs = dataStorageAccessArgs
            .Select(a => AttributeArgument(null, a.NameColon, a.Expression));
        IEnumerable<SyntaxNode> attributeArgs = [
            generator.AttributeArgument(generator.NameOfExpression(generator.IdentifierName(sessionName))),
            .. convertedAttributeArgs
        ];

        SyntaxNode decl = generator.FieldDeclaration(
            fieldName, 
            generator.IdentifierName("DataStorageElement"), 
            Accessibility.Private, 
            DeclarationModifiers.ReadOnly
        );
        return (FieldDeclarationSyntax) generator.AddAttributes(
            decl,
            generator.Attribute("DataStorageProperty", attributeArgs)
        );
    }

    /// <summary>
    /// Creates a bitwise comparison that works on any .NET version
    /// </summary>
    public static ExpressionSyntax CreateBitwiseFlagComparison(
        ExpressionSyntax dynamicPart,
        ExpressionSyntax constPart)
    {
        return BinaryExpression(
            SyntaxKind.EqualsExpression,
            ParenthesizedExpression(
                BinaryExpression(
                    SyntaxKind.BitwiseAndExpression,
                    dynamicPart,
                    constPart
                )
            ),
            constPart
        );
    }

    public static ExpressionSyntax CreateFlagComparison(
        Compilation? compilation,
        ExpressionSyntax dynamicPart,
        ExpressionSyntax constPart)
    {
        if (compilation == null)
        {
            // Fallback to bitwise & when we can't determine compilation - works on any .NET version
            return CreateBitwiseFlagComparison(dynamicPart, constPart);
        }

        // Check if Enum.HasFlag is available in the compilation
        INamedTypeSymbol? enumType = compilation.GetTypeByMetadataName("System.Enum");
        bool hasHasFlagMethod = enumType?.GetMembers("HasFlag").Any() ?? false;

        if (hasHasFlagMethod)
        {
            // Use HasFlag when available
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    dynamicPart,
                    IdentifierName("HasFlag")
                ),
                ArgumentList(SingletonSeparatedList(Argument(constPart)))
            );
        }
        else
        {
            // Use bitwise & when HasFlag is not available
            return CreateBitwiseFlagComparison(dynamicPart, constPart);
        }
    }
}
