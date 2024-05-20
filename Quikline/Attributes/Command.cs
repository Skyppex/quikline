namespace Quikline.Attributes;

internal interface ICommand
{
    /// <summary>
    /// The description of the command.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The prefix for the short names of the options. If not provided, the default prefix is `-`.
    /// </summary>
    public string ShortPrefix { get; init; }

    /// <summary>
    /// The prefix for the short names of the options. If not provided, the default prefix is `--`.
    /// </summary>
    public string LongPrefix { get; init; }
}

[AttributeUsage(validOn: AttributeTargets.Struct)]
public class CommandAttribute : Attribute, ICommand
{
    public string? Description { get; init; }
    public string ShortPrefix { get; init; } = "-";
    public string LongPrefix { get; init; } = "--";

    /// <summary>
    /// The version of the command - As generated from the assembly version.
    /// The version flag will skip the command execution and only print the version to stdout.
    /// <para>The default names are --version and -v. If you have defined a -v in your own interface, version will use -V instead.</para>
    /// </summary>
    /// <remarks>If both -v and -V is already used in your interface, a shorthand is not included.</remarks>
    public bool Version { get; init; }
}

[AttributeUsage(validOn: AttributeTargets.Struct)]
public class SubcommandAttribute : Attribute, ICommand
{
    public string? Description { get; init; }
    public string ShortPrefix { get; init; } = "-";
    public string LongPrefix { get; init; } = "--";
}

internal static class CommandExtensions
{
    public static CommandAttribute IntoCommand(this SubcommandAttribute subcommand) => new()
    {
        Description = subcommand.Description,
        ShortPrefix = subcommand.ShortPrefix,
        LongPrefix = subcommand.LongPrefix,
        Version = false,
    };
}

[AttributeUsage(validOn: AttributeTargets.Struct | AttributeTargets.Field)]
public class NameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}