using Quikline.Attributes;
using Quikline.Parser;

var a = Quik.Parse<Args>(args);

Console.WriteLine("Hello, World!");

[Command(Version=true, Description = "A test CLI program.")]
public readonly struct Args
{
    
}