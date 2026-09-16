# RAVELIN Continuity

## Fresh-agent reconstruction order

1. Verify `Kirch-Nairu/RAVELIN` remote and current `main` HEAD.
2. Read root `AGENTS.md`.
3. Read `.forge/AUTHORITY.md`.
4. Read `.forge/SSOT_CURRENT.md`.
5. Read `.forge/ARCHITECTURE.md`.
6. Read only ADRs/handoffs relevant to the next decision.
7. Use Project Second Brain / Notion as semantic context for RAVELIN requirements, decisions and risks; never substitute it for Git/runtime evidence.

## Stable project identity

RAVELIN is an offline-capable incident and resource command system for organizations coordinating field teams and scarce resources under unreliable connectivity.

## Accepted structural facts

- edge Authority Node is canonical operational authority;
- field clients are local-first;
- sync is explicit command/event reconciliation, not blind row merge;
- backend begins as .NET 10 modular monolith + PostgreSQL;
- command UI is React/TypeScript;
- field target is Android-first with local SQLite;
- core operation must work without Internet;
- V1 is one authority domain, not disconnected active-active multi-command federation.

## Current evidence ceiling

Planning/governance only. No application implementation, build, test, CI, runtime, deployment, recovery or device evidence exists yet.

## Product-owner escalation triggers

Return to the product owner before changing any of these boundaries:
- certified/sole life-safety or emergency-dispatch responsibility;
- cross-organization active-active authority;
- multiple disconnected command nodes with globally binding shared-resource decisions;
- mandatory cloud tenancy;
- compliance regime that materially changes architecture/security/data handling.
