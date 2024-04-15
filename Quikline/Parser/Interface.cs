using Quikline.Attributes;

namespace Quikline.Parser;

internal struct Interface(CommandAttribute commandAttribute)
{
    public readonly string ProgramName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
    public readonly string? Description = commandAttribute.Description;
    public readonly Prefix ShortPrefix = new(commandAttribute.ShortPrefix);
    public readonly Prefix LongPrefix = new(commandAttribute.LongPrefix);
    public ICollection<Option> Options { get; set; } = [];
    public ICollection<Argument> Arguments { get; set; } = [];
    
    public void AddOption(Option option) => Options.Add(option);
    public void AddArgument(Argument argument) => Arguments.Add(argument);

    public bool TryGetOption(string arg, out Option option)
    {
        option = Options.FirstOrDefault(o => (arg.StartsWith(o.Long.Prefix) && arg[o.Long.Prefix.Length..].Equals(o.Long.Name)) ||
            (o.Short != null && (arg.StartsWith(o.Short.Value.Prefix) && arg[o.Short.Value.Prefix.Length..].Equals(o.Short.Value.Name))));
        
        return option != default;
    }
}

internal readonly record struct Option(
    Short? Short,
    Long Long)
{
    public bool Matches(string argName)
    {
        if (Short != null)
            return argName == $"{Short.Value.Prefix}{Short.Value.Name}" ||
                   argName == $"{Long.Prefix}{Long.Name}";
            
        return argName == $"{Long.Prefix}{Long.Name}";
    }
}

internal readonly record struct Argument;
