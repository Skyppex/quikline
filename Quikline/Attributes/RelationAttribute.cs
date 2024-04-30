using Quikline.Parser;
using Quikline.Parser.Models;

namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public abstract class RelationAttribute(string name) : Attribute
{
    internal bool _required;
    
    /// <summary>
    /// If you wish for the relation to always be present, set this to `true`.
    /// Doing so will force the user to pass all arguments required to satisfy this relation.
    /// </summary>
    public bool Required { get => _required; init => _required = value; }
    public string Name { get; } = name;
    
    internal abstract void PrintUsage(Interface @interface);
}

public class ExclusiveRelationAttribute(string name, params string[] args) : RelationAttribute(name)
{
    public string[] Args { get; init; } = args;

    internal override void PrintUsage(Interface @interface)
    {
        var names = Args;
        
        Console.Out.Write("  ");

        using (new Help.Color(ConsoleColor.DarkCyan))
            Console.Out.Write(Name);

        Console.Out.Write(" - (");
        
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];

            using (new Help.Color(ConsoleColor.Blue))
                Console.Out.Write(@interface.GetPrefixedNameForOption(name) ?? name.SurroundWith("\"", "\""));

            if (i >= names.Length - 1)
                continue;

            using (new Help.Color(ConsoleColor.Gray))
                Console.Out.Write(" | ");
        }
        
        Console.Out.Write(")");

        if (!Required)
            return;

        Console.Out.Write(" (");
            
        using (new Help.Color(ConsoleColor.DarkRed))
            Console.Out.Write("required");
            
        Console.Out.Write(")");
    }
}

public class OneOrMoreRelationAttribute(string name, params string[] args) : RelationAttribute(name)
{
    public string[] Args { get; init; } = args;
    
    internal override void PrintUsage(Interface @interface)
    {
        var names = Args;
        
        Console.Out.Write("  ");

        using (new Help.Color(ConsoleColor.DarkCyan))
            Console.Out.Write(Name);

        Console.Out.Write(" - (");
        
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            
            using (new Help.Color(ConsoleColor.Blue))
                Console.Out.Write(@interface.GetPrefixedNameForOption(name) ?? name.SurroundWith("\"", "\""));

            if (i >= names.Length - 1)
                continue;

            using (new Help.Color(ConsoleColor.Gray))
                Console.Out.Write(" + ");
        }
        
        Console.Out.Write(")");
        
        if (!Required)
            return;

        Console.Out.Write(" (");
            
        using (new Help.Color(ConsoleColor.DarkRed))
            Console.Out.Write("required");
            
        Console.Out.Write(")");
    }
}

public class InclusiveRelationAttribute(string name, params string[] args) : RelationAttribute(name)
{
    public string[] Args { get; init; } = args;

    internal override void PrintUsage(Interface @interface)
    {
        var names = Args;
        
        Console.Out.Write("  ");
        
        using (new Help.Color(ConsoleColor.DarkCyan))
            Console.Out.Write(Name);

        Console.Out.Write(" - (");
        
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];

            using (new Help.Color(ConsoleColor.Blue))
                Console.Out.Write(@interface.GetPrefixedNameForOption(name) ?? name.SurroundWith("\"", "\""));

            if (i >= names.Length - 1)
                continue;

            using (new Help.Color(ConsoleColor.Gray))
                Console.Out.Write(" & ");
        }
        
        Console.Out.Write(")");
        
        if (!Required)
            return;

        Console.Out.Write(" (");
        
        using (new Help.Color(ConsoleColor.DarkRed))
            Console.Out.Write("required");
        
        Console.Out.Write(")");
    }
}

public class OneWayRelationAttribute(string name) : RelationAttribute(name)
{
    public required string From { get; init; }
    public required string To { get; init; }
    
    internal override void PrintUsage(Interface @interface)
    {
        Console.Out.Write("  ");
        
        using (new Help.Color(ConsoleColor.DarkCyan))
            Console.Out.Write(Name);

        Console.Out.Write(" - (");


        using (new Help.Color(ConsoleColor.Blue))
            Console.Out.Write(@interface.GetPrefixedNameForOption(From) ?? From.SurroundWith("\"", "\""));
        
        Console.Out.Write(" > ");
        
        using (new Help.Color(ConsoleColor.Blue))
            Console.Out.Write(@interface.GetPrefixedNameForOption(To) ?? To.SurroundWith("\"", "\""));

        Console.Out.Write(")");
    }
}

internal sealed class RelationComparer : IComparer<RelationAttribute>
{
    public int Compare(RelationAttribute? x, RelationAttribute? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (InclusiveRelationAttribute, _) => -1,
            (_, InclusiveRelationAttribute) => 1,
            (ExclusiveRelationAttribute, _) => -1,
            (_, ExclusiveRelationAttribute) => 1,
            (OneOrMoreRelationAttribute, _) => -1,
            (_, OneOrMoreRelationAttribute) => 1,
            (OneWayRelationAttribute, _) => -1,
            (_, OneWayRelationAttribute) => 1,
            _ => 0
        };
    }
}