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

    public OverdueCustomerSummary[] GetCustomersWithOverdueOrders()
    {
        logger.LogDebug("Querying customers with overdue orders");

        var today = DateTime.Today;
        var result = repository.GetEntities<SalesOrderHeader>()
            .Where(o => o.DueDate < today
                && o.Status != SalesOrderHeaderStatusValues.Shipped
                && o.Status != SalesOrderHeaderStatusValues.Cancelled)
            .GroupBy(o => new { o.Customer.CustomerID, o.Customer.FirstName, o.Customer.LastName })
            .Select(g => new OverdueCustomerSummary
            {
                CustomerName = g.Key.FirstName + " " + g.Key.LastName,
                OverdueOrderCount = g.Count(),
                OldestOverdueDueDate = g.Min(o => o.DueDate)
            })
            .OrderBy(x => x.OldestOverdueDueDate)
            .ToArray();

        logger.LogDebug("Found {Count} customers with overdue orders", result.Length);
        return result;
    }
}

