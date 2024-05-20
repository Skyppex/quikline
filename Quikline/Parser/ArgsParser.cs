using System.Reflection;
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

        if (argumentType == typeof(int))
        {
            if (!int.TryParse(value, out var intValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an integer value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument.Passed(intValue);
        }
        else if (argumentType == typeof(float))
        {
            if (!float.TryParse(value, out var floatValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a float value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument.Passed(floatValue);
        }
        else if (argumentType == typeof(double))
        {
            if (!double.TryParse(value, out var doubleValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a double value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument.Passed(doubleValue);
        }
        else if (argumentType == typeof(char))
        {
            if (!char.TryParse(value, out var charValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a char value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument.Passed(charValue);
        }
        else if (argumentType == typeof(string))
        {
            argument = argument.Passed(value);
        }
        else if (argumentType.IsEnum)
        {
            if (!Enum.TryParse(argumentType, value, ignoreCase: true, out var enumValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an enum value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument.Passed(enumValue);
        }
        else if (argumentType.IsValueType &&
                 argumentType.IsAssignableTo(typeof(IFromString<>)
                     .MakeGenericType(argumentType)))
        {
            var valueFromString = argumentType.GetMethod(
                    "FromString",
                    BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, [value]);
            
            if (valueFromString!.GetType()
                    .GetField("Item2")!
                    .GetValue(valueFromString) is string error)
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. {error}");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
                return;
            }
            
            argument = argument.Passed(valueFromString.GetType()
                .GetField("Item1")!
                .GetValue(valueFromString));
        }

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

        if (optionType == typeof(int))
        {
            if (!int.TryParse(value, out var intValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an integer value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option.Passed(intValue);
        }
        else if (optionType == typeof(float))
        {
            if (!float.TryParse(value, out var floatValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a float value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option.Passed(floatValue);
        }
        else if (optionType == typeof(double))
        {
            if (!double.TryParse(value, out var doubleValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a double value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option.Passed(doubleValue);
        }
        else if (optionType == typeof(char))
        {
            if (!char.TryParse(value, out var charValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a char value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option.Passed(charValue);
        }
        else if (optionType == typeof(string))
        {
            option = option.Passed(value);
        }
        else if (optionType.IsEnum)
        {
            if (!Enum.TryParse(
                    optionType,
                    value.SplitKebabCase().ToPascalCase(),
                    ignoreCase: true,
                    out var enumValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an enum value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option.Passed(enumValue);
        }
        else if (optionType.IsValueType &&
                 optionType.IsAssignableTo(typeof(IFromString<>)
                     .MakeGenericType(optionType)))
        {
            var valueFromString = optionType.GetMethod("FromString",
                BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, [value]);
            
            if (valueFromString!.GetType()
                    .GetField("Item2")!
                    .GetValue(valueFromString) is string error)
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. {error}");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
                return;
            }
            
            option = option.Passed(valueFromString.GetType()
                .GetField("Item1")!
                .GetValue(valueFromString));
        }

        parsed.AddOption(option);
    }
}
