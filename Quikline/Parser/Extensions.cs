using System.Reflection;
using System.Text;

namespace Quikline.Parser;

internal static class TypeExtensions
{
    public static Type GetUnderlyingType(this Type fieldType) => Nullable.GetUnderlyingType(fieldType)?.GetUnderlyingType() ?? fieldType;
    
    public static void PrintUsageName(this Type type)
    {
        if (type.IsEnum)
        {
            var names = Enum.GetNames(type);
            
            for (var i = 0; i < names.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Out.Write(names[i].SplitPascalCase().ToKebabCase());

                if (i >= names.Length - 1)
                    continue;

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Out.Write("|");
            }
            
            return;
        }

        Console.Out.Write(type.Name.SplitPascalCase().ToKebabCase());
    }
    
    public static bool IsNullable(this FieldInfo field)
    {
        if (field.FieldType.IsValueType)
            return Nullable.GetUnderlyingType(field.FieldType) is not null;

        var info = new NullabilityInfoContext().Create(field);
        return info.ReadState == NullabilityState.Nullable;
    }
}

internal static class StringExtensions
{
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
}
