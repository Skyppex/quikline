using Quikline.Attributes;

namespace Quikline.Parser;

internal static class Validation
{
    public static void ValidateOption(OptionAttribute? optionAttr, string fieldName)
    {
        if (optionAttr is null)
            return;
        
        if (char.IsWhiteSpace(optionAttr.ShortPrefix))
            throw new InvalidProgramException($"Incorrect setup. Short prefix cannot be whitespace. {fieldName}");
        
        if (char.IsWhiteSpace(optionAttr.Short))
            throw new InvalidProgramException($"Incorrect setup. Short name cannot be whitespace. {fieldName}");
        
        if (optionAttr.LongPrefix is not null && string.IsNullOrWhiteSpace(optionAttr.LongPrefix))
            throw new InvalidProgramException($"Incorrect setup. Long prefix cannot be whitespace. {fieldName}");
        
        if (optionAttr.Long is not null && string.IsNullOrWhiteSpace(optionAttr.Long))
            throw new InvalidProgramException($"Incorrect setup. Long name cannot be whitespace. {fieldName}");
    }

    public static void ValidateArgument(ArgumentAttribute? argAttribute, string fieldName)
    {
        if (argAttribute is null)
            return;
        
        if (argAttribute.Name is not null && string.IsNullOrWhiteSpace(argAttribute.Name))
            throw new InvalidProgramException($"Incorrect setup. Name cannot be whitespace. {fieldName}");
    }

    public static void ValidateRest(RestAttribute? restAttribute, string fieldName)
    {
        if (restAttribute is null)
            return;
        
        if (restAttribute.Name is not null && string.IsNullOrWhiteSpace(restAttribute.Name))
            throw new InvalidProgramException($"Incorrect setup. Name cannot be whitespace. {fieldName}");
        
        if (restAttribute.Separator is not null && string.IsNullOrWhiteSpace(restAttribute.Separator))
            throw new InvalidProgramException($"Incorrect setup. Separator cannot be whitespace. {fieldName}");
        
        if (restAttribute.Separator is not null && restAttribute.Separator.Length < 1)
            throw new InvalidProgramException($"Incorrect setup. Separator must be a single character or longer. {fieldName}");
    }

}