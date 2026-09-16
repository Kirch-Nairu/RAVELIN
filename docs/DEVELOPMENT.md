# RAVELIN Development

F01 established the buildable .NET 10 backend foundation. F02 adds the infrastructure-free operational domain model and application command contracts for incident, operational-period, team, assignment, discrete-resource custody, resource-request, observation, authority, event, and aggregate-version semantics.

F02 does not add persistence, synchronization transport, authentication implementation, product HTTP endpoints, UI, or deployment behavior.

## Prerequisites

- .NET SDK `10.0.401` (enforced by `global.json`).
- Network access to NuGet for the first dependency restore.

No PostgreSQL, SQLite, cloud account, secret, or paid service is required for the current gates.

## Canonical validation

From the repository root:

```bash
dotnet --info
dotnet restore Ravelin.slnx
dotnet build Ravelin.slnx --configuration Release --no-restore
dotnet test Ravelin.slnx --configuration Release --no-build
dotnet test tests/Ravelin.Architecture.Tests/Ravelin.Architecture.Tests.csproj --configuration Release --no-build
dotnet format Ravelin.slnx --verify-no-changes --no-restore
```

## Authority host smoke proof

The Authority project remains a composition/liveness proof only in F02:

```bash
dotnet run --project src/Ravelin.Authority/Ravelin.Authority.csproj --no-build --configuration Release
```

Then request `GET /health/live` on the bound local HTTP endpoint. This endpoint proves host startup only; F02 introduces no product HTTP endpoint.

## Dependency direction

Production projects follow this direction only:

```text
Ravelin.Domain
    ↑
Ravelin.Application
    ↑
Ravelin.Infrastructure
    ↑
Ravelin.Authority
```

`Ravelin.Domain` contains no ASP.NET Core, EF Core, Npgsql, database, or synchronization transport dependency. `Ravelin.Application` contains command contracts only and depends on Domain. Architecture tests enforce project-reference direction, centralized package versions, Domain package/framework purity, and that Infrastructure does not declare Domain namespaces.

## F02 concurrency contract

Mutable aggregates expose an explicit `AggregateVersion`. Commands that mutate authoritative aggregates carry an expected version where appropriate. A stale expected version is an operational rejection (`StaleVersion`), not a database-specific concurrency mechanism.

This is preparation for later transaction/persistence and disconnected-command precondition handling; F02 does not implement command receipt storage, deduplication, outboxes, cursors, reconciliation, or network synchronization.
