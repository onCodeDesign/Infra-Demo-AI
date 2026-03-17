namespace Contracts.Sales;

public sealed class CustomerOverdueOrdersData
{
    public required string CustomerName { get; init; }
    public int OverdueOrderCount { get; init; }
    public DateTime OldestOverdueOrderDate { get; init; }
}
