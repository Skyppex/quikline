namespace Quikline.Parser.Models;

internal class Args(Type type)
{
    public Type Type { get; } = type;
    public ICollection<Option> Options { get; init; } = [];
    public ICollection<Argument> Arguments { get; init; } = [];
    public Args? Subcommand { get; set; }

    public void AddOption(Option option) => Options.Add(option);
    public void AddArgument(Argument argument) => Arguments.Add(argument);
}
