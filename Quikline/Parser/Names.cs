namespace Quikline.Parser;

internal readonly record struct Prefix(string Value)
{
    public int Length => Value.Length;
    public static Prefix Empty = new(string.Empty);

    public override string ToString() => Value;

    public static implicit operator string(Prefix prefix) => prefix.Value;
}

internal readonly record struct Name(string Value)
{
    public int Length => Value.Length;
    public static Name Empty = new(string.Empty);
    
    public override string ToString() => Value;
    
    public static implicit operator string(Name name) => name.Value;
}

internal readonly record struct Short(Prefix Prefix, Name Name)
{
    public static Short Empty = new(Prefix.Empty, Name.Empty);
    public override string ToString() => $"{Prefix}{Name}";
}

internal readonly record struct Long(Prefix Prefix, Name Name)
{
    public static Long Empty = new(Prefix.Empty, Name.Empty);
    public override string ToString() => $"{Prefix}{Name}";
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

internal static class Extensions
{
    public static Prefix? ToPrefix(this string? value) => value is null ? null : new Prefix(value);
    public static Prefix? ToPrefix(this char value) => value is '\0' ? null : new Prefix(value.ToString());
    public static Name? ToName(this string? value) => value is null ? null : new Name(value);
}