# Quikline Design

As a user, I want to create a `struct` that can be decorated with attributes to 
define the command line interface for my app.

## Semantics

A command can include the following:
- A description
  - The description can be used to generate help text
- A list of options
  - The options can have a short name, a long name, a description, and a default value
  - The short name must be a single character, is case-sensitive, and is prefixed with a single 
    dash. The prefix can be overwritten on the command level, or for a single option
- A list of positional arguments
  - The positional arguments can have a name, a description, and a default value
  - Positional arguments are defined in the order they are expected to be provided
  - Positional arguments can be optional by defining it as nullable
- A list of subcommands
  - Subcommands has the same things as a command and can have further subcommands

## Types

Commands only support certain types:

#### MVP
- `bool`
- `int`
- `float`
- `double`
- `char`
- `string`
- `enums`

#### Nice to have
- `uint`
- `long`
- `ulong`
- `Guid`
- `DateTime`
- `TimeSpan`
- `DateOnly`
- `TimeOnly`
- `Uri`

## Attributes

- `[Command]` -> Defines the `struct` as the input type for the command.
  - Description: The description of the command.
  - ShortPrefix: The prefix for the short names of the options. If not provided, the default prefix 
    is `-`.
  - LongPrefix: The prefix for the long names of the options. If not provided, the default prefix 
    is `-`.
  - Version: The version of the command - Can be generated from the assembly version
- `[Subcommand]` -> Defines the `struct` as the input type for the subcommand.
  - Description: The description of the command.
  - ShortPrefix: The prefix for the short names of the options. If not provided, the default prefix
    is `-`.
  - LongPrefix: The prefix for the long names of the options. If not provided, the default prefix
    is `-`.
- `[Args]` -> Defines the `struct` as the input type for the arguments of the command.
  - Description: The description of the arguments.
  - ShortPrefix: The prefix for the short names of the options. If not provided, the default prefix
    is `-`.
  - LongPrefix: The prefix for the long names of the options. If not provided, the default prefix
    is `-`.
- `[Group]`
  - Name: The name of the group
  - Required: Whether the group is required or not
  - Multiple: Whether multiple options in the group can be provided
- `[Option]`
  - Short: The short name of the option
  - Long: The long name of the option
  - Description: The description of the option.
  - Default: The default value of the option
  - Required: Whether the option is required or not (default is `false`)
  - Groups: The group the option belongs to (default is no group)
  - ShortPrefix: The prefix for the short names of the options. If not provided, the default prefix
    is `-`.
  - LongPrefix: The prefix for the long names of the options. If not provided, the default prefix
    is `-`.
- `[Argument]`
  - Description: The description of the positional argument.
  - Default: The default value of the positional argument (can only be used on non-nullable types)
  - Groups: The group the argument belongs to (default is no group)
- 

## Example

```csharp
// Can have multiple Command attributes for different purposes
// If the same value is provided, the last one takes precedence
[Command(Version=true)]
[Command(Description="My command line tool")]
[Group(Name="logging", Required=false, Multiple=Multiple.OnlyOne)]
public readonly struct MyCommand {
    [Flatten]
    public readonly FileArgs FileArgs;
    
    [Option(Short='v', Long="verbose", Description="Enable verbose output", Groups=["logging"])]
    public readonly bool Verbose;
    
    [Option(Short='q', Long="quiet", Description="Enable quiet output", Groups=["logging"])]
    public readonly bool Quiet;
    
    [Argument(Description="Some argument")]
    public readonly string? Argument; // Nullable means optional
    
    [Argument(Description="Some other argument")]
    public readonly string OtherArgument; // Not nullable so its required
    
    [Argument(Description="Some third argument", Default="default")]
    public readonly string ThirdArgument; // Not nullable, but has default so its optional
    
    [Argument(Description="Some invalid argument", Default="default")]
    public readonly string? InvalidArgument; // Nullable and has default which is not allowed
    
    public readonly Create Create;
}

[Args(Description="File related arguments")]
[Group(Name="destination", Required=false, Multiple=Multiple.All)]
public readonly struct FileArgs {
    [Option(Short='s', Long="source", Description="Source file to replace stdin")]
    public readonly string? Source;
    
    [Option(Short='d', Long="destination", Description="Destination file to replace stdout")]
    public readonly string? Destination;
    
    [Option(Short='f', Long="force", Description="Force overwrite of destination file")]
    public readonly bool Force;
}

[SubcommandDef(Description="Create something")]
public readonly struct Create {
    [Argument(Description="Name of the thing")]
    public readonly string Name;
    
    [Argument(Description="Type of the thing")]
    public readonly string Type;
}
```