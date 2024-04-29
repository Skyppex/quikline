namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Field)]
public class ArgumentAttribute : Attribute
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public object? Default { get; init; }
}