using System.Reflection;
using System.Text.RegularExpressions;
using Quikline.Attributes;
using Quikline.Parser.Models;

namespace Quikline.Parser;

internal static class ArgsParser
{
    public static Args Parse(IEnumerator<string> argIterator, Interface @interface)
    {
        var parsed = new Args(@interface.CommandType);
        IEnumerator<Argument> interfaceArgumentIterator = @interface.Arguments.GetEnumerator();

        while (true)
        {
            if (!argIterator.MoveNext())
                break;

            var arg = argIterator.Current;

            var subcommand = @interface.Subcommands.SingleOrDefault(
                sc => sc.CommandName.SplitPascalCase().ToKebabCase().Equals(
                    arg,
                    StringComparison.CurrentCultureIgnoreCase));

            if (subcommand is not null)
            {
                parsed.Subcommand = Parse(argIterator, subcommand);
                return parsed;
            }

            if (arg.StartsWith(@interface.ShortPrefix) && !arg.StartsWith(@interface.LongPrefix))
            {
                var shortArgs = arg[@interface.ShortPrefix.Length..];

                foreach (var shortArg in shortArgs)
                {
                    if (@interface.TryGetOption(
                        $"{@interface.ShortPrefix}{shortArg}",
                        out var option))
                    {
                        ParseOption(shortArg.ToString(), parsed, argIterator, option);
                        continue;
                    }

                    Console.Error.WriteLine($"Incorrect usage. Unknown option: {@interface.ShortPrefix}{shortArg}");
                    Console.Error.Write("Use --help for more information.");
                    Environment.Exit(1);
                }

                continue;
            }

            ParseArg(arg, @interface, parsed, argIterator, interfaceArgumentIterator);
        }

        foreach (var option in @interface.Options.Where(
                     o => !parsed.Options.Contains(o, new OptionNameEqualityComparer())))
            parsed.AddOption(option);

        foreach (var argument in @interface.Arguments.Where(
                     o => !parsed.Arguments.Contains(o, new ArgumentNameEqualityComparer())))
            parsed.AddArgument(argument);

        return parsed;
    }

    private static void ParseArg(
        string arg,
        Interface @interface,
        Args parsed,
        IEnumerator<string> argIterator,
        IEnumerator<Argument> interfaceArgumentIterator)
    {
        if (@interface.TryGetOption(arg, out var option))
        {
            ParseOption(arg, parsed, argIterator, option);
            return;
        }

        ParseArgument(arg, parsed, argIterator, interfaceArgumentIterator);
    }

