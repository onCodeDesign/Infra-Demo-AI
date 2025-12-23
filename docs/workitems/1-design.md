# Design: Show customers with overdue orders

**Issue**: #1  
**Date**: 2025-12-23  
**Status**: Awaiting Review

## Requirements Summary

As a business user, I want to see all customers that have at least one overdue order, so that I can identify accounts that require follow up.

**Overdue Order Definition**: An order where `DueDate < Today` AND `Status != Closed`

**Key Requirements**:
- List only customers with at least one overdue order
- Group orders by customer
- Sort customers by oldest overdue order date (ascending)
- Display: Customer name, number of overdue orders, date of oldest overdue order
- Accessible through console command

## Module Impact

- [x] Sales
- [ ] ProductsManagement
- [ ] PersonsManagement
- [ ] Notifications
- [ ] Export
- [ ] New Module

**Justification**: This feature is entirely within the Sales domain, involving customers and their order information. All necessary entities already exist in `Sales.DataModel`.

## High-Level Design

### Services

**CustomerService** (`Sales.Services/CustomerService.cs`)
- **Extension**: Add method to query customers with overdue orders
- **Responsibility**: Execute repository query to filter customers based on overdue order criteria, aggregate order statistics per customer, and map to DTO

**Key Logic**:
- Filter orders where `DueDate < DateTime.Today` and `Status != ClosedStatusValue`
- Group by customer
- Calculate count of overdue orders per customer
- Find oldest overdue order date per customer
- Sort customers by oldest overdue order date (ascending)

### Data Transfer Objects

**New DTO**: `CustomerWithOverdueOrdersData` (`Modules/Contracts/Sales/`)
- Properties:
  - `CustomerName` (string): Display name of the customer
  - `OverdueOrdersCount` (int): Number of overdue orders
  - `OldestDueDate` (DateTime): Date of the oldest overdue order

### Console Commands

**New Command**: `CustomersWithOverdueOrdersConsoleCommand` (`Sales.ConsoleCommands/`)
- Menu label: "Show customers with overdue orders"
- Calls `ICustomerService.GetCustomersWithOverdueOrders()`
- Displays results using console helper methods

### Contracts

**ICustomerService** (`Modules/Contracts/Sales/ICustomerService.cs`)
- **Extension**: Add new method signature for retrieving customers with overdue orders

### Entities

**No Changes Required**
- `Customer` entity (Sales.DataModel.SalesLT) - already exists
- `SalesOrderHeader` entity (Sales.DataModel.SalesLT) - already exists
- Relationship: `Customer.SalesOrderHeaders` navigation property - already exists

**Status Field**: The `SalesOrderHeader.Status` field is of type `byte`. The existing `SalesOrderHeaderStatusValues` class (Sales.DataModel/Values/SalesOrderHeaderStatusValues.cs) defines status constants (InProcess=1, Approved=2, Backordered=3, Rejected=4, Shipped=5, Cancelled=6). For the overdue order definition, orders with status Shipped (5) or Cancelled (6) should be considered "closed" and excluded from the overdue list.

## Integration Flow

```
1. User selects console command
2. CustomersWithOverdueOrdersConsoleCommand.Execute() called
3. Command invokes ICustomerService.GetCustomersWithOverdueOrders()
4. CustomerService queries via IRepository:
   - GetEntities<Customer>() with navigation to SalesOrderHeaders
   - Filter orders: DueDate < Today AND Status != Closed
   - Group by customer
   - Aggregate: count overdue orders, find min(DueDate)
   - Order by min(DueDate) ascending
   - Project to CustomerWithOverdueOrdersData
5. Service returns array of CustomerWithOverdueOrdersData
6. Console command displays results to user
```

### Sequence Diagram

```mermaid
sequenceDiagram
    participant UI as ConsoleCommand
    participant CS as CustomerService
    participant Repo as IRepository
    
    UI->>CS: GetCustomersWithOverdueOrders()
    CS->>Repo: GetEntities<Customer>()
    Repo-->>CS: IQueryable<Customer>
    CS->>CS: Filter & aggregate<br/>(overdue logic)
    CS->>CS: Map to DTO
    CS-->>UI: CustomerWithOverdueOrdersData[]
    UI->>UI: Display results
```

## Boundary Verification

- [x] No `*.Services` → `*.Services` cross-module references
- [x] All cross-module communication via `Contracts` interfaces (N/A - single module)
- [x] No direct DbContext usage in Services (only `IRepository`)
- [x] New interface extension added to `Contracts` (ICustomerService)
- [x] Entity interceptors not needed for read-only operation
- [x] Services use primary constructors for DI (existing pattern)
- [ ] All public APIs are async (Current implementation uses synchronous patterns for customer queries to stay consistent with existing `CustomerService` methods. This API must be refactored to async when the rest of `CustomerService` is migrated to async.)

## Design Decisions

### Status Value for "Closed"
The implementation will use the existing `SalesOrderHeaderStatusValues` class (Sales.DataModel/Values/SalesOrderHeaderStatusValues.cs) to determine "closed" orders. Orders with status Shipped (5) or Cancelled (6) are considered closed and will be excluded from overdue calculations. The query logic should reference these constants rather than using magic numbers.

### Customer Name Display
The `Customer` entity has `FirstName`, `LastName`, and `CompanyName` fields. The display name logic will:
- Use `CompanyName` if available
- Otherwise combine `FirstName` and `LastName`

### Query Efficiency
The query uses a single database call with:
- Navigation property used in database query to filter and aggregate orders
- Filter applied at database level
- Aggregations performed in database (via LINQ to Entities)
- Projection to DTO to minimize data transfer

## Next Steps

1. Implement contract: Add method to `ICustomerService`
2. Create DTO: Define `CustomerWithOverdueOrdersData` class
3. Implement service: Add method to `CustomerService`
4. Create console command: Implement `CustomersWithOverdueOrdersConsoleCommand`
5. Manual testing via console UI
6. Ready for code review
