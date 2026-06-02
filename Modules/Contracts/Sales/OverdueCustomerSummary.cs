namespace Contracts.Sales;

public sealed record OverdueCustomerSummary
{
    public required string CustomerName { get; init; }
    public int OverdueOrderCount { get; init; }
    public DateTime OldestOverdueDueDate { get; init; }
}
