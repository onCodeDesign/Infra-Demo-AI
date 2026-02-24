# Unit Testing Cheatsheet

## FluentAssertions

### Boolean / Null

```csharp
result.Should().BeTrue();
result.Should().BeFalse();
result.Should().BeNull();
result.Should().NotBeNull();
```

### Equality

```csharp
result.Should().Be(expected);
result.Should().NotBe(unexpected);

// Deep equality — use for objects and DTOs
result.Should().BeEquivalentTo(expected);
```

### Collections ⭐ Primary assertion pattern for this codebase

#### Existence checks

```csharp
list.Should().BeEmpty();
list.Should().NotBeEmpty();
list.Should().HaveCount(3);
```

#### Contains — use when asserting a single item exists (never use [index])

```csharp
// ✅ Preferred — predicate, order-insensitive
list.Should().Contain(c => c.FirstName == "John" && c.LastName == "Doe");

// ✅ xUnit equivalent
Assert.Contains(list, c => c.FirstName == "John" && c.LastName == "Doe");

// ❌ Never do this — brittle, order-dependent
Assert.Equal("John", list[0].FirstName);
```

#### ContainSingle — use when exactly one item is expected

```csharp
// Existence only
list.Should().ContainSingle();

// Existence with predicate
list.Should().ContainSingle(c => c.FirstName == "John");

// Existence + further assertions on the matched item
list.Should().ContainSingle(c => c.FirstName == "John")
    .Which.LastName.Should().Be("Doe");
```

#### BeEquivalentTo — use when asserting the full collection matches and order doesn't matter

```csharp
// ✅ Full match, order-insensitive by default
list.Should().BeEquivalentTo(new[]
{
    new Customer { FirstName = "John", LastName = "Doe" },
    new Customer { FirstName = "Jane", LastName = "Smith" }
});

// ✅ Scope to relevant properties only — preferred to avoid over-specification
list.Should().BeEquivalentTo(
    new[]
    {
        new Customer { FirstName = "John", LastName = "Doe" },
        new Customer { FirstName = "Jane", LastName = "Smith" }
    },
    options => options
        .Including(c => c.FirstName)
        .Including(c => c.LastName)
);

// ✅ Exclude properties you don't care about (e.g. generated IDs, timestamps)
list.Should().BeEquivalentTo(expected,
    options => options
        .Excluding(c => c.Id)
        .Excluding(c => c.CreatedDate)
);

// ❌ Multiple assertions on individual items — brittle and verbose, order-dependent
result.Should().HaveCount(3);
result[0].CustomerName.Should().Be("Alpha Corp");
result[0].OldestOverdueOrderDate.Should().Be(DateTime.Today.AddDays(-10));
result[1].CustomerName.Should().Be("Beta Corp");
result[1].OldestOverdueOrderDate.Should().Be(DateTime.Today.AddDays(-5));
result[2].CustomerName.Should().Be("Gamma Corp");
result[2].OldestOverdueOrderDate.Should().Be(DateTime.Today.AddDays(-3));


// ❌ Over-specified — any unrelated property change breaks the test
list.Should().BeEquivalentTo(new Customer
{
    Id = 1, FirstName = "John", LastName = "Doe",
    CreatedDate = DateTime.Parse("2026-01-30"), Version = 1
});
```

### Decision guide

| Scenario | Use |
|---|---|
| One item exists matching conditions | `ContainSingle(predicate)` |
| At least one item matches | `Contain(predicate)` |
| Order-insensitive collection match, all properties | `BeEquivalentTo(expected)` |
| Order-insensitive collection match, selected properties | `BeEquivalentTo(expected, options => options.Including(...))` |
| Order-insensitive collection match, ignoring generated/audit properties | `BeEquivalentTo(expected, options => options.Excluding(...))` |
| Never | `list[0].Property` |

---

### Types

```csharp
result.Should().BeOfType<MyType>();
result.Should().BeAssignableTo<IMyInterface>();
```

### Strings

