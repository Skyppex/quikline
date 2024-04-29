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

        var (commandArgs, commandType) = GetCommand(passedArgs);
        ValidateRelations(commandArgs, commandType);

        if (passedArgs.Subcommand is not null)
        {
            var instance = Activator.CreateInstance(type)!;
            var subcommandType = passedArgs.Subcommand.CommandType;

            var subcommandInterface =
                @interface.Subcommands.Single(s => s.CommandName == subcommandType.Name);

            var subcommandUserArgs = CreateSubcommandUserArgs(
                passedArgs.Subcommand,
                subcommandInterface,
                subcommandType);

            SetValueOnInstance(type, subcommandType, instance, subcommandUserArgs);

            if (!MissingRequired(
                @interface,
                passedArgs.Subcommand,
                out var subcommandMissing))
                return instance;

            Console.Error.WriteLine(
                $"Incorrect usage. Missing required options: {string.Join(",", subcommandMissing)}");

            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
            return default;
        }

        if (!MissingRequired(@interface, passedArgs, out var missing))
            return CreateUserArgs(passedArgs.CommandType, passedArgs);

        Console.Error.WriteLine(
            $"Incorrect usage. Missing required options: {string.Join(",", missing)}");

        Console.Error.Write("Use --help for more information.");
        Environment.Exit(1);
        return default;
    }

    private static (Args Args, Type CommandType) GetCommand(Args args)
    {
        if (args.Subcommand is null)
            return (args, args.CommandType);
        
        return GetCommand(args.Subcommand);
    }

    private static void ValidateRelations(Args passedArgs, Type type)
    {
        var relations = type.GetRelations();

        foreach (var relation in relations)
        {
            switch (relation)
            {
                case ExclusiveRelationAttribute exclusive:
                    ValidateExclusiveRelation(exclusive, passedArgs);
                    break;

                case InclusiveRelationAttribute inclusive:
                    ValidateInclusiveRelation(inclusive, passedArgs);
                    break;

                case OneOrMoreRelationAttribute oneOrMore:
                    ValidateOneOrMoreRelation(oneOrMore, passedArgs);
                    break;
                
                case OneWayRelationAttribute oneWay:
                    ValidateOneWayRelation(oneWay, passedArgs);
                    break;
            }
        }
    }

    private static void ValidateExclusiveRelation(
        ExclusiveRelationAttribute exclusive,
        Args passedArgs)
    {
        var relevant = passedArgs.Options
            .Where(o => exclusive.Args.Contains(o.FieldName))
            .ToList();

        var passed = relevant.Count(r => r.Passed);

        if (exclusive.Required ? passed == 1 : passed <= 1)
            return;

        if (exclusive.Required && passed == 0)
        {
            Console.Error.WriteLine(
                $"Incorrect usage. One of {string.Join(", ",
                    relevant.Select(r => r.Long.ToString()))} must be present.");

            Console.Error.Write("Use --help for more information.");

            Environment.Exit(1);
        }

        Console.Error.WriteLine(
            $"Incorrect usage. Args {string.Join(", ",
                relevant.Select(r => r.Long.ToString()))} are mutually exclusive.");

        Console.Error.Write("Use --help for more information.");

        Environment.Exit(1);
    }

    private static void ValidateInclusiveRelation(
        InclusiveRelationAttribute inclusive,
        Args passedArgs)
    {
        var relevant = passedArgs.Options
            .Where(o => inclusive.Args.Contains(o.FieldName))
            .ToList();

        var passed = relevant.Count(r => r.Passed);

        if (inclusive.Required
            ? passed == inclusive.Args.Length
            : passed == 0 || passed == inclusive.Args.Length)
            return;

        if (inclusive.Required && passed == 0)
        {
            Console.Error.WriteLine(
                $"Incorrect usage. All of {string.Join(", ",
                    relevant.Select(r => r.Long.ToString()))} must be present.");

            Console.Error.Write("Use --help for more information.");

            Environment.Exit(1);
        }

        Console.Error.WriteLine(
            $"Incorrect usage. Args: {string.Join(", ",
                relevant.Select(r => r.Long.ToString()))} are mutually inclusive.");

        Console.Error.Write("Use --help for more information.");

        Environment.Exit(1);
    }

    private static void ValidateOneOrMoreRelation(
        OneOrMoreRelationAttribute oneOrMore,
        Args passedArgs)
    {
        var relevant = passedArgs.Options
            .Where(o => oneOrMore.Args.Contains(o.FieldName))
            .ToList();

        if (relevant.Count(r => r.Passed) >= 1)
            return;

        Console.Error.WriteLine(
            $"Incorrect usage. At least one of {string.Join(", ",
                relevant.Select(r => "--" + r.FieldName.SplitPascalCase().ToKebabCase()))} must be present.");

        Console.Error.Write("Use --help for more information.");

        Environment.Exit(1);
    }

    private static void ValidateOneWayRelation(
        OneWayRelationAttribute oneWay,
        Args passedArgs)
    {
        var fromOption = passedArgs.Options
            .First(o => o.FieldName == oneWay.From);
        
        var toOption = passedArgs.Options
            .First(o => o.FieldName == oneWay.To);

        if (toOption.Passed || !fromOption.Passed)
            return;

        Console.Error.WriteLine(
            $"Incorrect usage. {fromOption.Long} must be passed with {toOption.Long}.");
        Console.Error.Write("Use --help for more information.");
            
        Environment.Exit(1);
    }

    private static bool SetValueOnInstance(
        Type type,
        Type subcommandType,
        object instance,
        object subcommandUserArgs)
    {
        var fields = type.GetFields();

        foreach (var field in fields)
        {
            if (field.FieldType.GetCustomAttribute(typeof(ArgsAttribute)) is not null)
            {
                var subInstance = Activator.CreateInstance(field.FieldType)!;

                if (SetValueOnInstance(
                    field.FieldType,
                    subcommandType,
                    subInstance,
                    subcommandUserArgs))
                {
                    field.SetValue(instance, subInstance);
                    return true;
                }

                continue;
            }

            if (field.FieldType.GetUnderlyingType() != subcommandType)
                continue;

            field.SetValue(instance, subcommandUserArgs);
            return true;
        }

        return false;
    }

    private static object CreateSubcommandUserArgs(
        Args passedArgs,
        Interface @interface,
        Type type)
    {
        var args = CreateArgs(passedArgs, @interface, type);
        return args;
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
            .Where(
                o => o.Required && args.Options
                    .Where(o => !o.Passed)
                    .Contains(o, new ShortOptionEqualityComparer()))
            .Select(o => o.Long.Name.ToString())
            .Concat(
                @interface.Arguments
                    .Where(
                        a => !a.Optional && args.Arguments
                            .Where(a => !a.Passed)
                            .Contains(a, new ArgumentNameEqualityComparer()))
                    .Select(a => a.Name))
            .ToArray();

        return missing.Length > 0;
    }
}
