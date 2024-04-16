namespace Quikline.Parser;

internal class Args
{
    public ICollection<Option> Options { get; init; } = [];
    public ICollection<Argument> Arguments { get; init; } = [];

    public void AddOption(Option option) => Options.Add(option);
    public void AddArgument(Argument argument) => Arguments.Add(argument);
}
