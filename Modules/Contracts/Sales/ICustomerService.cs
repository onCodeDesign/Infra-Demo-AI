namespace Contracts.Sales;

public interface ICustomerService
{
    CustomerData[] GetCustomersWithOrders();

    CustomerData[] GetCustomersWithOrdersStartingWith(string prefix);

    CustomerData[] GetCustomersWithOrdersContaining(string fragment);

    /// <summary>
    /// Retrieves customers that have at least one overdue order.
    /// An order is overdue when DueDate &lt; Today and Status is not Shipped or Cancelled.
    /// </summary>
    /// <returns>
    /// Array of customers with overdue order information, ordered by oldest overdue date ascending.
    /// Returns empty array if no customers with overdue orders exist.
    /// </returns>
    CustomerWithOverdueOrdersData[] GetCustomersWithOverdueOrders();
}
