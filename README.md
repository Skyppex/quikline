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

## Examples

### Simple command
```csharp
[Command(Version=true, Description="Create some tea")]
public readonly struct Tea {  
    [Option(Short='m', Description="Add a number of sugar cubes to the tea")]
    public readonly int Sugar;
    
    [Option(Short='m', Description="Add milk to the tea")]
    public readonly bool Milk;
    
    [Argument(Description="The type of tea")]
    public readonly TeaType TeaType;
    
    [Argument(Description="The temperature of the water", Default = 90)] // Celcius
    public readonly int Temperature;
}

public enum TeaType {
    Green,
    Black,
    White,
    Oolong,
    Herbal
}
```

### Command with subcommands
```csharp
[Command(Version = true, Description="Create a beverage")]
public readonly struct Beverage {
    public readonly Tea Tea;
    public readonly Coffee Coffee;
}

[Arg

[Subcommand(Description="Create some tea")]
public readonly struct Tea {  
    [Option(Short='m', Description="Add a number of sugar cubes to the tea")]
    public readonly int Sugar;
    
    [Option(Short='m', Description="Add milk to the tea")]
    public readonly bool Milk;
    
    [Argument(Description="The type of tea")]
    public readonly TeaType TeaType;
    
    // Default value makes it optional, you can also use nullable types
    [Argument(Description="The temperature of the water", Default = 90)]
    public readonly int Temperature;
}

[Subcommand(Description="Create some coffee")]
public readonly struct Coffee {
    [Option(Short='m', Description="Add milk to the coffee")]
    public readonly bool Milk;
    
    [Argument(Description="The type of coffee")]
    public readonly CoffeeType CoffeeType;
    
    [Argument(Description="The temperature of the water", Default = 90)]
    public readonly int Temperature;
}

public enum TeaType {
    Green,
    Black,
    White,
    Oolong,
    Herbal
}

public enum CoffeeType {
    Espresso,
    Americano,
    Latte,
    Cappuccino,
    Mocha
}
```

Now that you've defined your commands structure,
all you have to do is let Quikline do the magic:

```csharp
var myCommand = Quik.Parse<MyCommand>();
```
