using Ravelin.Domain.Primitives;
using Ravelin.Domain.Teams;

namespace Ravelin.Domain.Events;

public sealed record TeamAvailabilityChangedEvent(
    AuthorityDomainId DomainId,
    TeamId TeamId,
    TeamStatus Status,
    ActorId ByActorId,
    DateTimeOffset At,
    AggregateVersion Version)
    : DomainEvent(DomainId, ByActorId, At, Version);
