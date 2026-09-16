using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Model;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Teams;

public enum TeamStatus
{
    Available,
    Committed,
    Unavailable,
}

public sealed class Team : AggregateRoot
{
    private Team(TeamId id, AuthorityDomainId authorityDomainId, string name)
    {
        Id = id;
        AuthorityDomainId = authorityDomainId;
        Name = name;
        Status = TeamStatus.Available;
    }

    public TeamId Id { get; }

    public AuthorityDomainId AuthorityDomainId { get; }

    public string Name { get; }

    public TeamStatus Status { get; private set; }

    public AssignmentId? ActiveAssignmentId { get; private set; }

    public static DomainResult<Team> Register(
        TeamId id,
        AuthorityDomainId authorityDomainId,
        string name,
        AuthorityContext authority,
        DateTimeOffset registeredAt)
    {
        if (id.IsEmpty || authorityDomainId.IsEmpty || string.IsNullOrWhiteSpace(name))
        {
            return DomainResult<Team>.Failure(DomainErrorCode.InvalidInput, "Team requires non-empty identity, authority domain, and name.");
        }

        DomainResult authorization = authority.Authorize(Capability.Administration, authorityDomainId, teamId: id);
        if (!authorization.Succeeded)
        {
            return DomainResult<Team>.Failure(authorization.Error!.Code, authorization.Error.Message);
        }

        Team team = new(id, authorityDomainId, name.Trim());
        team.RecordEvent(version => new TeamRegisteredEvent(
            authorityDomainId,
            id,
            authority.ActorId,
            registeredAt.ToUniversalTime(),
            version));

        return DomainResult<Team>.Success(team);
    }

    public DomainResult CommitToAssignment(
        AssignmentId assignmentId,
        IncidentId incidentId,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset committedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(
            Capability.AssignmentDispatch,
            AuthorityDomainId,
            incidentId,
            teamId: Id);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (assignmentId.IsEmpty || incidentId.IsEmpty)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidInput, "Assignment and incident identifiers are required.");
        }

        if (Status != TeamStatus.Available)
        {
            return DomainResult.Failure(DomainErrorCode.TeamUnavailable, "Team must be available before it can be committed.");
        }

        Status = TeamStatus.Committed;
        ActiveAssignmentId = assignmentId;
        RecordEvent(version => new TeamCommittedEvent(
            AuthorityDomainId,
            Id,
            assignmentId,
            authority.ActorId,
            committedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public DomainResult ReleaseFromAssignment(
        AssignmentId assignmentId,
        IncidentId incidentId,
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset releasedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(
            Capability.AssignmentDispatch,
            AuthorityDomainId,
            incidentId,
            teamId: Id);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status != TeamStatus.Committed || ActiveAssignmentId != assignmentId)
        {
            return DomainResult.Failure(DomainErrorCode.AssignmentConflict, "Team is not committed to the specified assignment.");
        }

        Status = TeamStatus.Available;
        ActiveAssignmentId = null;
        RecordEvent(version => new TeamReleasedEvent(
            AuthorityDomainId,
            Id,
            assignmentId,
            authority.ActorId,
            releasedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }

    public DomainResult SetUnavailable(
        AuthorityContext authority,
        AggregateVersion expectedVersion,
        DateTimeOffset changedAt)
    {
        DomainResult precondition = RequireVersion(expectedVersion);
        if (!precondition.Succeeded)
        {
            return precondition;
        }

        DomainResult authorization = authority.Authorize(Capability.AssignmentDispatch, AuthorityDomainId, teamId: Id);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status == TeamStatus.Committed)
        {
            return DomainResult.Failure(DomainErrorCode.TeamUnavailable, "Committed team cannot be marked unavailable before explicit release.");
        }

        if (Status == TeamStatus.Unavailable)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Team is already unavailable.");
        }

        Status = TeamStatus.Unavailable;
        RecordEvent(version => new TeamAvailabilityChangedEvent(
            AuthorityDomainId,
            Id,
            TeamStatus.Unavailable,
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

        DomainResult authorization = authority.Authorize(Capability.AssignmentDispatch, AuthorityDomainId, teamId: Id);
        if (!authorization.Succeeded)
        {
            return authorization;
        }

        if (Status != TeamStatus.Unavailable)
        {
            return DomainResult.Failure(DomainErrorCode.InvalidLifecycleTransition, "Only an unavailable team can be restored to available.");
        }

        Status = TeamStatus.Available;
        RecordEvent(version => new TeamAvailabilityChangedEvent(
            AuthorityDomainId,
            Id,
            TeamStatus.Available,
            authority.ActorId,
            changedAt.ToUniversalTime(),
            version));

        return DomainResult.Success();
    }
}
