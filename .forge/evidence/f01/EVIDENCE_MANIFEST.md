# RAVELIN F01 Evidence Manifest

Candidate branch: `KIRCH-RAVELIN-F01-FOUNDATION`
Starting authority: `a62ed69f5649fcb21fdfdbba6a135ea333053ca1`
Risk class: `RISK-2`

## Implemented evidence

Source inspection of this candidate establishes:

- .NET 10 SDK pin through `global.json`;
- centralized package versions through `Directory.Packages.props`;
- Domain → Application → Infrastructure → Authority production project layering;
- automated dependency-direction tests;
- minimal ASP.NET Core Authority host with `/health/live` liveness proof only;
- GitHub Actions validation workflow with restore, Release build, tests, architecture tests, and format verification;
- repository-local canonical validation documentation.

## Local execution environment

The writer shell provides Git and basic POSIX tooling but does not provide the `dotnet` executable. Shell outbound DNS/network access is also unavailable. Therefore local restore/build/test/format/runtime gates are `NOT RUN` or `UNAVAILABLE`; they must not be inferred from source inspection.

## Remote validation

Remote GitHub Actions evidence is recorded in the Writer final report after the candidate is pushed and an exact-head run is observed. The manifest intentionally does not chase its own final commit SHA or workflow run ID because updating this file would create another candidate HEAD and another validation run.

## Evidence classification

- IMPLEMENTED: repository source/configuration listed above.
- TESTED: requires observed test execution.
- OBSERVED runtime: requires actual Authority host start and request; source presence alone is insufficient.
- CI: workflow existence is implementation evidence only until a run against the candidate is observed.
