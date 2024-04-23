# Quikline

Quikline provides an easy, intuitive API for creating your Command Line Interface.

Quikline is attribute based, create a `struct` and tag it with the `[Command]` attribute.
Tag your options with `[Option]` and your positional arguments with `[Argument]`.

Each of these have several properties for you to fill in to customize your API, the most important of which is the `Description`.

## Features

- Command
  - Automatically generates a help text
  - Can have a --version flag which is generated from the assembly version
  - Can have a short and a long name which is used for all subcommands and options
  - Discriminates between lower and upper case short names (e.g. `-v` and `-V` are different)
  - Can have a description which is used in the help text

- Subcommand
  - Automatically generates a help text
  - Can have a description which is used in the help text

- Option
  - Can have a short and a long name
  - Can be required
  - Can have a default value
  - Choose prefix for short and long names (default is `-` and `--` respectively)
  - Discriminates between lower and upper case short names (e.g. `-v` and `-V` are different)
  - Provide a description which is used in the help text

- Argument
  - Can be nullable (optional) or non-nullable (required)
  - Can have a default value (only for non-nullable and makes it optional)
  - Can have a description which is used in the help text

- Args
  - Use this to group arguments and options together in a struct
  - Can have a short and a long name which is used for all subcommands and options inside the struct
  - Discriminates between lower and upper case short names (e.g. `-v` and `-V` are different)
  - Provide a description which is used in the help text

## Example

```csharp
[Command(Version=true, Description="My command line tool")]
public readonly struct MyCommand {    
    [Option(Short = 'v', Long = "verbose", Description = "Enable verbose output")]
    public readonly bool Verbose;
    
    [Option(Short = 'q', Long = "quiet", Description = "Enable quiet output")]
    public readonly bool Quiet;
    
    [Argument(Description = "Some argument")]
    public readonly string? Argument; // Nullable means optional
    
    [Argument(Description = "Some other argument")]
    public readonly string OtherArgument; // Not nullable so its required
    
    [Argument(Description = "Some third argument", Default="default")]
    public readonly string ThirdArgument; // Not nullable, but has default so its optional
    
    [Argument(Description = "Some invalid argument", Default="default")]
    public readonly string? InvalidArgument; // Nullable and has default which is not allowed
}
```

Now that you've defined your commands structure,
all you have to do is let Quikline do the magic:

```csharp
var myCommand = Quik.Parse<MyCommand>();
```
