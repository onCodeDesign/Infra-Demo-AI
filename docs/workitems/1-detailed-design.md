# Detailed Design: Show customers with overdue orders

**Issue**: #1
**Architecture Design**: docs/workitems/1-design.md
**Date**: January 15, 2026
**Status**: Approved

## Requirements Summary

Display customers with at least one overdue order via a console command. An order is overdue when its `DueDate` is earlier than today and its `Status` is not closed (Shipped or Cancelled). Results show customer name, count of overdue orders, and oldest overdue order date, sorted by oldest overdue date ascending.

## Module-Level Contracts

### Interfaces

**ICustomerService** (extend in `Modules/Contracts/Sales/ICustomerService.cs`)

```csharp
namespace Contracts.Sales;

public interface ICustomerService
{
    CustomerData[] GetCustomersWithOrders();
    CustomerData[] GetCustomersWithOrdersStartingWith(string prefix);
    CustomerData[] GetCustomersWithOrdersContaining(string fragment);
    
    /// <summary>
    /// Retrieves customers with at least one overdue order
    /// </summary>
    /// <returns>Array of customers with overdue order details, sorted by oldest overdue date ascending</returns>
    CustomerOverdueOrdersData[] GetCustomersWithOverdueOrders();
}
```

### DTOs

**CustomerOverdueOrdersData** (new in `Modules/Contracts/Sales/CustomerOverdueOrdersData.cs`)

```csharp
namespace Contracts.Sales;

/// <summary>
/// Customer data with overdue order aggregates
/// </summary>
public sealed class CustomerOverdueOrdersData
{
    /// <summary>
    /// Customer's full name (FirstName + LastName) or CompanyName if available
    /// </summary>
    public required string CustomerName { get; init; }
    
    /// <summary>
    /// Total count of overdue orders for this customer
    /// </summary>
    public int OverdueOrderCount { get; init; }
    
    /// <summary>
    /// Due date of the oldest overdue order for this customer
    /// </summary>
    public DateTime OldestOverdueOrderDate { get; init; }
}
```

### Fault Contracts

Not Required - This is a read-only query operation with no exceptional business scenarios. If no overdue customers exist, an empty array is returned. No specific exceptions are needed beyond standard framework exceptions (e.g., database connectivity issues).

## Internal API Contracts

Not Required - Business logic is simple enough to implement directly in `CustomerService` without internal abstractions. Query logic involves LINQ aggregation against `IRepository`, which does not warrant a separate internal service.

## Data Model

### Entities

**No Changes Required** - Existing entities are sufficient:
- `SalesOrderHeader` (Sales.DataModel/Generated/SalesLT/SalesOrderHeader.cs) - Has `DueDate` (DateTime), `Status` (byte), navigation to `Customer`
- `Customer` (Sales.DataModel/Generated/SalesLT/Customer.cs) - Has `FirstName`, `LastName`, `CompanyName`, reverse navigation to `SalesOrderHeaders`

**Status Values** - Use existing `SalesOrderHeaderStatusValues` (Sales.DataModel/Values/SalesOrderHeaderStatusValues.cs):
- Closed status values: `Shipped` (5), `Cancelled` (6)
- Overdue filter: `Status` NOT IN (5, 6) AND `DueDate` < Today

**CustomerName Construction Logic**:
1. If `CompanyName` is not null or whitespace: use `CompanyName`
2. Else: use `$"{FirstName} {LastName}".Trim()`
3. If result is empty/null: use `$"Customer {CustomerID}"`

**Query Logic Specification**:
1. Filter `SalesOrderHeader` where `DueDate < DateTime.Today` AND `Status` NOT IN (5, 6)
2. Include navigation to `Customer` entity
3. Group filtered orders by `Customer`
4. For each customer group, project to `CustomerOverdueOrdersData`:
   - `CustomerName`: Apply construction logic above
   - `OverdueOrderCount`: Count of orders in group
   - `OldestOverdueOrderDate`: Minimum `DueDate` in group
5. Order results by `OldestOverdueOrderDate` ascending

### Entity Interceptors

Not Required - No calculated fields or business logic needs to be applied globally to entities for this feature.

### Database Changes

Not Required - All necessary schema elements exist in the current database.

## External Systems Integration

Not Required - This feature operates entirely within the Sales module boundary using local database queries.

## Cross-Cutting Concerns

### Error Handling

- **Empty Results**: Return empty array `CustomerOverdueOrdersData[]` when no customers have overdue orders
- **Database Errors**: Framework-level exceptions (e.g., `DbException`) propagate to caller; no special handling required
- **Null References**: Customer name construction handles nulls by preferring `CompanyName` over concatenated `FirstName + LastName`

### Logging

