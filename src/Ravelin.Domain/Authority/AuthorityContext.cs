using Ravelin.Domain.Primitives;
using Ravelin.Domain.Results;

namespace Ravelin.Domain.Authority;

public enum Capability
{
    IncidentCommand,
    ResourceCoordination,
    AssignmentDispatch,
    FieldReporting,
    ResourceCustody,
    Administration,
}

public enum AuthorityMode
{
    Connected,
    DelegatedOffline,
}

public sealed class AuthorityContext
{
    private readonly HashSet<Capability> _capabilities;
    private readonly HashSet<IncidentId>? _incidentScope;
    private readonly HashSet<ResourceId>? _resourceScope;
    private readonly HashSet<TeamId>? _teamScope;

    public AuthorityContext(
        ActorId actorId,
        AuthorityDomainId authorityDomainId,
        IEnumerable<Capability> capabilities,
        DeviceId? deviceId = null,
        AuthorityMode mode = AuthorityMode.Connected,
        IEnumerable<IncidentId>? incidentScope = null,
        IEnumerable<ResourceId>? resourceScope = null,
        IEnumerable<TeamId>? teamScope = null)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        ActorId = actorId;
        AuthorityDomainId = authorityDomainId;
        DeviceId = deviceId;
        Mode = mode;
        _capabilities = new HashSet<Capability>(capabilities);
        _incidentScope = incidentScope is null ? null : new HashSet<IncidentId>(incidentScope);
        _resourceScope = resourceScope is null ? null : new HashSet<ResourceId>(resourceScope);
        _teamScope = teamScope is null ? null : new HashSet<TeamId>(teamScope);
    }

    public ActorId ActorId { get; }

    public AuthorityDomainId AuthorityDomainId { get; }

    public DeviceId? DeviceId { get; }

    public AuthorityMode Mode { get; }

    public DomainResult Authorize(
        Capability capability,
        AuthorityDomainId authorityDomainId,
        IncidentId? incidentId = null,
        ResourceId? resourceId = null,
        TeamId? teamId = null)
    {
        if (AuthorityDomainId != authorityDomainId)
        {
            return DomainResult.Failure(
                DomainErrorCode.WrongAuthorityDomain,
                "Authority context belongs to a different authority domain.");
        }

        if (!_capabilities.Contains(capability))
        {
            return DomainResult.Failure(
                DomainErrorCode.InsufficientCapability,
                $"Capability '{capability}' is required.");
        }

        if (incidentId is not null && _incidentScope is not null && !_incidentScope.Contains(incidentId.Value))
        {
            return DomainResult.Failure(DomainErrorCode.ScopeViolation, "Incident is outside the granted authority scope.");
        }

        if (resourceId is not null && _resourceScope is not null && !_resourceScope.Contains(resourceId.Value))
        {
            return DomainResult.Failure(DomainErrorCode.ScopeViolation, "Resource is outside the granted authority scope.");
        }

        if (teamId is not null && _teamScope is not null && !_teamScope.Contains(teamId.Value))
        {
            return DomainResult.Failure(DomainErrorCode.ScopeViolation, "Team is outside the granted authority scope.");
        }

        return DomainResult.Success();
    }
}

public sealed class AuthorityDomain
{
    private AuthorityDomain(AuthorityDomainId id, string name)
    {
        Id = id;
        Name = name;
    }

    public AuthorityDomainId Id { get; }

    public string Name { get; }

    public static DomainResult<AuthorityDomain> Create(AuthorityDomainId id, string name)
    {
        if (id.IsEmpty || string.IsNullOrWhiteSpace(name))
        {
            return DomainResult.Failure<AuthorityDomain>(
                DomainErrorCode.InvalidInput,
                "Authority domain requires a non-empty identifier and name.");
        }

        return DomainResult.Success(new AuthorityDomain(id, name.Trim()));
    }
}
