using Quikline.Attributes;

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
    
    public void Merge(Interface @interface)
    {
        Options.AddRange(@interface.Options);
        Arguments.AddRange(@interface.Arguments);
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
        using (new Help.Color(ConsoleColor.DarkCyan))
        {
            if (Short != null)
            {
                Console.Out.Write(Short);
                using (new Help.Color(ConsoleColor.Gray))
                    Console.Out.Write(", ");
                Console.Out.Write(Long);
            }
            else
                Console.Out.Write(Long);

            if (Type != typeof(bool))
            {
                using (new Help.Color(ConsoleColor.Gray))
                {
                    Console.Out.Write(" <");
                    using (new Help.Color(ConsoleColor.Blue))
                        Type.PrintUsageName();
                    Console.Out.Write(">");
                }
            }

            if (Description != null)
            {
                using (new Help.Color(ConsoleColor.Gray))
                {
                    Console.Out.Write(" - ");
                    Console.Out.Write(Description);
                }
            }
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
    bool IsRest = false,
    string? RestSeparator = null)
{
    public void PrintUsage()
    {
        Console.Out.Write("  ");

        if (IsRest)
        {
            if (RestSeparator is not null)
                using (new Help.Color(ConsoleColor.DarkYellow))
                    Console.Out.Write($"{RestSeparator} ");
            else
                Console.Out.Write("..");
        }
        
        using (new Help.Color(ConsoleColor.DarkCyan))
            Console.Out.Write(Name);

        using (new Help.Color(ConsoleColor.Gray))
        {
            Console.Out.Write(" <");
            using (new Help.Color(ConsoleColor.Blue))
                Type.PrintUsageName();
            Console.Out.Write(">");

            if (Description != null || Optional)
            {
                Console.Out.Write(" -");

                if (Description != null)
                    Console.Out.Write($" {Description}");

                if (Optional)
                {
                    Console.Out.Write(" (");
                    using (new Help.Color(ConsoleColor.DarkYellow))
                        Console.Out.Write("optional");
                    Console.Out.Write(")");
                }
            }
        }
    }
}

internal sealed class ArgumentComparer : IComparer<Argument>
{
    public int Compare(Argument x, Argument y)
    {
        return (x.Optional, y.Optional, x.IsRest, y.IsRest, x.RestSeparator is not null, y.RestSeparator is not null) switch
        {
            (false, true, false, false, false, false) => -1,
            (true, false, false, false, false, false) => 1,
            (_, _, true, false, _, _) => 1,
            (_, _, false, true, _, _) => -1,
            (_, _, true, true, true, false) => 1,
            (_, _, true, true, false, true) => -1,
            _ => 0,
        };
    }
}
