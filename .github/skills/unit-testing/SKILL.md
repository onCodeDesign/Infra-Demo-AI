---
name: unit-testing
description: "Generates unit tests using xUnit and NSubstitute. Implements Arrange-Act-Assert pattern with comprehensive test coverage for success and failure scenarios."
version: 1.0.0
language: C#
framework: .NET 10.0
dependencies: xUnit, NSubstitute, FluentAssertions
pattern: Arrange-Act-Assert, Test Doubles, Mocks and Stubs
---

# Unit Test Generator

## Overview

Unit tests for Clean Architecture handlers:

- **xUnit** - Test framework
- **NSubstitute** - Mocking library
- **FluentAssertions** - Readable assertions
- **AAA pattern** - Arrange, Act, Assert

## Quick Reference

| Test Type | Purpose | Example |
|-----------|---------|---------|
| Success test | Verify happy path | `{Method}_{SuccessScenario}_{Expectation}` |
| Failure test | Verify error handling | `{Method}_{FailureScenario}_{Expectation}` |
| Validation test | Verify input validation | `{Method}_{ValidationScenario}_{Expectation}` |
| Behavior test | Verify side effects | `{Method}_{BehaviorScenario}_{Expectation}` |

---

## Test Project Structure

```
src/
└── Modules/
    └── {Module}/
        └── {Module}.{AssemblyName}.UnitTests/
            └── {ClassName}Tests.cs   
```

---

## Template: Test Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\{Module}.{AssemblyName}\{Module}.{AssemblyName}.csproj" />
    <ProjectReference Include="..\{Module}.DataModel\{Module}.DataModel.csproj" />
    <ProjectReference Include="..\..\Contracts\Contracts.csproj" />
    <ProjectReference Include="..\..\..\Infra\DataAccess\DataAccess.csproj" />
  </ItemGroup>

</Project>
```

---

## Template: Unit Test Class

```csharp
// filepath: Modules/{Module}/{Module}.{AssemblyName}.UnitTests/{Class}Tests.cs
using DataAccess;
using FluentAssertions;
using {Module}.DataModel;
using Contracts.{Module};

namespace {Module}.{AssemblyName}.UnitTests;

public class {Class}Tests
{
    // ==================== EMPTY/EDGE CASE ====================
    
    [Fact]
    public void {Method}_{EmptyInputScenario}_{ExpectedBehavior}()
    {
        // Arrange
        var sut = GetTarget({emptyInputData}, {emptyDependencyData});
        
        // Act
        var result = sut.{Method}();
        
        // Assert
        Assert{ExpectedCondition}(result);
    }
    
    // ==================== SIMPLE CASE - ASSERT ON RESULT ====================
    
    [Fact]
    public void {Method}_{SimpleCaseScenario}_{ReturnsExpectedResult}()
    {
        // Arrange
        var {inputItem} = Create{Entity}({testData});
        var sut = GetTarget(new[] { {inputItem} }, {dependencyData});
        
        // Act
        var result = sut.{Method}();
        
        // Assert
        Assert.Equal({expectedValue}, result.{Property1});
        Assert.Equal({expectedValue}, result.{Property2});
    }
    
    // ==================== SINGLE ITEM - ASSERT ON MOCK ====================
    
    [Fact]
    public void {Method}_{SimpleCaseScenario}_{PerformsExpectedAction}()
    {
        // Arrange
        var {mockDependency} = new Fake{Dependency}({initialData});
        var {inputItem} = Create{Entity}({testData});
        var sut = GetTarget(new[] { {inputItem} }, {mockDependency});
        
        // Act
        sut.{Method}();
        
        // Assert
        Assert.Contains({mockDependency}.Get{TrackedEntities}<{EntityType}>(),
            e => e.{Property1} == {expectedValue} && e.{Property2} == {expectedValue});
    }
    
    // ==================== COMPLEX SCENARIO - FLUENT ASSERTIONS ====================
    
    [Fact]
    public void {Method}_{ComplexScenario}_{PerformsExpectedActions}()
    {
        // Arrange
        var {mockDependency} = new Fake{Dependency}({initialData});
        var {inputItems} = new[]
        {
            Create{Entity}({testData1}),
            Create{Entity}({testData2}),
            Create{Entity}({testData3})
        };
        var sut = GetTarget({inputItems}, {mockDependency});
        
        // Act
        sut.{Method}();
        
        // Assert
        {mockDependency}.Get{TrackedEntities}<{EntityType}>().Should().BeEquivalentTo(new[]
            {
                new {EntityType} { {Property1} = {value1}, {Property2} = {value2} },
                new {EntityType} { {Property1} = {value3}, {Property2} = {value4} },
                new {EntityType} { {Property1} = {value5}, {Property2} = {value6} }
            },
            options => options
                .Including(e => e.{Property1})
                .Including(e => e.{Property2})
        );
    }
    
    // ==================== COMPLEX SCENARIO - ASSERT ON RESULT ====================
    
