using System.Reflection;
using Quikline.Attributes;

namespace Quikline.Parser;

public static class Quik
{
    public static T Parse<T>(string[] args) where T : struct
    {
        var type = typeof(T);
        var @interface = ParseInterface(type);

        var passedArgs = ParseArgs(args, @interface);
        
        if (passedArgs.Options.Any(
                o => o.Matches($"{@interface.LongPrefix}help")))
        {
            PrintHelp(@interface);
            Environment.Exit(0);
            return default;
        }
        
        if (passedArgs.Options.Any(
                o => o.Matches($"{@interface.LongPrefix}version")))
        {
            var version = type.Assembly.GetName().Version;
            Console.Out.Write(version);
            Environment.Exit(0);
            return default;
        }
        
        var value = Activator.CreateInstance<T>();
        
        return value;
    }

    private static Interface ParseInterface(Type type)
    {
        var attributes = type.GetCustomAttributes();

        if (attributes.SingleOrDefault(a => a is CommandAttribute) is not CommandAttribute commandAttribute)
            throw new InvalidOperationException("The provided type does not have a Command attribute.");

        var @interface = new Interface(commandAttribute);
        
        // Add the options and arguments from the fields here.
        
        if (commandAttribute.Version)
        {
            bool isUsingLowerCaseV = @interface.Options.Contains(
                new Option(new Short(@interface.ShortPrefix, new Name("v")), Long.Empty),
                new ShortOptionEqualityComparer());
            
            bool isUsingUpperCaseV = @interface.Options.Contains(
                new Option(new Short(@interface.ShortPrefix, new Name("V")), Long.Empty),
                new ShortOptionEqualityComparer());

            Short? shortVersion = (hasv: isUsingLowerCaseV, hasV: isUsingUpperCaseV) switch
            {
                (false, _) => new Short(@interface.ShortPrefix, new Name("v")),
                (true, false) =>  new Short(@interface.ShortPrefix, new Name("V")),
                _ => null
            };
            
            @interface.AddOption(new Option(shortVersion, new Long(@interface.LongPrefix, new Name("version"))));
        }
        
        bool isUsingLowerCaseH = @interface.Options.Contains(
            new Option(new Short(@interface.ShortPrefix, new Name("h")), Long.Empty),
            new ShortOptionEqualityComparer());
        
        bool isUsingUpperCaseH = @interface.Options.Contains(
            new Option(new Short(@interface.ShortPrefix, new Name("H")), Long.Empty),
            new ShortOptionEqualityComparer());
        
        Short? shortHelp = (isUsingLowerCaseH, isUsingUpperCaseH) switch
        {
            (false, _) => new Short(@interface.ShortPrefix, new Name("h")),
            (true, false) =>  new Short(@interface.ShortPrefix, new Name("H")),
            _ => null
        };

        @interface.AddOption(new Option(shortHelp, new Long(@interface.LongPrefix, new Name("help"))));

        return @interface;
    }

    private static Args ParseArgs(string[] args, Interface @interface)
    {
        var parsed = new Args();

        foreach (var arg in args)
        {
            if (@interface.TryGetOption(arg, out var option))
            {
                parsed.AddOption(option);
                continue;
            }
            
            Console.Error.WriteLine($"Incorrect usage. Unknown option: {arg}");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        return parsed;
    }
    
    private static void PrintHelp(Interface @interface)
    {
        if (@interface.Description is not null)
        {
            Console.Out.WriteLine(@interface.Description);
            Console.Out.WriteLine("");
        }
        
        Console.Out.Write($"USAGE: {@interface.ProgramName}");
    }
}