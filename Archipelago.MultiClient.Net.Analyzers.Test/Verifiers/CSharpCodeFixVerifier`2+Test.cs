using Archipelago.MultiClient.Net.Analyzers.Generators;
using Archipelago.MultiClient.Net.Analyzers.Test.Util;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Collections.Generic;

namespace Archipelago.MultiClient.Net.Analyzers.Test
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            protected override IEnumerable<Type> GetSourceGenerators() => [
                typeof(DataStorageAttributeGenerator),
                typeof(DataStoragePropertyGenerator),
            ];

            public Test()
            {
                ReferenceAssemblies = ReferenceAssemblyBuilder.DefaultWithMultiClient;
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck;
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    return solution;
                });
            }
        }
    }
}
