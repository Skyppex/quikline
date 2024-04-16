using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

using Quikline.Attributes;
using Quikline.Parser;

var a = Quik.Parse<Args>();

Console.WriteLine(a);

[Command(Version = true, Description = "A test CLI program.")]
[SuppressMessage("ReSharper", "UnassignedReadonlyField")]
public readonly struct Args
{
    [Option(Short = 'v', Description = "Enable verbose output.")]
    public readonly bool Verbose;

    [Option(Short = 'f', Description = "Force the operation.")]
    public readonly bool Force;

    [Option(Short = 'l', Description = "LogLevel.", Default = "info")]
    public readonly LogLevel LogLevel;

    [Option(Short = 'n', Description = "Name.")]
    public readonly string Name;

    [Option(Short = 's', Description = "Case insensitive.")]
    public readonly bool CaseInsensitive;

    [Option(ShortPrefix = '+', Short = 's', Description = "Case sensitive.")]
    public readonly bool CaseSensitive;
    
    [Argument(Description = "The file to process.", Optional = false)]
    public readonly string File;

    [Argument(Description = "The other file to process.", Optional = true)]
    public readonly string OtherFile;

    [Rest(Description = "The rest of the arguments.")]
    public readonly string Rest;
    
    [Rest(Description = "The rest of the rest of the arguments.", Separator = "--")]
    public readonly string Rest2;

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
