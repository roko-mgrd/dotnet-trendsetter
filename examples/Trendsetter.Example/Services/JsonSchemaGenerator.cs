namespace Trendsetter.Example.Services;

using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

/// <summary>
/// Generates JSON Schema objects from CLR types via reflection.
/// Uses [Description] for field descriptions and [JsonPropertyName] for naming.
/// </summary>
public static class JsonSchemaGenerator
{
    public static JsonObject Generate<T>()
    {
        return GenerateObjectSchema(typeof(T));
    }

    public static JsonObject Generate(Type type)
    {
        return GenerateObjectSchema(type);
    }

    private static JsonObject GenerateObjectSchema(Type type)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = GetJsonPropertyName(prop);
            var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description;
            var schema = GeneratePropertySchema(prop.PropertyType);

            if (description is not null)
                schema["description"] = description;

            properties[name] = schema;
            required.Add(name);
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
        };
    }

    private static JsonObject GeneratePropertySchema(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
            return GeneratePropertySchema(underlying);

        if (type == typeof(string))
            return new JsonObject { ["type"] = "string" };

        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            return new JsonObject { ["type"] = "integer" };

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return new JsonObject { ["type"] = "number" };

        if (type == typeof(bool))
            return new JsonObject { ["type"] = "boolean" };

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
            return new JsonObject { ["type"] = "string", ["format"] = "date" };

        if (IsListType(type, out var elementType))
        {
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = GeneratePropertySchema(elementType),
            };
        }

        if (type.IsClass || type.IsValueType)
            return GenerateObjectSchema(type);

        return new JsonObject { ["type"] = "string" };
    }

    private static bool IsListType(Type type, out Type elementType)
    {
        if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            if (genDef == typeof(List<>) || genDef == typeof(IList<>) ||
                genDef == typeof(IReadOnlyList<>) || genDef == typeof(IEnumerable<>) ||
                genDef == typeof(ICollection<>) || genDef == typeof(IReadOnlyCollection<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var iface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (iface is not null)
            {
                elementType = iface.GetGenericArguments()[0];
                return true;
            }
        }

        elementType = typeof(object);
        return false;
    }

    private static string GetJsonPropertyName(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (attr is not null)
            return attr.Name;
        return JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
    }
}
