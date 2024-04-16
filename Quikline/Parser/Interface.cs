﻿using Quikline.Attributes;

namespace Quikline.Parser;

internal struct Interface(CommandAttribute commandAttribute)
{
    public readonly string ProgramName =
        Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);

    public readonly string? Description = commandAttribute.Description;
    public readonly Prefix ShortPrefix = new(commandAttribute.ShortPrefix);
    public readonly Prefix LongPrefix = new(commandAttribute.LongPrefix);
    public List<Option> Options { get; set; } = [];
    public List<Argument> Arguments { get; set; } = [];

    public void AddOption(Option option) => Options.Add(option);
    public void AddArgument(Argument argument) => Arguments.Add(argument);

    public bool TryGetOption(string arg, out Option option)
    {
        option = Options.FirstOrDefault(
            o => (arg.StartsWith(o.Long.Prefix) &&
                  arg[o.Long.Prefix.Length..].Equals(o.Long.Name)) ||
                 (o.Short != null && (arg.StartsWith(o.Short.Value.Prefix) &&
                                      arg[o.Short.Value.Prefix.Length..]
                                          .Equals(o.Short.Value.Name))));

        return option != default;
    }
}

internal readonly record struct Option(
    string FieldName,
    bool Required,
    Short? Short,
    Long Long,
    Type Type,
    object? Value,
    string? Description)
{
    public static Option ShortOnly(Short @short) => new Option(
        "",
        false,
        @short,
        Long.Empty,
        typeof(bool),
        false,
        null);

    public bool Matches(string argName)
    {
        if (Short != null)
            return argName == $"{Short.Value.Prefix}{Short.Value.Name}" ||
                   argName == $"{Long.Prefix}{Long.Name}";

        return argName == $"{Long.Prefix}{Long.Name}";
    }

    public void PrintUsage()
    {
        Console.Out.Write("  ");
        Console.ForegroundColor = ConsoleColor.DarkCyan;

        if (Short != null)
        {
            Console.Out.Write(Short);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.Write(", ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.Write(Long);
        }
        else
        {
            Console.Out.Write(Long);
        }

        if (Type != typeof(bool))
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.Write(" <");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Type.PrintUsageName();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.Write(">");
        }

        if (Description != null)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.Write(" - ");
            Console.Out.Write(Description);
        }
    }
}

internal readonly record struct Argument(
    string FieldName,
    bool Optional,
    string Name,
    Type Type,
    object? Value,
    string? Description,
    bool IsRest = false)
{
    public void PrintUsage()
    {
        Console.Out.Write("  ");
        Console.ForegroundColor = ConsoleColor.Gray;

        if (IsRest)
            Console.Out.Write("...");
        
        Console.ForegroundColor = ConsoleColor.DarkCyan;

        Console.Out.Write(Name);

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Out.Write(" <");

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Type.PrintUsageName();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Out.Write(">");

        if (Description != null || Optional)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.Write(" -");

            if (Description != null)
                Console.Out.Write($" {Description}");

            if (Optional)
            {
                Console.Out.Write(" (");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Out.Write("optional");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Out.Write(")");
            }
        }
    }
}

internal sealed class ArgumentComparer : IComparer<Argument>
{
    public int Compare(Argument x, Argument y)
    {
        return (x.Optional, y.Optional, x.IsRest, y.IsRest) switch
        {
            (false, true, false, false) => -1,
            (true, false, false, false) => 1,
            (_, _, true, false) => 1,
            (_, _, false, true) => -1,
            _ => 0,
        };
    }
}
