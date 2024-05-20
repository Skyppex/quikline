namespace Quikline.Attributes;

/// <summary>
/// Set the expected size of the list or array argument.
/// </summary>
/// <param name="size">
/// The size of the fixed size array or list.
/// </param>
[AttributeUsage(validOn: AttributeTargets.Field)]
public class FixedSizeAttribute(uint size) : Attribute
{
    /// <summary>
    /// The size of the fixed size array or list.
    /// </summary>
    public uint Size { get; } = size;
}

/// <summary>
/// Set the delimiter used to separate the elements of the list or array.
/// </summary>
/// <param name="delimiter">
/// The delimiter used to separate the elements of the list or array.
/// </param>
[AttributeUsage(validOn: AttributeTargets.Field)]
public class DelimiterAttribute(string delimiter) : Attribute
{
    /// <summary>
    /// The delimiter used to separate the elements of the list or array.
    /// </summary>
    public string Delimiter { get; } = delimiter;
    
    /// <summary>
    /// Whether the delimiter is a regular expression.
    /// </summary>
    public required bool Regex { get; init; } = false;
}
