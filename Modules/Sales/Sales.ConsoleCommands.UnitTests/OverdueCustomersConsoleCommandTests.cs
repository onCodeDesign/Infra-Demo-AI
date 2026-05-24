using Contracts.ConsoleUi;
using Contracts.Sales;
using NSubstitute;
using Sales.ConsoleCommands;

namespace Sales.ConsoleCommands.UnitTests;

public class OverdueCustomersConsoleCommandTests
{
    [Fact]
    public void Execute_WhenNoOverdueCustomers_WritesNoResultsMessage()
    {
        var consoleMock = Substitute.For<IConsole>();
        var customerServiceStub = Substitute.For<ICustomerService>();
        customerServiceStub.GetCustomersWithOverdueOrders().Returns(Array.Empty<OverdueCustomerSummary>());
        var target = new OverdueCustomersConsoleCommand(consoleMock, customerServiceStub);

        target.Execute();

        consoleMock.Received(1).WriteLine("No customers with overdue orders found.");
    }

    [Fact]
    public void Execute_WhenOverdueCustomersExist_WritesEachCustomer()
    {
        var consoleMock = Substitute.For<IConsole>();
        var dueDate = new DateTime(2026, 1, 15);
        var summaries = new[]
        {
            new OverdueCustomerSummary { CustomerName = "John Doe", OverdueOrderCount = 2, OldestOverdueDueDate = dueDate }
        };
        var customerServiceStub = Substitute.For<ICustomerService>();
        customerServiceStub.GetCustomersWithOverdueOrders().Returns(summaries);
        var target = new OverdueCustomersConsoleCommand(consoleMock, customerServiceStub);

        target.Execute();

        consoleMock.Received(1).WriteLine($"John Doe | Overdue: 2 | Oldest: {dueDate:d}");
    }
}
