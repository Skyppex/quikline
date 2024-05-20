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

#### Relations

- Inclusive
  - All options in the group must be provided
- Exclusive
  - Only one option in the group can be provided
- OneOrMore
  - At least one option in the group must be provided
- OneWay
  - If option 'a' is provided option 'b' must also be provided, but not the other way around

## Supported Types

- `bool`
- `int`
- `float`
- `double`
- `char`
- `string`
- `enums`
- `arrays` (not lists though)
  - Arrays can take advantage the `[FixedSize]` and `[Delimiter]` attributes
  - The `[FixedSize]` attribute can be used to specify the size of the array
  - The `[Delimiter]` attribute can be used to specify the delimiter used to split the string into an array
  - Note that arrays must be passed as a single argument. i.e. `--array 1,2,3,4` or `--array "1 2 3 4"`
- `custom data types` (as long as they implement `IFromString`)
- `nullable types` (note: elements in arrays can be nullable, but it doesn't matter)

## Examples

### Simple command
```csharp
[Command(Version=true, Description="Create some tea")]
public readonly struct Tea {  
    [Option(Short='s', Description="Add a number of sugar cubes to the tea")]
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

[Subcommand(Description="Create some tea")]
public readonly struct Tea {  
    [Option(Short='s', Description="Add a number of sugar cubes to the tea")]
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

### Command with relations
```csharp
[Command(Version = true, Description="Create a beverage")]
// This ensures that if you want extra milk, you also have to have milk... which just makes sense
[OneWayRelation("milk", From = nameof(ExtraMilk), To = nameof(Milk))]
public readonly struct Tea {  
    [Option(Short='s', Description="Add a number of sugar cubes to the tea")]
    public readonly int Sugar;
    
    [Option(Short='m', Description="Add milk to the tea")]
    public readonly bool Milk;
    
    [Option(Short='M', Description="Add more milk to the tea")]
    public readonly bool ExtraMilk;
    
    [Argument(Description="The type of tea")]
    public readonly TeaType TeaType;
    
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
```

Now that you've defined your commands structure,
all you have to do is let Quikline do the magic:

```csharp
var myCommand = Quik.Parse<MyCommand>();
```
