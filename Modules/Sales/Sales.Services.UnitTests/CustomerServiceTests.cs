using Contracts.Sales;
using Sales.DataModel.SalesLT;
using Sales.DataModel.Values;
using Sales.Services.UnitTests.Fakes;

namespace Sales.Services.UnitTests;

public class CustomerServiceTests
{
    [Fact]
    public void GetCustomersWithOverdueOrders_WithNoCustomers_ReturnsEmptyArray()
    {
        var target = GetTarget(new Customer[0]);

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Empty(result);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_WithNoOverdueOrders_ReturnsEmptyArray()
    {
        var customer = CreateCustomer(1, "Acme", "John", "Doe",
            CreateOrder(1, DateTime.Today.AddDays(10), SalesOrderHeaderStatusValues.InProcess));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Empty(result);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_WithOneCustomerOneOverdueOrder_ReturnsCustomer()
    {
        var dueDate = DateTime.Today.AddDays(-5);
        var customer = CreateCustomer(1, "Acme Corp", "John", "Doe",
            CreateOrder(1, dueDate, SalesOrderHeaderStatusValues.InProcess));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Single(result);
        Assert.Equal("Acme Corp", result[0].CustomerName);
        Assert.Equal(1, result[0].OverdueOrdersCount);
        Assert.Equal(dueDate, result[0].OldestDueDate);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_WithMultipleCustomers_ReturnsSortedByOldestDueDate()
    {
        var customer1 = CreateCustomer(1, "Beta Inc", "Jane", "Smith",
            CreateOrder(1, DateTime.Today.AddDays(-3), SalesOrderHeaderStatusValues.InProcess));
        var customer2 = CreateCustomer(2, "Alpha Corp", "Bob", "Jones",
            CreateOrder(2, DateTime.Today.AddDays(-10), SalesOrderHeaderStatusValues.Approved));
        var customer3 = CreateCustomer(3, "Gamma LLC", "Alice", "Brown",
            CreateOrder(3, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.Backordered));
        var target = GetTarget(new[] { customer1, customer2, customer3 });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Equal(3, result.Length);
        Assert.Equal("Alpha Corp", result[0].CustomerName);
        Assert.Equal("Gamma LLC", result[1].CustomerName);
        Assert.Equal("Beta Inc", result[2].CustomerName);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesShippedOrders_WhenDueDatePassed()
    {
        var customer = CreateCustomer(1, "Test Co", "John", "Doe",
            CreateOrder(1, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.Shipped));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Empty(result);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesCancelledOrders_WhenDueDatePassed()
    {
        var customer = CreateCustomer(1, "Test Co", "John", "Doe",
            CreateOrder(1, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.Cancelled));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Empty(result);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_IncludesInProcessOrders_WhenDueDatePassed()
    {
        var customer = CreateCustomer(1, "Test Co", "John", "Doe",
            CreateOrder(1, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.InProcess));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Single(result);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesOrdersNotYetDue_EvenIfNotClosed()
    {
        var customer = CreateCustomer(1, "Test Co", "John", "Doe",
            CreateOrder(1, DateTime.Today.AddDays(5), SalesOrderHeaderStatusValues.InProcess));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Empty(result);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_CalculatesCountCorrectly_ForMultipleOverdueOrders()
    {
        var oldestDueDate = DateTime.Today.AddDays(-10);
        var customer = CreateCustomer(1, "Test Co", "John", "Doe",
            CreateOrder(1, oldestDueDate, SalesOrderHeaderStatusValues.InProcess),
            CreateOrder(2, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.Approved),
            CreateOrder(3, DateTime.Today.AddDays(-2), SalesOrderHeaderStatusValues.Backordered));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Single(result);
        Assert.Equal(3, result[0].OverdueOrdersCount);
        Assert.Equal(oldestDueDate, result[0].OldestDueDate);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_UsesCompanyName_WhenAvailable()
    {
        var customer = CreateCustomer(1, "Acme Corporation", "John", "Doe",
            CreateOrder(1, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.InProcess));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Single(result);
        Assert.Equal("Acme Corporation", result[0].CustomerName);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_CombinesFirstAndLastName_WhenCompanyNameNull()
    {
        var customer = CreateCustomer(1, null, "Jane", "Smith",
            CreateOrder(1, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.InProcess));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Single(result);
        Assert.Equal("Jane Smith", result[0].CustomerName);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_MixedOrderStatuses_ReturnsOnlyOverdue()
    {
        var oldestOverdue = DateTime.Today.AddDays(-8);
        var customer = CreateCustomer(1, "Test Co", "John", "Doe",
            CreateOrder(1, oldestOverdue, SalesOrderHeaderStatusValues.InProcess),
            CreateOrder(2, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.Shipped),
            CreateOrder(3, DateTime.Today.AddDays(-3), SalesOrderHeaderStatusValues.Approved),
            CreateOrder(4, DateTime.Today.AddDays(5), SalesOrderHeaderStatusValues.InProcess));
        var target = GetTarget(new[] { customer });

        var result = target.GetCustomersWithOverdueOrders();

        Assert.Single(result);
        Assert.Equal(2, result[0].OverdueOrdersCount);
        Assert.Equal(oldestOverdue, result[0].OldestDueDate);
    }

    private static CustomerService GetTarget(Customer[] customers)
    {
        var repositoryStub = new FakeRepository(customers);
        return new CustomerService(repositoryStub);
    }

    private static Customer CreateCustomer(int id, string? companyName, string firstName, string lastName, params SalesOrderHeader[] orders)
    {
        var customer = new Customer
        {
            CustomerID = id,
            CompanyName = companyName,
            FirstName = firstName,
            LastName = lastName
        };

        foreach (var order in orders)
        {
            customer.SalesOrderHeaders.Add(order);
            order.Customer = customer;
            order.CustomerID = id;
        }

        return customer;
    }

    private static SalesOrderHeader CreateOrder(int id, DateTime dueDate, byte status)
    {
        return new SalesOrderHeader
        {
            SalesOrderID = id,
            DueDate = dueDate,
            Status = status,
            OrderDate = DateTime.Today.AddDays(-30)
        };
    }
}
