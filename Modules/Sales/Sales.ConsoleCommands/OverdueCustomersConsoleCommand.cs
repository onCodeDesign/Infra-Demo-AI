using AppBoot.DependencyInjection;
using Contracts.ConsoleUi;
using Contracts.Sales;

namespace Sales.ConsoleCommands;

[Service(typeof(IConsoleCommand))]
internal sealed class OverdueCustomersConsoleCommand(IConsole console, ICustomerService customerService) : IConsoleCommand
{
    public string MenuLabel => "Show customers with overdue orders";

    public void Execute()
    {
        OverdueCustomerSummary[] customers = customerService.GetCustomersWithOverdueOrders();

        if (customers.Length == 0)
        {
            console.WriteLine("No customers with overdue orders found.");
            return;
        }

        foreach (var c in customers)
        {
            console.WriteLine($"{c.CustomerName} | Overdue: {c.OverdueOrderCount} | Oldest: {c.OldestOverdueDueDate:d}");
        }
    }
}
