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

    public static DomainResult<T> Success<T>(T value) => new(value, null);

    public static DomainResult<T> Failure<T>(DomainErrorCode code, string message) =>
        new(default, new DomainError(code, message));
}

public sealed class DomainResult<T> : DomainResult
{
    internal DomainResult(T? value, DomainError? error)
        : base(error)
    {
        Value = value;
    }

    public T? Value { get; }

    internal static DomainResult<T> Success(T value) => new(value, null);

    internal new static DomainResult<T> Failure(DomainErrorCode code, string message) =>
        new(default, new DomainError(code, message));
}
