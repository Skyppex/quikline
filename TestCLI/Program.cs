using System.Reflection;
using System.Text;

using Quikline.Attributes;
using Quikline.Parser;

var a = Quik.Parse<Args>();

Console.WriteLine(a);

// ReSharper disable UnassignedReadonlyField

[Command(Version = true, Description = "A test CLI program.")]
public readonly struct Args
{
    [Option(Short = 'f', Description = "Force the operation.")]
    public readonly bool Force;

    [Option(Short = 'n', Description = "Name.")]
    public readonly string Name;

    [Option(Short = 's', Description = "Case insensitive.")]
    public readonly bool CaseInsensitive;

    [Option(ShortPrefix = '+', Short = 's', Description = "Case sensitive.")]
    public readonly bool CaseSensitive;
    
    [Argument(Description = "The file to process.")]
    public readonly string File;

    [Argument(Description = "The other file to process.", Optional = true)]
    public readonly string OtherFile;

    [Rest(Description = "The rest of the arguments.")]
    public readonly string Rest;
    
    [Rest(Description = "The rest of the rest of the arguments.", Separator = "--")]
    public readonly string Rest2;
    
    public readonly LoggingArgs LoggingArgs;

    public override string ToString()
    {
        FieldInfo[] fields = typeof(Args).GetFields();

        var builder = new StringBuilder();

        builder.Append("Args {\n");

        foreach (FieldInfo field in fields)
            builder.Append($"    {field.Name}: {field.GetValue(this)},\n");

        builder.Append('}');

        return builder.ToString();
    }
}

[Args]
public readonly struct LoggingArgs
{
    [Option(Short = 'v', Description = "Enable verbose output.")]
    public readonly bool Verbose;
    
    [Option(Short = 'l', Description = "LogLevel.", Default = "info")]
    public readonly LogLevel LogLevel;
}

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Critical,
    Fatal,
    ReallyBad,
}
