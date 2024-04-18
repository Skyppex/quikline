using System.Reflection;

using Quikline.Attributes;
using Quikline.Parser.Models;

namespace Quikline.Parser;

public static class Quik
{
    public static T Parse<T>()
        where T : struct
    {
        var args = Environment.GetCommandLineArgs()[1..];

        var type = typeof(T);
        var @interface = InterfaceParser.Parse(type, null);

        IEnumerator<string> argIterator = args.ToList().GetEnumerator();
        var passedArgs = ArgsParser.Parse(argIterator, @interface);

        return (T)CreateArgs(passedArgs, @interface, type);
    }

    private static object CreateArgs(
        Args passedArgs,
        Interface @interface,
        Type type)
    {
        if (passedArgs.Options.Where(o => o.Passed)
            .Any(
                o =>
                {
                    var interfaceLongPrefix = @interface.LongPrefix;
                    return o.Matches($"{interfaceLongPrefix}help");
                }))
        {
            Help.Print(@interface);
            Environment.Exit(0);
        }

        if (passedArgs.Options.Where(o => o.Passed)
            .Any(o => o.Matches($"{@interface.LongPrefix}version")))
        {
            var version = type.Assembly.GetName().Version;
            Console.Out.Write(version);
            Environment.Exit(0);
        }

        if (passedArgs.Subcommand is not null)
        {
            var subcommand = passedArgs.Subcommand;
            var subcommandType = subcommand.Type;
            var subcommandUseArgs = CreateArgs(subcommand, @interface.Subcommands.Single(sc => sc.CommandName == subcommandType.Name), subcommandType);
            var value = Activator.CreateInstance(type)!;
            var subcommandField = type.GetFields().Single(fi => fi.FieldType == subcommandType);
            subcommandField.SetValue(value, subcommandUseArgs);
            return value;
        }

        if (MissingRequired(@interface, passedArgs, out var missing))
        {
            Console.Error.WriteLine($"Incorrect usage. Missing required options: {missing}");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        return CreateUserArgs(passedArgs.Type, passedArgs);
    }

    private static object CreateUserArgs(Type type, Args passedArgs)
    {
        var instance = Activator.CreateInstance(type)!;
        var fields = type.GetFields();

        foreach (var field in fields)
        {
            var optionAttr = field.GetCustomAttribute<OptionAttribute>();

            if (optionAttr is not null)
            {
                SetOption(field, passedArgs, instance);
                continue;
            }

            var argumentAttr = field.GetCustomAttribute<ArgumentAttribute>();
            var restAttr = field.GetCustomAttribute<RestAttribute>();

            if (argumentAttr is not null || restAttr is not null)
            {
                SetArgument(field, passedArgs, instance);
                continue;
            }

            if (field.FieldType.GetCustomAttribute<ArgsAttribute>() is not null)
            {
                var nestedInstance = CreateUserArgs(field.FieldType, passedArgs);
                field.SetValue(instance, nestedInstance);
            }
        }

        return instance;
    }

    private static void SetOption(FieldInfo field, Args passedArgs, object instance)
    {
        var option = passedArgs.Options.First(o => o.FieldName == field.Name);

        switch (option.Passed, option.Type == typeof(bool))
        {
            case (false, true):
                field.SetValue(instance, false);
                break;

            case (true, true):
                field.SetValue(instance, true);
                break;

            default:
                field.SetValue(instance, option.Value);
                break;
        }
    }

    private static void SetArgument(FieldInfo field, Args passedArgs, object instance)
    {
        var argument = passedArgs.Arguments.First(o => o.FieldName == field.Name);

        switch (argument.Passed, argument.Type == typeof(bool))
        {
            case (false, true):
                field.SetValue(instance, false);
                break;

            case (true, true):
                field.SetValue(instance, true);
                break;

            default:
                field.SetValue(instance, argument.Value);
                break;
        }
    }

    private static bool MissingRequired(Interface @interface, Args args, out string[] missing)
    {
        missing = @interface.Options
            .Where(o => o.Required && args.Options
                .Where(o => !o.Passed)
                .Contains(o, new ShortOptionEqualityComparer()))
            .Select(o => o.Long.Name.ToString())
            .ToArray();

        return missing.Length > 0;
    }
}
