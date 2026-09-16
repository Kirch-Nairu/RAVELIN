using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Incidents;
using Ravelin.Domain.Model;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;
using Ravelin.Domain.Teams;

namespace Ravelin.Domain.Assignments;

public enum AssignmentStatus
{
    Planned,
    Dispatched,
    Acknowledged,
    InProgress,
    Completed,
    Cancelled,
}

public sealed class Assignment : AggregateRoot
{
    private Assignment(
        AssignmentId id,
        AuthorityDomainId authorityDomainId,
        IncidentId incidentId,
        OperationalPeriodId operationalPeriodId,
        TeamId teamId,
        string description)
    {
        Id = id;
        AuthorityDomainId = authorityDomainId;
        IncidentId = incidentId;
        OperationalPeriodId = operationalPeriodId;
        TeamId = teamId;
        Description = description;
        Status = AssignmentStatus.Planned;
    }

    public AssignmentId Id { get; }

    public AuthorityDomainId AuthorityDomainId { get; }

    public IncidentId IncidentId { get; }

    public OperationalPeriodId OperationalPeriodId { get; }

    public TeamId TeamId { get; }

    public string Description { get; }

    public AssignmentStatus Status { get; private set; }

    public static DomainResult<Assignment> Plan(
        AssignmentId id,
        Incident incident,
        OperationalPeriodId operationalPeriodId,
        Team team,
        string description,
        AuthorityContext authority,
        DateTimeOffset plannedAt)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(team);

        if (id.IsEmpty || string.IsNullOrWhiteSpace(description))
        {
            return DomainResult<Assignment>.Failure(DomainErrorCode.InvalidInput, "Assignment requires non-empty identity and description.");
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            return DomainResult<Assignment>.Failure(DomainErrorCode.ClosedIncident, "Closed incident rejects new assignments.");
        }

        if (!incident.IsOperationalPeriodOpen(operationalPeriodId))
        {
            return DomainResult<Assignment>.Failure(DomainErrorCode.OperationalPeriodConflict, "Assignment requires the current open operational period.");
        }

        if (team.AuthorityDomainId != incident.AuthorityDomainId)
        {
            return DomainResult<Assignment>.Failure(DomainErrorCode.WrongAuthorityDomain, "Assigned team belongs to another authority domain.");
        }

        if (team.Status != TeamStatus.Available)
        {
            return DomainResult<Assignment>.Failure(DomainErrorCode.TeamUnavailable, "Assigned team must be available when work is planned.");
        }

        DomainResult authorization = authority.Authorize(
            Capability.AssignmentDispatch,
            incident.AuthorityDomainId,
            incident.Id,
            teamId: team.Id);
        if (!authorization.Succeeded)
        {
            return DomainResult<Assignment>.Failure(authorization.Error!.Code, authorization.Error.Message);
        }

        Assignment assignment = new(
            id,
            incident.AuthorityDomainId,
            incident.Id,
            operationalPeriodId,
            team.Id,
            description.Trim());

        assignment.RecordEvent(version => new AssignmentPlannedEvent(
            assignment.AuthorityDomainId,
            assignment.Id,
            assignment.IncidentId,
            assignment.TeamId,
            authority.ActorId,
            plannedAt.ToUniversalTime(),
            version));

        return DomainResult<Assignment>.Success(assignment);
    }

    public DomainResult Dispatch(
        Team team,
        AuthorityContext authority,
        AggregateVersion expectedAssignmentVersion,
        AggregateVersion expectedTeamVersion,
        DateTimeOffset dispatchedAt)
    {
        ArgumentNullException.ThrowIfNull(team);

        DomainResult assignmentVersion = RequireVersion(expectedAssignmentVersion);
        if (!assignmentVersion.Succeeded)
        {
            return assignmentVersion;
        }

        if (Status != AssignmentStatus.Planned)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Only a planned assignment can be dispatched.");
        }

        if (team.Id != TeamId || team.AuthorityDomainId != AuthorityDomainId)
        {
            return DomainResult.Failure(DomainErrorCode.AssignmentConflict, "Dispatch team does not match the planned assignment.");
        }

        DomainResult authorization = authority.Authorize(
            Capability.AssignmentDispatch,
            AuthorityDomainId,
            IncidentId,
            teamId: TeamId);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        DomainResult teamCommit = team.CommitToAssignment(
            Id,
            IncidentId,
            authority,
            expectedTeamVersion,
            dispatchedAt);
        if (!teamCommit.Succeeded)
        {
            return teamCommit;
        }

        Status = AssignmentStatus.Dispatched;
        RecordEvent(version => new AssignmentDispatchedEvent(
            AuthorityDomainId,
            Id,
            authority.ActorId,
            dispatchedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public DomainResult Acknowledge(
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset acknowledgedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(
            Capability.FieldReporting,
            AuthorityDomainId,
            IncidentId,
            teamId: TeamId);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status != AssignmentStatus.Dispatched)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Only a dispatched assignment can be acknowledged.");
        }

        Status = AssignmentStatus.Acknowledged;
        RecordEvent(version => new AssignmentAcknowledgedEvent(
            AuthorityDomainId,
            Id,
            authority.ActorId,
            acknowledgedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public DomainResult Start(
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset startedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(
            Capability.FieldReporting,
            AuthorityDomainId,
            IncidentId,
            teamId: TeamId);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status != AssignmentStatus.Acknowledged)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Only an acknowledged assignment can start work.");
        }

        Status = AssignmentStatus.InProgress;
        RecordEvent(version => new AssignmentStartedEvent(
            AuthorityDomainId,
            Id,
            authority.ActorId,
            startedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public DomainResult Complete(
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset completedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(
            Capability.FieldReporting,
            AuthorityDomainId,
            IncidentId,
            teamId: TeamId);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status != AssignmentStatus.InProgress)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Only an in-progress assignment can complete.");
        }

        Status = AssignmentStatus.Completed;
        RecordEvent(version => new AssignmentCompletedEvent(
            AuthorityDomainId,
            Id,
            authority.ActorId,
            completedAt.ToUniversalTime(),
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

        DomainResult authorization = authority.Authorize(
            Capability.AssignmentDispatch,
            AuthorityDomainId,
            IncidentId,
            teamId: TeamId);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status is AssignmentStatus.Completed or AssignmentStatus.Cancelled)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Terminal assignment cannot be cancelled.");
        }

        Status = AssignmentStatus.Cancelled;
        RecordEvent(version => new AssignmentCancelledEvent(
            AuthorityDomainId,
            Id,
            authority.ActorId,
            cancelledAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }
}
