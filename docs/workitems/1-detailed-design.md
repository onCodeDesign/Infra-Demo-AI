# Detailed Design: Show customers with overdue orders

**Issue**: #1  
**Architecture Design**: docs/workitems/1-design.md  
**Date**: 2026-01-14  
**Status**: Awaiting Review

## Requirements Summary

As a business user, I want to see all customers that have at least one overdue order, so that I can identify accounts that require follow up.

**Overdue Order Definition**: An order where `DueDate < Today` AND `Status != Closed` (where Closed means Shipped=5 or Cancelled=6)

**Key Requirements**:
- List only customers with at least one overdue order
- Group orders by customer
- Sort customers by oldest overdue order date (ascending)
- Display: Customer name, number of overdue orders, date of oldest overdue order
- Accessible through console command

## Module-Level Contracts

### Interfaces

#### ICustomerService Extension

**File**: `Modules/Contracts/Sales/ICustomerService.cs`

```csharp
namespace Contracts.Sales;

public interface ICustomerService
{
    // Existing methods (not changed)
    CustomerData[] GetCustomersWithOrders();
    CustomerData[] GetCustomersWithOrdersStartingWith(string prefix);
    CustomerData[] GetCustomersWithOrdersContaining(string fragment);
    
    /// <summary>
    /// Retrieves all customers that have at least one overdue order.
    /// An order is considered overdue when its due date is earlier than today 
    /// and its status is not Shipped (5) or Cancelled (6).
    /// </summary>
    /// <returns>
    /// Array of customers with overdue order information, ordered by oldest overdue order date (ascending).
    /// Returns empty array if no customers have overdue orders.
    /// </returns>
    /// <remarks>
    /// This method follows the synchronous pattern established by existing CustomerService methods.
    /// When CustomerService is refactored to async, this method should also be converted.
    /// </remarks>
    CustomerWithOverdueOrdersData[] GetCustomersWithOverdueOrders();
}
```

**Design Notes**:
- Returns array (not nullable) - empty array indicates no results
- Synchronous to maintain consistency with existing `ICustomerService` methods
- No parameters needed - query logic is well-defined in requirements
- No exceptions thrown for normal operation (no validation errors possible)

### DTOs and Data Contracts

#### CustomerWithOverdueOrdersData

**File**: `Modules/Contracts/Sales/CustomerWithOverdueOrdersData.cs`

```csharp
namespace Contracts.Sales;

/// <summary>
/// Represents a customer with overdue order summary information.
/// </summary>
public sealed class CustomerWithOverdueOrdersData
{
    /// <summary>
    /// Display name of the customer.
    /// Uses CompanyName if available, otherwise combines FirstName and LastName.
    /// </summary>
    public required string CustomerName { get; init; }
    
    /// <summary>
    /// Total number of overdue orders for this customer.
    /// An order is overdue if DueDate is before today and status is not Shipped or Cancelled.
    /// </summary>
    public required int OverdueOrdersCount { get; init; }
    
    /// <summary>
    /// Date of the oldest overdue order for this customer.
    /// Used for sorting customers (ascending).
    /// </summary>
    public required DateTime OldestDueDate { get; init; }
}
```

**Design Notes**:
- Uses `record` type with `required` properties for immutability and compile-time enforcement
- No validation attributes needed - this is a read-only DTO populated by the service
- All properties are non-nullable - query guarantees valid data
- Property names are business-friendly (not technical)

### Fault Contracts

**None Required**

This is a read-only query operation with no validation or business rules that can fail:
- No user input to validate
- No external dependencies
- No state changes
- No concurrency concerns

The operation returns an empty array if no data matches criteria, which is a valid business outcome (not an error).

## Internal API Contracts

**None Required**

The query logic is straightforward enough to implement directly in `CustomerService` without additional internal abstractions:
- Single LINQ query with filtering and aggregation
- No complex validation logic
- No reusable components needed by other services

Adding internal abstractions would over-engineer this simple read operation.

## Data Model

### Entities

**No New Entities Required**

The feature uses existing entities from `Sales.DataModel.SalesLT`:

