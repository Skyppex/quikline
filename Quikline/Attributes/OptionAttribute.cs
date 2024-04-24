namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Field)]
public class OptionAttribute : Attribute
{
    public char ShortPrefix { get; init; }
    public char Short { get; init; }
    public string? LongPrefix { get; init; }
    public string? Long { get; init; }
    public string? Description { get; init; }
    public object? Default { get; init; }
    public bool Required { get; init; }
}