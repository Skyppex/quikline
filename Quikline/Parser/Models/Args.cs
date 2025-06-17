namespace Quikline.Parser.Models;

internal class Args(Type commandType, string? commandName = null)
{
    public Type CommandType { get; } = commandType;
    public ICollection<Option> Options { get; init; } = [];
    public ICollection<Argument> Arguments { get; init; } = [];
    public Args? Subcommand { get; set; }
    public string CommandName { get; } = commandName ?? commandType.Name;

    public void AddOption(Option option)
    {
        if (Options.Contains(option, new OptionNameEqualityComparer()))
        {
            var existing = Options.First(o => new OptionNameEqualityComparer().Equals(o, option));
            Options.Remove(existing);
        }

        Options.Add(option);
    }
    public void AddArgument(Argument argument) => Arguments.Add(argument);
}
