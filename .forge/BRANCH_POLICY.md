# RAVELIN Branch and Promotion Policy

## Authority

`main` is the accepted authority branch. Its exact current HEAD must be read from Git/GitHub.

## Writer candidates

Substantial implementation uses a dedicated bounded candidate branch created from the exact SHA in the handoff.

Recommended naming:

`KIRCH-RAVELIN-<WAVE>-<PURPOSE>`

## Promotion flow

```text
main authority
→ bounded Code Writer handoff
→ candidate branch
→ writer validation/report
→ Maintainer inspection/acceptance
→ non-force promotion/integration
→ remote HEAD verification
→ SSOT/evidence update
```

## Rules

- no force push by default;
- no writer self-promotion;
- no unrelated refactor/scope expansion;
- exact starting SHA mismatch is a stop condition;
- protected architecture changes require Maintainer return;
- application candidates do not mutate `main` directly unless an explicit handoff says otherwise;
- branch existence or successful push is not acceptance evidence.
