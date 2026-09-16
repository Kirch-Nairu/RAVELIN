using Ravelin.Domain.Events;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Model;

public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _pendingEvents = [];

    public AggregateVersion Version { get; private set; } = AggregateVersion.Initial;

    public IReadOnlyList<DomainEvent> PendingEvents => _pendingEvents;

    public void ClearPendingEvents() => _pendingEvents.Clear();

    protected DomainResult RequireVersion(AggregateVersion expectedVersion)
    {
        if (expectedVersion != Version)
        {
            return DomainResult.Failure(
                DomainErrorCode.StaleVersion,
                $"Expected aggregate version {expectedVersion.Value}, but authoritative version is {Version.Value}.");
        }

        return DomainResult.Success();
    }

    protected void RecordEvent(Func<AggregateVersion, DomainEvent> eventFactory)
    {
        ArgumentNullException.ThrowIfNull(eventFactory);

        AggregateVersion nextVersion = Version.Next();
        _pendingEvents.Add(eventFactory(nextVersion));
        Version = nextVersion;
    }
}