#### Customer Entity (Existing)
**File**: `Modules/Sales/Sales.DataModel/Generated/SalesLT/Customer.cs`

**Properties Used**:
- `CustomerID` (int) - Primary key
- `FirstName` (string) - For display name fallback
- `LastName` (string) - For display name fallback
- `CompanyName` (string?) - Preferred display name
- `SalesOrderHeaders` (List<SalesOrderHeader>) - Navigation property for orders

#### SalesOrderHeader Entity (Existing)
**File**: `Modules/Sales/Sales.DataModel/Generated/SalesLT/SalesOrderHeader.cs`

**Properties Used**:
- `SalesOrderID` (int) - Primary key
- `DueDate` (DateTime) - For overdue calculation
- `Status` (byte) - For filtering closed orders
- `CustomerID` (int) - Foreign key to Customer

#### SalesOrderHeaderStatusValues (Existing)
**File**: `Modules/Sales/Sales.DataModel/Values/SalesOrderHeaderStatusValues.cs`

**Constants Used**:
- `Shipped = 5` - Closed status (exclude from overdue)
- `Cancelled = 6` - Closed status (exclude from overdue)

### Entity Interceptors

**None Required**

This is a read-only operation. Entity interceptors are used for:
- Calculated fields on save (e.g., TotalDue calculation)
- Audit fields (e.g., ModifiedDate)
- Cross-cutting validation on entity changes

Since we're only querying data without modifications, no interceptors are needed.

### Database Changes

**None Required**

All required entities, relationships, and indexes already exist:
- `Customer` table with `CustomerID` primary key
- `SalesOrderHeader` table with `CustomerID` foreign key
- Navigation properties configured in DbContext
- Existing indexes on foreign keys (adequate for this query)

**Performance Note**: The query filters on `DueDate` and `Status`. If performance becomes an issue with large datasets, consider adding a composite index on `(CustomerID, DueDate, Status)` in a future optimization.

## External Systems Integration

**Not Applicable**

This feature operates entirely within the Sales module using local database queries. No external systems, APIs, or message queues are involved.

## Cross-Cutting Concerns

### Error Handling Strategy

| Scenario | Handling Strategy | Rationale |
|----------|------------------|-----------|
| **No customers found** | Return empty array `[]` | Valid business outcome, not an error |
| **Database connection failure** | Let exception propagate | Infrastructure concern; handled by host |
| **Query timeout** | Let exception propagate | Infrastructure concern; handled by host |
| **Invalid entity state** | N/A | Query is read-only, no state changes |

**No Custom Exceptions Needed**: This operation has no business-specific failure modes that require custom exception types.

### Logging Strategy

**Service Entry/Exit Logging**:
```csharp
_logger.LogInformation("Retrieving customers with overdue orders");
// ... query execution ...
_logger.LogInformation("Found {Count} customers with overdue orders", result.Length);
```

**No Debug/Trace Logging Needed**: Query is simple enough that INFO-level logging provides sufficient observability.

**Error Logging**: Not needed in service - infrastructure layer logs unhandled exceptions.

### Security Controls

| Control | Implementation | Status |
|---------|---------------|--------|
| **Authorization** | Not required - read-only reporting | ✓ Not applicable |
| **Input Validation** | Not required - no parameters | ✓ Not applicable |
| **SQL Injection** | Protected by EF Core parameterization | ✓ Built-in |
| **Sensitive Data** | No PII in result DTO (only customer names) | ✓ Compliant |
| **Output Encoding** | Console output - no XSS concerns | ✓ Not applicable |

### Idempotency

**Naturally Idempotent**: This is a read-only query operation. Multiple invocations with no intervening state changes return identical results.

No special idempotency handling required.

### Performance Considerations

**Query Efficiency**:
- Single database round-trip
- Filtering performed at database level (WHERE clause)
- Aggregation performed in database (COUNT, MIN)
- Projection to DTO minimizes data transfer
- Navigation properties loaded efficiently via JOIN

**Expected Complexity**: O(n) where n = number of customers with orders. Typical execution time: <100ms for databases with <10,000 customers.

