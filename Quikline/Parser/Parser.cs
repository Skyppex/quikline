using System.Reflection;

using Quikline.Attributes;

namespace Quikline.Parser;

public static class Quik
{
    public static T Parse<T>()
        where T : struct
    {
        string[] args = Environment.GetCommandLineArgs()[1..];

        Type type = typeof(T);
        Interface @interface = ParseInterface(type);

        Args passedArgs = ParseArgs(args, @interface);

        if (passedArgs.Options.Any(
                o =>
                {
                    Prefix interfaceLongPrefix = @interface.LongPrefix;

                    return o.Matches($"{interfaceLongPrefix}help");
                }))
        {
            PrintHelp(@interface);
            Environment.Exit(0);

            return default;
        }

        if (passedArgs.Options.Any(o => o.Matches($"{@interface.LongPrefix}version")))
        {
            Version? version = type.Assembly.GetName().Version;
            Console.Out.Write(version);
            Environment.Exit(0);

            return default;
        }

        if (MissingRequired(@interface, passedArgs, out string[] missing))
        {
            Console.Error.WriteLine($"Incorrect usage. Missing required options: {missing}");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        object value = Activator.CreateInstance<T>();

        foreach (Option option in passedArgs.Options)
        {
            string fieldName = option.FieldName;

            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.SetField)!;

            if (option.Type == typeof(bool))
            {
                field.SetValue(value, true);

                continue;
            }

            field.SetValue(value, option.Value);
        }

        foreach (Argument argument in passedArgs.Arguments)
        {
            string fieldName = argument.FieldName;

            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.SetField)!;

            if (argument.Type == typeof(bool))
            {
                field.SetValue(value, true);

                continue;
            }

            field.SetValue(value, argument.Value);
        }
        
