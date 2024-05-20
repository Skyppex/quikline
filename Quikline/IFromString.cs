namespace Quikline;

public interface IFromString<T> where T : struct, IFromString<T>
{
    public static abstract (T?, string?) FromString(string value);
}
