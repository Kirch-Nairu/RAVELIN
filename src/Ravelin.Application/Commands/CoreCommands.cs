using Ravelin.Domain.Assignments;
using Ravelin.Domain.Authority;
using Ravelin.Domain.Observations;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Requests;
using Ravelin.Domain.Resources;

namespace Ravelin.Application.Commands;

public interface IApplicationCommand
{
    CommandId CommandId { get; }

    AuthorityContext Authority { get; }

    DateTimeOffset IssuedAt { get; }
}

public sealed record OpenIncidentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    IncidentId IncidentId,
    AuthorityDomainId AuthorityDomainId,
    string Name,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record StartOperationalPeriodCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    IncidentId IncidentId,
    OperationalPeriodId OperationalPeriodId,
    AggregateVersion ExpectedIncidentVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record CloseOperationalPeriodCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    IncidentId IncidentId,
    OperationalPeriodId OperationalPeriodId,
    AggregateVersion ExpectedIncidentVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record CloseIncidentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    IncidentId IncidentId,
    AggregateVersion ExpectedIncidentVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record RegisterTeamCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    TeamId TeamId,
    AuthorityDomainId AuthorityDomainId,
    string Name,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record ReleaseTeamFromAssignmentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    TeamId TeamId,
    AssignmentId AssignmentId,
    IncidentId IncidentId,
    AggregateVersion ExpectedTeamVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record RegisterDiscreteResourceCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    ResourceId ResourceId,
    AuthorityDomainId AuthorityDomainId,
    ResourceCapability Capability,
    string Name,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record CreateResourceRequestCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    ResourceRequestId ResourceRequestId,
    IncidentId IncidentId,
    OperationalPeriodId OperationalPeriodId,
    ResourceCapability Capability,
    int RequestedCount,
    ResourceRequestPriority Priority,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record CancelResourceRequestCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    ResourceRequestId ResourceRequestId,
    AggregateVersion ExpectedRequestVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record PlanAssignmentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    AssignmentId AssignmentId,
    IncidentId IncidentId,
    OperationalPeriodId OperationalPeriodId,
    TeamId TeamId,
    string Description,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record DispatchAssignmentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    AssignmentId AssignmentId,
    AggregateVersion ExpectedAssignmentVersion,
    AggregateVersion ExpectedTeamVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record AcknowledgeAssignmentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    AssignmentId AssignmentId,
    AggregateVersion ExpectedAssignmentVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record StartAssignmentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    AssignmentId AssignmentId,
    AggregateVersion ExpectedAssignmentVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record CompleteAssignmentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    AssignmentId AssignmentId,
    AggregateVersion ExpectedAssignmentVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record CancelAssignmentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    AssignmentId AssignmentId,
    AggregateVersion ExpectedAssignmentVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record AllocateResourceCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    ResourceId ResourceId,
    AllocationId AllocationId,
    AllocationTarget Target,
    AggregateVersion ExpectedResourceVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record TransferResourceCustodyCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    ResourceId ResourceId,
    AllocationId CurrentAllocationId,
    AllocationId NewAllocationId,
    AllocationTarget NewTarget,
    AggregateVersion ExpectedResourceVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record ReleaseResourceCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    ResourceId ResourceId,
    AllocationId AllocationId,
    AggregateVersion ExpectedResourceVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record RecordResourceRequestFulfillmentCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    ResourceRequestId ResourceRequestId,
    AllocationId AllocationId,
    AggregateVersion ExpectedRequestVersion,
    DateTimeOffset IssuedAt) : IApplicationCommand;

public sealed record RecordOperationalObservationCommand(
    CommandId CommandId,
    AuthorityContext Authority,
    ObservationId ObservationId,
    IncidentId IncidentId,
    AssignmentId? AssignmentId,
    ObservationKind Kind,
    string Content,
    DateTimeOffset CapturedAt,
    DateTimeOffset IssuedAt) : IApplicationCommand;
