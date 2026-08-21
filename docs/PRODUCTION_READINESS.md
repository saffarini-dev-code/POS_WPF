# Production Readiness Matrix

This document is a release gate, not a feature wishlist. A release may only be declared Production Ready after every required gate is implemented and verified.

## Core business

- [x] Single-project WPF architecture
- [x] Product and product-unit model
- [x] Base-unit conversion model
- [x] Conversion factor persisted with inventory movement
- [x] Inventory transaction ledger foundation
- [x] Sales aggregate foundation
- [x] Purchase aggregate foundation
- [x] Customer and supplier foundation
- [x] Branch, warehouse and terminal foundation
- [x] Cash-register open/close rules
- [x] Pricing, discount and tax calculation foundation

## Security

- [x] User model
- [x] Roles and permission codes
- [x] PBKDF2-SHA256 password hashing
- [ ] End-to-end login UI
- [ ] Permission enforcement across every protected command
- [ ] Manager authorization workflow
- [ ] Audit trail integrated with every sensitive mutation

## Operations

- [ ] Complete product/category management UI
- [ ] Complete unit/conversion management UI
- [ ] Opening stock workflow
- [ ] Purchasing/receiving workflow with inventory posting
- [ ] POS barcode workflow
- [ ] Unit-aware cart and checkout
- [ ] Payment tender workflow and change calculation
- [ ] Sales return workflow
- [ ] Purchase return workflow
- [ ] Inventory adjustments
- [ ] Warehouse transfers
- [ ] Customer credit/account statements
- [ ] Supplier account statements
- [ ] Cash-register reconciliation
- [ ] Reports and exports

## Platform

- [x] SQLite offline database foundation
- [x] SQL Server configuration path
- [x] JSON external configuration
- [x] Localization service foundation
- [x] Printing contracts
- [x] Hardware contracts
- [x] Offline synchronization queue foundation
- [ ] Concrete sync transport and conflict resolution
- [ ] Backup/restore workflow
- [ ] Update/upgrade strategy
- [ ] Crash/error reporting

## Verification

- [ ] GitHub Actions restore succeeds
- [ ] GitHub Actions Release build succeeds
- [ ] Domain conversion tests pass
- [ ] Inventory posting tests pass
- [ ] Sales/payment tests pass
- [ ] Returns tests pass
- [ ] Permission/security tests pass
- [ ] Database migration test passes
- [ ] Fresh-install smoke test passes
- [ ] Upgrade-from-previous-version test passes
- [ ] Print workflow smoke tests pass
- [ ] Offline/online synchronization tests pass
- [ ] Performance smoke test passes

## Release rule

Do not label this repository Production Ready while any unchecked release gate remains. The final status must include a reproducible build artifact, version number, deployment instructions, database migration strategy, backup/recovery instructions, and a signed-off verification report.
