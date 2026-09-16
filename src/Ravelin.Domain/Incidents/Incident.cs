using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Model;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Incidents;

public enum IncidentStatus
{
    Open,
    Closed,
}

public enum OperationalPeriodStatus
{
    Open,
    Closed,
}

public sealed class OperationalPeriod
{
    internal OperationalPeriod(OperationalPeriodId id, IncidentId incidentId, DateTimeOffset openedAt)
    {
        Id = id;
        IncidentId = incidentId;
        OpenedAt = openedAt.ToUniversalTime();
        Status = OperationalPeriodStatus.Open;
    }

    public OperationalPeriodId Id { get; }

    public IncidentId IncidentId { get; }

    public OperationalPeriodStatus Status { get; private set; }

    public DateTimeOffset OpenedAt { get; }

    public DateTimeOffset? ClosedAt { get; private set; }

    internal void Close(DateTimeOffset closedAt)
    {
        Status = OperationalPeriodStatus.Closed;
        ClosedAt = closedAt.ToUniversalTime();
    }
}

public readonly record struct IncidentCommitments(int ActiveAssignments, int ActiveAllocations)
{
    public bool HasActiveCommitments => ActiveAssignments > 0 || ActiveAllocations > 0;

    public bool IsValid => ActiveAssignments >= 0 && ActiveAllocations >= 0;
}

public sealed class Incident : AggregateRoot
{
    private readonly List<OperationalPeriod> _operationalPeriods = [];

    private Incident(IncidentId id, AuthorityDomainId authorityDomainId, string name)
    {
        Id = id;
        AuthorityDomainId = authorityDomainId;
        Name = name;
        Status = IncidentStatus.Open;
    }

    public IncidentId Id { get; }

    public AuthorityDomainId AuthorityDomainId { get; }

    public string Name { get; }

    public IncidentStatus Status { get; private set; }

    public IReadOnlyList<OperationalPeriod> OperationalPeriods => _operationalPeriods;

    public OperationalPeriodId? CurrentOperationalPeriodId =>
        _operationalPeriods.Find(period => period.Status == OperationalPeriodStatus.Open)?.Id;

    public static DomainResult<Incident> Open(
        IncidentId id,
        AuthorityDomainId authorityDomainId,
        string name,
        AuthorityContext authority,
        DateTimeOffset openedAt)
    {
        if (id.IsEmpty || authorityDomainId.IsEmpty || string.IsNullOrWhiteSpace(name))
        {
            return DomainResult.Failure<Incident>(
                DomainErrorCode.InvalidInput,
                "Incident requires non-empty identity, authority domain, and name.");
        }

        DomainResult authorization = authority.Authorize(Capability.IncidentCommand, authorityDomainId, id);
        if (!authorization.Succeeded)
        {
            return DomainResult.Failure<Incident>(authorization.Error!.Code, authorization.Error.Message);
        }

        Incident incident = new(id, authorityDomainId, name.Trim());
        incident.RecordEvent(version => new IncidentOpenedEvent(
            authorityDomainId,
            id,
            authority.ActorId,
            openedAt.ToUniversalTime(),
            version));

        return DomainResult.Success(incident);
    }

    public DomainResult<OperationalPeriod> StartOperationalPeriod(
        OperationalPeriodId periodId,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset openedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return DomainResult.Failure<OperationalPeriod>(precondition.Error!.Code, precondition.Error.Message);
        }

        DomainResult authorization = authority.Authorize(Capability.IncidentCommand, AuthorityDomainId, Id);
        if (!authorization.Succeeded)
        {
            return DomainResult.Failure<OperationalPeriod>(authorization.Error!.Code, authorization.Error.Message);
        }

        if (Status == IncidentStatus.Closed)
        {
            return DomainResult.Failure<OperationalPeriod>(DomainErrorCode.ClosedIncident, "Closed incidents reject new operational periods.");
        }

        if (periodId.IsEmpty)
        {
            return DomainResult.Failure<OperationalPeriod>(DomainErrorCode.InvalidInput, "Operational period identifier is required.");
        }

        if (CurrentOperationalPeriodId is not null)
        {
            return DomainResult.Failure<OperationalPeriod>(
                DomainErrorCode.OperationalPeriodConflict,
                "An operational period is already current for this incident.");
        }

        if (_operationalPeriods.Exists(period => period.Id == periodId))
        {
            return DomainResult.Failure<OperationalPeriod>(DomainErrorCode.InvalidInput, "Operational period identifier already exists in this incident.");
        }

        OperationalPeriod period = new(periodId, Id, openedAt);
        _operationalPeriods.Add(period);
        RecordEvent(version => new OperationalPeriodOpenedEvent(
            AuthorityDomainId,
            Id,
            periodId,
            authority.ActorId,
            openedAt.ToUniversalTime(),
            version));

        return DomainResult.Success(period);
    }

    public DomainResult CloseOperationalPeriod(
        OperationalPeriodId periodId,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset closedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(Capability.IncidentCommand, AuthorityDomainId, Id);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        OperationalPeriod? current = _operationalPeriods.Find(period => period.Status == OperationalPeriodStatus.Open);
        if (current is null || current.Id != periodId)
        {
            return DomainResult.Failure(
                DomainErrorCode.OperationalPeriodConflict,
                "Only the current operational period can be closed.");
        }

        current.Close(closedAt);
        RecordEvent(version => new OperationalPeriodClosedEvent(
            AuthorityDomainId,
            Id,
            periodId,
            authority.ActorId,
            closedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public DomainResult Close(
        IncidentCommitments commitments,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset closedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(Capability.IncidentCommand, AuthorityDomainId, Id);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status == IncidentStatus.Closed)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Incident is already closed.");
        }

        if (!commitments.IsValid)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidInput, "Commitment counts cannot be negative.");
        }

        if (CurrentOperationalPeriodId is not null)
        {
            return DomainResult.Failure(
                DomainErrorCode.OperationalPeriodConflict,
                "Current operational period must be closed before closing the incident.");
        }

        if (commitments.HasActiveCommitments)
        {
            return DomainResult.Failure(
                DomainErrorCode.ActiveCommitmentsRemain,
                "Incident cannot close while active assignments or resource allocations remain.");
        }

        Status = IncidentStatus.Closed;
        RecordEvent(version => new IncidentClosedEvent(
            AuthorityDomainId,
            Id,
            authority.ActorId,
            closedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public bool HasOperationalPeriod(OperationalPeriodId periodId) =>
        _operationalPeriods.Exists(period => period.Id == periodId);

    public bool IsOperationalPeriodOpen(OperationalPeriodId periodId) =>
        _operationalPeriods.Exists(period => period.Id == periodId && period.Status == OperationalPeriodStatus.Open);
}
