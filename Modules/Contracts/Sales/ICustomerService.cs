namespace Contracts.Sales;

public interface ICustomerService
{
    CustomerData[] GetCustomersWithOrders();

    CustomerData[] GetCustomersWithOrdersStartingWith(string prefix);

    CustomerData[] GetCustomersWithOrdersContaining(string fragment);

    /// <summary>
    /// Returns all customers that have at least one overdue order.
    /// An order is overdue when DueDate is earlier than today and Status is not Shipped or Cancelled.
    /// Results are ordered by the oldest overdue due date ascending.
    /// </summary>
    /// <returns>
    /// Array of <see cref="OverdueCustomerSummary"/>, empty when no overdue customers exist.
    /// </returns>
    OverdueCustomerSummary[] GetCustomersWithOverdueOrders();
}
