using AppBoot.DependencyInjection;
using Contracts.ConsoleUi;
using Contracts.Sales;

namespace Sales.ConsoleCommands;

[Service(typeof(IConsoleCommand))]
internal sealed class CustomersWithOverdueOrdersConsoleCommand(IConsole console, ICustomerService customerService) : IConsoleCommand
{
    public string MenuLabel => "Show customers with overdue orders";

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
}
