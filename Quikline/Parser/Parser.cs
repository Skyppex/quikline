using System.Reflection;

using Quikline.Attributes;

namespace Quikline.Parser;

public static class Quik
{
    public static T Parse<T>()
        where T : struct
    {
        string[] args = Environment.GetCommandLineArgs()[1..];

        Type type = typeof(T);
        Interface @interface = InterfaceParser.Parse(type);

        Args passedArgs = ArgsParser.Parse(args, @interface);

        if (passedArgs.Options.Any(
                o =>
                {
                    Prefix interfaceLongPrefix = @interface.LongPrefix;

                    return o.Matches($"{interfaceLongPrefix}help");
                }))
        {
            Help.Print(@interface);
            Environment.Exit(0);

            return default;
        }

        if (passedArgs.Options.Any(o => o.Matches($"{@interface.LongPrefix}version")))
        {
            Version? version = type.Assembly.GetName().Version;
            Console.Out.Write(version);
            Environment.Exit(0);

            return default;
        }

        if (MissingRequired(@interface, passedArgs, out string[] missing))
        {
            Console.Error.WriteLine($"Incorrect usage. Missing required options: {missing}");
            Console.Error.Write("Use --help for more information.");
            Environment.Exit(1);
        }

        object value = Activator.CreateInstance<T>();

        foreach (Option option in passedArgs.Options)
        {
            string fieldName = option.FieldName;

            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.SetField)!;

            if (option.Type == typeof(bool))
            {
                field.SetValue(value, true);

                continue;
            }

            field.SetValue(value, option.Value);
        }

        foreach (Argument argument in passedArgs.Arguments)
        {
            string fieldName = argument.FieldName;

            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.SetField)!;

            if (argument.Type == typeof(bool))
            {
                field.SetValue(value, true);

                continue;
            }

            field.SetValue(value, argument.Value);
        }
        
        return (T)value;
    }

    private static bool MissingRequired(Interface @interface, Args args, out string[] missing)
    {
        missing = @interface.Options
            .Where(o => o.Required && !args.Options.Contains(o, new ShortOptionEqualityComparer()))
            .Select(o => o.Long.Name.ToString())
            .ToArray();

        return missing.Length > 0;
    }
}
