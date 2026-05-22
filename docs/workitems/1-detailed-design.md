# Detailed Design: Show Customers with Overdue Orders

**Issue**: #1  
**Architecture Design**: docs/workitems/1-design.md  
**Date**: 2026-05-22  
**Status**: Awaiting Review

---

## Requirements Summary

Display all customers with at least one overdue order. An order is overdue when its `DueDate < today` AND its `Status` is not closed (not `Shipped = 5` or `Cancelled = 6`). Results grouped by customer, ordered by the oldest overdue due date ascending. Per customer: name, overdue order count, oldest overdue date. Accessible via console command.

---

## Module-Level Contracts

### Interfaces

Extend `ICustomerService` in `Modules/Contracts/Sales/ICustomerService.cs`:

```csharp
namespace Contracts.Sales;

public interface ICustomerService
{
    CustomerData[] GetCustomersWithOrders();
    CustomerData[] GetCustomersWithOrdersStartingWith(string prefix);
    CustomerData[] GetCustomersWithOrdersContaining(string fragment);

    /// <summary>
    /// Returns all customers that have at least one overdue order.
    /// An order is overdue when DueDate is earlier than today and Status is not Shipped or Cancelled.
    /// Results are ordered by the oldest overdue due date ascending.
    /// </summary>
    /// <returns>
    /// Array of <see cref="OverdueCustomerSummary"/>, empty when no overdue customers exist.
    /// </returns>
    OverdueCustomerSummary[] GetCustomersWithOverdueOrders();
}
```

### DTOs

New file `Modules/Contracts/Sales/OverdueCustomerSummary.cs`:

```csharp
namespace Contracts.Sales;

public sealed class OverdueCustomerSummary
{
    /// <summary>
    /// Full name of the customer: "{FirstName} {LastName}".
    /// </summary>
    public required string CustomerName { get; init; }

    /// <summary>
    /// Total number of overdue orders for this customer.
    /// </summary>
    public int OverdueOrderCount { get; init; }

    /// <summary>
    /// Due date of the oldest overdue order for this customer.
    /// </summary>
    public DateTime OldestOverdueDueDate { get; init; }
}
```

### Fault Contracts

Not Required. `GetCustomersWithOverdueOrders()` is a pure read query. An empty result set is valid; no exception types are warranted.

---

## Internal API Contracts

Not Required. The query logic is straightforward and does not justify a dedicated internal validator or sub-service. All logic resides directly in `CustomerService`.

---

## Data Model

### Entities

Not Required. Existing entities cover all required data:

- `Sales.DataModel.SalesLT.Customer`: `CustomerID`, `FirstName`, `LastName`, navigation `SalesOrderHeaders`
- `Sales.DataModel.SalesLT.SalesOrderHeader`: `DueDate`, `Status`, `CustomerID`
- `Sales.DataModel.Values.SalesOrderHeaderStatusValues`: `Shipped = 5`, `Cancelled = 6`

### Entity Interceptors

Not Required. Feature is read-only.

### Database Changes

Not Required. No new tables, columns, or relationships.

---

## External Systems Integration

Not Required. Feature reads only from the local database.

---

## Cross-Cutting Concerns

### Error Handling

| Scenario | Handling |
|---|---|
| No overdue customers found | Return empty array — not an error |
| Unexpected repository exception | Allow to propagate; not caught at service level |

### Logging

Log at the service boundary using `ILogger<CustomerService>`:

| Point | Level | Message |
|---|---|---|
| Method entry | `Debug` | `"Querying customers with overdue orders"` |
| Result count | `Debug` | `"Found {Count} customers with overdue orders"` |

No PII fields (name) in log messages.

### Security

- No authorization required — read-only console feature.
- Input validation not applicable — no parameters.
- EF Core parameterization prevents SQL injection.
- Customer name (`FirstName`, `LastName`) is PII: do not include in log messages.

### Idempotency

Not Required. Read-only operation is inherently idempotent.

---

## Test Strategy

### Unit Tests

**`CustomerServiceTests`** (`Sales.Services.UnitTests/CustomerServiceTests.cs`)

