namespace Quikline.Parser;

internal static class Help
{
    public static void Print(Interface @interface)
    {
        ConsoleColor defaultColor = Console.ForegroundColor;

        if (@interface.Description is not null)
        {
            Console.Out.WriteLine(@interface.Description);
            Console.Out.WriteLine("");
        }

        List<Option> options = @interface.Options;
        List<Argument> arguments = @interface.Arguments;

        PrintUsage(@interface);

        if (arguments.Count > 0)
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Out.Write("Arguments");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.WriteLine(":");

            for (int i = 0; i < arguments.Count; i++)
            {
                Argument argument = arguments[i];
                argument.PrintUsage();

                if (i < arguments.Count - 1)
                    Console.Out.WriteLine();
            }
        }

        if (options.Count > 0)
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Out.Write("Options");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.WriteLine(":");

            for (int i = 0; i < options.Count; i++)
            {
                Option option = options[i];
                option.PrintUsage();

                if (i < options.Count - 1)
                    Console.Out.WriteLine();
            }
        }

        Console.ForegroundColor = defaultColor;
    }

    private static void PrintUsage(Interface @interface)
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.Out.Write("Usage");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Out.Write(": ");
        Console.Out.Write(@interface.ProgramName);

        if (@interface.Options.Count > 0)
        {
            Console.Out.Write(" {");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.Write("options");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Out.Write("}");
        }

        List<Argument> arguments = @interface.Arguments;

        if (arguments.Count > 0)
        {
            foreach (Argument argument in arguments)
            {
                Console.Out.Write(" ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                
                if (argument is { IsRest: true, RestSeparator: not null })
                    Console.Out.Write("-- ");
                
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Out.Write("<");

                if (argument is { IsRest: true, RestSeparator: null })
                    Console.Out.Write("..");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Out.Write(argument.Name);
                Console.ForegroundColor = ConsoleColor.Gray;

                if (argument.Optional)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Out.Write("?");
                }

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Out.Write(">");
            }
        }
    }
}