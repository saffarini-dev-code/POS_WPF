# Production Candidate Gate

Current candidate branch: `feature/pos-foundation`

A frozen `production-candidate` branch was created from the candidate head for inspection.

## Verified from repository state

- Single WPF project architecture is present under `src/POS_WPF`.
- Unit conversion, inventory ledger, purchasing, sales, returns, payments, accounts, cash register, reporting, printing, backup, synchronization, security and localization foundations are present.
- GitHub Actions build and production publish workflows are present.
- A deterministic `--verify` harness includes business-rule checks and an EF Core SQLite persistence smoke test.

## External verification still required

The GitHub connector reports PR #1 as `mergeable=false` and the feature branch is divergent from `main`. GitHub Actions execution could not be observed through the available repository connector in this session. Therefore this candidate must not be labeled Production Ready until:

1. The branch divergence/conflicts are reconciled on GitHub.
2. GitHub Actions Release build completes successfully.
3. The `--verify` harness completes successfully in Windows CI.
4. Real database integration, migration/upgrade, printing, hardware, offline/online synchronization and performance tests are executed.
5. The remaining unchecked gates in `docs/PRODUCTION_READINESS.md` are closed.

This file intentionally prevents an unsupported Production Ready claim.