        return (T)value;
    }

    private static Interface ParseInterface(Type type)
    {
        IEnumerable<Attribute> attributes = type.GetCustomAttributes();

        if (attributes.SingleOrDefault(a => a is CommandAttribute) is not CommandAttribute
            commandAttr)
            throw new InvalidOperationException(
                "The provided type does not have a Command attribute.");

        var @interface = new Interface(commandAttr);

        // Add the options and arguments from the fields.
        FieldInfo[] fields = type.GetFields();

        foreach (FieldInfo field in fields)
        {
            var optionAttr = (OptionAttribute?)field.GetCustomAttributes()
                .SingleOrDefault(a => a is OptionAttribute);

            var argumentAttr = (ArgumentAttribute?)field.GetCustomAttributes()
                .SingleOrDefault(a => a is ArgumentAttribute);

            var restAttr = (RestAttribute?)field.GetCustomAttributes()
                .SingleOrDefault(a => a is RestAttribute);

            if (optionAttr is null && argumentAttr is null && restAttr is null)
                continue;

            if (optionAttr is not null && argumentAttr is not null && restAttr is not null)
                throw new InvalidProgramException(
                    $"Incorrect setup. Field {field.Name} cannot be both an option and an argument.");

            if (optionAttr is not null)
                ParseInterfaceOption(optionAttr, @interface, field);
            else if (argumentAttr is not null)
                ParseInterfaceArgument(argumentAttr, @interface, field);
            else if (restAttr is not null)
                ParseInterfaceRestArgument(restAttr, @interface, field);
        }

        // Add generated options.
        if (commandAttr.Version)
        {
            bool isUsingLowerCaseV = @interface.Options.Contains(
                Option.ShortOnly(new Short(@interface.ShortPrefix, new Name("v"))),
                new ShortOptionEqualityComparer());

            bool isUsingUpperCaseV = @interface.Options.Contains(
                Option.ShortOnly(new Short(@interface.ShortPrefix, new Name("V"))),
                new ShortOptionEqualityComparer());

            Short? shortVersion = (hasv: isUsingLowerCaseV, hasV: isUsingUpperCaseV) switch
            {
                (false, _) => new Short(@interface.ShortPrefix, new Name("v")),
                (true, false) => new Short(@interface.ShortPrefix, new Name("V")),
                _ => null
            };

            @interface.Options.Insert(
                0,
                new Option(
                    "",
                    false,
                    shortVersion,
                    new Long(@interface.LongPrefix, new Name("version")),
                    typeof(bool),
                    null,
                    "Print the version"));
        }

        bool isUsingLowerCaseH = @interface.Options.Contains(
            Option.ShortOnly(new Short(@interface.ShortPrefix, new Name("h"))),
            new ShortOptionEqualityComparer());

        bool isUsingUpperCaseH = @interface.Options.Contains(
            Option.ShortOnly(new Short(@interface.ShortPrefix, new Name("H"))),
            new ShortOptionEqualityComparer());

        Short? shortHelp = (isUsingLowerCaseH, isUsingUpperCaseH) switch
        {
            (false, _) => new Short(@interface.ShortPrefix, new Name("h")),
            (true, false) => new Short(@interface.ShortPrefix, new Name("H")),
            _ => null
        };

        @interface.Options.Insert(
            0,
            new Option(
                "",
                false,
                shortHelp,
                new Long(@interface.LongPrefix, new Name("help")),
                typeof(bool),
                null,
                "Print this help message"));

        if (@interface.Arguments.Count(a => a.IsRest) > 2)
            throw new InvalidProgramException(
                "Incorrect setup. At most two fields can have the Rest attribute. One with a separator and one without.");

        if (@interface.Arguments.Count(a => a is { IsRest: true, RestSeparator: null }) > 1)
            throw new InvalidProgramException(
                "Incorrect setup. At most two fields can have the Rest attribute. One with a separator and one without.");
        
        if (@interface.Arguments.Count(a => a is { IsRest: true, RestSeparator: not null }) > 1)
            throw new InvalidProgramException(
                "Incorrect setup. At most two fields can have the Rest attribute. One with a separator and one without.");
        
        @interface.Arguments.Sort(new ArgumentComparer());

        return @interface;
    }

    private static void ParseInterfaceOption(
        OptionAttribute optionAttr,
        Interface @interface,
        FieldInfo field)
    {
        Short? @short = optionAttr.Short is '\0'
            ? null
            : new Short(
                optionAttr.ShortPrefix.ToPrefix() ?? @interface.ShortPrefix,
                new Name(optionAttr.Short.ToString().OrIfEmpty(field.Name.First().ToString())));

        Long @long = (optionAttr.LongPrefix, optionAttr.Long) switch
        {
            (null, null) => new Long(
                @interface.LongPrefix,
                new Name(field.Name.SplitPascalCase().ToKebabCase())),
            (not null, null) => new Long(
                new Prefix(optionAttr.LongPrefix),
                new Name(field.Name.SplitPascalCase().ToKebabCase())),
            (null, not null) => new Long(
                @interface.LongPrefix,
                new Name(optionAttr.Long)),
            (not null, not null) => new Long(
                new Prefix(optionAttr.LongPrefix),
                new Name(optionAttr.Long)),
        };

        var option = new Option(
            field.Name,
            optionAttr.Required,
            @short,
            @long,
            field.FieldType,
            null,
            optionAttr.Description);

        if (field.FieldType.IsEnum)
        {
            if (optionAttr.Default is null)
                option = option with
                {
                    Value = Enum.GetValues(field.FieldType).GetValue(0)
                };
            else
                option = option with
                {
                    Value = Enum.Parse(
                        field.FieldType,
                        optionAttr.Default.ToString()!,
                        ignoreCase: true)
                };
        }

        @interface.AddOption(option);
    }

    private static void ParseInterfaceArgument(
        ArgumentAttribute argumentAttr,
        Interface @interface,
        FieldInfo field)
    {
        var name = argumentAttr.Name ?? field.Name.SplitPascalCase().ToKebabCase();

        var argument = new Argument(
            field.Name,
            argumentAttr.Optional,
            name,
            field.FieldType,
            null,
            argumentAttr.Description);

        if (field.FieldType.IsEnum)
        {
            if (argumentAttr.Default is null)
                argument = argument with
                {
                    Value = Enum.GetValues(field.FieldType).GetValue(0)
                };
            else
                argument = argument with
                {
                    Value = Enum.Parse(
                        field.FieldType,
                        argumentAttr.Default.ToString()!,
                        ignoreCase: true)
                };
        }

        @interface.AddArgument(argument);
    }

    private static void ParseInterfaceRestArgument(
        RestAttribute restAttr,
        Interface @interface,
        FieldInfo field)
    {
        if (field.FieldType != typeof(string))
            throw new InvalidProgramException(
                $"Incorrect setup. Field {field.Name} with Rest attribute must be of type string.");

        var argument = new Argument(
            field.Name,
            false,
            restAttr.Name ?? field.Name.SplitPascalCase().ToKebabCase(),
            field.FieldType,
            null,
            restAttr.Description,
            IsRest: true,
            RestSeparator: restAttr.Separator);

        @interface.AddArgument(argument);
    }

    private static Args ParseArgs(string[] args, Interface @interface)
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
            ParseOption(arg, parsed, argIterator, option);

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

    private static bool MissingRequired(Interface @interface, Args args, out string[] missing)
    {
        missing = @interface.Options
            .Where(o => o.Required && !args.Options.Contains(o, new ShortOptionEqualityComparer()))
            .Select(o => o.Long.Name.ToString())
            .ToArray();

        return missing.Length > 0;
    }

    private static void PrintHelp(Interface @interface)
    {
        var defaultColor = Console.ForegroundColor;

        if (@interface.Description is not null)
        {
            Console.Out.WriteLine(@interface.Description);
            Console.Out.WriteLine("");
        }

        List<Option> options = @interface.Options;
        List<Argument> arguments = @interface.Arguments;

        PrintUsage(@interface);

        if (arguments.Count > 0)
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Out.Write("Arguments");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.WriteLine(":");

            for (int i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                argument.PrintUsage();

                if (i < arguments.Count - 1)
                    Console.Out.WriteLine();
            }
        }

        if (options.Count > 0)
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Out.Write("Options");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.WriteLine(":");

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                option.PrintUsage();

                if (i < options.Count - 1)
                    Console.Out.WriteLine();
            }
        }

        Console.ForegroundColor = defaultColor;
    }

    private static void PrintUsage(Interface @interface)
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.Out.Write("Usage");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Out.Write(": ");
        Console.Out.Write(@interface.ProgramName);

        if (@interface.Options.Count > 0)
        {
            Console.Out.Write(" {");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.Write("options");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.Write("}");
        }

        List<Argument> arguments = @interface.Arguments;

        if (arguments.Count > 0)
        {
            foreach (Argument argument in arguments)
            {
                Console.Out.Write(" ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                
                if (argument is { IsRest: true, RestSeparator: not null })
                    Console.Out.Write("-- ");
                
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Out.Write("<");

                if (argument is { IsRest: true, RestSeparator: null })
                    Console.Out.Write("..");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Out.Write(argument.Name);
                Console.ForegroundColor = ConsoleColor.Gray;

                if (argument.Optional)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Out.Write("?");
                }

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Out.Write(">");
            }
        }
    }
}
