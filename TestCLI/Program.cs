using System.Reflection;
using System.Text;
using Quikline;
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
    [Option(Short = 'r', Description = "Range (format: min..max).")]
    public readonly IntRange range;
    
    [Argument(Name = "range", Description = "Range (format: min..max).")]
    public readonly IntRange? rangeArg;

    [Argument(Description = "Array of numbers (format: x,y,z).")]
    public readonly int[] numArray;

    [FixedSize(3)]
    [Delimiter(" +", Regex = true)]
    [Argument(Description = "Array of ranges (format: x1..x2 y1..y2 z1..z2).")]
    public readonly IntRange[] rangeArray;

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

    [Rest(Description = "The rest of the rest of the arguments 2: electric boogaloo.", Separator = "--")]
    public readonly string Rest3;

    [Rest(Description = "The rest of the rest of the arguments 2: electric boogaloo.", Separator = "++")]
    public readonly string Rest4;

    [Rest(Description = "The rest of the rest of the arguments 2: electric boogaloo.", Separator = "minusminus")]
    public readonly string Rest5;

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

            if (field.FieldType.IsArray)
            {
                var array = (Array) field.GetValue(this)!;
                builder.Append($"    {field.Name}: [");
                
                for (var i = 0; i < array.Length; i++)
                {
                    builder.Append(array.GetValue(i));
                    if (i != array.Length - 1)
                        builder.Append(", ");
                }
                
                builder.Append("],\n");
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
[Name("with")]
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

public readonly record struct IntRange(int Min, int Max) : IFromString<IntRange>
{
    public override string ToString() => $"IntRange {{ Min: {Min}, Max: {Max} }}";
    public static (IntRange?, string?) FromString(string value)
    {
        var parts = value.Split("..");
        
        if (parts.Length != 2)
            return (default, "Invalid range format. Expected 'min..max'.");

        if (int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
            return (new IntRange(min, max), null);

        return (default, "Invalid range format. Expected 'min..max'.");
    }
}
