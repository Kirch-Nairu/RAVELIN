# RAVELIN Validation and Evidence Policy

## Evidence ladder

`INTENDED → IMPLEMENTED → TESTED → OBSERVED → DURABLE → DEPLOYED → VERIFIED`

Never infer a higher capability from a lower one.

## Current state

Repository governance/architecture: DURABLE after this nest is committed and remotely verified.
Application implementation: NOT PRESENT.
Application tests/build/runtime/device/deployment: NOT RUN.

## Required validation families

As implementation appears, establish and preserve:

1. build and static analysis;
2. domain unit tests;
3. PostgreSQL integration/migration tests;
4. API/contract tests;
5. sync idempotency/replay/cursor tests;
6. conflict and stale-precondition tests;
7. negative authorization/offline-grant tests;
8. fault tests for network interruption/process restart;
9. backup/restore tests;
10. load/performance tests against the accepted envelope;
11. observed device/runtime primary-journey acceptance.

## Critical scenarios

No release may claim operational readiness without evidence for:

- disconnect after assignment;
- local mutation while offline;
- field application restart while pending operations exist;
- duplicate command replay;
- reconnect and incremental catch-up;
- stale resource-allocation conflict;
- expired/wrong-scope offline grant;
- Authority Node restart;
- backup restore into a clean environment;
- final state convergence with no silent loss.

## Evidence language

Use `NOT RUN`, `NOT VERIFIED`, `SOURCE INSPECTED ONLY`, `REPORTED`, `TESTED`, `OBSERVED`, etc. precisely. Do not convert source inspection into runtime proof.
