# RAVELIN Foundation Development

F01 establishes the buildable backend foundation only. It does not implement incident, resource, synchronization, persistence, authentication, field-client, or command-web product behavior.

## Prerequisites

- .NET SDK `10.0.401` (enforced by `global.json`).
- Network access to NuGet for the first dependency restore.

No PostgreSQL, SQLite, cloud account, secret, or paid service is required for the F01 gates.

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

The Authority project intentionally exposes only a liveness proof endpoint in F01:

```bash
dotnet run --project src/Ravelin.Authority/Ravelin.Authority.csproj --no-build --configuration Release
```

Then request `GET /health/live` on the bound local HTTP endpoint. This endpoint proves host startup only; it is not RAVELIN product functionality.

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

`Ravelin.Architecture.Tests` inspects project references and fails when a production project references its own or a higher layer. It also rejects ASP.NET Core, Entity Framework Core, and Npgsql package references in `Ravelin.Domain`, and rejects package versions declared outside centralized package management.
