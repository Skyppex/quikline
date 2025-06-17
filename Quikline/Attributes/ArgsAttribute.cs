namespace Quikline.Attributes;

/// <summary>
/// Decorate a different struct with this attribute to indicate that it contains
/// additional arguments and options which should be flattened on the cli.
/// You main \[Command\] or any \[Subcommand\] must then have a field with the type
/// this decorates.
/// </summary>
/// <example>
/// <code>
/// \[Command\]
/// public readonly struct Cli
/// {
///     public readonly OtherArgs OtherArgs;
/// }
///
/// \[Args\]
/// public readonly struct OtherArgs
/// {
///     [Argument]
///     public readonly int Arg;
///
///     [Option]
///     public readonly bool Flag;
/// }
/// </code>
///
/// This results in a cli as if Arg and Flag existed directly on the Cli type
/// itself. It allows you to group your data logically in your code but still
/// provide a flat and easy-to-use cli for the user.
/// </example>
[AttributeUsage(validOn: AttributeTargets.Struct)]
public class ArgsAttribute : Attribute
{
    /// <summary>
    /// The prefix for the short names of the options. If not provided, the default prefix is `-`.
    /// </summary>
    public string ShortPrefix { get; init; } = "-";

    /// <summary>
    /// The prefix for the short names of the options. If not provided, the default prefix is `--`.
    /// </summary>
    public string LongPrefix { get; init; } = "--";
}
