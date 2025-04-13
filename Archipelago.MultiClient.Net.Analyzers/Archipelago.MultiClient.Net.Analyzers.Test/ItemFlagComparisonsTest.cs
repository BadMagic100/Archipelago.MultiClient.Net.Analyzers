using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Archipelago.MultiClient.Net.Analyzers.Test.CSharpCodeFixVerifier<
    Archipelago.MultiClient.Net.Analyzers.Analyzers.ItemFlagsDiagnostics,
    Archipelago.MultiClient.Net.Analyzers.Fixes.ItemFlagsFixes>;

namespace Archipelago.MultiClient.Net.Analyzers.Test
{
    [TestClass]
    public class ItemFlagComparisonsTest
    {
        [TestMethod]
        public async Task VerifyEmptySourceYieldsNoDiagnostic()
        {
            string test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyOtherTypeComparisonYieldsNoDiagnostic()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            int i = 0;
            bool b = i >= i;
            return i == i;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyHasFlagComparisonYieldsNoDiagnostic()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return i.HasFlag(ItemFlags.Advancement);
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyEqualsComparisonYieldsDiagnostic()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return {|#0:i == ItemFlags.Advancement|};
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT002").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task VerifyEqualsComparisonToFillerYieldsNoDiagnostic()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return i == ItemFlags.None;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyFixLhsIsConstantLocal()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            const ItemFlags ii = ItemFlags.Advancement;
            ItemFlags i = ItemFlags.Advancement;
            return {|#0:ii == i|};
        }
    }
}";
            string fixTest = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            const ItemFlags ii = ItemFlags.Advancement;
            ItemFlags i = ItemFlags.Advancement;
            return i.HasFlag(ii);
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT002").WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        [TestMethod]
        public async Task VerifyFixLhsIsConstantField()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        const ItemFlags ii = ItemFlags.Advancement;
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return {|#0:ii == i|};
        }
    }
}";
            string fixTest = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        const ItemFlags ii = ItemFlags.Advancement;
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return i.HasFlag(ii);
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT002").WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        [TestMethod]
        public async Task VerifyFixLhsIsMemberAccess()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return {|#0:ItemFlags.Advancement == i|};
        }
    }
}";
            string fixTest = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return i.HasFlag(ItemFlags.Advancement);
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT002").WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        [TestMethod]
        public async Task VerifyFixRhsIsMemberAccess()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return {|#0:i == ItemFlags.Advancement|};
        }
    }
}";
            string fixTest = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            return i.HasFlag(ItemFlags.Advancement);
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT002").WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }
    }
}
