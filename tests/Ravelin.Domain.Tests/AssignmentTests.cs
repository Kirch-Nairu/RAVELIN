using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ravelin.Domain.Assignments;
using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Incidents;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;
using Ravelin.Domain.Teams;

namespace Ravelin.Domain.Tests;

[TestClass]
public sealed class AssignmentTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void AssignmentRejectsTeamFromDifferentAuthorityDomain()
    {
        (Incident incident, OperationalPeriod period) = IncidentWithPeriod();
        Team team = RegisterTeam(AuthorityDomainId.New());
        AuthorityContext dispatch = Authority(incident.AuthorityDomainId, Capability.AssignmentDispatch);

        DomainResult<Assignment> result = Assignment.Plan(
            AssignmentId.New(), incident, period.Id, team, "Secure perimeter", dispatch, At);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.WrongAuthorityDomain, result.Error?.Code);
    }

    [TestMethod]
    public void AssignmentLifecycleDistinguishesDispatchAcknowledgementAndProgress()
    {
        (Assignment assignment, Team team, AuthorityContext dispatch, AuthorityContext field) = PlannedAssignment();

        Assert.IsTrue(assignment.Dispatch(team, dispatch, assignment.Version, team.Version, At.AddMinutes(1)).Succeeded);
        Assert.AreEqual(AssignmentStatus.Dispatched, assignment.Status);
        Assert.IsTrue(assignment.Acknowledge(field, assignment.Version, At.AddMinutes(2)).Succeeded);
        Assert.AreEqual(AssignmentStatus.Acknowledged, assignment.Status);
        Assert.IsTrue(assignment.Start(field, assignment.Version, At.AddMinutes(3)).Succeeded);
        Assert.AreEqual(AssignmentStatus.InProgress, assignment.Status);
        Assert.IsTrue(assignment.Complete(field, assignment.Version, At.AddMinutes(4)).Succeeded);
        Assert.AreEqual(AssignmentStatus.Completed, assignment.Status);
        Assert.IsInstanceOfType<AssignmentCompletedEvent>(assignment.PendingEvents.Last());
    }

    [TestMethod]
    public void AssignmentCannotStartBeforeAcknowledgement()
    {
        (Assignment assignment, Team team, AuthorityContext dispatch, AuthorityContext field) = PlannedAssignment();
        assignment.Dispatch(team, dispatch, assignment.Version, team.Version, At.AddMinutes(1));

        DomainResult result = assignment.Start(field, assignment.Version, At.AddMinutes(2));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.InvalidLifecycleTransition, result.Error?.Code);
        Assert.AreEqual(AssignmentStatus.Dispatched, assignment.Status);
    }

    [TestMethod]
    public void DispatchCommitsTeamAndSecondAssignmentCannotClaimIt()
    {
        (Incident incident, OperationalPeriod period) = IncidentWithPeriod();
        Team team = RegisterTeam(incident.AuthorityDomainId);
        AuthorityContext dispatch = Authority(incident.AuthorityDomainId, Capability.AssignmentDispatch);
        DomainResult<Assignment> firstResult = Assignment.Plan(AssignmentId.New(), incident, period.Id, team, "First", dispatch, At);
        DomainResult<Assignment> secondResult = Assignment.Plan(AssignmentId.New(), incident, period.Id, team, "Second", dispatch, At);
        Assignment first = firstResult.Value!;
        Assignment second = secondResult.Value!;
        Assert.IsTrue(first.Dispatch(team, dispatch, first.Version, team.Version, At.AddMinutes(1)).Succeeded);

        DomainResult secondDispatch = second.Dispatch(team, dispatch, second.Version, team.Version, At.AddMinutes(2));

        Assert.IsFalse(secondDispatch.Succeeded);
        Assert.AreEqual(DomainErrorCode.TeamUnavailable, secondDispatch.Error?.Code);
        Assert.AreEqual(first.Id, team.ActiveAssignmentId);
    }

    [TestMethod]
    public void CompletionDoesNotSilentlyReleaseCommittedTeam()
    {
        (Assignment assignment, Team team, AuthorityContext dispatch, AuthorityContext field) = PlannedAssignment();
        assignment.Dispatch(team, dispatch, assignment.Version, team.Version, At.AddMinutes(1));
        assignment.Acknowledge(field, assignment.Version, At.AddMinutes(2));
        assignment.Start(field, assignment.Version, At.AddMinutes(3));
        assignment.Complete(field, assignment.Version, At.AddMinutes(4));

        Assert.AreEqual(TeamStatus.Committed, team.Status);
        Assert.AreEqual(assignment.Id, team.ActiveAssignmentId);

        DomainResult release = team.ReleaseFromAssignment(assignment.Id, assignment.IncidentId, dispatch, team.Version, At.AddMinutes(5));
        Assert.IsTrue(release.Succeeded);
        Assert.AreEqual(TeamStatus.Available, team.Status);
    }

    [TestMethod]
    public void CancellationIsExplicitAndTerminal()
    {
        (Assignment assignment, _, AuthorityContext dispatch, _) = PlannedAssignment();

        DomainResult first = assignment.Cancel(dispatch, assignment.Version, At.AddMinutes(1));
        DomainResult second = assignment.Cancel(dispatch, assignment.Version, At.AddMinutes(2));

        Assert.IsTrue(first.Succeeded);
        Assert.IsFalse(second.Succeeded);
        Assert.AreEqual(AssignmentStatus.Cancelled, assignment.Status);
        Assert.AreEqual(DomainErrorCode.InvalidLifecycleTransition, second.Error?.Code);
    }

    [TestMethod]
    public void MissingCapabilityRejectsDispatchWithoutSuccessfulAssignmentEvent()
    {
        (Assignment assignment, Team team, _, _) = PlannedAssignment();
        AuthorityContext noDispatch = Authority(assignment.AuthorityDomainId, Capability.FieldReporting);
        int assignmentEventCount = assignment.PendingEvents.Count;

        DomainResult result = assignment.Dispatch(team, noDispatch, assignment.Version, team.Version, At.AddMinutes(1));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DomainErrorCode.InsufficientCapability, result.Error?.Code);
        Assert.AreEqual(assignmentEventCount, assignment.PendingEvents.Count);
        Assert.AreEqual(AssignmentStatus.Planned, assignment.Status);
    }

    private static (Assignment Assignment, Team Team, AuthorityContext Dispatch, AuthorityContext Field) PlannedAssignment()
    {
        (Incident incident, OperationalPeriod period) = IncidentWithPeriod();
        Team team = RegisterTeam(incident.AuthorityDomainId);
        AuthorityContext dispatch = Authority(incident.AuthorityDomainId, Capability.AssignmentDispatch);
        AuthorityContext field = Authority(incident.AuthorityDomainId, Capability.FieldReporting);
        DomainResult<Assignment> result = Assignment.Plan(
            AssignmentId.New(), incident, period.Id, team, "Operational mission", dispatch, At);
        Assert.IsTrue(result.Succeeded, result.Error?.Message);
        return (result.Value!, team, dispatch, field);
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

    private static Team RegisterTeam(AuthorityDomainId domainId)
    {
        AuthorityContext administration = Authority(domainId, Capability.Administration);
        return Team.Register(TeamId.New(), domainId, "Team", administration, At).Value!;
    }

    private static AuthorityContext Authority(AuthorityDomainId domainId, Capability capability) =>
        new(ActorId.New(), domainId, [capability], DeviceId.New());
}