- `GetCustomersWithOverdueOrders_WhenNoOrders_ReturnsEmptyArray`
- `GetCustomersWithOverdueOrders_WhenAllOrdersClosed_ReturnsEmptyArray`
- `GetCustomersWithOverdueOrders_WhenOrderNotYetDue_ReturnsEmptyArray`
- `GetCustomersWithOverdueOrders_WhenOrderIsOverdue_ReturnsCustomer`
- `GetCustomersWithOverdueOrders_ExcludesOrdersWithStatusShipped`
- `GetCustomersWithOverdueOrders_ExcludesOrdersWithStatusCancelled`
- `GetCustomersWithOverdueOrders_OrderedByOldestOverdueDueDate_Ascending`
- `GetCustomersWithOverdueOrders_CustomerName_IsConcatenatedFirstAndLastName`
- `GetCustomersWithOverdueOrders_OverdueOrderCount_ReflectsOnlyOverdueOrders`
- `GetCustomersWithOverdueOrders_OldestOverdueDueDate_IsMinimumDueDatePerCustomer`
- `GetCustomersWithOverdueOrders_CustomerWithMixedOrders_OnlyCountsOverdueOnes`

**`OverdueCustomersConsoleCommandTests`** (`Sales.ConsoleCommands` test project, if present)

- `Execute_WhenNoOverdueCustomers_WritesNoResultsMessage`
- `Execute_WhenOverdueCustomersExist_WritesEachCustomer`

### Integration Tests

Not Required. The feature involves a single module with a single query; no cross-module interactions.

### Acceptance Criteria

| Requirement | Test |
|---|---|
| Lists only customers with at least one overdue order | `GetCustomersWithOverdueOrders_WhenAllOrdersClosed_ReturnsEmptyArray` |
| Orders grouped by customer | `GetCustomersWithOverdueOrders_OverdueOrderCount_ReflectsOnlyOverdueOrders` |
| Customers ordered by oldest overdue date ascending | `GetCustomersWithOverdueOrders_OrderedByOldestOverdueDueDate_Ascending` |
| Customer name displayed | `GetCustomersWithOverdueOrders_CustomerName_IsConcatenatedFirstAndLastName` |
| Number of overdue orders displayed | `GetCustomersWithOverdueOrders_OverdueOrderCount_ReflectsOnlyOverdueOrders` |
| Oldest overdue date displayed | `GetCustomersWithOverdueOrders_OldestOverdueDueDate_IsMinimumDueDatePerCustomer` |
| Accessible via console command | `Execute_WhenOverdueCustomersExist_WritesEachCustomer` |

---

## Implementation Plan

**Phase 1: Contracts**
- [ ] Add `OverdueCustomerSummary` DTO to `Modules/Contracts/Sales/OverdueCustomerSummary.cs`
- [ ] Extend `ICustomerService` with `GetCustomersWithOverdueOrders()` in `Modules/Contracts/Sales/ICustomerService.cs`

**Phase 2: Service Implementation**
- [ ] Implement `GetCustomersWithOverdueOrders()` in `Modules/Sales/Sales.Services/CustomerService.cs`
  - Query via `IRepository.GetEntities<Customer>()`
  - Filter: any `SalesOrderHeader` where `DueDate < DateTime.Today` AND `Status` not in `{Shipped, Cancelled}`
  - Group by customer, keep only customers with at least one such order
  - Project to `OverdueCustomerSummary` with `CustomerName = $"{FirstName} {LastName}"`, `OverdueOrderCount`, `OldestOverdueDueDate = Min(DueDate)`
  - Order by `OldestOverdueDueDate` ascending
- [ ] Add `ILogger<CustomerService>` injection and log entry/result count at `Debug` level

**Phase 3: Console Command**
- [ ] Create `Modules/Sales/Sales.ConsoleCommands/OverdueCustomersConsoleCommand.cs`
  - `[Service(typeof(IConsoleCommand))]`
  - `MenuLabel` = `"Show customers with overdue orders"`
  - Call `ICustomerService.GetCustomersWithOverdueOrders()`
  - Write each `OverdueCustomerSummary` record to `IConsole`; write a no-results message when empty

**Phase 4: Unit Tests**
- [ ] Implement `CustomerServiceTests` unit tests (listed above) in `Sales.Services.UnitTests`
- [ ] Implement `OverdueCustomersConsoleCommandTests` (listed above)

---

## Quality Checklist

- [x] All interfaces have explicit signatures
- [x] All DTOs have required annotations and type-safe properties
- [x] No exceptions needed — empty array covers the not-found case
- [x] Error handling is specified (propagate unexpected, empty for no results)
- [x] Cross-cutting concerns are addressed (logging, security, idempotency)
- [x] Edge cases handled (all orders closed, no orders, not-yet-due orders, mixed orders)
- [x] Test strategy is complete with acceptance criteria mapping
- [x] Implementation plan is actionable with no ambiguity
