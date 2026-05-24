namespace Contracts.Sales;

public sealed record OverdueCustomerSummary
{
    /// <summary>
    /// Full name of the customer: "{FirstName} {LastName}".
    /// </summary>
    public required string CustomerName { get; init; }

    /// <summary>
    /// Total number of overdue orders for this customer.
    /// </summary>
    public int OverdueOrderCount { get; init; }

    /// <summary>
    /// Due date of the oldest overdue order for this customer.
    /// </summary>
    public DateTime OldestOverdueDueDate { get; init; }
}
