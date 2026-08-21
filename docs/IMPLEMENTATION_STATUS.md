# Production Implementation Status

## Source of truth

The supplied Enterprise Retail POS System — Complete Master Development Prompt is the authoritative specification for implementation.

## GitHub-first rule

All implementation work for this project is committed to GitHub. No local-only implementation is considered complete. Every phase must leave its source, configuration, migrations, tests, documentation, and verification artifacts in the repository.

## Execution policy

Proceed through the planned phases without requesting per-phase approval.

For every phase:

1. Inspect the existing repository state.
2. Implement the phase in the existing single WPF project.
3. Commit the changes to the active GitHub feature branch.
4. Review the resulting repository tree and changed files through GitHub.
5. Verify build/test configuration where an executable environment is available.
6. Fix discovered issues in GitHub before advancing.
7. Record verification status in repository documentation.
8. Continue automatically to the next phase.

## Architecture constraint

The POS client remains a single WPF solution/project. Do not split it into multiple application projects or convert it into a web solution.

## Production gates

The application is not considered Production Ready until the repository contains the implemented workflows and verification coverage for authentication, authorization, catalog, unit conversion, inventory ledger, purchasing, POS sales, returns, customers, suppliers, pricing, promotions, taxes, payments, cash register, reporting, printing, hardware integration, localization, audit, offline operation, synchronization readiness, security, error handling, performance, backup/recovery, and deployment configuration required by the master specification.
