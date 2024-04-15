namespace Quikline.Parser;

internal readonly record struct Prefix(string Value)
{
    public int Length => Value.Length;
    public static Prefix Empty = new(string.Empty);
    
    public static implicit operator string(Prefix prefix) => prefix.Value;
}

internal readonly record struct Name(string Value)
{
    public int Length => Value.Length;
    public static Name Empty = new(string.Empty);
    
    public static implicit operator string(Name name) => name.Value;
}

internal readonly record struct Short(Prefix Prefix, Name Name)
{
    public static Short Empty = new(Prefix.Empty, Name.Empty);
}

internal readonly record struct Long(Prefix Prefix, Name Name)
{
    public static Long Empty = new(Prefix.Empty, Name.Empty);
}

internal sealed class ShortOptionEqualityComparer : IEqualityComparer<Option>
{
    public bool Equals(Option x, Option y) => Nullable.Equals(x.Short, y.Short);
    public int GetHashCode(Option obj) => obj.Short.GetHashCode();
}

internal sealed class LongOptionEqualityComparer : IEqualityComparer<Option>
{
    public bool Equals(Option x, Option y) => x.Long.Equals(y.Long);
    public int GetHashCode(Option obj) => obj.Long.GetHashCode();
}