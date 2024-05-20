using System.Reflection;
using System.Text;

using Quikline.Attributes;

namespace Quikline.Parser;

internal static class TypeExtensions
{
    public static Type GetUnderlyingType(this Type fieldType) => Nullable.GetUnderlyingType(fieldType)?.GetUnderlyingType() ?? fieldType;
    
    public static void PrintUsageName(this Type type, FieldInfo? fieldInfo = null)
    {
        var underlyingType = type.GetUnderlyingType();

        using var _ = new Help.Color(ConsoleColor.Blue);
        
        if (underlyingType.IsEnum)
        {
            var names = Enum.GetNames(underlyingType);
            
            for (var i = 0; i < names.Length; i++)
            {
                Console.Out.Write(names[i].SplitPascalCase().ToKebabCase());

                if (i >= names.Length - 1)
                    continue;

                using (new Help.Color(ConsoleColor.Gray))
                    Console.Out.Write("|");
            }
            
            return;
        }
        
        if (underlyingType.IsArray)
        {
            if (fieldInfo is null)
                throw new ArgumentException("Missing field info for array or list type.");
            
            var genericType = underlyingType.GetElementType();
            
            using (new Help.Color(ConsoleColor.Gray))
                Console.Out.Write("[");

            genericType!.PrintUsageName();

            var fixedSizeAttribute = fieldInfo.GetCustomAttribute<FixedSizeAttribute>();

            if (fixedSizeAttribute is not null)
            {
                using (new Help.Color(ConsoleColor.Gray))
                    Console.Out.Write("; ");
                
                using (new Help.Color(ConsoleColor.DarkBlue))
                    Console.Out.Write(fixedSizeAttribute.Size);
            }

            using (new Help.Color(ConsoleColor.Gray))
                Console.Out.Write("]");

            return;
        }

        var typeName = underlyingType.Name
            .PrettifyTypeName()
            .SplitPascalCase()
            .ToKebabCase();
        
        Console.Out.Write(typeName);
    }
    
    public static bool IsNullable(this FieldInfo field)
    {
        if (field.FieldType.IsValueType)
            return Nullable.GetUnderlyingType(field.FieldType) is not null;

        var info = new NullabilityInfoContext().Create(field);
        return info.ReadState == NullabilityState.Nullable;
    }
    
    public static List<RelationAttribute> GetRelations(this Type type) =>
        type.GetCustomAttributes<RelationAttribute>()
            .Concat(type.GetFields()
                .Select(f => f.FieldType)
                .Where(t => t.GetCustomAttribute<ArgsAttribute>() is not null)
                .Select(t => t.GetCustomAttributes<RelationAttribute>())
                .Where(r => r.Any())
                .SelectMany(r => r))
            .ToList();
}

internal static class StringExtensions
{
    public static string SurroundWith(this string value, string prefix, string suffix) =>
        $"{prefix}{value}{suffix}";
    
    public static string[] SplitPascalCase(this string value)
    {
        var result = new List<string>();
        var currentWord = new StringBuilder();

        foreach (var c in value)
        {
            if (char.IsUpper(c) && currentWord.Length > 0)
            {
                result.Add(currentWord.ToString());
                currentWord.Clear();
            }

            currentWord.Append(c);
        }

        result.Add(currentWord.ToString());

        return result.ToArray();
    }

    public static string[] SplitKebabCase(this string value) => value.Split('-');
    public static string[] SplitSnakeCase(this string value) => value.Split('_');

    public static string[] SplitCamelCase(this string value) => value.SplitPascalCase();

    public static string ToPascalCase(this string[] words)
    {
        var result = new StringBuilder();

        foreach (var word in words)
        {
            result.Append(char.ToUpper(word[0]));
            result.Append(word[1..]);
        }

        return result.ToString();
    }

    public static string ToKebabCase(this string[] words) =>
        string.Join('-', words.Select(w => w.ToLower()));

    public static string ToUpperKebabCase(this string[] words) =>
        string.Join('-', words.Select(w => w.ToUpper()));

    public static string ToSnakeCase(this string[] words) =>
        string.Join('_', words.Select(w => w.ToLower()));

    public static string ToScreamingSnakeCase(this string[] words) =>
        string.Join('_', words.Select(w => w.ToUpper()));

    public static string ToCamelCase(this string[] words)
    {
        var result = new StringBuilder();

        result.Append(words[0].ToLower());

        for (var i = 1; i < words.Length; i++)
        {
            result.Append(char.ToUpper(words[i][0]));
            result.Append(words[i][1..]);
        }

        return result.ToString();
    }

    public static string OrIfEmpty(this string value, string defaultValue) =>
        string.IsNullOrEmpty(value) ? defaultValue : value;

    public static string PrettifyTypeName(this string typeName) => typeName switch
    {
        "Int16" => "short",
        "Int32" => "int",
        "Int64" => "long",
        "Single" => "float",
        "Boolean" => "bool",
        "String" => "string",
        _ => typeName
    };
}
