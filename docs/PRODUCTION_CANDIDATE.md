# Production Candidate Gate

Current candidate branch: `ci-production`.

## Verified

- Single WPF project architecture under `src/POS_WPF` and one project in `POS_WPF.sln`.
- Unit conversion, inventory ledger, purchasing, sales, returns, payments, accounts, cash register, reporting, printing, backup, synchronization, security and localization foundations are present.
- GitHub Actions build and production publish workflows are present.
- Windows/.NET 10 CI Run #189 completed successfully.
- Deterministic business verification and EF Core SQLite persistence smoke test completed successfully in Run #189.
- Production publish workflow now validates the exact source ref, runs verification before publishing, checks the executable, generates SHA-256 and build metadata, and uploads a ZIP artifact.
- Production qualification workflow performs Release build, business verification, self-contained win-x64 publish and package-integrity smoke checks.

## Remaining release gates

The repository must not be labeled Production Ready until the following environment-dependent gates are actually executed and signed off:

1. Real database integration tests for inventory, sales/payments, returns and permissions.
2. EF migration/upgrade test against the supported production database path.
3. Fresh-install and upgrade-from-previous-version smoke tests.
4. Physical receipt/label printer tests on target Windows hardware.
5. Offline/online synchronization test against the actual synchronization endpoint.
6. Performance/load smoke test using representative retail data.
7. Backup restore drill and scheduled backup policy validation.
8. Full privileged-command permission/audit review.

These are deliberately retained as release gates because they cannot be truthfully marked complete from static repository inspection alone.
