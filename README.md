# Enterprise Retail POS — POS_WPF

A single-project **WPF + .NET 10** enterprise retail POS and inventory application.

## Architecture

- Single WPF application project
- MVVM-ready presentation layer
- EF Core with SQLite for offline-first operation
- SQL Server configuration for centralized deployments
- Domain-driven business rules
- Unit-aware inventory with Base Unit normalization
- Atomic sales, purchasing, returns and inventory operations
- Role/permission authorization
- Arabic/English localization foundation
- Receipt, document and label printing
- Offline synchronization queue with configurable HTTP transport
- Audit and crash diagnostics
- GitHub Actions build, verification and production publish workflows

## Unit conversion

If the base unit is `PCS` and `1 BOX = 12 PCS`:

- Purchase `10 BOX` → inventory `120 PCS`
- Sell `1 BOX` → inventory decreases by `12 PCS`
- Historical transactions retain their conversion factor.

## Development verification

The repository contains a single-project verification harness:

```text
--verify
```

It validates core business rules and performs an EF Core SQLite persistence smoke test.

## Production documentation

- `docs/ARCHITECTURE.md`
- `docs/PRODUCTION_READINESS.md`
- `docs/DEPLOYMENT.md`
- `docs/IMPLEMENTATION_STATUS.md`

The project must not be declared Production Ready while any release gate in `docs/PRODUCTION_READINESS.md` remains unchecked.
