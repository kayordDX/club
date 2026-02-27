using NJsonSchema.Generation;

namespace Online.Common.Swagger;

public class CustomSchemaNameGenerator : DefaultSchemaNameGenerator, ISchemaNameGenerator
{
    // Known namespace roots to strip from type names
    private static readonly string[] KnownNamespacePrefixes =
    {
        "Online.Entities",
        "Online.Features",
        "Online.Models",
        // Add more as needed
    };

    public override string Generate(Type type)
    {
        // Handle generic types (e.g., PaginatedList<Response> -> OutletGetAllAdminPaginatedListResponse)
        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();

            // Special handling for PaginatedList<T>
            if (genericDefinition.Name == "PaginatedList`1" && genericArguments.Length == 1)
            {
                var itemType = genericArguments[0];
                var itemTypeName = itemType.Name;

                var operationName = GetOperationNameFromType(itemType);
                return $"PaginatedList{operationName}{itemTypeName}";
            }

            // Generic fallback for other generic types
            var baseName = genericDefinition.Name.Split('`')[0];
            var argNames = string.Join("", genericArguments.Select(arg => arg.Name));
            return $"{baseName}{argNames}";
        }

        if (type.Name == "Response")
        {
            var operationName = GetOperationNameFromType(type);
            return $"{operationName}{type.Name}";
        }
        if (type.Name == "Request")
        {
            var operationName = GetOperationNameFromType(type);
            return $"{operationName}{type.Name}";
        }
        return type.Name;
    }

    private static string? GetOperationNameFromType(Type type)
    {
        var fullName = type.FullName;
        if (string.IsNullOrEmpty(fullName))
        {
            return null;
        }

        // Strip known namespace prefixes
        var cleanNamespace = StripKnownPrefix(fullName);
        var parts = cleanNamespace.Split('.');

        // Find the index where "Features" starts
        var featuresIndex = Array.IndexOf(parts, "Features");

        if (featuresIndex >= 0 && featuresIndex < parts.Length - 1)
        {
            // Get all parts after "Features" except the last one (which is the class name)
            // For example: Outlet.GetAllAdmin.Response
            // Should extract: Outlet + GetAllAdmin = OutletGetAllAdmin
            var featureParts = new ArraySegment<string>(parts, featuresIndex + 1, parts.Length - featuresIndex - 2);

            if (featureParts.Count > 0)
            {
                // Join the feature folder structure with pascal case
                return string.Concat(featureParts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));
            }
        }

        // Fallback to the second-to-last part if Features not found
        if (parts.Length >= 2)
        {
            return parts[^2];
        }

        return null;
    }

    private static string StripKnownPrefix(string fullName)
    {
        foreach (var prefix in KnownNamespacePrefixes)
        {
            if (fullName.StartsWith(prefix + "."))
            {
                return fullName.Substring(prefix.Length + 1);
            }
        }

        return fullName;
    }
}
