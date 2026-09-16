using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Incidents;
using Ravelin.Domain.Model;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Resources;
using Ravelin.Domain.Results;
using AuthorityCapability = Ravelin.Domain.Authority.Capability;

namespace Ravelin.Domain.Requests;

public enum ResourceRequestPriority
{
    Routine,
    High,
    Immediate,
}

public enum ResourceRequestStatus
{
    Open,
    PartiallyFulfilled,
    Fulfilled,
    Cancelled,
}

public sealed class ResourceRequest : AggregateRoot
{
    private readonly HashSet<AllocationId> _fulfilledAllocationIds = [];

    private ResourceRequest(
        ResourceRequestId id,
        AuthorityDomainId authorityDomainId,
        IncidentId incidentId,
        OperationalPeriodId operationalPeriodId,
        ResourceCapability capability,
        int requestedCount,
        ResourceRequestPriority priority)
    {
        Id = id;
        AuthorityDomainId = authorityDomainId;
        IncidentId = incidentId;
        OperationalPeriodId = operationalPeriodId;
        Capability = capability;
        RequestedCount = requestedCount;
        Priority = priority;
        Status = ResourceRequestStatus.Open;
    }

    public ResourceRequestId Id { get; }

    public AuthorityDomainId AuthorityDomainId { get; }

    public IncidentId IncidentId { get; }

    public OperationalPeriodId OperationalPeriodId { get; }

    public ResourceCapability Capability { get; }

    public int RequestedCount { get; }

    public int FulfilledCount => _fulfilledAllocationIds.Count;

    public ResourceRequestPriority Priority { get; }

    public ResourceRequestStatus Status { get; private set; }

    public static DomainResult<ResourceRequest> Create(
        ResourceRequestId id,
        Incident incident,
        OperationalPeriodId operationalPeriodId,
        ResourceCapability capability,
        int requestedCount,
        ResourceRequestPriority priority,
        AuthorityContext authority,
        DateTimeOffset requestedAt)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (id.IsEmpty || string.IsNullOrWhiteSpace(capability.Code) || requestedCount <= 0)
        {
            return DomainResult<ResourceRequest>.Failure(
                DomainErrorCode.InvalidInput,
                "Resource request requires identity, capability, and positive requested count.");
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            return DomainResult<ResourceRequest>.Failure(DomainErrorCode.ClosedIncident, "Closed incident rejects new resource requests.");
        }

        if (!incident.IsOperationalPeriodOpen(operationalPeriodId))
        {
            return DomainResult<ResourceRequest>.Failure(DomainErrorCode.OperationalPeriodConflict, "Resource request requires the current open operational period.");
        }

        DomainResult authorization = authority.Authorize(AuthorityCapability.ResourceCoordination, incident.AuthorityDomainId, incident.Id);
        if (!authorization.Succeeded)
        {
            return DomainResult<ResourceRequest>.Failure(authorization.Error!.Code, authorization.Error.Message);
        }

        ResourceRequest request = new(
            id,
            incident.AuthorityDomainId,
            incident.Id,
            operationalPeriodId,
            new ResourceCapability(capability.Code.Trim()),
            requestedCount,
            priority);

        request.RecordEvent(version => new ResourceRequestedEvent(
            request.AuthorityDomainId,
            request.Id,
            request.IncidentId,
            authority.ActorId,
            requestedAt.ToUniversalTime(),
            version));

        return DomainResult<ResourceRequest>.Success(request);
    }

    public DomainResult RecordFulfillment(
        ResourceAllocation allocation,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset fulfilledAt)
    {
        ArgumentNullException.ThrowIfNull(allocation);

        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(
            AuthorityCapability.ResourceCoordination,
            AuthorityDomainId,
            IncidentId,
            allocation.ResourceId,
            allocation.CustodyHolder.TeamId);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status == ResourceRequestStatus.Cancelled)
        {
            return DomainResult.Failure(DomainErrorCode.RequestClosed, "Cancelled resource request cannot be fulfilled.");
        }

        if (Status == ResourceRequestStatus.Fulfilled)
        {
            return DomainResult.Failure(DomainErrorCode.RequestAlreadyFulfilled, "Resource request is already fully fulfilled.");
        }

        if (!allocation.IsActive || allocation.AuthorityDomainId != AuthorityDomainId || allocation.IncidentId != IncidentId)
        {
            return DomainResult.Failure(DomainErrorCode.WrongIncident, "Allocation must be active and belong to the same incident and authority domain.");
        }

        if (!string.Equals(allocation.Capability.Code, Capability.Code, StringComparison.OrdinalIgnoreCase))
        {
            return DomainResult.Failure(DomainErrorCode.InvalidInput, "Allocation capability does not satisfy this resource request.");
        }

        if (!_fulfilledAllocationIds.Add(allocation.Id))
        {
            return DomainResult.Failure(DomainErrorCode.InvalidInput, "Allocation is already linked to this request.");
        }

        Status = FulfilledCount == RequestedCount
            ? ResourceRequestStatus.Fulfilled
            : ResourceRequestStatus.PartiallyFulfilled;

        RecordEvent(version => new ResourceRequestFulfilledEvent(
            AuthorityDomainId,
            Id,
            allocation.Id,
            Status == ResourceRequestStatus.Fulfilled,
            authority.ActorId,
            fulfilledAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public DomainResult Cancel(
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset cancelledAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(AuthorityCapability.ResourceCoordination, AuthorityDomainId, IncidentId);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status == ResourceRequestStatus.Fulfilled)
        {
            return DomainResult.Failure(DomainErrorCode.RequestAlreadyFulfilled, "Fulfilled request cannot be cancelled.");
        }

        if (Status == ResourceRequestStatus.Cancelled)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Resource request is already cancelled.");
        }

        Status = ResourceRequestStatus.Cancelled;
        RecordEvent(version => new ResourceRequestCancelledEvent(
            AuthorityDomainId,
            Id,
            authority.ActorId,
            cancelledAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }
}
