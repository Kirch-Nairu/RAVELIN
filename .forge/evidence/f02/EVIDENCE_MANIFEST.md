# RAVELIN F02 Evidence Manifest

Wave: `F02 — Domain & Authority`
Candidate branch: `KIRCH-RAVELIN-F02-DOMAIN-AUTHORITY`
Starting authority: `1aa9dd6533f9ee3b78f0db9de780ea35e98cdb82`
Risk class: `RISK-3`

## IMPLEMENTED

- infrastructure-free strongly typed domain identifiers and aggregate versions;
- deterministic domain result/rejection contract;
- capability/domain/scope authority context without authentication or cryptographic implementation;
- incident and operational-period lifecycle;
- team availability/commitment model;
- assignment lifecycle with explicit dispatch, acknowledgement, progress, completion and cancellation;
- discrete-resource availability plus exclusive active custody/allocation, transfer and explicit release;
- resource-request lifecycle with allocation-linked partial/full fulfillment;
- append-oriented operational observations with actor/device attribution;
- immutable domain facts/events for accepted transitions;
- infrastructure-neutral application commands with stable `CommandId` and expected aggregate versions where applicable;
- architecture tests preserving Domain purity;
- F02 candidate branch added to the existing CI push allowlist without weakening gates.

## TESTED

Exact executed test counts and the exact candidate SHA are established by the final GitHub Actions run and Writer Report. This tracked manifest deliberately does not self-reference its own commit SHA, because changing the manifest would create a new untested HEAD.

Required validation families for this wave:

- Release build/static analysis;
- domain behavior tests;
- explicit architecture tests;
- formatting verification;
- unchanged Authority liveness smoke.

## OBSERVED

Authority-host runtime observation is limited to the existing `/health/live` smoke gate. Domain tests do not constitute operational runtime, persistence, network, device, or deployment evidence.

## NOT RUN / OUT OF SCOPE

- PostgreSQL, EF Core, migrations, repository implementations or database transactions;
- SQLite/local outbox;
- synchronization transport, command-receipt storage, event cursors or reconciliation persistence;
- JWT/auth provider, device enrollment, signing or cryptographic offline grants;
- product HTTP endpoints/controllers;
- React, Android UI or device acceptance;
- deployment, Docker, backup/restore, federation, GIS, GPS, chat, AI, attachments or consumable inventory.

## Environment limitations

The writer execution shell does not provide a usable local .NET SDK. Build/test/runtime evidence therefore comes only from explicitly observed GitHub Actions runs on the candidate branch; source inspection is not reported as test evidence.
