using Ravelin.Domain.Primitives;

namespace Ravelin.Domain.Events;

public abstract record DomainEvent(
    AuthorityDomainId AuthorityDomainId,
    ActorId ActorId,
    DateTimeOffset OccurredAt,
    AggregateVersion AggregateVersion)
{
    public DomainEventId EventId { get; } = DomainEventId.New();
}

public sealed record IncidentOpenedEvent(
    AuthorityDomainId DomainId,
    IncidentId IncidentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record IncidentClosedEvent(
    AuthorityDomainId DomainId,
    IncidentId IncidentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record OperationalPeriodOpenedEvent(
    AuthorityDomainId DomainId,
    IncidentId IncidentId,
    OperationalPeriodId OperationalPeriodId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record OperationalPeriodClosedEvent(
    AuthorityDomainId DomainId,
    IncidentId IncidentId,
    OperationalPeriodId OperationalPeriodId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record TeamRegisteredEvent(
    AuthorityDomainId DomainId,
    TeamId TeamId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record TeamCommittedEvent(
    AuthorityDomainId DomainId,
    TeamId TeamId,
    AssignmentId AssignmentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record TeamReleasedEvent(
    AuthorityDomainId DomainId,
    TeamId TeamId,
    AssignmentId AssignmentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record ResourceRegisteredEvent(
    AuthorityDomainId DomainId,
    ResourceId ResourceId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record ResourceAvailabilityChangedEvent(
    AuthorityDomainId DomainId,
    ResourceId ResourceId,
    string Status,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record ResourceAllocatedEvent(
    AuthorityDomainId DomainId,
    ResourceId ResourceId,
    AllocationId AllocationId,
    IncidentId IncidentId,
    AssignmentId? AssignmentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record ResourceReleasedEvent(
    AuthorityDomainId DomainId,
    ResourceId ResourceId,
    AllocationId AllocationId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record ResourceCustodyTransferredEvent(
    AuthorityDomainId DomainId,
    ResourceId ResourceId,
    AllocationId PreviousAllocationId,
    AllocationId NewAllocationId,
    IncidentId IncidentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record ResourceRequestedEvent(
    AuthorityDomainId DomainId,
    ResourceRequestId RequestId,
    IncidentId IncidentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record ResourceRequestFulfilledEvent(
    AuthorityDomainId DomainId,
    ResourceRequestId RequestId,
    AllocationId AllocationId,
    bool IsFullyFulfilled,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record ResourceRequestCancelledEvent(
    AuthorityDomainId DomainId,
    ResourceRequestId RequestId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record AssignmentPlannedEvent(
    AuthorityDomainId DomainId,
    AssignmentId AssignmentId,
    IncidentId IncidentId,
    TeamId TeamId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record AssignmentDispatchedEvent(
    AuthorityDomainId DomainId,
    AssignmentId AssignmentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record AssignmentAcknowledgedEvent(
    AuthorityDomainId DomainId,
    AssignmentId AssignmentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record AssignmentStartedEvent(
    AuthorityDomainId DomainId,
    AssignmentId AssignmentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record AssignmentCompletedEvent(
    AuthorityDomainId DomainId,
    AssignmentId AssignmentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record AssignmentCancelledEvent(
    AuthorityDomainId DomainId,
    AssignmentId AssignmentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);

public sealed record OperationalObservationRecordedEvent(
    AuthorityDomainId DomainId,
    ObservationId ObservationId,
    IncidentId IncidentId,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);