**No Caching Needed**: Data changes frequently (order statuses update regularly), and query is fast enough for on-demand execution.

## Test Strategy

### Unit Tests

**Test Project**: `Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs`

**Note**: Current repository has no test infrastructure for Services. If test project doesn't exist, create it following xUnit conventions.

#### Test Cases

```csharp
public class CustomerServiceTests
{
    // Happy Path Tests
    [Fact]
    public void GetCustomersWithOverdueOrders_WithOverdueOrders_ReturnsCustomersSortedByOldestDueDate()
    {
        // Arrange: Mock repository with customers having overdue orders
        // - Customer A: 2 overdue orders (oldest: 2026-01-01)
        // - Customer B: 1 overdue order (oldest: 2025-12-15)
        // - Customer C: 3 overdue orders (oldest: 2026-01-10)
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Customer B", result[0].CustomerName); // Oldest first
        Assert.Equal("Customer A", result[1].CustomerName);
        Assert.Equal("Customer C", result[2].CustomerName);
    }
    
    [Fact]
    public void GetCustomersWithOverdueOrders_CalculatesCorrectOverdueCount()
    {
        // Arrange: Customer with mixed order statuses
        // - Order 1: DueDate = yesterday, Status = InProcess (1) → OVERDUE
        // - Order 2: DueDate = yesterday, Status = Shipped (5) → NOT OVERDUE
        // - Order 3: DueDate = last week, Status = Approved (2) → OVERDUE
        // - Order 4: DueDate = tomorrow, Status = InProcess (1) → NOT OVERDUE
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].OverdueOrdersCount); // Only 2 are overdue
    }
    
    [Fact]
    public void GetCustomersWithOverdueOrders_UsesCompanyName_WhenAvailable()
    {
        // Arrange: Customer with CompanyName set
        // - CompanyName = "Acme Corp"
        // - FirstName = "John"
        // - LastName = "Doe"
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Equal("Acme Corp", result[0].CustomerName);
    }
    
    [Fact]
    public void GetCustomersWithOverdueOrders_CombinesFirstAndLastName_WhenCompanyNameIsNull()
    {
        // Arrange: Customer without CompanyName
        // - CompanyName = null
        // - FirstName = "Jane"
        // - LastName = "Smith"
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Equal("Jane Smith", result[0].CustomerName);
    }
    
    // Edge Case Tests
    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesShippedOrders()
    {
        // Arrange: Customer with all orders Shipped (status = 5)
        // - Order 1: DueDate = yesterday, Status = Shipped (5)
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Empty(result); // Shipped orders are not overdue
    }
    
    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesCancelledOrders()
    {
        // Arrange: Customer with all orders Cancelled (status = 6)
        // - Order 1: DueDate = yesterday, Status = Cancelled (6)
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Empty(result); // Cancelled orders are not overdue
    }
    
    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesFutureOrders()
    {
        // Arrange: Customer with all orders due in the future
        // - Order 1: DueDate = tomorrow, Status = InProcess (1)
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Empty(result); // Future orders are not overdue
    }
    
    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesCustomersWithNoOrders()
    {
        // Arrange: Customer with SalesOrderHeaders = empty list
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public void GetCustomersWithOverdueOrders_ReturnsEmptyArray_WhenNoCustomersExist()
    {
        // Arrange: Empty database
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    [Fact]
    public void GetCustomersWithOverdueOrders_IncludesOrdersDueToday()
    {
        // Arrange: Order with DueDate = today, Status = InProcess
        // Note: "earlier than today" means DueDate < DateTime.Today
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert
        Assert.Empty(result); // Today's orders are NOT overdue
    }
}
```

**Mocking Strategy**:
- Mock `IRepository.GetEntities<Customer>()` to return in-memory test data
- Use real LINQ evaluation (not mocking LINQ operators)
- Set up test data with various order statuses and due dates

### Integration Tests

**Test Project**: `Modules/Sales/Sales.Services.IntegrationTests/CustomerServiceIntegrationTests.cs`

**Note**: Integration tests require actual database (or in-memory EF provider).

