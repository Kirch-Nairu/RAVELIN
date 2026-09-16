# RAVELIN Accepted Architecture Baseline

Status: ACCEPTED ENGINEERING BASELINE
Date: 2026-09-16
Scope: greenfield architecture before application implementation

## 1. Problem boundary

RAVELIN must coordinate incidents, teams and scarce resources when command staff and field personnel cannot assume continuous connectivity. Offline work must remain useful without creating false claims of global authority.

## 2. Operational model

A deployment represents one organization/authority domain. It may operate multiple incidents. Each incident has command operators, field teams, discrete resources, resource requests, assignments, custody/allocation changes, status observations and an auditable timeline.

V1 does not attempt disconnected active-active command across independent authority domains.

## 3. Runtime topology

```text
Field Client(s)
  local encrypted SQLite
  pending command outbox
  authoritative event cursor
        |
        | HTTPS sync when reachable
        v
RAVELIN Authority Node
  ASP.NET Core modular monolith
  command/query/sync API
  SignalR freshness hints
        |
        v
PostgreSQL
  canonical relational state
  accepted command/idempotency records
  append-only domain events
  audit records
        |
        +--> local observability / backups

Command Web --> Authority Node over HTTPS

Optional future cloud relay/federation is outside V1 authority.
```

## 4. Source of truth

PostgreSQL on the Authority Node is canonical for accepted operational state.

A field SQLite database is authoritative only for the fact that a device locally captured/persisted a pending operation. It is not global truth for resource custody or incident authority until accepted under valid authority.

## 5. Mutation model

Clients submit commands with:
- UUIDv7 command ID;
- device ID and monotonic device sequence;
- user/authority context;
- target aggregate/entity IDs;
- expected version/preconditions where relevant;
- client-captured timestamp as evidence metadata;
- payload and schema/protocol version.

The Authority Node:
1. authenticates device/user/grant;
2. enforces authorization and custody scope;
3. checks idempotency and preconditions;
4. applies domain invariants in one server transaction;
5. updates canonical relational state;
6. appends domain/audit records;
7. assigns authoritative server sequence/time;
8. returns accepted/rejected/conflicted outcome.

## 6. Synchronization model

Clients maintain a durable server event cursor. Synchronization has two independent directions:

PUSH: durable local commands are batched/retried until terminal server outcome.
PULL: authoritative events/projection deltas are fetched after the last durable cursor.

Realtime sockets may tell a client to synchronize sooner but cannot carry unique correctness state.

Bootstrap uses a versioned snapshot plus a cursor; ordinary reconnect uses incremental events.

## 7. Conflict policy

Never use silent last-write-wins for:
- incident command authority;
- exclusive resource custody/allocation;
- assignment ownership;
- offline grant/security state;
- destructive closure/release where stale state matters.

Conflicts become explicit reconciliation cases with both attempted intent and current authoritative state preserved.

Append-only observations/status reports may coexist when semantically independent.

## 8. Offline authority

An enrolled device has an asymmetric identity key protected by OS secure storage.

A connected Authority Node may issue signed offline authority grants bounded by:
- organization/authority domain;
- user;
- device;
- permitted scopes/actions;
- resource/team custody where applicable;
- issue and expiry time;
- grant identifier/version.

Offline clients may execute local actions inside the grant. Commands outside scope remain impossible or explicitly provisional. Reconnect revalidates all queued commands.

Revocation cannot be assumed to cross a partition; expiry and server-side revalidation bound exposure.

## 9. Domain boundaries

Modules inside the initial modular monolith:

- Identity & Devices
- Incidents
- Teams
- Resources
- Assignments / Resource Requests
- Sync & Reconciliation
- Audit & Timeline
- Operations

Boundaries should be explicit in code and persistence naming, but no service extraction is authorized.

## 10. Core state model

Primary entities/contracts:
- Organization / AuthorityDomain
- User / Role / Device / OfflineAuthorityGrant
- Incident / OperationalPeriod
- Team / Unit
- ResourceType / ResourceAsset
- ResourceRequest
- Assignment
- ResourceAllocation / Custody
- StatusReport / OperationalObservation
- DomainEvent / AuditEvent
- SyncCommand / SyncCursor / ReconciliationCase

Consumable-stock inventory is parked until discrete-resource operations are stable.

## 11. Technology baseline

- .NET 10 LTS / ASP.NET Core authority runtime.
- PostgreSQL 18.x canonical database line.
- React + TypeScript command web.
- Android-first .NET field client; UI framework must not own domain/sync contracts.
- SQLite local persistence; WAL may be used only on the device-local filesystem, never as a network-shared database.
- containerized Linux edge deployment.
- structured logs, health endpoints, metrics and OpenTelemetry-compatible instrumentation.

## 12. Security boundary

- TLS for every network API.
- no mandatory external identity SaaS.
- server authorization is mandatory for canonical mutation.
- field storage encryption is a production requirement.
- server secrets never enter repository/client bundles.
- auditability for privileged and resource-authority changes.
- security/configuration administration is connected-only in V1.

## 13. Reliability / recovery

Required properties:
- locally confirmed pending command survives client process restart;
- duplicate command replay is idempotent;
- event sync resumes from durable cursor;
- Authority Node restart does not corrupt accepted state;
- field pending work survives Authority Node downtime;
- production backup restore is exercised, not merely documented;
- no ordinary hard-delete path for active incident history.

Production HA may add warm standby/replication when deployment RTO requires it; HA topology is not assumed to exist until proven.

## 14. Performance design envelope

Initial architecture should tolerate approximately:
- 250 concurrently active clients;
- 25,000 discrete resources;
- 100 active incidents;
- at least 1,000,000 historical domain/audit events per deployment.

These are engineering test assumptions, not commercial limits.

## 15. Parked scope

- cross-organization active-active federation;
- full GIS/live tracking;
- chat/messaging replacement;
- AI dispatch/recommendation;
- SMS/telephony/satellite integration;
- binary evidence/attachment pipeline;
- supply/procurement ERP;
- advanced analytics;
- certified public-safety CAD / sole life-safety dispatch positioning.

## 16. Review triggers

Architecture review is mandatory when:
- multiple disconnected command authorities must issue globally binding allocation;
- RAVELIN becomes sole/certified life-safety dispatch authority;
- target data becomes regulated/restricted under a specific regime;
- measured scale invalidates the modular-monolith envelope;
- operational exercises show the offline-grant model blocks legitimate work;
- required RPO/RTO cannot be met by the edge authority topology;
- an integration becomes an external source of identity or operational truth.
