namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Field)]
public class RestAttribute : Attribute
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Separator { get; init; }
}