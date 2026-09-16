using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Model;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;
using AuthorityCapability = Ravelin.Domain.Authority.Capability;

namespace Ravelin.Domain.Resources;

public enum ResourceStatus
{
    Available,
    Allocated,
    Unavailable,
}

public enum ResourceAllocationStatus
{
    Active,
    Released,
}

public readonly record struct ResourceCapability(string Code);

public readonly record struct CustodyHolder(ActorId ActorId, TeamId? TeamId);

public readonly record struct AllocationTarget(
    AuthorityDomainId AuthorityDomainId,
    IncidentId IncidentId,
    AssignmentId? AssignmentId,
    CustodyHolder CustodyHolder);

public sealed record ResourceAllocation(
    AllocationId Id,
    ResourceId ResourceId,
    ResourceCapability Capability,
    AuthorityDomainId AuthorityDomainId,
    IncidentId IncidentId,
    AssignmentId? AssignmentId,
    CustodyHolder CustodyHolder,
    DateTimeOffset EffectiveAt,
    ResourceAllocationStatus Status,
    DateTimeOffset? ReleasedAt,
    ActorId? ReleasedByActorId)
{
    public bool IsActive => Status == ResourceAllocationStatus.Active;

    internal ResourceAllocation Release(ActorId actorId, DateTimeOffset releasedAt) =>
        this with
        {
            Status = ResourceAllocationStatus.Released,
            ReleasedAt = releasedAt.ToUniversalTime(),
            ReleasedByActorId = actorId,
        };
}

public sealed record CustodyTransfer(ResourceAllocation PreviousAllocation, ResourceAllocation CurrentAllocation);

public sealed class DiscreteResource : AggregateRoot
{
    private DiscreteResource(
        ResourceId id,
        AuthorityDomainId authorityDomainId,
        ResourceCapability capability,
        string name)
    {
        Id = id;
        AuthorityDomainId = authorityDomainId;
        Capability = capability;
        Name = name;
        Status = ResourceStatus.Available;
    }

    public ResourceId Id { get; }

    public AuthorityDomainId AuthorityDomainId { get; }

    public ResourceCapability Capability { get; }

    public string Name { get; }

    public ResourceStatus Status { get; private set; }

    public ResourceAllocation? CurrentAllocation { get; private set; }

    public static DomainResult<DiscreteResource> Register(
        ResourceId id,
        AuthorityDomainId authorityDomainId,
        ResourceCapability capability,
        string name,
        AuthorityContext authority,
        DateTimeOffset registeredAt)
    {
        if (id.IsEmpty || authorityDomainId.IsEmpty || string.IsNullOrWhiteSpace(capability.Code) || string.IsNullOrWhiteSpace(name))
        {
            return DomainResult.Failure<DiscreteResource>(
                DomainErrorCode.InvalidInput,
                "Discrete resource requires identity, authority domain, capability, and name.");
        }

        DomainResult authorization = authority.Authorize(AuthorityCapability.Administration, authorityDomainId, resourceId: id);
        if (!authorization.Succeeded)
        {
            return DomainResult.Failure<DiscreteResource>(authorization.Error!.Code, authorization.Error.Message);
        }

        DiscreteResource resource = new(id, authorityDomainId, new ResourceCapability(capability.Code.Trim()), name.Trim());
        resource.RecordEvent(version => new ResourceRegisteredEvent(
            authorityDomainId,
            id,
            authority.ActorId,
            registeredAt.ToUniversalTime(),
            version));

        return DomainResult.Success(resource);
    }

    public DomainResult<ResourceAllocation> Allocate(
        AllocationId allocationId,
        AllocationTarget target,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset effectiveAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return DomainResult.Failure<ResourceAllocation>(precondition.Error!.Code, precondition.Error.Message);
        }

        DomainResult authorization = authority.Authorize(
            AuthorityCapability.ResourceCustody,
            AuthorityDomainId,
            target.IncidentId,
            Id,
            target.CustodyHolder.TeamId);
        if (!authorization.Succeeded)
        {
            return DomainResult.Failure<ResourceAllocation>(authorization.Error!.Code, authorization.Error.Message);
        }

        if (allocationId.IsEmpty || target.IncidentId.IsEmpty || target.CustodyHolder.ActorId.IsEmpty)
        {
            return DomainResult.Failure<ResourceAllocation>(DomainErrorCode.InvalidInput, "Allocation identity, incident, and custodian actor are required.");
        }

        if (target.AuthorityDomainId != AuthorityDomainId)
        {
            return DomainResult.Failure<ResourceAllocation>(DomainErrorCode.WrongAuthorityDomain, "Allocation target belongs to another authority domain.");
        }

        if (Status == ResourceStatus.Unavailable)
        {
            return DomainResult.Failure<ResourceAllocation>(DomainErrorCode.ResourceUnavailable, "Unavailable resource cannot be allocated.");
        }

        if (CurrentAllocation is not null || Status == ResourceStatus.Allocated)
        {
            return DomainResult.Failure<ResourceAllocation>(DomainErrorCode.ResourceAlreadyAllocated, "Resource already has authoritative active custody.");
        }

