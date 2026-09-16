# ADR-0001 — Edge Authority and Command/Event Synchronization

STATUS: ACCEPTED
DATE: 2026-09-16
RISK: High
REVERSIBILITY: R-C — expensive to reverse after protocol/data adoption

## Context

RAVELIN must coordinate scarce resources and field teams when clients can be disconnected. A cloud-only system fails the offline requirement. Generic multi-master row replication or last-write-wins can create false resource ownership. Full peer-to-peer CRDT design would add major complexity while still requiring domain-specific authority semantics.

## Decision

Adopt one canonical edge Authority Node per deployment/authority domain.

Field clients persist local state and pending commands, work offline under bounded authority, and synchronize through idempotent commands plus authoritative server events/cursors.

Critical conflicting intent becomes an explicit reconciliation case. The system does not silently overwrite scarce-resource custody, assignment ownership, incident authority or security state.

## Consequences

Positive:
- local-LAN operations survive Internet loss;
- central authority remains explicit;
- field work survives client/network interruption;
- sync behavior is testable and auditable;
- domain conflicts are visible rather than hidden.

Costs:
- sync protocol is a first-class subsystem;
- offline authorization/delegation must be designed carefully;
- Authority Node availability becomes operationally important;
- disconnected multi-command-center active-active operation is not provided by V1.

## Rejected alternatives

1. Cloud-first web/PWA — violates Internet-independence and weakens field durability.
2. Shared/network SQLite — wrong concurrency/failure boundary; SQLite client databases remain local only.
3. Generic row LWW replication — cannot safely encode scarce-resource authority.
4. Universal CRDT/peer-to-peer state — complexity does not eliminate authority conflicts and pays insufficient rent for V1.

## Reconsider when

- product requires multiple disconnected command authorities with globally binding writes;
- recovery/availability targets cannot be met by edge authority plus standby/recovery;
- operational exercises prove delegated offline authority insufficient;
- a required external system becomes canonical authority for resources/identity.

## Verification requirement

The architecture is not operationally proven until real client/runtime tests demonstrate disconnect, durable local mutation, restart, duplicate replay, reconnect, explicit conflict, convergence and recovery without silent loss.
