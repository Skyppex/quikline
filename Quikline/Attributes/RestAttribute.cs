namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Field)]
public class RestAttribute : Attribute
{
    private readonly string? _separator;
    
    public string? Name { get; init; }
    public string? Description { get; init; }

    public string? Separator
    {
        get => _separator;
        init
        {
            if (value?.Contains(' ') ?? false)
                throw new ArgumentException("Separator cannot contain spaces.");
            
            _separator = value;
        }
    }
}