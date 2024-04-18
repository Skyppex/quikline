namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Struct)]
public sealed class ArgsAttribute : Attribute
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