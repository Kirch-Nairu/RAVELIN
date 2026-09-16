using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ravelin.Domain.Authority;
using Ravelin.Domain.Incidents;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Requests;
using Ravelin.Domain.Resources;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Tests;

[TestClass]
public sealed class ResourceRequestTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 16, 11, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void RequestCreationCapturesOperationalNeed()
    {
        (Incident incident, OperationalPeriod period) = IncidentWithPeriod();
        AuthorityContext coordination = Authority(incident.AuthorityDomainId, Capability.ResourceCoordination);

        DomainResult<ResourceRequest> result = ResourceRequest.Create(
            ResourceRequestId.New(),
            incident,
            period.Id,
            new ResourceCapability("vehicle"),
            2,
            ResourceRequestPriority.High,
            coordination,
            At);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(2, result.Value?.RequestedCount);
        Assert.AreEqual(0, result.Value?.FulfilledCount);
        Assert.AreEqual(ResourceRequestStatus.Open, result.Value?.Status);
    }

    [TestMethod]
    public void RequestProgressesFromPartialToFullFulfillment()
    {
        (ResourceRequest request, Incident incident) = OpenRequest(2, "vehicle");
        AuthorityContext coordination = Authority(incident.AuthorityDomainId, Capability.ResourceCoordination);
        ResourceAllocation first = Allocate(incident.AuthorityDomainId, incident.Id, "vehicle");
        ResourceAllocation second = Allocate(incident.AuthorityDomainId, incident.Id, "vehicle");

        DomainResult partial = request.RecordFulfillment(first, coordination, request.Version, At.AddMinutes(1));
        DomainResult full = request.RecordFulfillment(second, coordination, request.Version, At.AddMinutes(2));

        Assert.IsTrue(partial.Succeeded);
        Assert.IsTrue(full.Succeeded);
        Assert.AreEqual(2, request.FulfilledCount);
        Assert.AreEqual(ResourceRequestStatus.Fulfilled, request.Status);
    }

    [TestMethod]
    public void OpenRequestCanBeCancelledExplicitly()
    {
        (ResourceRequest request, Incident incident) = OpenRequest(1, "vehicle");
        AuthorityContext coordination = Authority(incident.AuthorityDomainId, Capability.ResourceCoordination);

        DomainResult cancel = request.Cancel(coordination, request.Version, At.AddMinutes(1));

        Assert.IsTrue(cancel.Succeeded);
        Assert.AreEqual(ResourceRequestStatus.Cancelled, request.Status);
    }

    [TestMethod]
    public void FulfilledRequestCannotBeCancelled()
    {
        (ResourceRequest request, Incident incident) = OpenRequest(1, "vehicle");
        AuthorityContext coordination = Authority(incident.AuthorityDomainId, Capability.ResourceCoordination);
        request.RecordFulfillment(Allocate(incident.AuthorityDomainId, incident.Id, "vehicle"), coordination, request.Version, At.AddMinutes(1));

        DomainResult cancel = request.Cancel(coordination, request.Version, At.AddMinutes(2));

        Assert.IsFalse(cancel.Succeeded);
        Assert.AreEqual(DomainErrorCode.RequestAlreadyFulfilled, cancel.Error?.Code);
    }

    [TestMethod]
    public void AllocationFromAnotherIncidentCannotFulfillRequest()
    {
        (ResourceRequest request, Incident incident) = OpenRequest(1, "vehicle");
        AuthorityContext coordination = Authority(incident.AuthorityDomainId, Capability.ResourceCoordination);
        ResourceAllocation wrongIncident = Allocate(incident.AuthorityDomainId, IncidentId.New(), "vehicle");

        DomainResult result = request.RecordFulfillment(wrongIncident, coordination, request.Version, At.AddMinutes(1));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.WrongIncident, result.Error?.Code);
        Assert.AreEqual(ResourceRequestStatus.Open, request.Status);
    }

    [TestMethod]
    public void AllocationWithWrongCapabilityCannotFulfillRequest()
    {
        (ResourceRequest request, Incident incident) = OpenRequest(1, "vehicle");
        AuthorityContext coordination = Authority(incident.AuthorityDomainId, Capability.ResourceCoordination);
        ResourceAllocation wrongCapability = Allocate(incident.AuthorityDomainId, incident.Id, "radio");

        DomainResult result = request.RecordFulfillment(wrongCapability, coordination, request.Version, At.AddMinutes(1));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.InvalidInput, result.Error?.Code);
    }

    [TestMethod]
    public void CancelledRequestCannotAcceptLaterFulfillment()
    {
        (ResourceRequest request, Incident incident) = OpenRequest(1, "vehicle");
        AuthorityContext coordination = Authority(incident.AuthorityDomainId, Capability.ResourceCoordination);
        request.Cancel(coordination, request.Version, At.AddMinutes(1));

        DomainResult result = request.RecordFulfillment(
            Allocate(incident.AuthorityDomainId, incident.Id, "vehicle"),
            coordination,
            request.Version,
            At.AddMinutes(2));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.RequestClosed, result.Error?.Code);
    }

    private static (ResourceRequest Request, Incident Incident) OpenRequest(int count, string capability)
    {
        (Incident incident, OperationalPeriod period) = IncidentWithPeriod();
        AuthorityContext coordination = Authority(incident.AuthorityDomainId, Capability.ResourceCoordination);
        ResourceRequest request = ResourceRequest.Create(
            ResourceRequestId.New(), incident, period.Id, new ResourceCapability(capability), count,
            ResourceRequestPriority.Routine, coordination, At).Value!;
        return (request, incident);
    }

    private static (Incident Incident, OperationalPeriod Period) IncidentWithPeriod()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        AuthorityContext incidentAuthority = Authority(domainId, Capability.IncidentCommand);
        Incident incident = Incident.Open(IncidentId.New(), domainId, "Incident", incidentAuthority, At).Value!;
        OperationalPeriod period = incident.StartOperationalPeriod(
            OperationalPeriodId.New(), incidentAuthority, incident.Version, At.AddMinutes(1)).Value!;
        return (incident, period);
    }

    private static ResourceAllocation Allocate(AuthorityDomainId domainId, IncidentId incidentId, string capability)
    {
        DiscreteResource resource = DiscreteResource.Register(
            ResourceId.New(), domainId, new ResourceCapability(capability), "Resource",
            Authority(domainId, Capability.Administration), At).Value!;
        AuthorityContext custody = Authority(domainId, Capability.ResourceCustody);
        return resource.Allocate(
            AllocationId.New(),
            new AllocationTarget(domainId, incidentId, null, new CustodyHolder(custody.ActorId, null)),
            custody,
            resource.Version,
            At.AddMinutes(1)).Value!;
    }

    private static AuthorityContext Authority(AuthorityDomainId domainId, Capability capability) =>
        new(ActorId.New(), domainId, [capability], DeviceId.New());
}
