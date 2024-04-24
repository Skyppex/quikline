namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public abstract class Relation : Attribute
{
    public required string Name { get; init; }
}

public class ExclusiveRelation : Relation
{
    public bool Required { get; init; } = false;
    public required string[] Args { get; init; }
}

public class OneOrMoreRelation : Relation
{
    public required string[] Args { get; init; }
}

public class InclusiveRelation : Relation
{
    public bool Required { get; init; } = false;
    public required string[] Args { get; init; }
}

// public class OneWayRelation : Relation
// {
//     public bool Required { get; init; } = false;
//     public required string[] From { get; init; }
//     public required string[] To { get; init; }
// }
