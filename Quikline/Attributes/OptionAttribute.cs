namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Field)]
public class OptionAttribute : Attribute
{
    private readonly char _shortPrefix;
    private readonly char _short;
    private readonly string? _longPrefix;
    private readonly string? _long;

    public char ShortPrefix
    {
        get => _shortPrefix;
        init
        {
            if (char.IsWhiteSpace(value))
                throw new ArgumentException("Short prefix cannot be a whitespace character.");
            
            _shortPrefix = value;
        }
    }

    public char Short 
    {
        get => _short;
        init
        {
            if (char.IsWhiteSpace(value))
                throw new ArgumentException("Short cannot be a whitespace character.");
            
            _short = value;
        }
    }
    
    public string? LongPrefix
    {
        get => _longPrefix;
        init
        {
            if (value?.Contains(' ') ?? false)
                throw new ArgumentException("Long prefix cannot contain spaces.");
            
            _longPrefix = value;
        }
    }
    
    public string? Long
    {
        get => _long;
        init
        {
            if (value?.Contains(' ') ?? false)
                throw new ArgumentException("Long cannot contain spaces.");
            
            _long = value;
        }
    }
    
    public string? Description { get; init; }
    public object? Default { get; init; }
    public bool Required { get; init; }
}