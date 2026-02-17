namespace Contracts.Sales;

/// <summary>
/// Customer information with overdue order statistics
/// </summary>
public sealed class CustomerWithOverdueOrdersData
{
    /// <summary>
    /// Customer display name (CompanyName if available, otherwise FirstName + LastName)
    /// </summary>
    public required string CustomerName { get; init; }

    /// <summary>
    /// Total count of overdue orders for this customer
    /// </summary>
    public int OverdueOrdersCount { get; init; }

    /// <summary>
    /// Due date of the oldest overdue order
    /// </summary>
    public DateTime OldestDueDate { get; init; }
}
