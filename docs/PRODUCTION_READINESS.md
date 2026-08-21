# Production Readiness Matrix

This document is a release gate, not a feature wishlist. A release may only be declared Production Ready after every required gate is implemented and verified.

## Core business

- [x] Single-project WPF architecture
- [x] Product and product-unit model
- [x] Base-unit conversion model
- [x] Explicit multi-level conversion graph with cycle validation
- [x] Conversion factor persisted with inventory movement
- [x] Historical conversion factor persisted on sales/purchase/return lines
- [x] Inventory transaction ledger foundation
- [x] Atomic inventory adjustment and warehouse transfer services
- [x] Sales aggregate and atomic posting workflow
- [x] Purchase aggregate and atomic receiving workflow
- [x] Sales return aggregate and atomic inventory restoration workflow
- [x] Customer and supplier account foundation
- [x] Branch, warehouse and terminal foundation
- [x] Cash-register open/close and reconciliation rules
- [x] Pricing, discount and tax calculation foundation
- [x] Promotion rule foundation

## Security

- [x] User model
- [x] Roles and permission codes
- [x] PBKDF2-SHA256 password hashing
- [x] End-to-end login UI
- [x] First-login mandatory password change
- [x] Manager visibility restrictions
- [x] Last Super Administrator protection
- [ ] Permission enforcement across every protected command
- [ ] Manager authorization workflow for every privileged POS action
- [ ] Audit trail integrated with every sensitive mutation

## Operations

- [x] Product/category management foundation
- [x] Product/unit management UI
- [x] Unit-aware barcode lookup
- [x] Unit-aware POS cart and checkout UI
- [x] Payment tender and change calculation
- [x] Purchasing/receiving service with inventory posting
- [x] Sales return service with historical unit conversion
- [x] Inventory adjustments
- [x] Warehouse transfers
- [x] Cash-register reconciliation service
- [x] Sales/inventory report query services
- [ ] Opening stock workflow UI
- [ ] Purchase return workflow UI/service
- [ ] Customer credit/account statement UI
- [ ] Supplier account statement UI
- [ ] Reports/export UI
- [ ] Full POS discounts/promotions/tax UI
- [ ] Receipt reprint/void workflow UI

## Platform

- [x] SQLite offline database foundation
- [x] SQL Server configuration path with retry resiliency
- [x] JSON external configuration
- [x] Arabic/English localization foundation
- [x] Receipt/document/label printing contracts
- [x] Native Windows printing implementation
- [x] Offline synchronization queue foundation
- [x] Sync batch/conflict contracts and queue processor
- [x] SQLite backup service
- [x] Crash/error logging
- [ ] Concrete remote sync transport configuration
- [ ] Restore workflow UI and scheduled backup policy
- [ ] Update/upgrade strategy
- [ ] Hardware-specific smoke tests

## Verification

- [x] Single-project deterministic business-rule verification harness
- [x] GitHub Actions Release build workflow defined
- [x] GitHub Actions verification command defined
- [ ] GitHub Actions restore/build execution confirmed green
- [ ] Inventory posting integration tests pass against a real database
- [ ] Sales/payment integration tests pass against a real database
- [ ] Returns integration tests pass against a real database
- [ ] Permission/security integration tests pass against a real database
- [ ] Database migration test passes
- [ ] Fresh-install smoke test passes
- [ ] Upgrade-from-previous-version test passes
- [ ] Print workflow smoke tests pass on target hardware
- [ ] Offline/online synchronization tests pass against a real endpoint
- [ ] Performance smoke test passes

## Release rule

Do not label this repository Production Ready while any unchecked release gate remains. The final status must include a reproducible build artifact, version number, deployment instructions, database migration strategy, backup/recovery instructions, and a signed-off verification report.
