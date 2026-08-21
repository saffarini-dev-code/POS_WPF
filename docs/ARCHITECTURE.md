# Enterprise Retail POS Architecture

## 1. Product Boundary

The primary product is one Windows WPF desktop application. It remains one deployable POS client and one solution; internal module boundaries are represented by namespaces, folders, interfaces, domain services, and feature-specific views/view-models rather than separate solution projects.

ASP.NET Core is reserved for a future cloud/API layer and is not part of the primary POS UI.

## 2. Runtime Stack

- .NET 10
- C#
- WPF
- MVVM
- Entity Framework Core
- SQL Server for centralized/online deployments
- SQLite for local/offline deployments
- Microsoft.Extensions.DependencyInjection/configuration/logging
- Async/await + CancellationToken
- Secure credential storage and role/permission authorization

## 3. Proposed Single-Solution Structure

```text
POS_WPF/
  src/
    POS_WPF.sln
    POS_WPF/
      App.xaml
      App.xaml.cs
      Bootstrap/
      Configuration/
      Localization/
      Infrastructure/
        Database/
        Security/
        Logging/
        Printing/
        Hardware/
        Synchronization/
      Domain/
        Common/
        Company/
        Branches/
        Catalog/
        Units/
        Inventory/
        Purchasing/
        Sales/
        Returns/
        Customers/
        Suppliers/
        Pricing/
        Promotions/
        Taxes/
        Payments/
        CashRegister/
        Identity/
        Reporting/
        Audit/
      Application/
        Services/
        Interfaces/
        DTOs/
        Validators/
        Workflows/
      Presentation/
        Shell/
        Login/
        POS/
        Products/
        ProductUnits/
        Inventory/
        Purchasing/
        Sales/
        Returns/
        Customers/
        Suppliers/
        Reports/
        CashRegister/
        Users/
        Settings/
      Resources/
        Styles/
        Themes/
        Images/
      Tests/
        Unit/
        Integration/
        Workflow/
```

The solution remains one WPF project unless implementation constraints prove a split is required. Future cloud services can live outside this POS client.

## 4. Domain Rules

### Inventory

All stock quantities are represented internally in the product Base Unit.

### Product Unit

A ProductUnit identifies a transaction unit and stores:

- ProductId
- UnitName
- Abbreviation
- ConversionFactorToBase
- Barcode
- SellingPrice
- PurchasePrice
- CanSell
- CanPurchase
- IsBaseUnit
- IsActive

### Transaction Snapshot

Every sale/purchase/return line stores the unit and conversion information used at transaction time:

- UnitId
- TransactionQuantity
- ConversionFactor
- BaseQuantity
- UnitPrice

Historical records are immutable.

## 5. Unit Conversion Engine

Canonical rule:

```text
BaseQuantity = TransactionQuantity * EffectiveConversionFactor
```

The ProductUnit conversion factor means:

```text
1 configured unit = X Base Units
```

Example:

```text
PCS = 1
BOX = 12
CARTON = 48
```

The engine must:

1. Resolve the unit directly to the product Base Unit whenever possible.
2. Support multi-level relationships safely.
3. Detect zero/negative factors.
4. Detect circular conversion graphs.
5. Prevent deletion of units referenced by transactions.
6. Snapshot conversion values onto transaction lines/ledger rows.

Pricing is independent from conversion.

## 6. Core Entity Map

```text
Company
  └─ Branch
      ├─ Warehouse
      │   └─ InventoryStock
      ├─ Terminal
      │   └─ CashRegisterSession
      └─ Users

Product
  ├─ Category
  ├─ ProductUnit
  │   ├─ Barcode
  │   └─ Pricing
  └─ TaxProfile

Sale
  ├─ SaleLine
  ├─ Payment
  ├─ InventoryTransaction
  ├─ CashMovement
  └─ CustomerBalanceEntry

Purchase
  ├─ PurchaseLine
  ├─ SupplierPayment
  └─ InventoryTransaction

Return
  ├─ ReturnLine
  ├─ Refund/StoreCredit
  └─ InventoryTransaction

User
  └─ UserRole
      └─ RolePermission

StoreSettings
InvoiceSettings
TaxSettings
AuditLog
SyncQueueItem
```

## 7. Inventory Ledger

Every stock movement records:

```text
ProductId
UnitId
TransactionQuantity
ConversionFactor
BaseQuantity
TransactionType
ReferenceType
ReferenceId
CompanyId
BranchId
WarehouseId
TerminalId
UserId
OccurredAt
Reason
```

Positive BaseQuantity increases stock; negative BaseQuantity decreases stock.

Stock balances may be maintained as optimized projections/caches, but the inventory ledger remains authoritative for audit and reconstruction.

## 8. Sales Atomicity

A completed sale is one transaction boundary covering, as applicable:

```text
Sale
SaleLines
Payments
InventoryTransactions
CashMovements
CustomerBalanceEntries
LoyaltyEntries
AuditLog
```

Any critical failure rolls back the operation.

## 9. Security Model

Primary roles:

- Super Administrator
- Manager
- Cashier
- Store Keeper
- Accountant

Authorization is permission-based, not role-name-only.

The Super Administrator cannot be deleted/disabled and at least one must always exist. Managers can have equivalent operational permissions but must never be able to view or modify Super Administrator accounts.

Sensitive cashier actions support manager authorization without abandoning the active POS transaction.

## 10. Localization

Arabic and English are first-class languages. Runtime language changes do not require restarting the application. RTL layout is enabled for Arabic, including product names, documents, reports, and settings.

## 11. Printing

```text
IPrinterService
      ↓
PrinterManager
      ├─ ThermalPrinter / ESC-POS
      ├─ A4Printer
      └─ LabelPrinter
```

Store and invoice content is loaded from configuration. No store-specific text is hard-coded into printer implementations.

Native WPF/Windows printing is preferred. ESC/POS is used when thermal hardware requires direct command printing. QZ Tray is not mandatory.

## 12. Offline-First Boundary

```text
WPF POS
  ↓
Local SQLite
  ↓
Local Transaction/Outbox
  ↓
Sync Queue
  ↓
Future ASP.NET Core API
  ↓
SQL Server
```

All local transactions receive stable identifiers. Sync operations must be idempotent. Conflicts are explicitly classified rather than silently overwritten.

## 13. Verification Gate

Each feature passes:

```text
Analyze → Implement → Build → Test → Fix → Verify DB → Verify UI → Verify Workflow
```

Completion means verified behavior, not merely compiled code.
