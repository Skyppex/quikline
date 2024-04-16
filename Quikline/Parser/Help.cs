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

        List<Option> options = @interface.Options;
        List<Argument> arguments = @interface.Arguments;

        PrintUsage(@interface);

        if (arguments.Count > 0)
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("");

            using (new Color(ConsoleColor.DarkGreen))
                Console.Out.Write("Arguments");

            using (new Color(ConsoleColor.Gray))
            {
                Console.Out.WriteLine(":");

                for (int i = 0; i < arguments.Count; i++)
                {
                    Argument argument = arguments[i];
                    argument.PrintUsage();

                    if (i < arguments.Count - 1)
                        Console.Out.WriteLine();
                }
            }
        }

        if (options.Count <= 0)
            return;

        Console.Out.WriteLine("");
        Console.Out.WriteLine("");
        
        using (new Color(ConsoleColor.DarkGreen))
            Console.Out.Write("Options");

        using (new Color(ConsoleColor.Gray))
        {
            Console.Out.WriteLine(":");

            for (int i = 0; i < options.Count; i++)
            {
                Option option = options[i];
                option.PrintUsage();

                if (i < options.Count - 1)
                    Console.Out.WriteLine();
            }
        }
    }

    private static void PrintUsage(Interface @interface)
    {
        using (new Color(ConsoleColor.DarkGreen))
            Console.Out.Write("Usage");

        using (new Color(ConsoleColor.Gray))
        {
            Console.Out.Write(": ");
            Console.Out.Write(@interface.ProgramName);

            if (@interface.Options.Count > 0)
            {
                Console.Out.Write(" {");
                using (new Color(ConsoleColor.DarkCyan))
                    Console.Out.Write("options");
                Console.Out.Write("}");
            }
        }

        List<Argument> arguments = @interface.Arguments;

        if (arguments.Count > 0)
        {
            using (new Color(ConsoleColor.Gray))
            {
                foreach (Argument argument in arguments)
                {
                    Console.Out.Write(" ");
                    
                    if (argument is { IsRest: true, RestSeparator: not null })
                        using (new Color(ConsoleColor.DarkYellow))
                            Console.Out.Write("-- ");

                    using (new Color(ConsoleColor.Gray))
                    {
                        Console.Out.Write("<");

                        if (argument is { IsRest: true, RestSeparator: null })
                            Console.Out.Write("..");

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