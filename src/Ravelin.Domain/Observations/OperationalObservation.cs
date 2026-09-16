using Ravelin.Domain.Authority;
using Ravelin.Domain.Events;
using Ravelin.Domain.Model;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Observations;

public enum ObservationKind
{
    Status,
    Situation,
    Safety,
    Resource,
}

public sealed class OperationalObservation : AggregateRoot
{
    private OperationalObservation(
        ObservationId id,
        AuthorityDomainId authorityDomainId,
        IncidentId incidentId,
        AssignmentId? assignmentId,
        ActorId actorId,
        DeviceId? deviceId,
        ObservationKind kind,
        string content,
        DateTimeOffset capturedAt)
    {
        Id = id;
        AuthorityDomainId = authorityDomainId;
        IncidentId = incidentId;
        AssignmentId = assignmentId;
        ActorId = actorId;
        DeviceId = deviceId;
        Kind = kind;
        Content = content;
        CapturedAt = capturedAt.ToUniversalTime();
    }

    public ObservationId Id { get; }

    public AuthorityDomainId AuthorityDomainId { get; }

    public IncidentId IncidentId { get; }

    public AssignmentId? AssignmentId { get; }

    public ActorId ActorId { get; }

    public DeviceId? DeviceId { get; }

    public ObservationKind Kind { get; }

    public string Content { get; }

    public DateTimeOffset CapturedAt { get; }

    public static DomainResult<OperationalObservation> Record(
        ObservationId id,
        AuthorityDomainId authorityDomainId,
        IncidentId incidentId,
        AssignmentId? assignmentId,
        ObservationKind kind,
        string content,
        DateTimeOffset capturedAt,
        AuthorityContext authority,
        DateTimeOffset recordedAt)
    {
        if (id.IsEmpty || authorityDomainId.IsEmpty || incidentId.IsEmpty || string.IsNullOrWhiteSpace(content))
        {
            return DomainResult<OperationalObservation>.Failure(
                DomainErrorCode.InvalidInput,
                "Observation requires identity, authority domain, incident, and content.");
        }

        DomainResult authorization = authority.Authorize(Capability.FieldReporting, authorityDomainId, incidentId);
        if (!authorization.Succeeded)
        {
            return DomainResult<OperationalObservation>.Failure(authorization.Error!.Code, authorization.Error.Message);
        }

        OperationalObservation observation = new(
            id,
            authorityDomainId,
            incidentId,
            assignmentId,
            authority.ActorId,
            authority.DeviceId,
            kind,
            content.Trim(),
            capturedAt);

        observation.RecordEvent(version => new OperationalObservationRecordedEvent(
            authorityDomainId,
            id,
            incidentId,
            authority.ActorId,
            recordedAt.ToUniversalTime(),
            version));

        return DomainResult<OperationalObservation>.Success(observation);
    }
}
