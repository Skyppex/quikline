using System.Text;

namespace Quikline.Parser;

internal static class TypeExtensions
{
    public static void PrintUsageName(this Type type)
    {
        if (type.IsEnum)
        {
            string[] names = Enum.GetNames(type);
            
            for (int i = 0; i < names.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Out.Write(names[i].SplitPascalCase().ToKebabCase());
                
                if (i < names.Length - 1)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Out.Write("|");
                }
            }
            
            return;
        }

        Console.Out.Write(type.Name.SplitPascalCase().ToKebabCase());
    }
}

internal static class StringExtensions
{
    public static string[] SplitPascalCase(this string value)
    {
        var result = new List<string>();
        var currentWord = new StringBuilder();

        foreach (char c in value)
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

        foreach (string word in words)
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

        for (int i = 1; i < words.Length; i++)
        {
            result.Append(char.ToUpper(words[i][0]));
            result.Append(words[i][1..]);
        }

        return result.ToString();
    }

    public static string OrIfEmpty(this string value, string defaultValue) =>
        string.IsNullOrEmpty(value) ? defaultValue : value;
}
