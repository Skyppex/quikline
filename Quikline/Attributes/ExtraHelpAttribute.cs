using Quikline.Parser;

namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Struct)]
public class ExtraHelpAttribute : Attribute
{
    public required string Header { get; init; }
    public required string Text { get; init; }
    
    public void PrintUsage()
    {
        using (new Help.Color(ConsoleColor.DarkGreen))
            Console.WriteLine(Header);

        var chars = Text.ToCharArray().GetEnumerator();

        while (chars.MoveNext())
        {
            var c = (char)chars.Current;
            
            if (c is '\n')
            {
                Console.Write("\n  ");
                continue;
            }

            if (c is '<')
            {
                
            }
        }
    }
}

public static class ColoredText
{
    public static ConsoleColor? GetColor(IEnumerator<char>)
    {
        
    }
}