using Microsoft.CodeAnalysis;

namespace Archipelago.MultiClient.Net.Analyzers.Util;
internal static class NameGenerator
{
    public static string GetUniqueVariableName(string baseName, SemanticModel model, int startPosition)
    {
        string name = baseName;
        int i = 1;
        while (model.LookupSymbols(startPosition, name: name).Length > 0)
        {
            name = baseName + i;
            i++;
        }
        return name;
    }
}
