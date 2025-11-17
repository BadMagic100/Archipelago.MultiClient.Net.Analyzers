using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace Archipelago.MultiClient.Net.Analyzers.Generators;

[Generator(LanguageNames.CSharp)]
public class DataStoragePropertyGenerator : IIncrementalGenerator
{
    private record Model(
        string ContainingNamespace,
        string ClassAccessibility,
        string ClassName,
        string PropertyAccessibility,
        string PropertyName,
        bool IsTargetPartialProperty,
        string SessionReference,
        string? Scope,
        string DataStorageKey
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Model> modelProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            DataStorageAttributeGenerator.AttributeFullName,
            predicate: static (node, ct) => 
                node.FirstAncestorOrSelf<FieldDeclarationSyntax>() is not null || node.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is not null,
            transform: static (context, ct) =>
            {
                // we know we are on an annotated field declaration which is huge! but now we need
                // to transform to a cacheable type.
                AttributeData attr = context.Attributes.First();
                string dataStorageKey;
                string? scope = null; 
                if (attr.ConstructorArguments.Length == 2)
                {
                    // session, key
                    dataStorageKey = attr.ConstructorArguments[1].ToCSharpString();
                }
                else
                {
                    // session, scope, key
                    scope = attr.ConstructorArguments[1].ToCSharpString();
                    dataStorageKey = attr.ConstructorArguments[2].ToCSharpString();
                }

                return new Model(
                    context.TargetSymbol.ContainingNamespace.ToDisplayString(),
                    SyntaxFacts.GetText(context.TargetSymbol.ContainingType.DeclaredAccessibility),
                    context.TargetSymbol.ContainingType.Name,
                    GeneratePropAccessibility(context.TargetSymbol),
                    GeneratePropName(context.TargetSymbol),
                    context.TargetSymbol.Kind == SymbolKind.Property,
                    (string)attr.ConstructorArguments[0].Value!,
                    scope,
                    dataStorageKey
                );
            }
        );

        context.RegisterSourceOutput(modelProvider, GenerateSource);
    }

    private void GenerateSource(SourceProductionContext context, Model model)
    {
        StringBuilder source = new($$"""
            #nullable enable annotations

            using Archipelago.MultiClient.Net.Models;

            namespace {{model.ContainingNamespace}}
            {
                {{model.ClassAccessibility}} partial class {{model.ClassName}}
                {

            """
        );

        string referenceText;
        if (model.Scope != null)
        {
            // session, scope, key
            referenceText = $"{model.SessionReference}.DataStorage[{model.Scope}, {model.DataStorageKey}]";
        }
        else
        {
            // session, key
            referenceText = $"{model.SessionReference}.DataStorage[{model.DataStorageKey}]";
        }

        string modifiers = model.PropertyAccessibility;
        if (model.IsTargetPartialProperty)
        {
            modifiers += " partial";
        }

        source.AppendLine($$"""
                    [System.CodeDom.Compiler.GeneratedCode(tool: "{{nameof(DataStoragePropertyGenerator)}}", version: null)]
                    {{modifiers}} DataStorageElement {{model.PropertyName}}
                    {
                        get => {{referenceText}};
                        set => {{referenceText}} = value;
                    }
                }
            }
            """);
        context.AddSource(model.ClassName + "_" + model.PropertyName + ".g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static string GeneratePropAccessibility(ISymbol symbol)
    {
        if (symbol.Kind == SymbolKind.Property)
        {
            return SyntaxFacts.GetText(symbol.DeclaredAccessibility);
        }
        return "private";
    }

    private static string GeneratePropName(ISymbol symbol)
    {
        if (symbol.Kind == SymbolKind.Property)
        {
            return symbol.Name;
        }
        return GeneratePropName(symbol.Name);
    }

    public static string GeneratePropName(string fieldName)
    {
        string propName = fieldName.Trim('_');
        propName = char.ToUpper(propName[0]) + propName[1..];
        return propName;
    }
}
