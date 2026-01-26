# Detailed Design: Show customers with overdue orders

**Issue**: #1
**Architecture Design**: docs/workitems/1-design.md
**Date**: 2026-01-15
**Status**: Approved

## Requirements Summary

Display customers with at least one overdue order, where an order is overdue when `DueDate < Today` AND `Status != Closed` (Shipped or Cancelled).

**Display Requirements**:
- Customer name (CompanyName or FirstName + LastName)
- Number of overdue orders
- Date of oldest overdue order
- Sorted by oldest overdue order date (ascending)

**Access**: Console command

## Module-Level Contracts

### Interfaces

**ICustomerService Extension** (`Modules/Contracts/Sales/ICustomerService.cs`)

```csharp
namespace Contracts.Sales;

public interface ICustomerService
{
    CustomerData[] GetCustomersWithOrders();
    CustomerData[] GetCustomersWithOrdersStartingWith(string prefix);
    CustomerData[] GetCustomersWithOrdersContaining(string fragment);
    
    /// <summary>
    /// Retrieves customers that have at least one overdue order.
    /// An order is overdue when DueDate &lt; Today and Status is not Shipped or Cancelled.
    /// </summary>
    /// <returns>
    /// Array of customers with overdue order information, ordered by oldest overdue date ascending.
    /// Returns empty array if no customers with overdue orders exist.
    /// </returns>
    CustomerWithOverdueOrdersData[] GetCustomersWithOverdueOrders();
}
```

### DTOs

**CustomerWithOverdueOrdersData** (`Modules/Contracts/Sales/CustomerWithOverdueOrdersData.cs`)

```csharp
namespace Contracts.Sales;

/// <summary>
/// Customer information with overdue order statistics
/// </summary>
public sealed class CustomerWithOverdueOrdersData
{
    /// <summary>
    /// Customer display name (CompanyName if available, otherwise FirstName + LastName)
    /// </summary>
    public required string CustomerName { get; init; }
    
    /// <summary>
    /// Total count of overdue orders for this customer
    /// </summary>
    public int OverdueOrdersCount { get; init; }
    
    /// <summary>
    /// Due date of the oldest overdue order
    /// </summary>
    public DateTime OldestDueDate { get; init; }
}
```

### Fault Contracts

Not Required - Query operation returns empty array when no results found. No exceptional business rule violations expected.

## Internal API Contracts

Not Required - Business logic is straightforward and contained within a single service method.

## Data Model

### Entities

No changes required. Existing entities used:
- `Customer` (Sales.DataModel.SalesLT) - has FirstName, LastName, CompanyName, SalesOrderHeaders navigation
- `SalesOrderHeader` (Sales.DataModel.SalesLT) - has DueDate, Status, Customer navigation

### Entity Interceptors

Not Required - Read-only query operation with no data modifications.

### Database Changes

Not Required - No schema changes needed.

## External Systems Integration

Not Required - Feature operates entirely within Sales module using local database.

## Cross-Cutting Concerns

### Error Handling

**Strategy**: Fail-fast on infrastructure errors, graceful handling of empty results.

- **No customers found**: Return empty array (not an error)
- **Database connection errors**: Allow exception to propagate (infrastructure failure)
- **Null reference errors**: Prevented by null-conditional operators and LINQ aggregations

### Logging

**Strategy**: Minimal logging for query execution context.

```csharp
// Optional: Log at service boundary for diagnostics
// Example: _logger.LogDebug("Querying customers with overdue orders");
```

No error logging needed for this read-only operation unless infrastructure exceptions occur (handled at framework level).

### Security

**Input Validation**: Not applicable - no user input parameters

**Authorization**: Not specified in requirements - assumes console user has access to all customer data

**PII Handling**: Customer names displayed in console output. No PII logged.

**SQL Injection Prevention**: Ensured by Entity Framework parameterization (no raw SQL).

### Idempotency

Not applicable - Read-only query operation is naturally idempotent.

## Test Strategy

### Unit Tests

**CustomerServiceTests** (`Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs`)

**Note**: The `Sales.Services.UnitTests` project does not currently exist and will need to be created as part of the implementation (see Phase 4 below).

