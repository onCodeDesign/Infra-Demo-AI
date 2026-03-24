using System.Linq.Expressions;
using AppBoot.DependencyInjection;
using Contracts.Sales;
using DataAccess;
using Microsoft.Extensions.Logging;
using Sales.DataModel.SalesLT;
using Sales.DataModel.Values;

namespace Sales.Services;

[Service(typeof(ICustomerService))]
class CustomerService(IRepository repository, ILogger<CustomerService> logger) : ICustomerService
{
    public CustomerData[] GetCustomersWithOrders()
    {
        return GetCustomersWithOrdersFilteredBy(x => true);
    }

    private CustomerData[] GetCustomersWithOrdersFilteredBy(Expression<Func<Customer, bool>> filter)
    {
        var q = repository.GetEntities<Customer>()
            .Where(c => c.SalesOrderHeaders != null && c.SalesOrderHeaders.Any())
            .Where(filter)
            .OrderBy(c => c.CompanyName)
            .Select(c => new CustomerData
            {
                Id = c.CustomerID,
                CompanyName = c.CompanyName,
                SalesPerson = c.SalesPerson
            });

        return q.ToArray();
    }

    public CustomerData[] GetCustomersWithOrdersStartingWith(string prefix)
    {
        Expression<Func<Customer, bool>> filter;
        if (string.IsNullOrEmpty(prefix))
            filter = x => true;
        else
            filter = c => c.CompanyName != null && c.CompanyName.StartsWith(prefix);

        return GetCustomersWithOrdersFilteredBy(filter);
    }

    public CustomerData[] GetCustomersWithOrdersContaining(string fragment)
    {
        Expression<Func<Customer, bool>> filter;
        if (string.IsNullOrEmpty(fragment))
            filter = x => true;
        else
            filter = c => c.CompanyName != null && c.CompanyName.Contains(fragment);

        return GetCustomersWithOrdersFilteredBy(filter);
    }

    public CustomerOverdueOrdersData[] GetCustomersWithOverdueOrders()
    {
        logger.LogInformation("Retrieving customers with overdue orders");

        var today = DateTime.Today;
        var closedStatuses = new[] { SalesOrderHeaderStatusValues.Shipped, SalesOrderHeaderStatusValues.Cancelled };

        var results = repository.GetEntities<SalesOrderHeader>()
            .Where(o => o.DueDate < today && !closedStatuses.Contains(o.Status))
            .GroupBy(o => o.Customer)
            .Select(g => new CustomerOverdueOrdersData
            {
                CustomerName = !string.IsNullOrWhiteSpace(g.Key.CompanyName)
                    ? g.Key.CompanyName
                    : !string.IsNullOrWhiteSpace(g.Key.FirstName) || !string.IsNullOrWhiteSpace(g.Key.LastName)
                        ? $"{g.Key.FirstName} {g.Key.LastName}".Trim()
                        : $"Customer {g.Key.CustomerID}",
                OverdueOrderCount = g.Count(),
                OldestOverdueOrderDate = g.Min(o => o.DueDate)
            })
            .OrderBy(c => c.OldestOverdueOrderDate)
            .ToArray();

        logger.LogDebug("Found {Count} customers with overdue orders", results.Length);

        return results;
    }
}
