using NJsonSchema.Generation;

namespace Online.Common.Swagger;

public class CustomSchemaNameGenerator : DefaultSchemaNameGenerator, ISchemaNameGenerator
{
    public override string Generate(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();

            // Special handling for PaginatedList<T>
            // if (genericDefinition.Name == "PaginatedList`1" && genericArguments.Length == 1)
            // {
            //     var itemTypeName = genericArguments[0].Name;
            //     return $"PaginatedList{itemTypeName}";
            // }

            // Generic fallback for other generic types
            var baseName = genericDefinition.Name.Split('`')[0];
            var argNames = string.Join("", genericArguments.Select(arg => arg.Name));
            if (argNames == "Response")
            {
                var operationName = GetOperationNameFromType(type);
                argNames = $"{operationName}Response";
            }
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
        var parts = fullName?.Split('.') ?? [];

        // Find the index where "Features" starts
        var featuresIndex = Array.IndexOf(parts, "Features");

        if (featuresIndex >= 0 && featuresIndex < parts.Length - 1)
        {
            // Get all parts after "Features" except the last one (which is the class name)
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
}
