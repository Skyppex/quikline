using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Quikline.Attributes;
using Quikline.Parser;

var a = Quik.Parse<Args>(args);

Console.WriteLine(a);

[Command(Version=true, Description = "A test CLI program.")]
[SuppressMessage("ReSharper", "UnassignedReadonlyField")]
public readonly struct Args
{
    [Option(Short = 'f', Description = "Force the operation.")]
    public readonly bool Force;
    
    [Option(Short = 'v', Description = "Verbose output.")]
    public readonly bool Verbose;
    
    [Option(Short = 'q', Description = "Quiet output.")]
    public readonly bool Quiet;

    [Option(Short = 's', Description = "Case insensitive.")]
    public readonly bool CaseInsensitive;
    
    [Option(ShortPrefix = '+', Short = 's', Description = "Case sensitive.")]
    public readonly bool CaseSensitive;

    public override string ToString()
    {
        FieldInfo[] fields = typeof(Args).GetFields();

        var builder = new StringBuilder();
        
        builder.Append("Args {\n");
        
        foreach (FieldInfo field in fields)
            builder.Append($"    {field.Name}: {field.GetValue(this)},\n");
        
        builder.Append("}");
        return builder.ToString();
    }
}