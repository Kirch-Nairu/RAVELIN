# RAVELIN Authority Record

## Human technical authority

Kirch Ivan Balite

## Repository

`Kirch-Nairu/RAVELIN`

## Default / current authority branch

`main`

## Greenfield baseline

`045571df669c6e0878320f2099fad958396c27cc`

## Current authority rule

The directly observed `main` HEAD is the current accepted repository authority unless a later explicit Forge handoff establishes a bounded candidate/integration branch.

Exact Git state outranks SHA text in documentation. A tracked commit cannot reliably contain its own final SHA; do not create authority-chasing commits solely to self-reference HEAD.

## Delegations

- ROLE: Maintainer
  SCOPE: architecture governance, decomposition, acceptance, integration, evidence, project-memory maintenance and promotion.
  CONDITION: active Forge Maintainer role and current repository verification.

- ROLE: Code Writer
  SCOPE: only the files/branch/tasks granted by an explicit bounded handoff.
  CONDITION: exact source SHA verification and writer preflight.

- ROLE: Integration Writer / Reviewer / Acceptance
  SCOPE: only when explicitly transitioned or assigned.

## Restrictions

- no force push by default;
- no writer self-promotion;
- no destructive history rewrite;
- no implementation outside bounded handoff;
- no deployment without explicit authority;
- no claim that Notion, memory or architecture intent substitutes for Git/test/runtime evidence.
