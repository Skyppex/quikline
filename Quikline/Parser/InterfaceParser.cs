﻿using System.Reflection;
using Quikline.Attributes;
using Quikline.Parser.Models;

namespace Quikline.Parser;

internal static class InterfaceParser
{
    public static Interface Parse(Type type, Interface? parent)
    {
        var underlyingType = type.GetUnderlyingType();
        var attributes = underlyingType.GetCustomAttributes().ToList();

        var commandAttr = (CommandAttribute?)attributes.SingleOrDefault(a => a is CommandAttribute);
        var subcommandAttr = (SubcommandAttribute?)attributes.SingleOrDefault(a => a is SubcommandAttribute);
        var argsAttr = (ArgsAttribute?)attributes.SingleOrDefault(a => a is ArgsAttribute);
        var nameAttr = (NameAttribute?)attributes.SingleOrDefault(a => a is NameAttribute);

        if (commandAttr is not null && subcommandAttr is not null)
            throw new InvalidProgramException(
                "Incorrect setup. Type cannot have both Command and Subcommand attributes.");

        if (subcommandAttr is not null)
            commandAttr = subcommandAttr.IntoCommand();

        if (commandAttr is null && argsAttr is null)
            throw new InvalidProgramException(
                "Incorrect setup. Type must have either Command or Args attribute.");
        
        if (commandAttr is not null && argsAttr is not null)
            throw new InvalidProgramException(
                "Incorrect setup. Type cannot have both Command and Args attributes.");

        if (argsAttr is not null)
        {
            if (type != underlyingType)
                throw new InvalidProgramException(
                    $"Incorrect setup. Field of Type: {underlyingType.Name} with Args attribute cannot be nullable.");
            
            commandAttr = new CommandAttribute
            {
                Description = null,
                ShortPrefix = argsAttr.ShortPrefix,
                LongPrefix = argsAttr.LongPrefix,
                Version = false,
            };
        }
        
        var @interface = new Interface(
            commandAttr!,
            underlyingType,
            subcommandAttr is null
                ? null
                : nameAttr?.Name ?? underlyingType.Name,
            parent);

        // Add the options and arguments from the fields.
        var fields = underlyingType.GetFields();

        foreach (var field in fields)
        {
            var fieldAttributes = field.GetCustomAttributes().ToList();

            var fieldTypeAttributes =
                field.FieldType.GetUnderlyingType().GetCustomAttributes().ToList();

            ValidateSupportedType(field.FieldType, fieldTypeAttributes, field.DeclaringType, field.Name);

            if (fieldTypeAttributes.SingleOrDefault(a => a is ArgsAttribute) is not null)
            {
                @interface.Merge(Parse(field.FieldType, null));
                continue;
            }

            if (fieldTypeAttributes.SingleOrDefault(a => a is SubcommandAttribute) is not null)
            {
                if (Nullable.GetUnderlyingType(field.FieldType) is null)
                    throw new InvalidProgramException(
                        $"Incorrect setup. Field {field.Name} is a Subcommand. Fields for Subcommands must be nullable.");
                
                @interface.AddSubcommand(Parse(field.FieldType, @interface));
                continue;
            }

            var optionAttr = (OptionAttribute?)fieldAttributes
                .SingleOrDefault(a => a is OptionAttribute);

            Validation.ValidateOption(optionAttr, field.Name);
            
            var argumentAttr = (ArgumentAttribute?)fieldAttributes
                .SingleOrDefault(a => a is ArgumentAttribute);

            Validation.ValidateArgument(argumentAttr, field.Name);
            
            var restAttr = (RestAttribute?)fieldAttributes
                .SingleOrDefault(a => a is RestAttribute);

            Validation.ValidateRest(restAttr, field.Name);
            
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

        var distinctShortNames = @interface.Options.Distinct(new ShortOptionEqualityComparer()).ToList();

        if (distinctShortNames.Count <
            @interface.Options.Count)
        {
            var duplicates = @interface.Options.GroupBy(o => o.Short)
                .Where(g => g.Count() > 1)
                .Select(g => $"{string.Join(" & ", g.Select(o => o.FieldName))} for {g.Key}");
            
            throw new InvalidProgramException("Incorrect setup. Duplicate short names options found:\n" +
                                              string.Join("\n", duplicates));
        }

        // Add generated options.
        if (commandAttr!.Version)
        {
            var isUsingLowerCaseV = @interface.Options.Contains(
                Option.ShortOnly(new Short(@interface.ShortPrefix, new Name("v"))),
                new ShortOptionEqualityComparer());

            var isUsingUpperCaseV = @interface.Options.Contains(
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
                    false,
                    0,
                    "",
                    false,
                    shortVersion,
                    new Long(@interface.LongPrefix, new Name("version")),
                    typeof(bool),
                    null,
                    null,
                    "Print the version"));
        }

        if (argsAttr is null)
        {
            var isUsingLowerCaseH = @interface.Options.Contains(
                Option.ShortOnly(new Short(@interface.ShortPrefix, new Name("h"))),
                new ShortOptionEqualityComparer());

            var isUsingUpperCaseH = @interface.Options.Contains(
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
                    false,
                    0,
                    "",
                    false,
                    shortHelp,
                    new Long(@interface.LongPrefix, new Name("help")),
                    typeof(bool),
                    null,
                    null,
                    "Print this help message"));
        }

