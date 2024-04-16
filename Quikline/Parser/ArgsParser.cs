namespace Quikline.Parser;

internal static class ArgsParser
{
    public static Args Parse(string[] args, Interface @interface)
    {
        var parsed = new Args();

        IEnumerator<string> argIterator = args.ToList().GetEnumerator();
        IEnumerator<Argument> interfaceArgumentIterator = @interface.Arguments.GetEnumerator();

        while (true)
        {
            if (!argIterator.MoveNext())
                break;

            string arg = argIterator.Current;

            if (!arg.StartsWith(@interface.LongPrefix) && arg.StartsWith(@interface.ShortPrefix))
            {
                string shortArgs = arg[@interface.ShortPrefix.Length..];

                foreach (char shortArg in shortArgs)
                    if (@interface.TryGetOption($"{@interface.ShortPrefix}{shortArg}", out Option option))
                        ParseOption(shortArg.ToString(), parsed, argIterator, option);

                continue;
            }
            
            ParseArg(arg, @interface, parsed, argIterator, interfaceArgumentIterator);
        }

        return parsed;
    }

    private static void ParseArg(
        string arg,
        Interface @interface,
        Args parsed,
        IEnumerator<string> argIterator,
        IEnumerator<Argument> interfaceArgumentIterator)
    {
        if (@interface.TryGetOption(arg, out Option option))
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

        string value = argIterator.Current;

        Argument argument = interfaceArgumentIterator.Current;

        if (argument.IsRest)
        {
            Argument? separatedArgument = null;
            
            if (interfaceArgumentIterator.MoveNext())
                separatedArgument = interfaceArgumentIterator.Current;
            
            List<string> restList = [value];

            while (argIterator.MoveNext())
            {
                string current = argIterator.Current;

                if (separatedArgument is not null &&
                    separatedArgument.Value.RestSeparator == current)
                {
                    List<string> separatedRestList = [];

                    while (argIterator.MoveNext())
                        separatedRestList.Add(argIterator.Current);

                    separatedArgument = separatedArgument.Value with
                    {
                        Value = string.Join(" ", separatedRestList)
                    };

                    parsed.AddArgument(separatedArgument.Value);

                    break;
                }
                
                restList.Add(current);
            }

            argument = argument with
            {
                Value = string.Join(" ", restList)
            };

            parsed.AddArgument(argument);

            return;
        }

        if (argument.Type == typeof(int))
        {
            if (!int.TryParse(value, out int intValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an integer value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument with
            {
                Value = intValue
            };
        }
        else if (argument.Type == typeof(float))
        {
            if (!float.TryParse(value, out float floatValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a float value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument with
            {
                Value = floatValue
            };
        }
        else if (argument.Type == typeof(double))
        {
            if (!double.TryParse(value, out double doubleValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a double value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument with
            {
                Value = doubleValue
            };
        }
        else if (argument.Type == typeof(char))
        {
            if (!char.TryParse(value, out char charValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a char value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument with
            {
                Value = charValue
            };
        }
        else if (argument.Type == typeof(string))
        {
            argument = argument with
            {
                Value = value
            };
        }
        else if (argument.Type.IsEnum)
        {
            if (!Enum.TryParse(argument.Type, value, ignoreCase: true, out object? enumValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an enum value for argument {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            argument = argument with
            {
                Value = enumValue
            };
        }

        parsed.AddArgument(argument);
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

        if (option.Type == typeof(bool))
        {
            parsed.AddOption(option);

            return;
        }

        if (!argIterator.MoveNext())
        {
            Console.Error.WriteLine($"Incorrect usage. Expected a value for option {arg}.");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        string value = argIterator.Current;

        if (option.Type == typeof(int))
        {
            if (!int.TryParse(value, out int intValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an integer value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option with
            {
                Value = intValue
            };
        }
        else if (option.Type == typeof(float))
        {
            if (!float.TryParse(value, out float floatValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a float value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option with
            {
                Value = floatValue
            };
        }
        else if (option.Type == typeof(double))
        {
            if (!double.TryParse(value, out double doubleValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a double value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option with
            {
                Value = doubleValue
            };
        }
        else if (option.Type == typeof(char))
        {
            if (!char.TryParse(value, out char charValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected a char value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option with
            {
                Value = charValue
            };
        }
        else if (option.Type == typeof(string))
        {
            option = option with
            {
                Value = value
            };
        }
        else if (option.Type.IsEnum)
        {
            if (!Enum.TryParse(option.Type, value, ignoreCase: true, out object? enumValue))
            {
                Console.Error.WriteLine(
                    $"Incorrect usage. Expected an enum value for option {arg}.");

                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option with
            {
                Value = enumValue
            };
        }

        parsed.AddOption(option);
    }
}