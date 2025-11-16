using Microsoft.CodeAnalysis.Testing;

namespace Archipelago.MultiClient.Net.Analyzers.Test.Util;

internal static class ReferenceAssemblyBuilder
{
    public static ReferenceAssemblies DefaultWithMultiClient => ReferenceAssemblies.Default.WithMultiClient();

    public static ReferenceAssemblies WithMultiClient(this ReferenceAssemblies referenceAssemblies)
    {
        return referenceAssemblies.AddPackages([new PackageIdentity("Archipelago.MultiClient.Net", "6.3.0")]);
    }
}