    [Fact]
    public void {Method}_{ComplexScenario}_{ReturnsExpectedResult}()
    {
        // Arrange
        var {inputItems} = new[]
        {
            Create{Entity}({testData1}),
            Create{Entity}({testData2}),
            Create{Entity}({testData3})
        };
        var sut = GetTarget({inputItems}, {dependencyData});
        
        // Act
        var result = sut.{Method}();
        
        // Assert
        Assert.Equal({expectedCount}, result.{CountProperty});
    }
    
    // ==================== GETTARGET HELPERS ====================
    
    // Primary overload - accepts test data arrays, creates fakes internally
    private static {Class} GetTarget({InputType}[] {inputData}, {DependencyDataType}[] {dependencyData})
        => GetTarget(new Fake{Dependency1}({inputData}), new Fake{Dependency2}({dependencyData}));
    
    // Overload - accepts pre-built mock for assertion
    private static {Class} GetTarget({InputType}[] {inputData}, Fake{Dependency2} {mockDependency})
        => GetTarget(new Fake{Dependency1}({inputData}), {mockDependency});
    
    // Core factory - accepts all fake instances
    private static {Class} GetTarget(Fake{Dependency1} {dependency1}, Fake{Dependency2} {dependency2})
    {
        return new {Class}({dependency1}, {dependency2});
    }
    
    // ==================== TEST DATA BUILDERS ====================
    
    private static {EntityType} Create{Entity}({ParamType} {param1}, {ParamType} {param2}, {ParamType} {param3})
    {
        return new {EntityType}
        {
            {Property1} = {param1},
            {Property2} = {param2},
            {Property3} = {param3}
        };
    }
    
    // ==================== ASSERTION HELPERS ====================
    
    private static void Assert{ConditionName}({ResultType} result)
    {
        Assert.True(result.{Property1} == {expectedValue}
            && result.{Property2} == {expectedValue}
            && result.{Property3} == {expectedValue}
            , "Expected {description}."
        );
    }
}
```

---

## Template: Fake Test Double (Stub + Mock)

Handwritten fake class that can act as both stub (returns data) and mock (tracks interactions):

```csharp
// filepath: Modules/{Module}/{Module}.{AssemblyName}.UnitTests/Fakes/Fake{Dependency}.cs
using {Module}.DataModel;
using Contracts.{Module};

namespace {Module}.{AssemblyName}.UnitTests.Fakes;

internal class Fake{Dependency} : I{Dependency}
{
    private readonly List<{EntityType}> _{initialData} = new();
    private readonly List<{EntityType}> _{trackedEntities} = new();
    
    public Fake{Dependency}({EntityType}[] {initialData})
    {
        _{initialData}.AddRange({initialData});
    }
    
    // Stub behavior - returns canned data
    public {ReturnType} {QueryMethod}()
    {
        return _{initialData}.{TransformToReturnType}();
    }
    
    // Mock behavior - tracks interactions
    public void {CommandMethod}({EntityType} {entity})
    {
        _{trackedEntities}.Add({entity});
    }
    
    // Test inspection method - for assertions
    public IReadOnlyList<{EntityType}> Get{TrackedEntities}<T>() where T : {EntityType}
    {
        return _{trackedEntities}.OfType<T>().ToList();
    }
}
```

---

## FluentAssertions Quick Reference

```csharp
// Basic assertions
result.Should().BeTrue();
result.Should().BeFalse();
result.Should().BeNull();
result.Should().NotBeNull();

// Equality
result.Should().Be(expected);
result.Should().NotBe(unexpected);
result.Should().BeEquivalentTo(expected);

// Collections
list.Should().BeEmpty();
list.Should().NotBeEmpty();
list.Should().HaveCount(3);
list.Should().Contain(item);
list.Should().ContainSingle();
list.Should().ContainSingle().Which.Should().BeOfType<MyType>();

// Types
result.Should().BeOfType<MyType>();
result.Should().BeAssignableTo<IMyInterface>();

// Strings
name.Should().StartWith("Test");
name.Should().Contain("Entity");
name.Should().BeNullOrEmpty();

// Exceptions
action.Should().Throw<InvalidOperationException>()
    .WithMessage("*not found*");

action.Should().NotThrow();

// Result pattern
result.IsSuccess.Should().BeTrue();
result.Error.Should().Be(ExpectedError);
```

---

## Critical Rules

1. **One assert concept per test** - Focus on single behavior
2. **Strict naming convention** - based on three parts `{MethodUnderTest}_{TestScenario}_{ExpectedBehavior}`
3. **Arrange-Act-Assert** - Clear structure in every test
4. **Mock only dependencies** - Don't mock the SUT
5. **Test behavior, not implementation** - Focus on outcomes
6. **Use Theory for data-driven tests** - Avoid duplicate test logic
7. **Test edge cases** - Empty, null, boundaries
8. **Fast tests** - No I/O, no database
9. **Independent tests** - No shared state
10. **Meaningful assertions** - Test what matters
11. **Use GetTarget helpers** - Simplify SUT creation and reduce setup duplication
12. **Use helpers for asserts** - Encapsulate complex assertions for readability
13. **Remove duplication code in tests using helpers** - Keep tests clean and focused

---

## Anti-Patterns to Avoid

```csharp
// ❌ WRONG: Multiple assertions
[Fact]
public void ImportPersonsAsCustomers_NoPersons_ReturnsZeroResults()
{
    // multiple calls to Assert and verify different behaviors -> hard to know what failed
    Assert.Equal(0, result.TotalPersonsProcessed);
    Assert.Equal(0, result.CustomersAdded);
    Assert.Equal(0, result.CustomersUpdated);
    Assert.Equal(0, result.CustomersSkipped);
}

