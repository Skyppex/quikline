﻿using System.Reflection;
using Quikline.Attributes;

namespace Quikline.Parser;

internal static class InterfaceParser
{
    public static Interface Parse(Type type)
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

            Validation.ValidateOption(optionAttr, field.Name);
            
            var argumentAttr = (ArgumentAttribute?)field.GetCustomAttributes()
                .SingleOrDefault(a => a is ArgumentAttribute);

            Validation.ValidateArgument(argumentAttr, field.Name);
            
            var restAttr = (RestAttribute?)field.GetCustomAttributes()
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
        string name = argumentAttr.Name ?? field.Name.SplitPascalCase().ToKebabCase();

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

}