        ResourceAllocation allocation = new(
            allocationId,
            Id,
            Capability,
            AuthorityDomainId,
            target.IncidentId,
            target.AssignmentId,
            target.CustodyHolder,
            effectiveAt.ToUniversalTime(),
            ResourceAllocationStatus.Active,
            null,
            null);

        CurrentAllocation = allocation;
        Status = ResourceStatus.Allocated;
        RecordEvent(version => new ResourceAllocatedEvent(
            AuthorityDomainId,
            Id,
            allocationId,
            target.IncidentId,
            target.AssignmentId,
            authority.ActorId,
            effectiveAt.ToUniversalTime(),
            version));

        return DomainResult.Success(allocation);
    }

    public DomainResult<ResourceAllocation> Release(
        AllocationId allocationId,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset releasedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return DomainResult.Failure<ResourceAllocation>(precondition.Error!.Code, precondition.Error.Message);
        }

        if (CurrentAllocation is null || CurrentAllocation.Id != allocationId)
        {
            return DomainResult.Failure<ResourceAllocation>(DomainErrorCode.AllocationMismatch, "Specified allocation is not the active custody record.");
        }

        DomainResult authorization = authority.Authorize(
            AuthorityCapability.ResourceCustody,
            AuthorityDomainId,
            CurrentAllocation.IncidentId,
            Id,
            CurrentAllocation.CustodyHolder.TeamId);
        if (!authorization.Succeeded)
        {
            return DomainResult.Failure<ResourceAllocation>(authorization.Error!.Code, authorization.Error.Message);
        }

        ResourceAllocation released = CurrentAllocation.Release(authority.ActorId, releasedAt);
        CurrentAllocation = null;
        Status = ResourceStatus.Available;
        RecordEvent(version => new ResourceReleasedEvent(
            AuthorityDomainId,
            Id,
            allocationId,
            authority.ActorId,
            releasedAt.ToUniversalTime(),
            version));

        return DomainResult.Success(released);
    }

    public DomainResult<CustodyTransfer> TransferCustody(
        AllocationId currentAllocationId,
        AllocationId newAllocationId,
        AllocationTarget newTarget,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset transferredAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return DomainResult.Failure<CustodyTransfer>(precondition.Error!.Code, precondition.Error.Message);
        }

        if (CurrentAllocation is null || CurrentAllocation.Id != currentAllocationId)
        {
            return DomainResult.Failure<CustodyTransfer>(DomainErrorCode.AllocationMismatch, "Specified allocation is not the active custody record.");
        }

        DomainResult authorization = authority.Authorize(
            AuthorityCapability.ResourceCustody,
            AuthorityDomainId,
            newTarget.IncidentId,
            Id,
            newTarget.CustodyHolder.TeamId);
        if (!authorization.Succeeded)
        {
            return DomainResult.Failure<CustodyTransfer>(authorization.Error!.Code, authorization.Error.Message);
        }

        if (newAllocationId.IsEmpty || newTarget.AuthorityDomainId != AuthorityDomainId || newTarget.IncidentId.IsEmpty || newTarget.CustodyHolder.ActorId.IsEmpty)
        {
            return DomainResult.Failure<CustodyTransfer>(DomainErrorCode.InvalidInput, "New custody target is invalid or belongs to another authority domain.");
        }

        ResourceAllocation previous = CurrentAllocation.Release(authority.ActorId, transferredAt);
        ResourceAllocation current = new(
            newAllocationId,
            Id,
            Capability,
            AuthorityDomainId,
            newTarget.IncidentId,
            newTarget.AssignmentId,
            newTarget.CustodyHolder,
            transferredAt.ToUniversalTime(),
            ResourceAllocationStatus.Active,
            null,
            null);

        CurrentAllocation = current;
        RecordEvent(version => new ResourceCustodyTransferredEvent(
            AuthorityDomainId,
            Id,
            previous.Id,
            current.Id,
            current.IncidentId,
            authority.ActorId,
            transferredAt.ToUniversalTime(),
            version));

        return DomainResult.Success(new CustodyTransfer(previous, current));
    }

    public DomainResult MarkUnavailable(
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset changedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(AuthorityCapability.ResourceCoordination, AuthorityDomainId, resourceId: Id);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (CurrentAllocation is not null)
        {
            return DomainResult.Failure(DomainErrorCode.ResourceAlreadyAllocated, "Allocated resource requires explicit custody release before becoming unavailable.");
        }

        if (Status == ResourceStatus.Unavailable)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Resource is already unavailable.");
        }

        Status = ResourceStatus.Unavailable;
        RecordEvent(version => new ResourceAvailabilityChangedEvent(
            AuthorityDomainId,
            Id,
            ResourceStatus.Unavailable.ToString(),
            authority.ActorId,
            changedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public DomainResult RestoreAvailable(
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset changedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(AuthorityCapability.ResourceCoordination, AuthorityDomainId, resourceId: Id);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status != ResourceStatus.Unavailable)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Only an unavailable resource can be restored to available.");
        }

        Status = ResourceStatus.Available;
        RecordEvent(version => new ResourceAvailabilityChangedEvent(
            AuthorityDomainId,
            Id,
            ResourceStatus.Available.ToString(),
            authority.ActorId,
            changedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }
}
