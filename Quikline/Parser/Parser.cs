using System.Reflection;
using Quikline.Attributes;

namespace Quikline.Parser;

public static class Quik
{
    public static T Parse<T>(string[] args) where T : struct
    {
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
        
        if (passedArgs.Options.Any(
                o => o.Matches($"{@interface.LongPrefix}version")))
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
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField)!;
            
            if (option.Type == typeof(bool))
            {
                field.SetValue(value, true);
                continue;
            }
            
            field.SetValue(value, option.Value);
        }
        
        return (T)value;
    }

    private static Interface ParseInterface(Type type)
    {
        IEnumerable<Attribute> attributes = type.GetCustomAttributes();

        if (attributes.SingleOrDefault(a => a is CommandAttribute) is not CommandAttribute commandAttr)
            throw new InvalidOperationException("The provided type does not have a Command attribute.");

        var @interface = new Interface(commandAttr);
        
        // Add the options and arguments from the fields.
        FieldInfo[] fields = type.GetFields();
        
        foreach (FieldInfo field in fields)
        {
            if (field.GetCustomAttributes().SingleOrDefault(a => a is OptionAttribute) is not OptionAttribute optionAttr)
                continue;
            
            Short? @short = optionAttr.Short is '\0' ? null
                : new Short(optionAttr.ShortPrefix.ToPrefix() ?? @interface.ShortPrefix,
                    new Name(optionAttr.Short.ToString().OrIfEmpty(field.Name.First().ToString())));

            Long @long = optionAttr.Long is null ?
                new Long(@interface.LongPrefix,
                    new Name(field.Name.SplitPascalCase().ToKebabCase())) :
                new Long(optionAttr.LongPrefix.ToPrefix() ?? @interface.LongPrefix,
                    new Name(optionAttr.Long ?? field.Name.SplitPascalCase().ToKebabCase()));

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
                    option = option with { Value = Enum.GetValues(field.FieldType).GetValue(0) };
                else
                    option = option with { Value = Enum.Parse(field.FieldType, optionAttr.Default.ToString()!, ignoreCase: true) };
            }
            
            @interface.AddOption(option);
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
                (true, false) =>  new Short(@interface.ShortPrefix, new Name("V")),
                _ => null
            };
            
            @interface.Options.Insert(0, new Option("", false, shortVersion, new Long(@interface.LongPrefix, new Name("version")), typeof(bool), null, "Print the version"));
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
            (true, false) =>  new Short(@interface.ShortPrefix, new Name("H")),
            _ => null
        };

        @interface.Options.Insert(0, new Option("", false, shortHelp, new Long(@interface.LongPrefix, new Name("help")), typeof(bool), null, "Print this help message"));

        return @interface;
    }

    private static Args ParseArgs(string[] args, Interface @interface)
    {
        var parsed = new Args();

        IEnumerator<string> iterator = args.ToList().GetEnumerator();

        while (true)
        {
            if (!iterator.MoveNext())
                break;

            string arg = iterator.Current;

            if (arg.StartsWith(@interface.ShortPrefix) && arg[@interface.ShortPrefix.Length].ToString() != @interface.ShortPrefix)
            {
                string shortArgs = arg[@interface.ShortPrefix.Length..];

                foreach (char shortArg in shortArgs)
                    ParseArg($"{@interface.ShortPrefix}{shortArg}", @interface, parsed, iterator);
                
                continue;
            }
            
            ParseArg(arg, @interface, parsed, iterator);
        }

        return parsed;
    }

    private static void ParseArg(
        string arg,
        Interface @interface,
        Args parsed,
        IEnumerator<string> iterator)
    {
        if (!@interface.TryGetOption(arg, out Option option))
        {
            Console.Error.WriteLine($"Incorrect usage. Unknown option: {arg}");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        if (option.Type == typeof(bool))
        {
            parsed.AddOption(option);
            return;
        }
                
        if (!iterator.MoveNext())
        {
            Console.Error.WriteLine($"Incorrect usage. Expected a value for option {arg}.");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }
                
        string value = iterator.Current;
        
        if (option.Type == typeof(int))
        {
            if (!int.TryParse(value, out int intValue))
            {
                Console.Error.WriteLine($"Incorrect usage. Expected an integer value for option {arg}.");
                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }
                    
            option = option with { Value = intValue };
        }
        else if (option.Type == typeof(float))
        {
            if (!float.TryParse(value, out float floatValue))
            {
                Console.Error.WriteLine($"Incorrect usage. Expected a float value for option {arg}.");
                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }
                    
            option = option with { Value = floatValue };
        }
        else if (option.Type == typeof(double))
        {
            if (!double.TryParse(value, out double doubleValue))
            {
                Console.Error.WriteLine($"Incorrect usage. Expected a double value for option {arg}.");
                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }
                    
            option = option with { Value = doubleValue };
        }
        else if (option.Type == typeof(char))
        {
            if (!char.TryParse(value, out char charValue))
            {
                Console.Error.WriteLine($"Incorrect usage. Expected a char value for option {arg}.");
                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }
                    
            option = option with { Value = charValue };
        }
        else if (option.Type == typeof(string))
        {
            option = option with { Value = value };
        }
        else if (option.Type.IsEnum)
        {
            if (!Enum.TryParse(option.Type, value, ignoreCase: true, out object? enumValue))
            {
                Console.Error.WriteLine($"Incorrect usage. Expected an enum value for option {arg}.");
                Console.Error.Write("Use --help for more information.");
                Environment.Exit(1);
            }

            option = option with { Value = enumValue };
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
        if (@interface.Description is not null)
        {
            Console.Out.WriteLine(@interface.Description);
            Console.Out.WriteLine("");
        }

        ICollection<Option> options = @interface.Options;
        ICollection<Argument> arguments = @interface.Arguments;
        
        string argumentString = string.Join(" ", arguments.Select(a => a.Name));
        
        Console.Out.Write($"Usage: {@interface.ProgramName} {{options}} {argumentString}");

        if (options.Count > 0)
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("");
            Console.Out.WriteLine("Options:");
            
            foreach (Option option in options)
                Console.Out.WriteLine($"{option}");
        }
    }
}