    private static void ParseArgument(
        string arg,
        Args parsed,
        IEnumerator<string> argIterator,
        IEnumerator<Argument> interfaceArgumentIterator)
    {
        if (!interfaceArgumentIterator.MoveNext())
        {
            Console.Error.WriteLine($"Incorrect usage. Unexpected argument: {arg}");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        var value = argIterator.Current;

        var argument = interfaceArgumentIterator.Current;

        if (argument.IsRest)
        {
            ParseRestArguments(parsed, argIterator, interfaceArgumentIterator, value, argument);
            return;
        }

        var argumentType = argument.Type;
        argument = argument.Passed(GetValue(argumentType, value, argument.FieldInfo, arg, "argument"));
        parsed.AddArgument(argument);
    }

    private static void ParseRestArguments(
        Args parsed,
        IEnumerator<string> argIterator,
        IEnumerator<Argument> interfaceArgumentIterator,
        string value,
        Argument argument)
    {
        Argument? separatedArgument = null;

        if (interfaceArgumentIterator.MoveNext())
            separatedArgument = interfaceArgumentIterator.Current;

        List<string> restList = [value];

        while (argIterator.MoveNext())
            if (ParseSingleRestArgument(parsed, argIterator, interfaceArgumentIterator, separatedArgument, restList))
                break;

        argument = argument.Passed(string.Join(" ", restList));
        parsed.AddArgument(argument);
    }

    private static bool ParseSingleRestArgument(
        Args parsed,
        IEnumerator<string> argIterator,
        IEnumerator<Argument> interfaceArgumentIterator,
        Argument? separatedArgument,
        List<string> restList)
    {
        var current = argIterator.Current;

        if (separatedArgument is not null &&
            separatedArgument.Value.RestSeparator == current)
        {
            var currentArgument = separatedArgument;
            
            if (interfaceArgumentIterator.MoveNext())
                separatedArgument = interfaceArgumentIterator.Current;

            List<string> separatedRestList = [];

            while (argIterator.MoveNext())
                if (ParseSingleRestArgument(parsed, argIterator, interfaceArgumentIterator, separatedArgument, separatedRestList))
                    break;

            currentArgument =
                currentArgument.Value.Passed(string.Join(" ", separatedRestList));

            parsed.AddArgument(currentArgument.Value);

            return true;
        }

        restList.Add(current);
        return false;
    }

    private static void ParseOption(
        string arg,
        Args parsed,
        IEnumerator<string> argIterator,
        Option option)
    {
        if (parsed.Options.Contains(option, new OptionNameEqualityComparer()))
        {
            Console.Error.WriteLine($"Incorrect usage. Duplicate option: {arg}");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        var optionType = option.Type;

        if (optionType == typeof(bool))
        {
            parsed.AddOption(option.Passed(true));

            return;
        }

        if (!argIterator.MoveNext())
        {
            Console.Error.WriteLine($"Incorrect usage. Expected a value for option {arg}.");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        var value = argIterator.Current;
        option = option.Passed(GetValue(optionType, value, option.FieldInfo, arg, "argument"));
        parsed.AddOption(option);
    }
    
    private static object GetValue(Type type, string value, FieldInfo? fieldInfo, string arg, string text)
    {
        var underlyingType = type.GetUnderlyingType();
        
        if (underlyingType == typeof(int))
        {
            if (!int.TryParse(value, out var intValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an integer value for {text} {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            return intValue;
        }
        
        if (underlyingType == typeof(float))
        {
            if (!float.TryParse(value, out var floatValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a float value for {text} {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            return floatValue;
        }
        
        if (underlyingType == typeof(double))
        {
            if (!double.TryParse(value, out var doubleValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a double value for {text} {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            return doubleValue;
        }
        
        if (underlyingType == typeof(char))
        {
            if (!char.TryParse(value, out var charValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a char value for {text} {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            return charValue;
        }
        
        if (underlyingType == typeof(string))
            return value;
        
        if (underlyingType.IsEnum)
        {
            if (!Enum.TryParse(underlyingType, value, ignoreCase: true, out var enumValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an enum value for {text} {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            return enumValue;
        }
        
        if (underlyingType.IsValueType &&
                 underlyingType.IsAssignableTo(typeof(IFromString<>)
                     .MakeGenericType(underlyingType)))
        {
            var valueFromString = underlyingType.GetMethod(
                    "FromString",
                    BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, [value]);
            
            if (valueFromString!.GetType()
                    .GetField("Item2")!
                    .GetValue(valueFromString) is string error)
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. {error} | {text} {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            return valueFromString.GetType()
                .GetField("Item1")!
                .GetValue(valueFromString)!;
        }

        if (underlyingType.IsArray)
        {
            var genericType = underlyingType.GetElementType();
            
            var delimiterAttribute = fieldInfo?
                .GetCustomAttribute<DelimiterAttribute>();

            var delimiter = delimiterAttribute?.Delimiter 
                            ?? ","; // Default delimiter

            var useRegex = delimiterAttribute?.Regex ?? false;

            var values = useRegex ? SplitRegex(value, delimiter).ToList()
                : value.Split(delimiter).ToList();

            var fixedSizeAttribute = fieldInfo?.GetCustomAttribute<FixedSizeAttribute>();

            if (fixedSizeAttribute != null)
            {
                if (values.Count != fixedSizeAttribute.Size)
                {
                    Console.Error.WriteLine(
                        $"Incorrect usage. Expected {fixedSizeAttribute.Size} values for {text} {arg}.");

                    Console.Error.Write("Use --help for more information.");
                    Environment.Exit(1);
                }
            }
            
            var enumerable = values.Select(val => GetValue(genericType!, val, fieldInfo, arg, text)).ToArray();

            if (underlyingType.IsArray)
            {
                var array = Array.CreateInstance(genericType!, values.Count);
                
                for (var i = 0; i < values.Count; i++)
                    array.SetValue(enumerable[i], i);
                
                return array;
            }
        }
        
        Console.Error.WriteLine(
            $"Incorrect setup. Unsupported type for {text} {arg}.");
        
        Console.Error.Write("Use --help for more information.");
        Environment.Exit(1);
        return null;
    }
    
    private static IEnumerable<string> SplitRegex(string value, string delimiter)
    {
        var regex = new Regex(delimiter);
        var matches = regex.Matches(value).ToList();

        var prevIndex = 0;
        foreach (var match in matches)
        {
            yield return value[prevIndex..match.Index];
            prevIndex = match.Index + match.Length;
        }
        
        yield return value[prevIndex..];
    }
}