#### Test Cases

```csharp
public class CustomerServiceIntegrationTests : IDisposable
{
    // End-to-End Test
    [Fact]
    public void GetCustomersWithOverdueOrders_WithRealDatabase_ReturnsCorrectResults()
    {
        // Arrange: Seed database with test data
        // - Insert 3 customers
        // - Insert mix of overdue and non-overdue orders
        
        // Act
        var result = _customerService.GetCustomersWithOverdueOrders();
        
        // Assert: Verify against known test data
        Assert.Equal(expectedCount, result.Length);
        // ... detailed assertions
    }
    
    // Performance Test
    [Fact]
    public void GetCustomersWithOverdueOrders_WithLargeDataset_CompletesWithinTimeout()
    {
        // Arrange: Seed database with 1000 customers, 10,000 orders
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = _customerService.GetCustomersWithOverdueOrders();
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Query took {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

### Console Command Tests

**Test Project**: `Modules/Sales/Sales.ConsoleCommands.UnitTests/CustomersWithOverdueOrdersConsoleCommandTests.cs`

#### Test Cases

```csharp
public class CustomersWithOverdueOrdersConsoleCommandTests
{
    [Fact]
    public void Execute_WithResults_DisplaysCustomerInformation()
    {
        // Arrange: Mock ICustomerService to return test data
        // Mock IConsole to capture output
        
        // Act
        _command.Execute();
        
        // Assert
        _mockConsole.Verify(c => c.WriteEntity(It.IsAny<CustomerWithOverdueOrdersData>()), 
            Times.Exactly(3));
    }
    
    [Fact]
    public void Execute_WithNoResults_DisplaysNoCustomersMessage()
    {
        // Arrange: Mock ICustomerService to return empty array
        
        // Act
        _command.Execute();
        
        // Assert
        _mockConsole.Verify(c => c.WriteLine("No customers with overdue orders found."));
    }
    
