using DataAccess;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sales.DataModel.SalesLT;
using Sales.DataModel.Values;
using Contracts.Sales;

namespace Sales.Services.UnitTests;

public class CustomerServiceTests
{
    [Fact]
    public void GetCustomersWithOverdueOrders_WithNoOverdueOrders_ReturnsEmptyArray()
    {
        var repositoryStub = new FakeRepository(Array.Empty<SalesOrderHeader>());
        var target = GetTarget(repositoryStub);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_WithOverdueOrders_ReturnsCustomers()
    {
        var customer = CreateCustomer(1, "John", "Doe", "Acme Corp");
        var overdueOrder = CreateOrder(1, customer, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.InProcess);
        var repositoryStub = new FakeRepository(new[] { overdueOrder });
        var target = GetTarget(repositoryStub);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(c => c.CustomerName == "Acme Corp" && c.OverdueOrderCount == 1);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_WithMultipleOverduePerCustomer_AggregatesCorrectly()
    {
        var customer = CreateCustomer(1, "Jane", "Smith", "Beta Inc");
        var order1 = CreateOrder(1, customer, DateTime.Today.AddDays(-10), SalesOrderHeaderStatusValues.InProcess);
        var order2 = CreateOrder(2, customer, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.Approved);
        var order3 = CreateOrder(3, customer, DateTime.Today.AddDays(-3), SalesOrderHeaderStatusValues.Backordered);
        var repositoryStub = new FakeRepository(new[] { order1, order2, order3 });
        var target = GetTarget(repositoryStub);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(c => 
            c.CustomerName == "Beta Inc" && 
            c.OverdueOrderCount == 3 && 
            c.OldestOverdueOrderDate == DateTime.Today.AddDays(-10));
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_FiltersByStatusShipped_ExcludesShippedOrders()
    {
        var customer = CreateCustomer(1, "Bob", "Jones", "Gamma Ltd");
        var overdueOrder = CreateOrder(1, customer, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.InProcess);
        var shippedOrder = CreateOrder(2, customer, DateTime.Today.AddDays(-10), SalesOrderHeaderStatusValues.Shipped);
        var repositoryStub = new FakeRepository(new[] { overdueOrder, shippedOrder });
        var target = GetTarget(repositoryStub);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(c => 
            c.CustomerName == "Gamma Ltd" && 
            c.OverdueOrderCount == 1 && 
            c.OldestOverdueOrderDate == DateTime.Today.AddDays(-5));
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_FiltersByStatusCancelled_ExcludesCancelledOrders()
    {
        var customer = CreateCustomer(1, "Alice", "Brown", "Delta Co");
        var overdueOrder = CreateOrder(1, customer, DateTime.Today.AddDays(-7), SalesOrderHeaderStatusValues.InProcess);
        var cancelledOrder = CreateOrder(2, customer, DateTime.Today.AddDays(-15), SalesOrderHeaderStatusValues.Cancelled);
        var repositoryStub = new FakeRepository(new[] { overdueOrder, cancelledOrder });
        var target = GetTarget(repositoryStub);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(c => 
            c.CustomerName == "Delta Co" && 
            c.OverdueOrderCount == 1 && 
            c.OldestOverdueOrderDate == DateTime.Today.AddDays(-7));
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_FiltersByDueDate_ExcludesFutureOrders()
    {
        var customer = CreateCustomer(1, "Tom", "Green", "Epsilon Corp");
        var overdueOrder = CreateOrder(1, customer, DateTime.Today.AddDays(-3), SalesOrderHeaderStatusValues.InProcess);
        var futureOrder = CreateOrder(2, customer, DateTime.Today.AddDays(5), SalesOrderHeaderStatusValues.InProcess);
        var repositoryStub = new FakeRepository(new[] { overdueOrder, futureOrder });
        var target = GetTarget(repositoryStub);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(c => 
            c.CustomerName == "Epsilon Corp" && 
            c.OverdueOrderCount == 1 && 
            c.OldestOverdueOrderDate == DateTime.Today.AddDays(-3));
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_SortsByOldestDueDate_Ascending()
    {
        var customer1 = CreateCustomer(1, "Alex", "White", "Zeta Inc");
        var customer2 = CreateCustomer(2, "Chris", "Black", "Alpha Corp");
        var customer3 = CreateCustomer(3, "Dana", "Gray", "Mega Ltd");
        
        var order1 = CreateOrder(1, customer1, DateTime.Today.AddDays(-3), SalesOrderHeaderStatusValues.InProcess);
        var order2 = CreateOrder(2, customer2, DateTime.Today.AddDays(-10), SalesOrderHeaderStatusValues.InProcess);
        var order3 = CreateOrder(3, customer3, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.InProcess);
        
        var repositoryStub = new FakeRepository(new[] { order1, order2, order3 });
        var target = GetTarget(repositoryStub);

        var result = target.GetCustomersWithOverdueOrders();

        var expected = new[]
        {
            new CustomerOverdueOrdersData 
            { 
                CustomerName = "Alpha Corp", 
                OverdueOrderCount = 1, 
                OldestOverdueOrderDate = DateTime.Today.AddDays(-10) 
            },
            new CustomerOverdueOrdersData 
            { 
                CustomerName = "Mega Ltd", 
                OverdueOrderCount = 1, 
                OldestOverdueOrderDate = DateTime.Today.AddDays(-5) 
            },
            new CustomerOverdueOrdersData 
            { 
                CustomerName = "Zeta Inc", 
                OverdueOrderCount = 1, 
                OldestOverdueOrderDate = DateTime.Today.AddDays(-3) 
            }
        };
        
        result.Should().BeEquivalentTo(expected, options => options
            .Including(c => c.CustomerName)
            .Including(c => c.OverdueOrderCount)
            .Including(c => c.OldestOverdueOrderDate)
            .WithStrictOrdering());
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_HandlesNullCompanyName_UsesFirstLastName()
    {
        var customer = CreateCustomer(1, "Sarah", "Wilson", null);
        var overdueOrder = CreateOrder(1, customer, DateTime.Today.AddDays(-2), SalesOrderHeaderStatusValues.InProcess);
        var repositoryStub = new FakeRepository(new[] { overdueOrder });
        var target = GetTarget(repositoryStub);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(c => c.CustomerName == "Sarah Wilson");
    }

    // ── Helpers ────────────────────────────────────────────────

    private static CustomerService GetTarget(FakeRepository repositoryStub)
        => new CustomerService(repositoryStub, Substitute.For<ILogger<CustomerService>>());

    private static Customer CreateCustomer(int id, string firstName, string lastName, string? companyName)
        => new Customer
        {
            CustomerID = id,
            FirstName = firstName,
            LastName = lastName,
            CompanyName = companyName,
            SalesOrderHeaders = new List<SalesOrderHeader>()
        };

    private static SalesOrderHeader CreateOrder(int id, Customer customer, DateTime dueDate, byte status)
    {
        var order = new SalesOrderHeader
        {
            SalesOrderID = id,
            CustomerID = customer.CustomerID,
            Customer = customer,
            DueDate = dueDate,
            Status = status,
            OrderDate = DateTime.Today.AddDays(-30),
            ShipMethod = "CARGO TRANSPORT 5",
            RevisionNumber = 1,
            SalesOrderNumber = $"SO{id}"
        };
        customer.SalesOrderHeaders.Add(order);
        return order;
    }

    private class FakeRepository : IRepository
    {
        private readonly List<object> data = new();

        public FakeRepository(SalesOrderHeader[] orders)
        {
            data.AddRange(orders);
        }

        public IQueryable<T> GetEntities<T>() where T : class
        {
            return data.OfType<T>().AsQueryable();
        }

        public IUnitOfWork CreateUnitOfWork() => throw new NotImplementedException();
    }
}
