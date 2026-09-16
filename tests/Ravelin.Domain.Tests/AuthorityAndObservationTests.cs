using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Observations;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Tests;

[TestClass]
public sealed class AuthorityAndObservationTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void MissingCapabilityIsRejected()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        AuthorityContext authority = new(ActorId.New(), domainId, [Capability.FieldReporting]);

        DomainResult result = authority.Authorize(Capability.ResourceCustody, domainId);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.InsufficientCapability, result.Error?.Code);
    }

    [TestMethod]
    public void WrongAuthorityDomainIsRejectedBeforeCapabilityUse()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        AuthorityContext authority = new(ActorId.New(), domainId, [Capability.IncidentCommand]);

        DomainResult result = authority.Authorize(Capability.IncidentCommand, AuthorityDomainId.New());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.WrongAuthorityDomain, result.Error?.Code);
    }

    [TestMethod]
    public void DelegatedIncidentScopeAllowsOnlyNamedIncident()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        IncidentId allowed = IncidentId.New();
        IncidentId denied = IncidentId.New();
        AuthorityContext authority = new(
            ActorId.New(),
            domainId,
            [Capability.FieldReporting],
            DeviceId.New(),
            AuthorityMode.DelegatedOffline,
            [allowed]);

        DomainResult allowedResult = authority.Authorize(Capability.FieldReporting, domainId, allowed);
        DomainResult deniedResult = authority.Authorize(Capability.FieldReporting, domainId, denied);

        Assert.IsTrue(allowedResult.Succeeded);
        Assert.IsFalse(deniedResult.Succeeded);
        Assert.AreEqual(DomainErrorCode.ScopeViolation, deniedResult.Error?.Code);
    }

    [TestMethod]
    public void OperationalObservationPreservesActorDeviceCaptureTimeAndFact()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        IncidentId incidentId = IncidentId.New();
        ActorId actorId = ActorId.New();
        DeviceId deviceId = DeviceId.New();
        AuthorityContext authority = new(actorId, domainId, [Capability.FieldReporting], deviceId);
        DateTimeOffset captured = new(2026, 9, 16, 20, 0, 0, TimeSpan.FromHours(8));

        DomainResult<OperationalObservation> result = OperationalObservation.Record(
            ObservationId.New(),
            domainId,
            incidentId,
            AssignmentId.New(),
            ObservationKind.Situation,
            "Road access remains clear.",
            captured,
            authority,
            At);

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(actorId, result.Value.ActorId);
        Assert.AreEqual(deviceId, result.Value.DeviceId);
        Assert.AreEqual(captured.ToUniversalTime(), result.Value.CapturedAt);
        Assert.AreEqual(1L, result.Value.Version.Value);
        Assert.IsInstanceOfType<OperationalObservationRecordedEvent>(result.Value.PendingEvents.Single());
    }

    [TestMethod]
    public void ObservationOutsideDelegatedScopeIsRejected()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        AuthorityContext authority = new(
            ActorId.New(),
            domainId,
            [Capability.FieldReporting],
            DeviceId.New(),
            AuthorityMode.DelegatedOffline,
            [IncidentId.New()]);

        DomainResult<OperationalObservation> result = OperationalObservation.Record(
            ObservationId.New(),
            domainId,
            IncidentId.New(),
            null,
            ObservationKind.Status,
            "Status update",
            At,
            authority,
            At);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.ScopeViolation, result.Error?.Code);
        Assert.IsNull(result.Value);
    }

    [TestMethod]
    public void AuthorityDomainRequiresStableIdentityAndName()
    {
        DomainResult<AuthorityDomain> invalid = AuthorityDomain.Create(new AuthorityDomainId(Guid.Empty), "");
        DomainResult<AuthorityDomain> valid = AuthorityDomain.Create(AuthorityDomainId.New(), "Municipal Operations");

        Assert.IsFalse(invalid.Succeeded);
        Assert.IsTrue(valid.Succeeded);
    }
}