// ✅ CORRECT: Use assertion helper
[Fact]
public void ImportPersonsAsCustomers_NoPersons_ReturnsZeroResults()
{
    // single call to Assert helper -> clear what failed
    AssertZeroResults(result);
}

private static void AssertZeroResults(CustomerImportResult result)
{
    Assert.True(result.TotalPersonsProcessed == 0
        && result.CustomersAdded == 0
        && result.CustomersUpdated == 0
        && result.CustomersSkipped == 0
        , "Expected all result counts to be zero."
        );
}


// ❌ WRONG: Asserting different implementation aspects
[Fact]
public void ImportPersonsAsCustomers_NewPerson_AddsCustomer()
{
    var person = CreatePersonData(1, "John", "Doe", DateTime.UtcNow);
    var personService = new FakePersonService([person]);
    var repository = new FakeRepository([]);
    var service = new CustomerImportService(personService, repository);
    var result = service.ImportPersonsAsCustomers();
    Assert.Equal(1, result.TotalPersonsProcessed);
    Assert.Equal(1, result.CustomersAdded);
    Assert.Equal(0, result.CustomersUpdated);
    Assert.Equal(0, result.CustomersSkipped);
    Assert.Single(repository.AddedEntities);
    
    var addedCustomer = repository.AddedEntities[0] as Customer;
    Assert.NotNull(addedCustomer);
    Assert.Equal("John", addedCustomer.FirstName);
    Assert.Equal("Doe", addedCustomer.LastName);
}

// ✅ CORRECT: Separate tests for separate expectations
 [Fact]
 public void ImportPersonsAsCustomers_NewPerson_AddsCustomer()
 {
     var repoMock = new FakeUnitOfWork(new Customer[0]);
     var person = CreatePersonData(1, "John", "Doe", DateTime.UtcNow);
     var service = GetTarget(new[] { person }, repoMock);

     CustomerImportResult result = service.ImportPersonsAsCustomers();

     Assert.Contains(repoMock.GetAddedEntities<Customer>(),
         c => c.FirstName == "John" && c.LastName == "Doe");
 }

 [Fact]
 public void ImportPersonsAsCustomers_NewPerson_ReturnsResultWithAddedCustomer()
 {
     var person = CreatePersonData(1, "John", "Doe", DateTime.UtcNow);
     var service = GetTarget(new[] { person }, new Customer[0]);

     CustomerImportResult result = service.ImportPersonsAsCustomers();

     Assert.Equal(1, result.TotalPersonsProcessed);
     Assert.Equal(1, result.CustomersAdded);
 }


 // ❌ WRONG: Overspecified assertions (expected values may be at any position in collection)
     repoMock.GetAddedEntities<Customer>()[0].FirstName.Should().Be("John");
     repoMock.GetAddedEntities<Customer>()[0].LastName.Should().Be("Doe");


// ✅ CORRECT: Use Contains with predicate 
     Assert.Contains(repoMock.GetAddedEntities<Customer>(),
         c => c.FirstName == "John" && c.LastName == "Doe");

// ✅ CORRECT: Use BeEquivalentTo for full collection match
     repoMock.GetAddedEntities<Customer>().Should().BeEquivalentTo(new[]
     {
         new Customer { FirstName = "John", LastName = "Doe" }
     });


// ❌ WRONG: Testing implementation details
repository.Received(1).GetByIdAsync(entityId, CancellationToken);
repository.Received(1).Add(Arg.Any<Entity>());
// ... testing every single call

// ✅ CORRECT: Testing outcomes
result.IsSuccess.Should().BeTrue();
result.Value.Id.Should().NotBeEmpty();

// ❌ WRONG: Shared mutable state
private Entity _sharedEntity;  // Modified by tests, causes flaky tests

// ✅ CORRECT: Fresh setup per test
[Fact]
public void Test()
{
    var entity = CreateEntity();  // Fresh instance
}

// ❌ WRONG: Create SUT inline with setup duplication
var repoMock = new FakeUnitOfWork(new Customer[0]);
var person = CreatePersonData(1, "John", "Doe", DateTime.UtcNow);
var service = new CustomerImportService(personService, repoMock);


// ✅ CORRECT: Create SUT via GetTarget helper to reduce duplication by reusing setup code
var repoMock = new FakeUnitOfWork(new Customer[0]);
var person = CreatePersonData(1, "John", "Doe", DateTime.UtcNow);
var service = GetTarget(new[] { person }, repoMock);

```
