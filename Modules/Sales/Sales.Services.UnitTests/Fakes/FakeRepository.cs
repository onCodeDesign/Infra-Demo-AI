using Sales.DataModel.SalesLT;

namespace Sales.Services.UnitTests.Fakes;

internal class FakeRepository : DataAccess.IRepository
{
    private readonly List<Customer> _customers = new();

    public FakeRepository(Customer[] customers)
    {
        _customers.AddRange(customers);
    }

    public IQueryable<TEntity> GetEntities<TEntity>() where TEntity : class
    {
        if (typeof(TEntity) == typeof(Customer))
        {
            return (IQueryable<TEntity>)_customers.AsQueryable();
        }

        throw new NotImplementedException($"Entity type {typeof(TEntity).Name} not supported in FakeRepository");
    }

    public DataAccess.IUnitOfWork CreateUnitOfWork()
    {
        throw new NotImplementedException("CreateUnitOfWork not needed for read-only tests");
    }
}
