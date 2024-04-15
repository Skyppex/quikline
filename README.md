# Quikline

Quikline provides an easy, intuitive API for creating your Command Line Interface.

## Features

Quikline is attribute based, create a `struct` and tag it with the `[Command]` attribute.
Tag your options with `[Option]` and your positional arguments with `[Argument]`.

Each of these have several properties for you to fill in to customize your API, the most important of which is the `Description`.

## Example

```csharp
// Can have multiple Command attributes for different purposes
// If the same value is provided, the last one takes precedence
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
var myCommand = Quik.Parse<MyCommand>(args);
```