    [Fact]
    public void MenuLabel_ReturnsCorrectLabel()
    {
        // Assert
        Assert.Equal("Show customers with overdue orders", _command.MenuLabel);
    }
}
```

### Acceptance Criteria Mapping

| Requirement | Test Case | Acceptance Criteria |
|-------------|-----------|---------------------|
| "List only customers with at least one overdue order" | `ExcludesCustomersWithNoOrders` | Result contains only customers with Status != Shipped/Cancelled AND DueDate < Today |
| "Orders are grouped by customer" | `CalculatesCorrectOverdueCount` | Each customer appears once with aggregate count |
| "Customers are ordered by oldest overdue order date" | `ReturnsCustomersSortedByOldestDueDate` | Result[0].OldestDueDate <= Result[1].OldestDueDate |
| "Display: Customer name" | `UsesCompanyName_WhenAvailable` | CustomerName = CompanyName ?? $"{FirstName} {LastName}" |
| "Display: Number of overdue orders" | `CalculatesCorrectOverdueCount` | OverdueOrdersCount matches filtered order count |
| "Display: Date of oldest overdue order" | `ReturnsCustomersSortedByOldestDueDate` | OldestDueDate = MIN(DueDate) for overdue orders |
| "Feature is accessible through console command" | Console command tests | Command appears in menu and executes successfully |

## Implementation Plan

### Phase 1: Contracts & DTOs (0.5 day)

**Task 1.1**: Extend `ICustomerService` interface
- **File**: `Modules/Contracts/Sales/ICustomerService.cs`
- **Changes**: Add `GetCustomersWithOverdueOrders()` method signature
- **Dependencies**: None
- **Validation**: Compile successfully, no breaking changes

**Task 1.2**: Create `CustomerWithOverdueOrdersData` DTO
- **File**: `Modules/Contracts/Sales/CustomerWithOverdueOrdersData.cs` (new)
- **Changes**: Define DTO class with 3 properties
- **Dependencies**: None
- **Validation**: Compile successfully

### Phase 2: Service Implementation (1 day)

**Task 2.1**: Implement `GetCustomersWithOverdueOrders()` in `CustomerService`
- **File**: `Modules/Sales/Sales.Services/CustomerService.cs`
- **Changes**: Add method implementation with LINQ query
- **Dependencies**: Task 1.1, Task 1.2
- **Key Logic**:
  ```csharp
  public CustomerWithOverdueOrdersData[] GetCustomersWithOverdueOrders()
  {
      var today = DateTime.Today;
      
      var query = repository.GetEntities<Customer>()
          .Where(c => c.SalesOrderHeaders.Any(o => 
              o.DueDate < today && 
              o.Status != SalesOrderHeaderStatusValues.Shipped && 
              o.Status != SalesOrderHeaderStatusValues.Cancelled))
          .Select(c => new
          {
              Customer = c,
              OverdueOrders = c.SalesOrderHeaders
                  .Where(o => o.DueDate < today && 
                              o.Status != SalesOrderHeaderStatusValues.Shipped && 
                              o.Status != SalesOrderHeaderStatusValues.Cancelled)
                  .ToList()
          })
          .Select(x => new CustomerWithOverdueOrdersData
          {
              CustomerName = x.Customer.CompanyName ?? 
                            $"{x.Customer.FirstName} {x.Customer.LastName}",
              OverdueOrdersCount = x.OverdueOrders.Count,
              OldestDueDate = x.OverdueOrders.Min(o => o.DueDate)
          })
          .OrderBy(x => x.OldestDueDate);
      
      return query.ToArray();
  }
  ```
- **Validation**: 
  - Unit tests pass
  - Query translates to SQL correctly (verify with logging)

**Task 2.2**: Add required using statements
- **File**: `Modules/Sales/Sales.Services/CustomerService.cs`
- **Changes**: Add `using Sales.DataModel.Values;` if not present
- **Dependencies**: Task 2.1
- **Validation**: Compile successfully

### Phase 3: Console Command (0.5 day)

**Task 3.1**: Create `CustomersWithOverdueOrdersConsoleCommand`
- **File**: `Modules/Sales/Sales.ConsoleCommands/CustomersWithOverdueOrdersConsoleCommand.cs` (new)
- **Changes**: Implement `IConsoleCommand` interface
- **Dependencies**: Task 1.1, Task 1.2
- **Key Logic**:
  ```csharp
  [Service(typeof(IConsoleCommand))]
  internal sealed class CustomersWithOverdueOrdersConsoleCommand(
      IConsole console, 
      ICustomerService customerService) : IConsoleCommand
  {
      public string MenuLabel => "Show customers with overdue orders";
      
      public void Execute()
      {
          console.WriteLine("Retrieving customers with overdue orders...");
          
          CustomerWithOverdueOrdersData[] customers = 
              customerService.GetCustomersWithOverdueOrders();
          
          if (customers.Length == 0)
          {
              console.WriteLine("No customers with overdue orders found.");
              return;
          }
          
          console.WriteLine($"Found {customers.Length} customer(s) with overdue orders:");
          console.WriteLine();
          
          foreach (var customer in customers)
          {
              console.WriteEntity(customer);
          }
      }
  }
  ```
- **Validation**: 
  - Compile successfully
  - Command auto-discovered via `[Service]` attribute

### Phase 4: Testing (1 day)

**Task 4.1**: Write unit tests for `CustomerService`
- **File**: `Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs` (new or extend)
- **Changes**: Add 10 test cases covering happy path and edge cases
- **Dependencies**: Task 2.1
- **Validation**: All tests pass

**Task 4.2**: Write unit tests for console command
- **File**: `Modules/Sales/Sales.ConsoleCommands.UnitTests/CustomersWithOverdueOrdersConsoleCommandTests.cs` (new)
- **Changes**: Add 3 test cases for command behavior
- **Dependencies**: Task 3.1
- **Validation**: All tests pass

**Task 4.3**: Manual testing via console UI
- **Action**: Run `dotnet run --project UI/ConsoleUi`
- **Steps**: 
  1. Select "Show customers with overdue orders" from menu
  2. Verify output format and data accuracy
  3. Test with empty database
  4. Test with various order statuses
- **Dependencies**: Task 3.1
- **Validation**: Results match expectations

### Phase 5: Integration & Finalization (0.5 day)

**Task 5.1**: Verify plugin loading
- **Action**: Ensure `Sales.ConsoleCommands` is included in plugin configuration
- **File**: `UI/ConsoleUi/Program.cs`
- **Changes**: None expected (already configured)
- **Validation**: Console command appears in menu

**Task 5.2**: Code review
- **Action**: Self-review code for consistency with repository patterns
- **Checklist**:
  - [x] Dependency rules followed (no cross-module refs)
  - [x] Service registered via `[Service]` attribute
  - [x] Read-only query uses `IRepository` (not `IUnitOfWork`)
  - [x] DTO in Contracts, implementation in Services
  - [x] Console command follows existing patterns
  - [x] No magic numbers (uses `SalesOrderHeaderStatusValues`)
- **Validation**: All checklist items confirmed

**Task 5.3**: Documentation update
- **File**: This document
- **Changes**: Mark as "Implemented" after code completion
- **Validation**: Document reflects actual implementation

### Total Estimate: 3.5 days

**Breakdown**:
- Phase 1 (Contracts): 0.5 day
- Phase 2 (Service): 1.0 day
- Phase 3 (Console Command): 0.5 day
- Phase 4 (Testing): 1.0 day
- Phase 5 (Integration): 0.5 day

**Risk Factors**:
- Test project setup (if not exists): +0.5 day
- Query performance issues (if large dataset): +0.5 day
- Unexpected EF Core translation issues: +0.25 day

**Dependencies**:
- No external team dependencies
- No infrastructure changes required
- No database migrations needed

## Quality Checklist

- [x] **Clear Interfaces**: `ICustomerService.GetCustomersWithOverdueOrders()` method fully specified with return type, parameters, and XML documentation
- [x] **Complete DTOs**: `CustomerWithOverdueOrdersData` defined with all properties, types, and nullability
- [x] **Fault Contracts**: No custom exceptions needed - operation cannot fail in business-specific ways
- [x] **Internal APIs**: No internal abstractions needed - query logic is straightforward
- [x] **Data Model**: All entities identified, no new entities or migrations required
- [x] **Entity Interceptors**: Not applicable - read-only operation
- [x] **Cross-Cutting Concerns**: Error handling, logging, and security addressed
- [x] **Edge Cases**: Empty results, null handling, closed orders, future orders all specified
- [x] **Test Strategy**: 13 test cases defined covering unit, integration, and acceptance criteria
- [x] **Implementation Plan**: 5 phases with 12 tasks, estimated at 3.5 days
- [x] **Architecture Compliance**: Follows dependency rules, uses IRepository, no cross-module refs
- [x] **Minimal Ambiguity**: Implementation details clear enough to code without questions

## Notes

### Design Decisions Rationale

**Why synchronous instead of async?**
- Existing `ICustomerService` methods are synchronous
- Maintaining consistency within the interface
- Architecture design document notes this should be refactored later

**Why no caching?**
- Order statuses change frequently (business requirement)
- Query is fast enough for on-demand execution
- Caching would add complexity without clear benefit

**Why no pagination?**
- Console UI displays all results (not web UI)
- Expected dataset size is manageable (<1000 customers)
- Can add pagination later if performance becomes an issue

**Why CompanyName over FirstName + LastName?**
- Business users typically think in terms of company names
- Matches existing `CustomerData` DTO pattern
- Fallback to person name ensures all customers have display names

### Future Enhancements

These are **not** part of this implementation but documented for future reference:

1. **Async Refactoring**: Convert all `ICustomerService` methods to async when infrastructure supports it
2. **Filtering**: Add optional parameters for date range or minimum overdue count
3. **Pagination**: Add skip/take parameters for large result sets
4. **Export**: Allow exporting results to CSV or Excel
5. **Notifications**: Auto-email customers with overdue orders
6. **Dashboard**: Display overdue orders in web UI with charts

### Open Questions

**None** - All design aspects are fully specified and ready for implementation.
