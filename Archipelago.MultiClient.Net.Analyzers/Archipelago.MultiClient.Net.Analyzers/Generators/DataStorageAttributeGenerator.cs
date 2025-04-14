using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Archipelago.MultiClient.Net.Analyzers.Generators
{
    [Generator(LanguageNames.CSharp)]
    public class DataStorageAttributeGenerator : IIncrementalGenerator
    {
        public const string AttributeFullName = "Archipelago.MultiClient.Net.DataStoragePropertyAttribute";

        public const string AttributeSource = """
            #nullable enable annotations

            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace Archipelago.MultiClient.Net
            {
                [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                internal sealed class DataStoragePropertyAttribute : Attribute
                {
                    public string? SessionVariable { get; }
                    public Scope Scope { get; }
                    public string Key { get; }

                    public DataStoragePropertyAttribute(string sessionVariable, Scope scope, string key)
                    {
                        this.SessionVariable = sessionVariable;
                        this.Scope = scope;
                        this.Key = key;
                    }
             
                    public DataStoragePropertyAttribute(string sessionVariable, string key) : this(sessionVariable, Scope.Global, key)
                    {
                    }
                }
            }
            """;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(postInit =>
            {
                postInit.AddSource("DataStoragePropertyAttribute.g.cs", SourceText.From(AttributeSource, Encoding.UTF8));
            });
        }

        public void Execute(GeneratorExecutionContext context)
        {
        }
    }
}
