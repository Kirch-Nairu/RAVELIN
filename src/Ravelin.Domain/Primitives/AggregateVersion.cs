namespace Ravelin.Domain.Primitives;

public readonly record struct AggregateVersion(long Value)
{
    public static AggregateVersion Initial => new(0);

    public AggregateVersion Next() => new(checked(Value + 1));
}