```
GetCustomersWithOverdueOrders_WithNoCustomers_ReturnsEmptyArray
GetCustomersWithOverdueOrders_WithNoOverdueOrders_ReturnsEmptyArray
GetCustomersWithOverdueOrders_WithOneCustomerOneOverdueOrder_ReturnsCustomer
GetCustomersWithOverdueOrders_WithMultipleCustomers_ReturnsSortedByOldestDueDate
GetCustomersWithOverdueOrders_ExcludesShippedOrders_WhenDueDatePassed
GetCustomersWithOverdueOrders_ExcludesCancelledOrders_WhenDueDatePassed
GetCustomersWithOverdueOrders_IncludesInProcessOrders_WhenDueDatePassed
GetCustomersWithOverdueOrders_ExcludesOrdersNotYetDue_EvenIfNotClosed
GetCustomersWithOverdueOrders_CalculatesCountCorrectly_ForMultipleOverdueOrders
GetCustomersWithOverdueOrders_UsesCompanyName_WhenAvailable
GetCustomersWithOverdueOrders_CombinesFirstAndLastName_WhenCompanyNameNull
```

### Integration Tests

Not Required - Sufficient coverage with unit tests using in-memory repository. Feature does not integrate with other modules.

### Acceptance Criteria

**Requirement**: "List only customers with at least one overdue order"
- **Tests**: `GetCustomersWithOverdueOrders_WithNoOverdueOrders_ReturnsEmptyArray`
- **Acceptance**: Customers with only closed or future-due orders are excluded

**Requirement**: "Orders are grouped by customer"
- **Tests**: `GetCustomersWithOverdueOrders_CalculatesCountCorrectly_ForMultipleOverdueOrders`
- **Acceptance**: Single customer entry with aggregated count

**Requirement**: "Customers ordered by oldest overdue order date, ascending"
- **Tests**: `GetCustomersWithOverdueOrders_WithMultipleCustomers_ReturnsSortedByOldestDueDate`
- **Acceptance**: Array sorted with earliest due date first

**Requirement**: "Display customer name, count, oldest due date"
- **Tests**: `GetCustomersWithOverdueOrders_WithOneCustomerOneOverdueOrder_ReturnsCustomer`
- **Acceptance**: DTO contains all three required fields

**Requirement**: "Overdue = DueDate < Today AND Status != Closed"
- **Tests**: `GetCustomersWithOverdueOrders_ExcludesShippedOrders_WhenDueDatePassed`, `GetCustomersWithOverdueOrders_IncludesInProcessOrders_WhenDueDatePassed`
- **Acceptance**: Shipped (5) and Cancelled (6) excluded; InProcess (1), Approved (2), Backordered (3), Rejected (4) included

**Requirement**: "Accessible through console command"
- **Manual Test**: Run console app, verify menu displays "Show customers with overdue orders"
- **Acceptance**: Command executes and displays formatted results

## Implementation Plan

**Phase 1: Contracts & DTOs** (0.5 day)
- [ ] Add `GetCustomersWithOverdueOrders()` method to `ICustomerService` interface in `Modules/Contracts/Sales/ICustomerService.cs`
- [ ] Create `CustomerWithOverdueOrdersData` DTO in `Modules/Contracts/Sales/CustomerWithOverdueOrdersData.cs`

**Phase 2: Service Implementation** (1 day)
- [ ] Implement `GetCustomersWithOverdueOrders()` in `Sales.Services/CustomerService.cs`
- [ ] Use `repository.GetEntities<Customer>()` and navigate `SalesOrderHeaders` via LINQ (no explicit `Include`)
- [ ] Filter orders: `DueDate < DateTime.Today` AND `Status != Shipped AND Status != Cancelled`
- [ ] Use database-level aggregations with `Count()` and `Min()` directly in projection (avoid `.ToList()`)
- [ ] Order by min(DueDate) ascending
- [ ] Project to `CustomerWithOverdueOrdersData` with name logic: `CompanyName ?? $"{FirstName} {LastName}"`
- [ ] Reference `SalesOrderHeaderStatusValues.Shipped` and `SalesOrderHeaderStatusValues.Cancelled` constants

