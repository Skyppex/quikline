using System.Reflection;
using System.Text;

using Quikline.Attributes;
using Quikline.Parser;

using TestCLI;

var a = Quik.Parse<Args>();

Console.WriteLine(a);

namespace TestCLI
{
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
            var fields = typeof(Args).GetFields();

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
    public readonly struct LoggingArgs
    {
        [Option(Short = 'v', Description = "Enable verbose output.")]
        public readonly bool Verbose;
    
        [Option(Short = 'l', Description = "LogLevel.", Default = "info")]
        public readonly LogLevel LogLevel;
        
        public readonly LoggingArgs2 LoggingArgs2;

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
    public readonly struct LoggingArgs2
    {
        [Option(Description = "Number.", Default = 42)]
        public readonly int Number;
        
        public override string ToString()
        {
            var fields = typeof(LoggingArgs2).GetFields();

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
}