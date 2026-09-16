namespace Ravelin.Domain.Results;

public enum DomainErrorCode
{
    InvalidInput,
    InvalidLifecycleTransition,
    WrongAuthorityDomain,
    ClosedIncident,
    StaleVersion,
    InsufficientCapability,
    ScopeViolation,
    OperationalPeriodConflict,
    ActiveCommitmentsRemain,
    TeamUnavailable,
    AssignmentConflict,
    ResourceUnavailable,
    ResourceAlreadyAllocated,
    AllocationMismatch,
    RequestClosed,
    RequestAlreadyFulfilled,
    WrongIncident,
}

public sealed record DomainError(DomainErrorCode Code, string Message);

public class DomainResult
{
    protected DomainResult(DomainError? error)
    {
        Error = error;
    }

    public bool Succeeded => Error is null;

    public DomainError? Error { get; }

    public static DomainResult Success() => new(null);

    public static DomainResult Failure(DomainErrorCode code, string message) =>
        new(new DomainError(code, message));
}

public sealed class DomainResult<T> : DomainResult
{
    private DomainResult(T? value, DomainError? error)
        : base(error)
    {
        Value = value;
    }

    public T? Value { get; }

    public new static DomainResult<T> Success(T value) => new(value, null);

    public new static DomainResult<T> Failure(DomainErrorCode code, string message) =>
        new(default, new DomainError(code, message));
}