```csharp
name.Should().StartWith("Test");
name.Should().EndWith("Service");
name.Should().Contain("Entity");
name.Should().BeNullOrEmpty();
name.Should().NotBeNullOrWhiteSpace();
```

---

### Exceptions

```csharp
// Verify exception type
Action act = () => target.Execute(invalidInput);
act.Should().Throw<InvalidOperationException>();

// Verify exception message (wildcards supported)
act.Should().Throw<ArgumentException>()
    .WithMessage("*cannot be null*");

// Verify no exception thrown
act.Should().NotThrow();
```

---

### Result Pattern

```csharp
result.IsSuccess.Should().BeTrue();
result.IsFailure.Should().BeTrue();
result.Error.Should().Be(ExpectedError);
result.Value.Should().NotBeNull();
```

---

## Test Project File Template

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
     <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="xunit.v3" Version="3.2.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\{Module}.{AssemblyName}\{Module}.{AssemblyName}.csproj" />
    <ProjectReference Include="..\{Module}.DataModel\{Module}.DataModel.csproj" />
    <ProjectReference Include="..\..\Contracts\Contracts.csproj" />
    <ProjectReference Include="..\..\..\Infra\DataAccess\DataAccess.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

---

## Template: Fake Test Double (Stub + Mock)

```csharp
public class {Class}Tests
{
    // ... tests ...
    // ... method helpers ...

    private class Fake{Dependency} : I{Dependency}
    {
        private readonly List<{EntityType}> {initialData} = new();
        private readonly List<{EntityType}> {trackedEntities} = new();

        public Fake{Dependency}({EntityType}[] {data})
        {
            this.{initialData}.AddRange({data});
        }

        public {ReturnType} {QueryMethod}()
        {
            return {initialData}.{TransformToReturnType}();
        }

        public void {CommandMethod}({EntityType} {entity})
        {
            {trackedEntities}.Add({entity});
        }

        public IReadOnlyList<{EntityType}> Get{TrackedEntities}<T>() where T : {EntityType}
        {
            return {trackedEntities}.OfType<T>().ToList();
        }
    }
}
```

```csharp
// Example for Fake UnitOfWork
public class {Class}Tests
{
    // ... tests ...
    // ... method helpers ...

    private class FakeUnitOfWork : IUnitOfWork
    {
       private readonly List<object> data = new();
       private readonly List<object> addedEntities = new();
       private readonly List<object> savedEntities = new();
       private List<object> deletedEntities = new();

       public FakeUnitOfWork(params object[] initialData)
       {
          data.AddRange(initialData);
       }

       public IQueryable<T> GetEntities<T>() where T : class
       {
          return data
            .Union(addedEntities)
            .Union(savedEntities)
          .OfType<T>().AsQueryable();
       }

       public void Add<T>(T entity) where T : class
       {
          addedEntities.Add(entity);
       }

       public void Delete<T>(T entity) where T : class
       {
          deletedEntities.Add(entity);
       }

       public void SaveChanges()
       {
          savedEntities.AddRange(addedEntities);
       }

       public Task SaveChangesAsync()
       {
          SaveChanges();
          return Task.CompletedTask;
       }

       public IReadOnlyList<T> GetAddedEntities<T>() where T : class => addedEntities.OfType<T>().ToList();
       public IReadOnlyList<T> GetSavedEntities<T>() where T : class => savedEntities.OfType<T>().ToList();
       public IReadOnlyList<T> GetDeletedEntities<T>() where T : class => deletedEntities.OfType<T>().ToList();

       public void BeginTransactionScope(SimplifiedIsolationLevel isolationLevel) { }
       public IUnitOfWork CreateUnitOfWork() => this;
       public void Dispose() { }
    }
}
```

---

## Approved Packages

| Purpose       | Package              | Version |
|--------------|----------------------|---------|
| Test framework | xUnit                | 3.x     |
| Mocking       | NSubstitute          | 5.x     |
| Assertions    | FluentAssertions     | 8.x     |
| Code coverage  | coverlet.collector   | 6.x     |