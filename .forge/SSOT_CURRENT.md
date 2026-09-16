# RAVELIN Current Project SSOT

LAST UPDATED: 2026-09-16
UPDATED UNDER AUTHORITY: KIRION Forge Maintainer, greenfield architecture/nesting authority

## Current authority

ACCEPTED BRANCH: `main`
ACCEPTED SHA: read current `main` HEAD directly from Git/GitHub
CURRENT PHASE: Foundation / PRE_IMPLEMENTATION

## System purpose

RAVELIN coordinates incidents, field teams and scarce resources when connectivity is unreliable. It must preserve a trustworthy operational picture, allow bounded offline work, and reconcile disconnected operations without silent state loss or false authority.

## Architecture

Accepted shape: one edge Authority Node per deployment/authority domain, local-first field clients, command/event synchronization, explicit conflict/reconciliation, modular-monolith backend, local-LAN operation without required Internet.

See `.forge/ARCHITECTURE.md` and `.forge/decisions/ADR-0001-edge-authority-sync.md`.

## Frontend

No frontend implementation exists.

Planned command surface: React + TypeScript web client.
Planned field surface: Android-first .NET client with local SQLite and protocol/domain libraries isolated from UI technology.

## Backend

No backend implementation exists.

Planned authority runtime: .NET 10 LTS / ASP.NET Core modular monolith.

## Data

No schema exists.

Accepted data authority:
- PostgreSQL 18.x production line: canonical server state;
- SQLite: device-local cache, pending commands, sync cursors and local observations;
- accepted server mutations update relational state and append domain/audit records transactionally;
- UUIDv7 default IDs; server-assigned monotonic event sequence for authoritative sync order.

## Authentication and authorization

No auth implementation exists.

Accepted model: enrolled device identity, user/session authority, device-bound scoped expiring offline grants, server revalidation on reconnect, connected-only privileged security/configuration changes.

## Infrastructure and deployment

No deployment exists.

Accepted target: Linux edge appliance/VM, containerized authority services, PostgreSQL, TLS, local-LAN operation, no mandatory external SaaS. Production backup/restore evidence is required before operational acceptance.

## External integrations

None accepted or implemented.

Cloud relay/federation, SMS, GIS, live GPS, enterprise OIDC and other integrations remain unapproved until scoped.

## Accepted capabilities

Governance and architecture only. No application capability is implemented or accepted.

## Known limitations

- no code or build system;
- no database/schema;
- no sync implementation;
- no clients;
- no tests/CI;
- no deployment/runtime evidence;
- no backup/restore evidence;
- no device validation.

## Known risks

- split-brain scarce-resource allocation;
- stale offline authorization;
- loss of an unsynchronized field device;
- Authority Node outage during an active incident;
- sensitive operational data on field devices;
- accidental expansion into certified/sole life-safety dispatch responsibility.

## Open work

1. foundation solution/repository skeleton and automated gates;
2. core domain and authority contracts;
3. sync protocol and persistence;
4. field client journey;
5. command web journey;
6. security/recovery hardening;
7. observed operational acceptance.

## Closed decisions

- ADR-0001: Edge Authority + Command/Event Sync.
- Project Second Brain records additionally close the production stack, single authority-domain V1, and bounded offline-authority model.

## Current evidence

- remote repository baseline observed at `045571df669c6e0878320f2099fad958396c27cc`;
- architecture and requirements are INTENDED/ACCEPTED planning state only;
- application validation: NOT RUN because no implementation exists.
