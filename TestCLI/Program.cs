using System.Reflection;
using System.Text;
using Quikline.Attributes;
using Quikline.Parser;

var a = Quik.Parse<TestArgs>();

Console.WriteLine(a);

// ReSharper disable UnassignedReadonlyField

[Command(Version = true, Description = "A test CLI program.")]
[ExclusiveRelation("casing", nameof(CaseSensitive), nameof(CaseInsensitive))]
[InclusiveRelation("rel", nameof(Force), "scripting")]
[OneOrMoreRelation("scripting", nameof(None), "casing")]
[OneWayRelation("naming", From = "casing", To = "logging")]
public readonly struct TestArgs
{
    [Option(Short = '0', Description = "No elements.")]
    public readonly bool None;

    [Option(Short = '1', Description = "Single element.")]
    public readonly bool Single;

    [Option(Short = 'f', Description = "Force the operation.")]
    public readonly bool Force;

    [Option(Short = 'n', Description = "Name.")]
    public readonly string Name;

    [Option(Short = 's', Description = "Case insensitive.")]
    public readonly bool CaseInsensitive;

    [Option(ShortPrefix = '+', Short = 's', Description = "Case sensitive.")]
    public readonly bool CaseSensitive;

    [Argument(Description = "The file to process.")]
    public readonly string? File;

    [Argument(Description = "The other file to process.")]
    public readonly string? OtherFile;
    
    [Argument(Description = "The other file to process.", Default = 100)]
    public readonly int Temperature;

    [Rest(Description = "The rest of the arguments.")]
    public readonly string Rest;

    [Rest(Description = "The rest of the rest of the arguments.", Separator = "--")]
    public readonly string Rest2;

    public readonly LoggingArgs LoggingArgs;
    public readonly Commands Commands;

    public override string ToString()
    {
        var fields = typeof(TestArgs).GetFields();

        var builder = new StringBuilder();

        builder.Append("Args {\n");

        foreach (var field in fields)
        {
            if (field.FieldType.GetCustomAttribute<ArgsAttribute>() is not null)
            {
                var subThis = field.GetValue(this)!;
                builder.Append(subThis);
                continue;
            }

            builder.Append($"    {field.Name}: {field.GetValue(this)},\n");
        }

        builder.Append('}');
        return builder.ToString();
    }
}

[Args]
[InclusiveRelation("logging", nameof(Verbose), nameof(LogLevel))]
public readonly struct LoggingArgs
{
    [Option(Short = 'v', Description = "Enable verbose output.")]
    public readonly bool Verbose;

    [Option(Short = 'l', Description = "LogLevel.", Default = "info")]
    public readonly LogLevel LogLevel;

    public override string ToString()
    {
        var fields = typeof(LoggingArgs).GetFields();

        var builder = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.FieldType.GetCustomAttribute<ArgsAttribute>() is not null)
            {
                var subThis = field.GetValue(this)!;
                builder.Append(subThis);
                continue;
            }

            builder.Append($"    {field.Name}: {field.GetValue(this)},\n");
        }

        return builder.ToString();
    }
}

[Args]
public readonly struct Commands
{
    public readonly Sub? Sub;

    public override string ToString()
    {
        var fields = typeof(Commands).GetFields();

        var builder = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.FieldType.GetCustomAttribute<ArgsAttribute>() is not null)
            {
                var subThis = field.GetValue(this)!;
                builder.Append(subThis);
                continue;
            }

            builder.Append($"    {field.Name}: {field.GetValue(this)},\n");
        }

        return builder.ToString();
    }
}

[Subcommand(Description = "A test subcommand.")]
[OneOrMoreRelation("oof", nameof(Woofer), nameof(Poofer))]
[ExclusiveRelation("casing", nameof(CaseSensitive), nameof(CaseInsensitive))]
[InclusiveRelation("rel", nameof(Force), "scripting")]
[OneOrMoreRelation("scripting", nameof(None), "casing")]
public readonly struct Sub
{
    [Option(Short = 'w', Description = "Woofer.")]
    public readonly bool Woofer;

    [Option(Short = 'p', Description = "Poofer.")]
    public readonly bool Poofer;

    [Option(Short = '0', Description = "No elements.")]
    public readonly bool None;

    [Option(Short = '1', Description = "Single element.")]
    public readonly bool Single;

    [Option(Short = 'f', Description = "Force the operation.")]
    public readonly bool Force;

    [Option(Short = 'n', Description = "Name.")]
    public readonly string Name;

    [Option(Short = 's', Description = "Case insensitive.")]
    public readonly bool CaseInsensitive;

    [Option(ShortPrefix = '+', Short = 's', Description = "Case sensitive.")]
    public readonly bool CaseSensitive;

    [Argument(Description = "The file to process.", Default = 10f)]
    public readonly float Threshold;

    public readonly SubSub? SubSub;

    public override string ToString()
    {
        var fields = typeof(Sub).GetFields();

        var builder = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.FieldType.GetCustomAttribute<ArgsAttribute>() is not null)
            {
                var subThis = field.GetValue(this)!;
                builder.Append(subThis);
                continue;
            }

            builder.Append($"    {field.Name}: {field.GetValue(this)},\n");
        }

        return builder.ToString();
    }
}

[Subcommand(Description = "A test subsubcommand.")]
public readonly struct SubSub
{
    [Option(Short = 'w', Description = "Woofer.")]
    public readonly bool Woofer;

    [Argument(Description = "The file to process.")]
    public readonly float? Threshold;

    public override string ToString()
    {
        var fields = typeof(SubSub).GetFields();

        var builder = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.FieldType.GetCustomAttribute<ArgsAttribute>() is not null)
            {
                var subThis = field.GetValue(this)!;
                builder.Append(subThis);
                continue;
            }

            builder.Append($"    {field.Name}: {field.GetValue(this)},\n");
        }

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
