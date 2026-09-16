# RAVELIN Current Project SSOT

LAST UPDATED: 2026-09-16
UPDATED UNDER AUTHORITY: KIRION Forge Maintainer

## Current authority

ACCEPTED BRANCH: `main`
ACCEPTED SHA: read current `main` HEAD directly from Git/GitHub
CURRENT PHASE: Foundation / PRE_IMPLEMENTATION — F01 accepted

## System purpose

RAVELIN coordinates incidents, field teams and scarce resources when connectivity is unreliable. It must preserve a trustworthy operational picture, allow bounded offline work, and reconcile disconnected operations without silent state loss or false authority.

## Architecture

Accepted shape: one edge Authority Node per deployment/authority domain, local-first field clients, command/event synchronization, explicit conflict/reconciliation, modular-monolith backend, local-LAN operation without required Internet.

See `.forge/ARCHITECTURE.md` and `.forge/decisions/ADR-0001-edge-authority-sync.md`.

## Frontend

No product frontend implementation exists.

Planned command surface: React + TypeScript web client.
Planned field surface: Android-first .NET client with local SQLite and protocol/domain libraries isolated from UI technology.

## Backend

F01 establishes a buildable .NET 10 solution with `Ravelin.Domain`, `Ravelin.Application`, `Ravelin.Infrastructure`, and `Ravelin.Authority` projects. The Authority host contains only a minimal `/health/live` endpoint; no incident/resource product behavior exists yet.

## Data

No product schema exists.

Accepted data authority remains PostgreSQL 18.x for canonical server state and SQLite for device-local cache/pending work. No database provider or migrations are implemented by F01.

## Authentication and authorization

No auth implementation exists. The accepted future model remains enrolled device identity, user/session authority, device-bound scoped expiring offline grants, reconnect revalidation, and connected-only privileged security/configuration changes.

## Infrastructure and deployment

F01 adds GitHub Actions validation only. No deployment exists.

Accepted target remains Linux edge appliance/VM, containerized authority services, PostgreSQL, TLS, local-LAN operation, and no mandatory external SaaS.

## External integrations

None accepted or implemented.

## Accepted capabilities

- reproducible .NET 10 repository/solution foundation;
- centralized package governance and analyzer/build policy;
- executable production-project dependency-direction tests;
- GitHub Actions restore/build/test/architecture/format gates;
- observed minimal Authority process start and liveness endpoint.

These are engineering-foundation capabilities, not operational RAVELIN product capabilities.

## Known limitations

- no incident/resource domain implementation;
- no database/schema/migrations;
- no synchronization/reconciliation implementation;
- no authentication/offline grants;
- no command web or field client;
- no deployment, backup/restore or device evidence;
- CI push allowlist currently names `main` and F01; future candidates may rely on PR validation or adjust the trigger deliberately.

## Known risks

- split-brain scarce-resource allocation;
- stale offline authorization;
- loss of an unsynchronized field device;
- Authority Node outage during an active incident;
- sensitive operational data on field devices;
- accidental expansion into certified/sole life-safety dispatch responsibility.

## Open work

1. F02 core domain and authority contracts;
2. synchronization protocol and persistence;
3. field client journey;
4. command web journey;
5. security/recovery hardening;
6. observed operational acceptance.

## Closed decisions

- ADR-0001: Edge Authority + Command/Event Sync.
- Project Second Brain records additionally close the production stack, single authority-domain V1, and bounded offline-authority model.

## Current evidence

F01 candidate `ea36339844a2e859430ab506daacfda63c16423b` was independently accepted by the Maintainer. GitHub Actions run `35071654539` on that exact SHA completed successfully with Release build `0` warnings/errors, 4/4 solution tests, 3/3 explicit architecture tests, format verification, and an observed `/health/live` smoke check. See `.forge/evidence/f01/ACCEPTANCE_REPORT.md`.
