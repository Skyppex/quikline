namespace Quikline.Attributes;

[AttributeUsage(validOn: AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public abstract class RelationAttribute(params string[] args) : Attribute
{
    public string[] Args { get; init; } = args;
}

public class ExclusiveRelationAttribute(params string[] args) : RelationAttribute(args)
{
    public bool Required { get; init; } = false;
}

public class OneOrMoreRelationAttribute(params string[] args) : RelationAttribute(args);

public class InclusiveRelationAttribute(params string[] args) : RelationAttribute(args)
{
    public bool Required { get; init; } = false;
}

// public class OneWayRelation : Relation
// {
//     public bool Required { get; init; } = false;
//     public required string[] From { get; init; }
//     public required string[] To { get; init; }
// }
