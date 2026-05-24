using DataAccess;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sales.DataModel.SalesLT;
using Sales.DataModel.Values;
using Sales.Services;

namespace Sales.Services.UnitTests;

public class CustomerServiceTests
{
    [Fact]
    public void GetCustomersWithOverdueOrders_WhenNoOrders_ReturnsEmptyArray()
    {
        var target = GetTarget(Array.Empty<SalesOrderHeader>());

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_WhenAllOrdersClosed_ReturnsEmptyArray()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var orders = new[]
        {
            CreateOrder(customer, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.Shipped),
            CreateOrder(customer, DateTime.Today.AddDays(-3), SalesOrderHeaderStatusValues.Cancelled)
        };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_WhenOrderNotYetDue_ReturnsEmptyArray()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var orders = new[] { CreateOrder(customer, DateTime.Today.AddDays(1), SalesOrderHeaderStatusValues.InProcess) };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_WhenOrderIsOverdue_ReturnsCustomer()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var orders = new[] { CreateOrder(customer, DateTime.Today.AddDays(-1), SalesOrderHeaderStatusValues.InProcess) };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle();
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesOrdersWithStatusShipped()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var orders = new[] { CreateOrder(customer, DateTime.Today.AddDays(-1), SalesOrderHeaderStatusValues.Shipped) };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_ExcludesOrdersWithStatusCancelled()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var orders = new[] { CreateOrder(customer, DateTime.Today.AddDays(-1), SalesOrderHeaderStatusValues.Cancelled) };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_OrderedByOldestOverdueDueDate_Ascending()
    {
        var customerA = CreateCustomer(1, "Alpha", "A");
        var customerB = CreateCustomer(2, "Beta", "B");
        var customerC = CreateCustomer(3, "Gamma", "C");
        var dateA = DateTime.Today.AddDays(-10);
        var dateB = DateTime.Today.AddDays(-5);
        var dateC = DateTime.Today.AddDays(-3);
        var orders = new[]
        {
            CreateOrder(customerB, dateB, SalesOrderHeaderStatusValues.InProcess),
            CreateOrder(customerC, dateC, SalesOrderHeaderStatusValues.InProcess),
            CreateOrder(customerA, dateA, SalesOrderHeaderStatusValues.InProcess)
        };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().BeEquivalentTo(
            new[]
            {
                new { CustomerName = "Alpha A", OldestOverdueDueDate = dateA },
                new { CustomerName = "Beta B", OldestOverdueDueDate = dateB },
                new { CustomerName = "Gamma C", OldestOverdueDueDate = dateC }
            },
            options => options.Including(x => x.CustomerName).Including(x => x.OldestOverdueDueDate).WithStrictOrdering()
        );
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_CustomerName_IsConcatenatedFirstAndLastName()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var orders = new[] { CreateOrder(customer, DateTime.Today.AddDays(-1), SalesOrderHeaderStatusValues.InProcess) };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(x => x.CustomerName == "John Doe");
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_OverdueOrderCount_ReflectsOnlyOverdueOrders()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var orders = new[]
        {
            CreateOrder(customer, DateTime.Today.AddDays(-3), SalesOrderHeaderStatusValues.InProcess),
            CreateOrder(customer, DateTime.Today.AddDays(-1), SalesOrderHeaderStatusValues.Approved),
            CreateOrder(customer, DateTime.Today.AddDays(-2), SalesOrderHeaderStatusValues.Shipped)
        };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(x => x.OverdueOrderCount == 2);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_OldestOverdueDueDate_IsMinimumDueDatePerCustomer()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var oldestDate = DateTime.Today.AddDays(-10);
        var orders = new[]
        {
            CreateOrder(customer, oldestDate, SalesOrderHeaderStatusValues.InProcess),
            CreateOrder(customer, DateTime.Today.AddDays(-3), SalesOrderHeaderStatusValues.InProcess)
        };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(x => x.OldestOverdueDueDate == oldestDate);
    }

    [Fact]
    public void GetCustomersWithOverdueOrders_CustomerWithMixedOrders_OnlyCountsOverdueOnes()
    {
        var customer = CreateCustomer(1, "John", "Doe");
        var orders = new[]
        {
            CreateOrder(customer, DateTime.Today.AddDays(-5), SalesOrderHeaderStatusValues.InProcess),
            CreateOrder(customer, DateTime.Today.AddDays(2), SalesOrderHeaderStatusValues.InProcess),
            CreateOrder(customer, DateTime.Today.AddDays(-1), SalesOrderHeaderStatusValues.Cancelled)
        };
        var target = GetTarget(orders);

        var result = target.GetCustomersWithOverdueOrders();

        result.Should().ContainSingle(x => x.OverdueOrderCount == 1);
    }

    private static CustomerService GetTarget(SalesOrderHeader[] orders)
    {
        var repositoryStub = new FakeRepository(orders);
        return new CustomerService(repositoryStub, NullLogger<CustomerService>.Instance);
    }

    private static Customer CreateCustomer(int id, string firstName, string lastName)
        => new Customer { CustomerID = id, FirstName = firstName, LastName = lastName };

    private static SalesOrderHeader CreateOrder(Customer customer, DateTime dueDate, byte status)
        => new SalesOrderHeader { Customer = customer, CustomerID = customer.CustomerID, DueDate = dueDate, Status = status };

    private class FakeRepository : IRepository
    {
        private readonly List<object> data = new();

        public FakeRepository(SalesOrderHeader[] orders)
        {
            data.AddRange(orders);
        }

        public IQueryable<T> GetEntities<T>() where T : class
            => data.OfType<T>().AsQueryable();

        public IUnitOfWork CreateUnitOfWork() => throw new NotImplementedException();
    }
}