```csharp
// At method entry
_logger.LogInformation("Retrieving customers with overdue orders");

// On query execution
_logger.LogDebug("Found {Count} customers with overdue orders", results.Length);
```

### Security

Not Required - No authorization checks, PII handling, or input validation needed. This is an internal console command for operational use, not exposed to external users.

### Idempotency

Not Required - Read-only query operation is inherently idempotent.

## Test Strategy

### Unit Tests

**CustomerServiceTests** (Sales.Services.UnitTests/CustomerServiceTests.cs)

```
GetCustomersWithOverdueOrders Tests:
├─ GetCustomersWithOverdueOrders_WithNoOverdueOrders_ReturnsEmptyArray
├─ GetCustomersWithOverdueOrders_WithOverdueOrders_ReturnsCustomers
├─ GetCustomersWithOverdueOrders_WithMultipleOverduePerCustomer_AggregatesCorrectly
├─ GetCustomersWithOverdueOrders_FiltersByStatusShipped_ExcludesShippedOrders
├─ GetCustomersWithOverdueOrders_FiltersByStatusCancelled_ExcludesCancelledOrders
├─ GetCustomersWithOverdueOrders_FiltersByDueDate_ExcludesFutureOrders
├─ GetCustomersWithOverdueOrders_SortsByOldestDueDate_Ascending
└─ GetCustomersWithOverdueOrders_HandlesNullCompanyName_UsesFirstLastName
```

### Integration Tests

Not Required - Unit tests with in-memory repository or mocked `IRepository` are sufficient. Full integration testing would require database setup with complex fixture data.

### Acceptance Criteria

**Requirement**: "Display customers with at least one overdue order"
- **Test**: `GetCustomersWithOverdueOrders_WithOverdueOrders_ReturnsCustomers`
- **Acceptance**: Result array contains only customers with at least one order where `DueDate < Today` AND `Status NOT IN (5, 6)`

**Requirement**: "Show count of overdue orders per customer"
- **Test**: `GetCustomersWithOverdueOrders_WithMultipleOverduePerCustomer_AggregatesCorrectly`
- **Acceptance**: `OverdueOrderCount` matches actual count of overdue orders per customer

**Requirement**: "Show oldest overdue order date"
- **Test**: `GetCustomersWithOverdueOrders_SortsByOldestDueDate_Ascending`
- **Acceptance**: `OldestOverdueOrderDate` equals MIN(DueDate) for each customer's overdue orders; results sorted ascending by this field

**Requirement**: "Exclude closed orders"
- **Tests**: `FiltersByStatusShipped_ExcludesShippedOrders`, `FiltersByStatusCancelled_ExcludesCancelledOrders`
- **Acceptance**: Orders with `Status = 5` (Shipped) or `Status = 6` (Cancelled) are excluded from results

## Implementation Plan

**Phase 1: Contracts & DTOs** (0.25 day)
- [ ] Add `GetCustomersWithOverdueOrders()` method to `ICustomerService` interface in `Modules/Contracts/Sales/ICustomerService.cs`
- [ ] Create `CustomerOverdueOrdersData` DTO in `Modules/Contracts/Sales/CustomerOverdueOrdersData.cs`

**Phase 2: Service Implementation** (0.5 day)
- [ ] Implement `GetCustomersWithOverdueOrders()` method in `Sales.Services/CustomerService.cs`
- [ ] Add LINQ query to aggregate overdue orders per customer
- [ ] Map results to `CustomerOverdueOrdersData[]`
- [ ] Add logging statements

**Phase 3: Console Command** (0.25 day)
- [ ] Create `ShowCustomersWithOverdueOrdersConsoleCommand` in `Sales.ConsoleCommands/`
- [ ] Implement `IConsoleCommand` interface with `[Service]` attribute
- [ ] Set `MenuLabel` property to "Show customers with overdue orders"
- [ ] Format output as table with columns: Customer Name, Overdue Orders Count, Oldest Overdue Date
- [ ] Use `IConsole.WriteEntity()` for structured output of each `CustomerOverdueOrdersData` result
- [ ] Display "No customers with overdue orders found." when results are empty

**Phase 4: Testing** (0.5 day)
- [ ] Write unit tests in `Sales.Services.UnitTests/CustomerServiceTests.cs`
- [ ] Verify edge cases: empty results, multiple orders per customer, status filtering, date filtering, sorting

## Quality Checklist
- [x] All interfaces have explicit signatures
- [x] All DTOs have validation attributes (N/A - output-only DTO)
- [x] All exceptions are documented (none needed for this feature)
- [x] Error handling is specified
- [x] Cross-cutting concerns are addressed
- [x] Edge cases are handled
- [x] Test strategy is complete
- [x] Implementation plan is actionable
