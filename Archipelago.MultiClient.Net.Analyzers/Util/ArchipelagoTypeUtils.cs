using Microsoft.CodeAnalysis;

namespace Archipelago.MultiClient.Net.Analyzers.Util
{
    internal static class ArchipelagoTypeUtils
    {
        public static bool IsTypeArchipelagoSession(ITypeSymbol? type, Compilation compilation)
        {
            return IsTypeDesiredTypeOrInterface("Archipelago.MultiClient.Net.IArchipelagoSession", type, compilation);
        }

        public static bool IsTypeDataStorageElement(ITypeSymbol? type, Compilation compilation)
        {
            return IsTypeDesiredType("Archipelago.MultiClient.Net.Models.DataStorageElement", type, compilation);
        }

        public static bool IsTypeDataStorageHelper(ITypeSymbol? type, Compilation compilation)
        {
            return IsTypeDesiredTypeOrInterface("Archipelago.MultiClient.Net.Helpers.IDataStorageHelper", type, compilation);
        }

        public static bool IsTypeItemFlags(ITypeSymbol? type, Compilation compilation)
        {
            return IsTypeDesiredType("Archipelago.MultiClient.Net.Enums.ItemFlags", type, compilation);
        }

        private static bool IsTypeDesiredType(string checkTypeFullName, ITypeSymbol? type, Compilation compilation)
        {
            INamedTypeSymbol? checkType = compilation.GetTypeByMetadataName(checkTypeFullName);
            return type != null && checkType != null && checkType.Equals(type, SymbolEqualityComparer.Default);
        }

        private static bool IsTypeDesiredTypeOrInterface(string checkTypeFullName, ITypeSymbol? type, Compilation compilation)
        {
            INamedTypeSymbol? checkType = compilation.GetTypeByMetadataName(checkTypeFullName);
            return type != null && checkType != null && (
                checkType.Equals(type, SymbolEqualityComparer.Default)
                || type.AllInterfaces.Contains(checkType, SymbolEqualityComparer.Default)
            );
        }
    }
}
