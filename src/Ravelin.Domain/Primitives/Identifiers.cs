namespace Ravelin.Domain.Primitives;

public readonly record struct AuthorityDomainId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static AuthorityDomainId New() => new(Guid.CreateVersion7());
}

public readonly record struct IncidentId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static IncidentId New() => new(Guid.CreateVersion7());
}

public readonly record struct OperationalPeriodId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static OperationalPeriodId New() => new(Guid.CreateVersion7());
}

public readonly record struct TeamId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static TeamId New() => new(Guid.CreateVersion7());
}

public readonly record struct ResourceId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ResourceId New() => new(Guid.CreateVersion7());
}

public readonly record struct ResourceRequestId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ResourceRequestId New() => new(Guid.CreateVersion7());
}

public readonly record struct AssignmentId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static AssignmentId New() => new(Guid.CreateVersion7());
}

public readonly record struct AllocationId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static AllocationId New() => new(Guid.CreateVersion7());
}

public readonly record struct ObservationId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ObservationId New() => new(Guid.CreateVersion7());
}

public readonly record struct ActorId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ActorId New() => new(Guid.CreateVersion7());
}

public readonly record struct DeviceId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static DeviceId New() => new(Guid.CreateVersion7());
}

public readonly record struct CommandId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CommandId New() => new(Guid.CreateVersion7());
}

public readonly record struct DomainEventId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static DomainEventId New() => new(Guid.CreateVersion7());
}
