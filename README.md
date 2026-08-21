# Enterprise Retail POS — WPF

Production-grade Windows POS and Inventory Management System built with .NET 10, C#, WPF, MVVM, Entity Framework Core, SQL Server, and SQLite for offline-first operation.

## Architecture Principles

- Single WPF application/solution; no multi-project solution split for the POS client.
- Domain boundaries are enforced through folders, namespaces, services, and interfaces.
- Inventory is always stored in Base Units.
- Product Units retain transaction quantity, unit, conversion factor, and base quantity.
- Historical transactions are immutable; conversion changes affect only future transactions.
- Sales, payments, inventory, cash, customer balance, and audit operations are atomic.
- Arabic/English localization with runtime language switching and RTL support.
- Printing is abstracted behind replaceable printer services.
- Offline-first local storage with a synchronization-ready architecture.

## Planned Modules

POS, Products, Product Units, Unit Conversions, Inventory, Purchasing, Sales, Returns, Customers, Suppliers, Pricing, Promotions, Taxes, Payments, Cash Register, Users, Roles & Permissions, Reports, Printing, Hardware Integration, Settings, Audit Logs, Synchronization.

## Source Requirements

The implementation follows the supplied Enterprise Retail POS System — Complete Master Development Prompt, including the critical product-unit conversion engine, unit-specific pricing/barcodes, inventory ledger, role model, printing configuration, offline-first requirements, and phased verification workflow.
