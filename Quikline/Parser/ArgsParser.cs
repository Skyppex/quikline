using System.Reflection;
using System.Text.RegularExpressions;
using Quikline.Attributes;
using Quikline.Parser.Models;

namespace Quikline.Parser;

internal static class ArgsParser
{
    public static Args Parse(IEnumerator<string> argIterator, Interface @interface)
    {
        var parsed = new Args(@interface.CommandType, @interface.CommandName);
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
        if (parsed.Options.Contains(option, new OptionNameEqualityComparer()) && option.MultiFlag == 0)
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

        if (option.MultiFlag is -1 or > 0)
        {
            object val;
            var comparer = new OptionNameEqualityComparer();

            if (parsed.Options.Contains(option, comparer))
            {
                var existingOption = parsed.Options.First(o => comparer.Equals(o, option));
                val = existingOption.Value;

                val = val switch {
                    sbyte v => (object)(v + 1),
                    byte v => (object)(v + 1),
                    short v => (object)(v + 1),
                    ushort v => (object)(v + 1),
                    int v => (object)(v + 1),
                    uint v => (object)(v + 1),
                    long v => (object)(v + 1),
                    ulong v => (object)(v + 1),
                    _ => throw new System.Diagnostics.UnreachableException(),
                };

                val = Convert.ChangeType(val, optionType);
            }
            else
            {
                val = Convert.ChangeType(1, optionType);
            }

            if (option.MultiFlag > 0 && Convert.ToInt64(val) > option.MultiFlag)
            {
                Console.Error.WriteLine($"Incorrect usage. {arg} cannot be passed more than {option.MultiFlag} times");
                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            parsed.AddOption(option.Passed(val));
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
    
    private static object GetValue(Type type, string valueArg, FieldInfo? fieldInfo, string arg, string text)
    {
        var underlyingType = type.GetUnderlyingType();
        
        if (underlyingType == typeof(int))
        {
            if (!int.TryParse(valueArg, out var intValue))
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
            if (!float.TryParse(valueArg, out var floatValue))
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
            if (!double.TryParse(valueArg, out var doubleValue))
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
            if (!char.TryParse(valueArg, out var charValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a char value for {text} {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            return charValue;
        }
        
        if (underlyingType == typeof(string))
            return valueArg;
        
        if (underlyingType.IsEnum)
        {
            var enumFields = underlyingType.GetFields();
            var names = new Dictionary<string, string>();

            foreach (var enumField in enumFields)
            {
                var nameAttr = enumField.GetCustomAttribute<NameAttribute>();
                names.Add(nameAttr is null ? enumField.Name : nameAttr.Name, enumField.Name);
            }

            if (!names.TryGetValue(valueArg, out var enumVariantName))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an enum value for {text} {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }
            
            if (!Enum.TryParse(underlyingType, enumVariantName, ignoreCase: true, out var enumValue))
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
                .Invoke(null, [valueArg]);
            
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

            var values = useRegex ? SplitRegex(valueArg, delimiter).ToList()
                : valueArg.Split(delimiter).ToList();

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
