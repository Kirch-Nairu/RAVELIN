using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Incidents;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Tests;

[TestClass]
public sealed class IncidentTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void OpenIncidentCreatesAcceptedFactAndVersion()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        AuthorityContext authority = IncidentAuthority(domainId);

        DomainResult<Incident> result = Incident.Open(IncidentId.New(), domainId, "Warehouse fire", authority, At);

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(IncidentStatus.Open, result.Value.Status);
        Assert.AreEqual(1L, result.Value.Version.Value);
        Assert.IsInstanceOfType<IncidentOpenedEvent>(result.Value.PendingEvents.Single());
    }

    [TestMethod]
    public void IncidentAllowsOnlyOneCurrentOperationalPeriod()
    {
        Incident incident = OpenIncident();
        AuthorityContext authority = IncidentAuthority(incident.AuthorityDomainId);
        DomainResult<OperationalPeriod> first = incident.StartOperationalPeriod(OperationalPeriodId.New(), authority, incident.Version, At.AddMinutes(1));

        DomainResult<OperationalPeriod> second = incident.StartOperationalPeriod(OperationalPeriodId.New(), authority, incident.Version, At.AddMinutes(2));

        Assert.IsTrue(first.Succeeded);
        Assert.IsFalse(second.Succeeded);
        Assert.AreEqual(DomainErrorCode.OperationalPeriodConflict, second.Error?.Code);
    }

    [TestMethod]
    public void ClosedIncidentRejectsNewOperationalMutation()
    {
        Incident incident = OpenIncident();
        AuthorityContext authority = IncidentAuthority(incident.AuthorityDomainId);
        DomainResult close = incident.Close(new IncidentCommitments(0, 0), authority, incident.Version, At.AddMinutes(3));

        DomainResult<OperationalPeriod> period = incident.StartOperationalPeriod(OperationalPeriodId.New(), authority, incident.Version, At.AddMinutes(4));

        Assert.IsTrue(close.Succeeded);
        Assert.IsFalse(period.Succeeded);
        Assert.AreEqual(DomainErrorCode.ClosedIncident, period.Error?.Code);
    }

    [TestMethod]
    public void IncidentCannotCloseWithCurrentOperationalPeriod()
    {
        Incident incident = OpenIncident();
        AuthorityContext authority = IncidentAuthority(incident.AuthorityDomainId);
        incident.StartOperationalPeriod(OperationalPeriodId.New(), authority, incident.Version, At.AddMinutes(1));

        DomainResult close = incident.Close(new IncidentCommitments(0, 0), authority, incident.Version, At.AddMinutes(2));

        Assert.IsFalse(close.Succeeded);
        Assert.AreEqual(DomainErrorCode.OperationalPeriodConflict, close.Error?.Code);
    }

    [TestMethod]
    public void IncidentCannotCloseWithActiveCommitments()
    {
        Incident incident = OpenIncident();
        AuthorityContext authority = IncidentAuthority(incident.AuthorityDomainId);

        DomainResult close = incident.Close(new IncidentCommitments(1, 1), authority, incident.Version, At.AddMinutes(2));

        Assert.IsFalse(close.Succeeded);
        Assert.AreEqual(DomainErrorCode.ActiveCommitmentsRemain, close.Error?.Code);
        Assert.AreEqual(IncidentStatus.Open, incident.Status);
    }

    [TestMethod]
    public void StaleVersionRejectsMutationWithoutEventOrVersionAdvance()
    {
        Incident incident = OpenIncident();
        AuthorityContext authority = IncidentAuthority(incident.AuthorityDomainId);
        int eventCount = incident.PendingEvents.Count;

        DomainResult<OperationalPeriod> result = incident.StartOperationalPeriod(
            OperationalPeriodId.New(),
            authority,
            AggregateVersion.Initial,
            At.AddMinutes(1));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.StaleVersion, result.Error?.Code);
        Assert.AreEqual(1L, incident.Version.Value);
        Assert.AreEqual(eventCount, incident.PendingEvents.Count);
    }

    [TestMethod]
    public void ClosingPeriodAllowsNextPeriodAndThenIncidentClose()
    {
        Incident incident = OpenIncident();
        AuthorityContext authority = IncidentAuthority(incident.AuthorityDomainId);
        OperationalPeriodId firstId = OperationalPeriodId.New();
        incident.StartOperationalPeriod(firstId, authority, incident.Version, At.AddMinutes(1));
        DomainResult closePeriod = incident.CloseOperationalPeriod(firstId, authority, incident.Version, At.AddMinutes(2));
        DomainResult<OperationalPeriod> second = incident.StartOperationalPeriod(OperationalPeriodId.New(), authority, incident.Version, At.AddMinutes(3));
        Assert.IsTrue(second.Succeeded);
        DomainResult closeSecond = incident.CloseOperationalPeriod(second.Value!.Id, authority, incident.Version, At.AddMinutes(4));

        DomainResult closeIncident = incident.Close(new IncidentCommitments(0, 0), authority, incident.Version, At.AddMinutes(5));

        Assert.IsTrue(closePeriod.Succeeded);
        Assert.IsTrue(closeSecond.Succeeded);
        Assert.IsTrue(closeIncident.Succeeded);
        Assert.AreEqual(IncidentStatus.Closed, incident.Status);
        Assert.IsInstanceOfType<IncidentClosedEvent>(incident.PendingEvents[^1]);
    }

    private static Incident OpenIncident()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        DomainResult<Incident> result = Incident.Open(IncidentId.New(), domainId, "Incident", IncidentAuthority(domainId), At);
        Assert.IsTrue(result.Succeeded);
        return result.Value!;
    }

    private static AuthorityContext IncidentAuthority(AuthorityDomainId domainId) =>
        new(ActorId.New(), domainId, [Capability.IncidentCommand], DeviceId.New());
}
