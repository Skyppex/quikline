using System.Diagnostics;
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
            var result = ValidateRelation(passedArgs, relation, relations, out _);

            if (result is null)
                continue;

            Console.Error.WriteLine("Incorrect usage. ");
            Console.Error.WriteLine(result);
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }
    }

    private static string? ValidateRelation(
        Args passedArgs,
        RelationAttribute relation,
        List<RelationAttribute> relations,
        out bool relationPassed)
    {
        if (relation is ExclusiveRelationAttribute exclusive)
            return ValidateExclusiveRelation(exclusive, passedArgs, relations, out relationPassed);
        
        if (relation is InclusiveRelationAttribute inclusive)
            return ValidateInclusiveRelation(inclusive, passedArgs, relations, out relationPassed);
        
        if (relation is OneOrMoreRelationAttribute oneOrMore)
            return ValidateOneOrMoreRelation(oneOrMore, passedArgs, relations, out relationPassed);
        
        if (relation is OneWayRelationAttribute oneWay)
            return ValidateOneWayRelation(oneWay, passedArgs, relations, out relationPassed);

        throw new UnreachableException("Unknown relation type.");
    }

    private static string? ValidateExclusiveRelation(
        ExclusiveRelationAttribute exclusive,
        Args passedArgs,
        List<RelationAttribute> relations,
        out bool relationPassed)
    {
        var relevantOptions = passedArgs.Options
            .Where(o => exclusive.Args.Contains(o.FieldName))
            .ToList();

        var relevantRelations = relations
            .Where(r => exclusive.Args.Contains(r.Name))
            .ToList();

        var passedRelations = relevantRelations.Select(
            r =>
            {
                var result = CheckRelation(
                    passedArgs,
                    relations,
                    exclusive.Name,
                    r.Name,
                    false,
                    out bool relationPassed);

                var name = r.Name;
                return (name, relationPassed, result);
            })
            .ToList();

        var passed = relevantOptions.Count(r => r.Passed) +
            passedRelations.Count(r => r.relationPassed);

        if (exclusive.Required ? passed == 1 : passed <= 1)
        {
            relationPassed = passed == 1;
            return null;
        }

        relationPassed = false;

        var names = relevantOptions.Select(r => r.Long.ToString())
            .Concat(passedRelations.Select(r => $"\"{r.name}\""));

        if (exclusive.Required && passed == 0)
            return $"One of {string.Join(", ", names)} must be present.";

        var errors = passedRelations
            .Where(r => r.result is not null)
            .Select(r => r.result)
            .ToList();
        
        return $"{string.Join(", ", names)} are mutually exclusive." +
               (errors.Count != 0 ? "\n" + string.Join("\n", errors) : "");
    }

    private static string? ValidateInclusiveRelation(
        InclusiveRelationAttribute inclusive,
        Args passedArgs,
        List<RelationAttribute> relations,
        out bool relationPassed)
    {
        var relevantOptions = passedArgs.Options
            .Where(o => inclusive.Args.Contains(o.FieldName))
            .ToList();
        
        var relevantRelations = relations
            .Where(r => inclusive.Args.Contains(r.Name))
            .ToList();

        var passedRelations = relevantRelations.Select(
                r =>
                {
                    var result = CheckRelation(
                        passedArgs,
                        relations,
                        inclusive.Name,
                        r.Name,
                        false,
                        out bool relationPassed);

                    var name = r.Name;
                    return (name, relationPassed, result);
                })
            .ToList();
        
        var passed = relevantOptions.Count(r => r.Passed) +
            passedRelations.Count(r => r.relationPassed);

        if (inclusive.Required
            ? passed == inclusive.Args.Length
            : passed == 0 || passed == inclusive.Args.Length)
        {
            relationPassed = passed == inclusive.Args.Length;
            return null;
        }
        
        relationPassed = false;

        var names = relevantOptions.Select(r => r.Long.ToString())
            .Concat(passedRelations.Select(r => $"\"{r.name}\""));

        if (inclusive.Required && passed == 0)
            return $"All of {string.Join(", ", names)} must be present.";

        var errors = passedRelations
            .Where(r => r.result is not null)
            .Select(r => r.result)
            .ToList();
        
        return $"{string.Join(", ", names)} are mutually inclusive." +
               (errors.Count != 0 ? "\n" + string.Join("\n", errors) : "");
    }

    private static string? ValidateOneOrMoreRelation(
        OneOrMoreRelationAttribute oneOrMore,
        Args passedArgs,
        List<RelationAttribute> relations,
        out bool relationPassed)
    {
        var relevantOptions = passedArgs.Options
            .Where(o => oneOrMore.Args.Contains(o.FieldName))
            .ToList();
        
        var relevantRelations = relations
            .Where(r => oneOrMore.Args.Contains(r.Name))
            .ToList();

        var passedRelations = relevantRelations.Select(
                r =>
                {
                    var result = CheckRelation(
                        passedArgs,
                        relations,
                        oneOrMore.Name,
                        r.Name,
                        false,
                        out bool relationPassed);

                    var name = r.Name;
                    return (name, relationPassed, result);
                })
            .ToList();
        
        var passed = relevantOptions.Count(r => r.Passed) +
            passedRelations.Count(r => r.relationPassed);

        if (!oneOrMore.Required || passed > 0)
        {
            relationPassed = passed > 0;
            return null;
        }
        
        relationPassed = false;

        var names = relevantOptions.Select(r => r.Long.ToString())
            .Concat(passedRelations.Select(r => $"\"{r.name}\""));

        var errors = passedRelations
            .Where(r => r.result is not null)
            .Select(r => r.result)
            .ToList();

        return $"At least one of {string.Join(", ", names)} must be present." +
            (errors.Count != 0 ? "\n" + string.Join("\n", errors) : "");
    }

    private static string? ValidateOneWayRelation(
        OneWayRelationAttribute oneWay,
        Args passedArgs,
        List<RelationAttribute> relations,
        out bool relationPassed)
    {
        var fromOption = passedArgs.Options
            .FirstOrDefault(o => o.FieldName == oneWay.From);
        
        var fromPassed = fromOption.Passed;
        
        if (fromOption == default)
        {
            var result = CheckRelation(passedArgs, relations, oneWay.Name, oneWay.From, false, out relationPassed);

            if (result is not null || !relationPassed)
                return result;

            fromPassed = true;
        }

        if (!fromPassed)
        {
            relationPassed = false;
            return null;
        }

        var toOption = passedArgs.Options
            .FirstOrDefault(o => o.FieldName == oneWay.To);

        var toPassed = toOption.Passed;
        
        if (toOption == default)
        {
            var result = CheckRelation(passedArgs, relations, oneWay.Name, oneWay.To, true, out relationPassed);

            if (result is not null || !relationPassed)
                return result;

            toPassed = true;
        }

        if (toPassed)
        {
            relationPassed = true;
            return null;
        }

        relationPassed = false;
        return $"{fromOption.Long} must be passed with {toOption.Long}.";
    }

    private static string? CheckRelation(
        Args passedArgs,
        List<RelationAttribute> relations,
        string relationName,
        string relatedRelationName,
        bool required,
        out bool relationPassed)
    {
        var relation = relations.FirstOrDefault(r => r.Name == relatedRelationName);

        if (relation is null)
            throw new InvalidOperationException(
                $"Incorrect setup. No option or relation with name: {relatedRelationName}");

        if (required)
            relation._required = true;
        
        var result = ValidateRelation(passedArgs, relation, relations, out relationPassed);

        return result is null ? null
            : $"Relation \"{relationName}\" requires \"{relatedRelationName}\":\n{result}";
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