        if (@interface.Arguments.Count(a => a is { IsRest: true, RestSeparator: null}) > 1)
            throw new InvalidProgramException(
                "Incorrect setup. Only one field can be a Rest field without a separator.\n" +
                "The other Rest fields must have a separator defined.");
        
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

        var @long = (optionAttr.LongPrefix, optionAttr.Long) switch
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

        var multiFlagAttr = field.GetCustomAttribute<MultiFlagAttribute>();

        var option = new Option(
            false,
            multiFlagAttr is null
                ? 0
                : (multiFlagAttr is { Max: 0 }
                    ? -1
                    : (int)multiFlagAttr.Max),
            field.Name,
            optionAttr.Required,
            @short,
            @long,
            field.FieldType,
            field,
            optionAttr.Default,
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
            false,
            field.Name,
            field.IsNullable() || argumentAttr.Default is not null,
            name,
            field.FieldType,
            field,
            argumentAttr.Default,
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
                $"Incorrect setup. Field {field.Name} with Rest attribute must be of type string (cannot be nullable).");

        var argument = new Argument(
            false,
            field.Name,
            true,
            restAttr.Name ?? field.Name.SplitPascalCase().ToKebabCase(),
            field.FieldType,
            field,
            null,
            restAttr.Description,
            IsRest: true,
            RestSeparator: restAttr.Separator);

        @interface.AddArgument(argument);
    }

    private static void ValidateSupportedType(Type fieldType, List<Attribute> fieldAttributes, Type? fieldDeclaringType, string fieldName)
    {
        List<Type> supportedTypes =
        [
            typeof(bool),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(char),
            typeof(string)
        ];

        if (supportedTypes.Contains(fieldType))
            return;

        if (Nullable.GetUnderlyingType(fieldType) is not null)
        {
            var underlyingType = Nullable.GetUnderlyingType(fieldType)!;
            ValidateSupportedType(underlyingType, fieldAttributes, fieldDeclaringType, fieldName);
            return;
        }
        
        if (fieldType.IsEnum)
            return;
        
        if (fieldType.IsArray)
        {
            var elementType = fieldType.GetElementType();
            ValidateSupportedType(elementType!, fieldAttributes, fieldDeclaringType, fieldName);
            return;
        }

        if (fieldAttributes.SingleOrDefault(a => a is ArgsAttribute) is not null)
            return;
        
        if (fieldAttributes.SingleOrDefault(a => a is SubcommandAttribute) is not null)
            return;

        // Do this last to avoid unnecessary throws due to
        // structs which use above attributes.
        try
        {
            var concretizedFromString = typeof(IFromString<>).MakeGenericType(fieldType);
            
            if (fieldType.IsValueType && fieldType.IsAssignableTo(concretizedFromString))
                return;
        }
        catch
        {
            // Ignored.
            // MakeGenericType will throw an exception if
            // the fieldType is and invalid generic type for IFromString.
            // We don't care about the exception, just that it's not a valid type.
        }

        throw new InvalidProgramException(
            "Incorrect setup. Unsupported type found: " +
            $"{fieldType.Name} for field {(fieldDeclaringType is null ? "" : fieldDeclaringType + ".")}{fieldName}.");
    }
}
