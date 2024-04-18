namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Struct)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// The description of the command.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The prefix for the short names of the options. If not provided, the default prefix is `-`.
    /// </summary>
    public string ShortPrefix { get; init; } = "-";

    /// <summary>
    /// The prefix for the short names of the options. If not provided, the default prefix is `--`.
    /// </summary>
    public string LongPrefix { get; init; } = "--";

    /// <summary>
    /// The version of the command - As generated from the assembly version.
    /// The version flag will skip the command execution and only print the version to stdout.
    /// <para>The default names are --version and -v. If you have defined a -v in your own interface, version will use -V instead.</para>
    /// </summary>
    /// <remarks>If both -v and -V is already used in your interface, Version cannot be included.</remarks>
    public bool Version { get; init; }
}