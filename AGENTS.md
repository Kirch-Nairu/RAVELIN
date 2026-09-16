# RAVELIN Agent Constitution

## Project identity

PROJECT: RAVELIN
REPOSITORY: `Kirch-Nairu/RAVELIN`
TECHNICAL AUTHORITY: Kirch Ivan Balite
FORGE NEST LEVEL: NEST-2 — Governed

## Purpose

RAVELIN is an offline-capable incident and resource command system for organizations coordinating field teams and scarce resources under unreliable connectivity.

RAVELIN V1 is not a certified public-safety CAD, emergency-number dispatch platform, mandatory cloud service, general ERP, live fleet-tracking product, or peer-to-peer multi-master database.

## Current authority

DEFAULT BRANCH: `main`
CURRENT ACCEPTED BRANCH: `main`
CURRENT ACCEPTED SHA: read directly from Git/GitHub; exact Git state outranks SHA text in tracked files.
CURRENT PHASE: Foundation / PRE_IMPLEMENTATION

## Architecture invariants

1. One canonical Authority Node governs operational state per deployment/authority domain.
2. Field clients are local-first and may continue bounded work offline.
3. Synchronization is command/event based; no generic row-level last-write-wins for critical state.
4. Resource custody, assignment ownership, incident authority, and security state require explicit authority and conflict handling.
5. PostgreSQL is canonical server persistence; client SQLite is local cache/pending-work persistence, not shared server truth.
6. SignalR/WebSocket may improve freshness but cannot be required for correctness.
7. The backend begins as a modular monolith. No microservice split without Maintainer authorization and evidence.
8. Core operation must not require Internet or a paid SaaS dependency.
9. Offline authorization is bounded, scoped, device-bound, expiring, and revalidated on reconnect.
10. No claim of life-safety certification or sole emergency-dispatch authority without explicit product-owner escalation.

## Planned repository boundaries

- `src/Ravelin.Domain/` — domain model and invariants.
- `src/Ravelin.Application/` — use cases, command/query contracts, policy orchestration.
- `src/Ravelin.Infrastructure/` — PostgreSQL, persistence, crypto/device integration, observability adapters.
- `src/Ravelin.Authority/` — ASP.NET Core host/API and sync authority.
- `src/Ravelin.CommandWeb/` — React + TypeScript command surface.
- `src/Ravelin.Field/` — Android-first local-first field client.
- `tests/` — unit, integration, contract, sync/fault, architecture and later acceptance harnesses.

These paths are architecture targets until created by an authorized writer.

## Security boundaries

- No secrets, signing private keys, production credentials, or real incident data in Git.
- Privileged security/configuration operations require connected authority.
- Device enrollment and offline grants must be auditable.
- Local field persistence must be designed for encrypted-at-rest operation with OS-keystore-backed key material.
- Destructive deletion of active incident history is prohibited through ordinary product workflows.

## Validation expectations

No application build/test commands exist yet. The first foundation writer must establish reproducible build/test/static-analysis commands and record truthful evidence.

Later acceptance must include observed disconnect → local mutation → process restart → reconnect → reconciliation behavior. Automated tests alone cannot satisfy that journey.

## Git policy

- exact starting SHA required for bounded writer work;
- substantial work uses a dedicated candidate branch;
- no force push by default;
- no silent history rewriting;
- preserve unrelated work;
- writers produce candidates and do not self-promote;
- Maintainer governs acceptance, integration and promotion.

## Forge memory

SSOT: `.forge/SSOT_CURRENT.md`
AUTHORITY: `.forge/AUTHORITY.md`
ARCHITECTURE: `.forge/ARCHITECTURE.md`
DECISIONS: `.forge/decisions/`
ENGINEERING LOG: `.forge/ENGINEERING_LOG.md`
HANDOFFS: `.forge/handoffs/`
EVIDENCE: `.forge/evidence/`

Project Second Brain / Notion contains supplemental semantic records for RAVELIN requirements, decisions and risks. It never replaces observable Git/runtime truth.

## Prohibited behavior

Without explicit Maintainer authority, do not:

- redesign the authority/sync model;
- introduce mandatory cloud infrastructure;
- add microservices;
- add active-active multi-authority allocation;
- use last-write-wins for scarce-resource custody;
- weaken offline authorization boundaries;
- implement parked scope such as GIS, live GPS, chat, AI dispatch, SMS, federation or supply ERP;
- deploy;
- force push;
- claim tests, runtime, device, CI or recovery evidence that was not observed.
