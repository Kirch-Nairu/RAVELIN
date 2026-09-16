using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Resources;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Tests;

[TestClass]
public sealed class ResourceTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 16, 10, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void AvailableResourceCanBeAllocated()
    {
        (DiscreteResource resource, AuthorityContext custody) = RegisteredResource();
        AllocationId allocationId = AllocationId.New();

        DomainResult<ResourceAllocation> result = resource.Allocate(
            allocationId,
            Target(resource.AuthorityDomainId),
            custody,
            resource.Version,
            At.AddMinutes(1));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ResourceStatus.Allocated, resource.Status);
        Assert.AreEqual(allocationId, resource.CurrentAllocation?.Id);
        Assert.IsInstanceOfType<ResourceAllocatedEvent>(resource.PendingEvents[^1]);
    }

    [TestMethod]
    public void DoubleActiveAllocationIsRejectedWithoutVersionAdvance()
    {
        (DiscreteResource resource, AuthorityContext custody) = RegisteredResource();
        resource.Allocate(AllocationId.New(), Target(resource.AuthorityDomainId), custody, resource.Version, At.AddMinutes(1));
        long version = resource.Version.Value;

        DomainResult<ResourceAllocation> second = resource.Allocate(
            AllocationId.New(),
            Target(resource.AuthorityDomainId),
            custody,
            resource.Version,
            At.AddMinutes(2));

        Assert.IsFalse(second.Succeeded);
        Assert.AreEqual(DomainErrorCode.ResourceAlreadyAllocated, second.Error?.Code);
        Assert.AreEqual(version, resource.Version.Value);
    }

    [TestMethod]
    public void ExplicitReleaseMakesResourceEligibleAgain()
    {
        (DiscreteResource resource, AuthorityContext custody) = RegisteredResource();
        ResourceAllocation allocation = resource.Allocate(
            AllocationId.New(), Target(resource.AuthorityDomainId), custody, resource.Version, At.AddMinutes(1)).Value!;

        DomainResult<ResourceAllocation> release = resource.Release(
            allocation.Id,
            custody,
            resource.Version,
            At.AddMinutes(2));

        Assert.IsTrue(release.Succeeded);
        Assert.AreEqual(ResourceAllocationStatus.Released, release.Value?.Status);
        Assert.AreEqual(ResourceStatus.Available, resource.Status);
        Assert.IsNull(resource.CurrentAllocation);

        DomainResult<ResourceAllocation> next = resource.Allocate(
            AllocationId.New(), Target(resource.AuthorityDomainId), custody, resource.Version, At.AddMinutes(3));
        Assert.IsTrue(next.Succeeded);
    }

    [TestMethod]
    public void UnavailableResourceCannotBeAllocated()
    {
        (DiscreteResource resource, _) = RegisteredResource();
        AuthorityContext coordination = Authority(resource.AuthorityDomainId, Capability.ResourceCoordination);
        AuthorityContext custody = Authority(resource.AuthorityDomainId, Capability.ResourceCustody);
        Assert.IsTrue(resource.MarkUnavailable(coordination, resource.Version, At.AddMinutes(1)).Succeeded);

        DomainResult<ResourceAllocation> allocation = resource.Allocate(
            AllocationId.New(), Target(resource.AuthorityDomainId), custody, resource.Version, At.AddMinutes(2));

        Assert.IsFalse(allocation.Succeeded);
        Assert.AreEqual(DomainErrorCode.ResourceUnavailable, allocation.Error?.Code);
    }

    [TestMethod]
    public void CrossDomainAllocationTargetIsRejected()
    {
        (DiscreteResource resource, AuthorityContext custody) = RegisteredResource();

        DomainResult<ResourceAllocation> allocation = resource.Allocate(
            AllocationId.New(),
            Target(AuthorityDomainId.New()),
            custody,
            resource.Version,
            At.AddMinutes(1));

        Assert.IsFalse(allocation.Succeeded);
        Assert.AreEqual(DomainErrorCode.WrongAuthorityDomain, allocation.Error?.Code);
    }

    [TestMethod]
    public void CustodyTransferEndsPreviousClaimAndCreatesOneActiveClaim()
    {
        (DiscreteResource resource, AuthorityContext custody) = RegisteredResource();
        ResourceAllocation first = resource.Allocate(
            AllocationId.New(), Target(resource.AuthorityDomainId), custody, resource.Version, At.AddMinutes(1)).Value!;
        AllocationTarget nextTarget = new(
            resource.AuthorityDomainId,
            IncidentId.New(),
            AssignmentId.New(),
            new CustodyHolder(custody.ActorId, TeamId.New()));

        DomainResult<CustodyTransfer> transfer = resource.TransferCustody(
            first.Id,
            AllocationId.New(),
            nextTarget,
            custody,
            resource.Version,
            At.AddMinutes(2));

        Assert.IsTrue(transfer.Succeeded);
        Assert.AreEqual(ResourceAllocationStatus.Released, transfer.Value?.PreviousAllocation.Status);
        Assert.AreEqual(ResourceAllocationStatus.Active, transfer.Value?.CurrentAllocation.Status);
        Assert.AreEqual(transfer.Value?.CurrentAllocation.Id, resource.CurrentAllocation?.Id);
        Assert.IsInstanceOfType<ResourceCustodyTransferredEvent>(resource.PendingEvents[^1]);
    }

    [TestMethod]
    public void StaleReleasePreconditionCannotReleaseCurrentCustody()
    {
        (DiscreteResource resource, AuthorityContext custody) = RegisteredResource();
        ResourceAllocation allocation = resource.Allocate(
            AllocationId.New(), Target(resource.AuthorityDomainId), custody, resource.Version, At.AddMinutes(1)).Value!;

        DomainResult<ResourceAllocation> release = resource.Release(
            allocation.Id,
            custody,
            new AggregateVersion(resource.Version.Value - 1),
            At.AddMinutes(2));

        Assert.IsFalse(release.Succeeded);
        Assert.AreEqual(DomainErrorCode.StaleVersion, release.Error?.Code);
        Assert.AreEqual(ResourceStatus.Allocated, resource.Status);
        Assert.AreEqual(allocation.Id, resource.CurrentAllocation?.Id);
    }

    private static (DiscreteResource Resource, AuthorityContext Custody) RegisteredResource()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        AuthorityContext administration = Authority(domainId, Capability.Administration);
        DiscreteResource resource = DiscreteResource.Register(
            ResourceId.New(), domainId, new ResourceCapability("vehicle"), "Engine 1", administration, At).Value!;
        return (resource, Authority(domainId, Capability.ResourceCustody));
    }

    private static AllocationTarget Target(AuthorityDomainId domainId) =>
        new(domainId, IncidentId.New(), AssignmentId.New(), new CustodyHolder(ActorId.New(), TeamId.New()));

    private static AuthorityContext Authority(AuthorityDomainId domainId, Capability capability) =>
        new(ActorId.New(), domainId, [capability], DeviceId.New());
}
