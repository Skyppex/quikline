using System.Text;

using Quikline.Attributes;
using Quikline.Parser.Models;

namespace Quikline.Parser;

internal static class Help
{
    public static void Print(Interface @interface)
    {
        if (@interface.Description is not null)
        {
            Console.Out.WriteLine(@interface.Description);
            Console.Out.WriteLine("");
        }

        var options = @interface.Options;
        var arguments = @interface.Arguments;
        var subcommands = @interface.Subcommands;

        PrintUsage(@interface);

        if (subcommands.Count > 0)
            PrintSubcommands(subcommands);

        if (arguments.Count > 0)
            PrintArguments(arguments);

        if (options.Count > 0)
            PrintOptions(options);

        var relations = @interface.CommandType.GetRelations();

        if (relations.Count > 0)
            PrintRelations(relations, @interface);
    }

    private static void PrintUsage(Interface @interface)
    {
        using (new Color(ConsoleColor.DarkGreen))
            Console.Out.Write("Usage");

        using (new Color(ConsoleColor.Gray))
        {
            Console.Out.Write(": ");
            Console.Out.Write(GetCommandName(@interface));

            if (@interface.Options.Count > 0)
            {
                Console.Out.Write(" {");
                using (new Color(ConsoleColor.DarkCyan))
                    Console.Out.Write("options");
                Console.Out.Write("}");
            }
        }

        var arguments = @interface.Arguments;

        if (arguments.Count > 0)
        {
            using (new Color(ConsoleColor.Gray))
            {
                foreach (var argument in arguments)
                {
                    Console.Out.Write(" ");
                    
                    if (argument is { IsRest: true, RestSeparator: not null })
                        using (new Color(ConsoleColor.DarkYellow))
                            Console.Out.Write($"{argument.RestSeparator} ");

                    using (new Color(ConsoleColor.Gray))
                    {
                        Console.Out.Write("<");

                        if (argument is { IsRest: true, RestSeparator: null })
                            Console.Out.Write("...");

                        using (new Color(ConsoleColor.DarkCyan))
                            Console.Out.Write(argument.Name);

                        if (argument.Optional)
                        {
                            using (new Color(ConsoleColor.DarkYellow))
                                Console.Out.Write("?");
                        }

                        Console.Out.Write(">");
                    }
                }
            }
        }
    }

    private static string GetCommandName(Interface @interface)
    {
        var name = "";
        
        if (@interface.Parent is not null)
            name = GetCommandName(@interface.Parent) + " ";
        
        return name + @interface.CommandName.SplitPascalCase().ToKebabCase();
    }

    private static void PrintSubcommands(List<Interface> subcommands)
    {
        Console.Out.WriteLine("");
        Console.Out.WriteLine("");

        using (new Color(ConsoleColor.DarkGreen))
            Console.Out.Write("Subcommands");

        using (new Color(ConsoleColor.Gray))
        {
            Console.Out.WriteLine(":");

            for (var i = 0; i < subcommands.Count; i++)
            {
                var subcommand = subcommands[i];
                PrintSubcommand(subcommand);

                if (i < subcommands.Count - 1)
                    Console.Out.WriteLine();
            }
        }
    }

    private static void PrintSubcommand(Interface subcommand)
    {
        using (new Color(ConsoleColor.DarkCyan))
        {
            Console.Out.Write("  ");
            Console.Out.Write(subcommand.CommandName.SplitPascalCase().ToKebabCase());

            if (subcommand.Description is null)
                return;

            using (new Color(ConsoleColor.Gray))
            {
                Console.Out.Write(" - ");
                Console.Out.Write(subcommand.Description);
            }
        }
    }

    private static void PrintArguments(List<Argument> arguments)
    {
        Console.Out.WriteLine("");
        Console.Out.WriteLine("");

        using (new Color(ConsoleColor.DarkGreen))
            Console.Out.Write("Arguments");

        using (new Color(ConsoleColor.Gray))
        {
            Console.Out.WriteLine(":");

            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                argument.PrintUsage();

                if (i < arguments.Count - 1)
                    Console.Out.WriteLine();
            }
        }
    }

    private static void PrintOptions(List<Option> options)
    {
        Console.Out.WriteLine("");
        Console.Out.WriteLine("");
        
        using (new Color(ConsoleColor.DarkGreen))
            Console.Out.Write("Options");

        using (new Color(ConsoleColor.Gray))
        {
            Console.Out.WriteLine(":");

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                option.PrintUsage();

                if (i < options.Count - 1)
                    Console.Out.WriteLine();
            }
        }
    }

    private static void PrintRelations(List<RelationAttribute> relations, Interface @interface)
    {
        relations.Sort(new RelationComparer());
        Console.Out.WriteLine("");
        Console.Out.WriteLine("");
        
        using (new Color(ConsoleColor.DarkGreen))
            Console.Out.Write("Relations");

        using (new Color(ConsoleColor.Gray))
        {
            Console.Out.WriteLine(":");
            
            for (int i = 0; i < relations.Count; i++)
            {
                var relation = relations[i];
                relation.PrintUsage(@interface);
                
                if (i < relations.Count - 1)
                    Console.Out.WriteLine();
            }
        }
    }

    internal class Color : IDisposable
    {
        private readonly ConsoleColor _defaultColor;

        public Color(ConsoleColor color)
        {
            _defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public void Dispose() => Console.ForegroundColor = _defaultColor;
    }
}