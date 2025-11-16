using Archipelago.MultiClient.Net.Analyzers.Test.Util;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Archipelago.MultiClient.Net.Analyzers.Test.CSharpCodeFixVerifier<
    Archipelago.MultiClient.Net.Analyzers.Analyzers.ItemFlagsComparisonInSwitchCaseDiagnostics,
    Archipelago.MultiClient.Net.Analyzers.Fixes.ItemFlagsInSwitchFixes>;

namespace Archipelago.MultiClient.Net.Analyzers.Test;

[TestClass]
public class ItemFlagsSwitchTest
{
    [TestMethod]
    public async Task VerifyItemFlagsInSwitchStatementYieldsDiagnostic()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            // pattern matches are safe
                            case ItemFlags f when f.HasFlag(ItemFlags.Advancement):
                                return true;
                            {|#0:case ItemFlags.Trap:|}
                                return true;
                            // comparisons to None (0) are safe
                            case ItemFlags.None:
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task VerifyMultipleItemFlagsInSwitchStatementYieldsDiagnostics()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                                return true;
                            {|#1:case ItemFlags.Trap:|}
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult[] expected = [
            VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0),
            VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1)
        ];
        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task VerifyFallthroughItemFlagsInSwitchStatementYieldsDiagnostics()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                            {|#1:case ItemFlags.Trap:|}
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult[] expected = [
            VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0),
            VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1)
        ];
        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }    

    [TestMethod]
    public async Task VerifyItemFlagsInSwitchExpressionYieldsDiagnostic()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test(ItemFlags flags)
                    {
                        return flags switch
                        {
                            // valid because it uses pattern matching
                            ItemFlags f when f.HasFlag(ItemFlags.Advancement) => true,
                            // comparisons to None (0) are safe
                            ItemFlags.None => false,
                            {|#0:ItemFlags.Advancement|} => true,
                            _ => false
                        };
                    }
                }
            }
            """;
        DiagnosticResult expectedDiagnostic = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        await VerifyCS.VerifyAnalyzerAsync(test, expectedDiagnostic);
    }

    [TestMethod]
    public async Task VerifyBitwiseComparisonInSwitchStatementYieldsNoDiagnostic()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            case ItemFlags f when (f & ItemFlags.Advancement) == ItemFlags.Advancement:
                                return true;
                            case ItemFlags f when (f & ItemFlags.Trap) == ItemFlags.Trap:
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [TestMethod]
    public async Task VerifyFixSingleItemFlagsInSwitchStatement()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            case ItemFlags f when f.HasFlag(ItemFlags.Advancement):
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
    }

    [TestMethod]
    public async Task VerifyFixMultipleItemFlagsInSwitchStatement()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                                return true;
                            {|#1:case ItemFlags.Trap:|}
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            case ItemFlags f when f.HasFlag(ItemFlags.Advancement):
                                return true;
                            case ItemFlags f when f.HasFlag(ItemFlags.Trap):
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1);
        await VerifyCS.VerifyCodeFixAsync(test, [expected1, expected2], fixTest);
    }

    [TestMethod]
    public async Task VerifyFixFallthroughItemFlagsInSwitchStatement()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                            {|#1:case ItemFlags.Trap:|}
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            case ItemFlags f when f.HasFlag(ItemFlags.Advancement):
                            case ItemFlags f1 when f1.HasFlag(ItemFlags.Trap):
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1);
        await VerifyCS.VerifyCodeFixAsync(test, [expected1, expected2], fixTest);
    }

    [TestMethod]
    public async Task VerifyFixMixedUseItemFlagsInSwitchStatement()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            case ItemFlags f when f.HasFlag(ItemFlags.Advancement):
                                return true;
                            {|#0:case ItemFlags.Trap:|}
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            case ItemFlags f when f.HasFlag(ItemFlags.Advancement):
                                return true;
                            case ItemFlags f when f.HasFlag(ItemFlags.Trap):
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
    }

    [TestMethod]
    public async Task VerifyFixMixedUseItemFlagsInSwitchExpression()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        return i switch
                        {
                            ItemFlags f when f.HasFlag(ItemFlags.Advancement) => true,
                            {|#0:ItemFlags.Trap|} => true,
                            _ => false
                        };
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        return i switch
                        {
                            ItemFlags f when f.HasFlag(ItemFlags.Advancement) => true,
                            ItemFlags f when f.HasFlag(ItemFlags.Trap) => true,
                            _ => false
                        };
                    }
                }
            }
            """;
        DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
    }

    [TestMethod]
    public async Task VerifyFixMultipleItemFlagsInSwitchStatement_WithUnrelatedFVariable()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        string f = "testString";
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                                return true;
                            {|#1:case ItemFlags.Trap:|}
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        string f = "testString";
                        switch (i)
                        {
                            case ItemFlags f1 when f1.HasFlag(ItemFlags.Advancement):
                                return true;
                            case ItemFlags f1 when f1.HasFlag(ItemFlags.Trap):
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1);
        await VerifyCS.VerifyCodeFixAsync(test, [expected1, expected2], fixTest);
    }

    [TestMethod]
    public async Task VerifyFixMultipleItemFlagsInSwitchStatement_WithMultipleUnrelatedFVariables()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        string f = "testString";
                        int f1 = 1;
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                                return true;
                            {|#1:case ItemFlags.Trap:|}
                            {|#2:case ItemFlags.NeverExclude:|}
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        string f = "testString";
                        int f1 = 1;
                        switch (i)
                        {
                            case ItemFlags f2 when f2.HasFlag(ItemFlags.Advancement):
                                return true;
                            case ItemFlags f2 when f2.HasFlag(ItemFlags.Trap):
                            case ItemFlags f3 when f3.HasFlag(ItemFlags.NeverExclude):
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1);
        DiagnosticResult expected3 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(2);
        await VerifyCS.VerifyCodeFixAsync(test, [expected1, expected2, expected3], fixTest);
    }

    [TestMethod]
    public async Task VerifyFixSingleItemFlagsInSwitchStatement_Net35()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            case ItemFlags f when (f & ItemFlags.Advancement) == ItemFlags.Advancement:
                                return true;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;

        var testConfig = new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = fixTest,
            ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net35.Default.WithMultiClient(),
        };

        DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        testConfig.ExpectedDiagnostics.Add(expected);
        await testConfig.RunAsync();
    }

    [TestMethod]
    public async Task VerifyFixMultipleItemFlagsInSwitchStatement_Net35()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            {|#0:case ItemFlags.Advancement:|}
                                return true;
                            {|#1:case ItemFlags.Trap:|}
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        switch (i)
                        {
                            case ItemFlags f when (f & ItemFlags.Advancement) == ItemFlags.Advancement:
                                return true;
                            case ItemFlags f when (f & ItemFlags.Trap) == ItemFlags.Trap:
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }
            """;

        var testConfig = new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = fixTest,
            ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net35.Default.WithMultiClient(),
        };

        DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1);
        testConfig.ExpectedDiagnostics.Add(expected1);
        testConfig.ExpectedDiagnostics.Add(expected2);
        await testConfig.RunAsync();
    }

    [TestMethod]
    public async Task VerifyFixItemFlagsInSwitchExpression_Net35()
    {
        string test = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        return i switch
                        {
                            {|#0:ItemFlags.Advancement|} => true,
                            _ => false
                        };
                    }
                }
            }
            """;
        string fixTest = """
            using System;
            using Archipelago.MultiClient.Net.Enums;

            namespace MyClient
            {
                class MyClass
                {
                    public bool Test()
                    {
                        ItemFlags i = ItemFlags.Advancement;
                        return i switch
                        {
                            ItemFlags f when (f & ItemFlags.Advancement) == ItemFlags.Advancement => true,
                            _ => false
                        };
                    }
                }
            }
            """;

        var testConfig = new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = fixTest,
            ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net35.Default.WithMultiClient(),
        };

        DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
        testConfig.ExpectedDiagnostics.Add(expected);
        await testConfig.RunAsync();
    }
}