**Phase 3: Console Command** (0.5 day)
- [ ] Create `CustomersWithOverdueOrdersConsoleCommand` in `Sales.ConsoleCommands/CustomersWithOverdueOrdersConsoleCommand.cs`
- [ ] Implement `IConsoleCommand` with `MenuLabel = "Show customers with overdue orders"`
- [ ] Call `ICustomerService.GetCustomersWithOverdueOrders()`
- [ ] Display results using `console.WriteEntity()` or formatted output

**Phase 4: Testing** (1 day)
- [ ] Create `Sales.Services.UnitTests` project with xUnit.v3 package reference (project does not exist yet)
- [ ] Add project reference to `Sales.Services` and test framework dependencies
- [ ] Write unit tests for `GetCustomersWithOverdueOrders()` covering all scenarios
- [ ] Mock `IRepository` to return test data with various order statuses and due dates
- [ ] Verify sorting, filtering, and aggregation logic

**Phase 5: Manual Verification** (0.5 day)
- [ ] Build and run console application
- [ ] Execute "Show customers with overdue orders" command
- [ ] Verify output matches expected format and data
- [ ] Test with empty results, single customer, multiple customers

## Implementation Details

### Service Method Signature

```csharp
using Sales.DataModel.Values;

public CustomerWithOverdueOrdersData[] GetCustomersWithOverdueOrders()
{
    var today = DateTime.Today;
    var closedStatuses = new[] { SalesOrderHeaderStatusValues.Shipped, SalesOrderHeaderStatusValues.Cancelled };
    
    // Query logic: filter, group, aggregate, sort, project
}
```

### LINQ Query Structure

```csharp
repository.GetEntities<Customer>()
    .Where(c => c.SalesOrderHeaders.Any(o => 
        o.DueDate < today && 
        !closedStatuses.Contains(o.Status)))
    .Select(c => new CustomerWithOverdueOrdersData
    {
        CustomerName = c.CompanyName ?? $"{c.FirstName} {c.LastName}",
        OverdueOrdersCount = c.SalesOrderHeaders
            .Where(o => o.DueDate < today && !closedStatuses.Contains(o.Status))
            .Count(),
        OldestDueDate = c.SalesOrderHeaders
            .Where(o => o.DueDate < today && !closedStatuses.Contains(o.Status))
            .Min(o => o.DueDate)
    })
    .OrderBy(x => x.OldestDueDate)
    .ToArray();
```

**Note**: The overdue order filter is repeated in the projection for `OverdueOrdersCount` and `OldestDueDate`. While this appears duplicative, it's necessary for EF Core to translate the aggregations correctly to SQL. The filter in the initial `Where` clause ensures only customers with overdue orders are included. An alternative would be to extract the filter logic to a variable for better maintainability if needed during implementation.

### Console Command Display Logic

```csharp
public void Execute()
{
    console.WriteLine("Retrieving customers with overdue orders...");
    
    var customers = customerService.GetCustomersWithOverdueOrders();
    
    if (customers.Length == 0)
    {
        console.WriteLine("No customers with overdue orders found.");
        return;
    }
    
    console.WriteLine($"Found {customers.Length} customer(s) with overdue orders:\n");
    
    foreach (var customer in customers)
    {
        console.WriteEntity(customer);
    }
}
```

## Quality Checklist

- [x] All interfaces have explicit signatures
- [x] All DTOs have validation attributes (N/A - no validation needed for output DTO)
- [x] All exceptions are documented (N/A - no custom exceptions)
- [x] Error handling is specified
- [x] Cross-cutting concerns are addressed
- [x] Edge cases are handled (empty results, null company name, multiple overdue orders)
- [x] Test strategy is complete
- [x] Implementation plan is actionable
- [x] Follows repository dependency rules (Services → Contracts, DataModel, DataAccess only)
- [x] Uses existing patterns (synchronous methods like other `CustomerService` methods)
- [x] No cross-module dependencies
- [x] Read-only operation uses `IRepository.GetEntities<T>()`
