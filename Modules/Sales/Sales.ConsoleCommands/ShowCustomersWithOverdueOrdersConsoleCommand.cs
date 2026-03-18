using AppBoot.DependencyInjection;
using Contracts.ConsoleUi;
using Contracts.Sales;

namespace Sales.ConsoleCommands;

[Service(typeof(IConsoleCommand))]
internal sealed class ShowCustomersWithOverdueOrdersConsoleCommand(IConsole console, ICustomerService customerService) : IConsoleCommand
{
    public string MenuLabel => "Show customers with overdue orders";

    public void Execute()
    {
        console.WriteLine("Retrieving customers with overdue orders...");

        CustomerOverdueOrdersData[] customers = customerService.GetCustomersWithOverdueOrders();

        if (customers.Length == 0)
        {
            console.WriteLine("No customers with overdue orders found.");
            return;
        }

        foreach (var c in customers)
        {
            console.WriteEntity(c);
        }
    }
}
