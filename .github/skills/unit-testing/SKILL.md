---
name: unit-testing
description: "Generates unit tests using xUnit and NSubstitute. Implements Arrange-Act-Assert pattern with comprehensive test coverage for success and failure scenarios."
version: 1.0.0
language: C#
framework: .NET 10.0
dependencies: xUnit, NSubstitute, FluentAssertions
pattern: Arrange-Act-Assert, Test Doubles
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
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="NSubstitute" Version="5.1.0" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" Version="1.0.16">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>    
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\{Module}.{AssemblyName}\{Module}.{AssemblyName}.csproj" />
    <ProjectReference Include="..\Sales.DataModel\Sales.DataModel.csproj" />
  </ItemGroup>

</Project>
```

---

## Template: Base Test Class

```csharp
// tests/{name}.Application.UnitTests/Abstractions/BaseTest.cs
using NSubstitute;
using {name}.domain.abstractions;

namespace {name}.Application.UnitTests.Abstractions;

public abstract class BaseTest
{
    protected static CancellationToken CancellationToken => CancellationToken.None;

    /// <summary>
    /// Creates a mock that returns the provided result
    /// </summary>
    protected static T CreateMock<T>() where T : class
    {
        return Substitute.For<T>();
    }

    /// <summary>
    /// Helper to create a successful Result
    /// </summary>
    protected static Result<T> SuccessResult<T>(T value)
    {
        return Result.Success(value);
    }

    /// <summary>
    /// Helper to create a failed Result
    /// </summary>
    protected static Result<T> FailureResult<T>(Error error)
    {
        return Result.Failure<T>(error);
    }
}
```

---


---

## Template: Domain Entity Tests

```csharp
// tests/{name}.Domain.UnitTests/{Aggregate}/{Entity}Tests.cs
using FluentAssertions;
using {name}.domain.{aggregate};
using {name}.domain.{aggregate}.events;

namespace {name}.Domain.UnitTests.{Aggregate};

public sealed class {Entity}Tests
{
    // ═══════════════════════════════════════════════════════════════
    // CREATE TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_Should_ReturnSuccess_When_ValidParameters()
    {
        // Arrange
        var name = "Test Entity";
        var description = "Test Description";
        var organizationId = Guid.NewGuid();

        // Act
        var result = {Entity}.Create(name, description, organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.OrganizationId.Should().Be(organizationId);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_NameIsEmpty()
    {
        // Arrange
        var name = string.Empty;
        var description = "Test Description";
        var organizationId = Guid.NewGuid();

        // Act
        var result = {Entity}.Create(name, description, organizationId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({Entity}Errors.NameRequired);
    }

    [Fact]
    public void Create_Should_RaiseDomainEvent_When_Success()
    {
        // Arrange
        var name = "Test Entity";
        var description = "Test Description";
        var organizationId = Guid.NewGuid();

        // Act
        var result = {Entity}.Create(name, description, organizationId);

        // Assert
        result.Value.GetDomainEvents()
            .Should().ContainSingle()
            .Which.Should().BeOfType<{Entity}CreatedDomainEvent>();
    }

    // ═══════════════════════════════════════════════════════════════
    // UPDATE TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateName_Should_ReturnSuccess_When_ValidName()
    {
        // Arrange
        var entity = Create{Entity}();
        var newName = "Updated Name";

        // Act
        var result = entity.UpdateName(newName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        entity.Name.Should().Be(newName);
    }

    [Fact]
    public void UpdateName_Should_ReturnFailure_When_EmptyName()
    {
        // Arrange
        var entity = Create{Entity}();

        // Act
        var result = entity.UpdateName(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({Entity}Errors.NameRequired);
    }

    // ═══════════════════════════════════════════════════════════════
    // DEACTIVATE TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Deactivate_Should_SetIsActiveToFalse()
    {
        // Arrange
        var entity = Create{Entity}();
        entity.IsActive.Should().BeTrue();

        // Act
        entity.Deactivate();

        // Assert
        entity.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Should_RaiseDomainEvent()
    {
        // Arrange
        var entity = Create{Entity}();
        entity.ClearDomainEvents();  // Clear create event

        // Act
        entity.Deactivate();

        // Assert
        entity.GetDomainEvents()
            .Should().ContainSingle()
            .Which.Should().BeOfType<{Entity}DeactivatedDomainEvent>();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════

    private static {Entity} Create{Entity}()
    {
        var result = {Entity}.Create(
            "Test Entity",
            "Test Description",
            Guid.NewGuid());

        return result.Value;
    }
}
```

---

## Template: Test Data Builders

```csharp
// tests/{name}.Application.UnitTests/TestData/{Entity}Builder.cs
using {name}.application.{feature}.Create{Entity};

namespace {name}.Application.UnitTests.TestData;

public sealed class {Entity}CommandBuilder
{
    private string _name = "Default Name";
    private string _description = "Default Description";
    private Guid _organizationId = Guid.NewGuid();

    public {Entity}CommandBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public {Entity}CommandBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public {Entity}CommandBuilder WithOrganizationId(Guid organizationId)
    {
        _organizationId = organizationId;
        return this;
    }

    public Create{Entity}Command Build()
    {
        return new Create{Entity}Command(_name, _description, _organizationId);
    }
}

// Usage in tests:
// var command = new {Entity}CommandBuilder()
//     .WithName("Custom Name")
//     .Build();
```

---

## NSubstitute Quick Reference

```csharp
// Create mock
var repository = Substitute.For<IRepository>();

// Setup return value
repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
    .Returns(entity);

// Setup return null
repository.GetByIdAsync(entityId, CancellationToken)
    .Returns((Entity?)null);

// Verify method was called
repository.Received(1).Add(Arg.Any<Entity>());

// Verify method was NOT called
repository.DidNotReceive().Add(Arg.Any<Entity>());

// Verify with argument matching
repository.Received().Add(Arg.Is<Entity>(e => e.Name == "Test"));

// Verify call order (advanced)
Received.InOrder(() =>
{
    repository.Add(Arg.Any<Entity>());
    unitOfWork.SaveChangesAsync(CancellationToken);
});

// Setup to throw exception
repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
    .ThrowsAsync(new Exception("Database error"));
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
2. **Descriptive test names** - `Should_{ExpectedBehavior}_When_{Condition}`
3. **Arrange-Act-Assert** - Clear structure in every test
4. **Mock only dependencies** - Don't mock the SUT
5. **Test behavior, not implementation** - Focus on outcomes
6. **Use Theory for data-driven tests** - Avoid duplicate test logic
7. **Test edge cases** - Empty, null, boundaries
8. **Fast tests** - No I/O, no database
9. **Independent tests** - No shared state
10. **Meaningful assertions** - Test what matters

---

## Anti-Patterns to Avoid

```csharp
// ❌ WRONG: Multiple unrelated assertions
[Fact]
public void Test_Everything()
{
    // Tests too many things at once
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be("Test");
    repository.Received(1).Add(Arg.Any<Entity>());
    unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
}

// ✅ CORRECT: Focused tests
[Fact]
public void Handle_Should_ReturnSuccess_When_ValidRequest() { }

[Fact]
public void Handle_Should_AddEntity_When_ValidRequest() { }

[Fact]
public void Handle_Should_CallSaveChanges_When_ValidRequest() { }

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
```

---

## Related Skills

- `cqrs-command-generator` - Commands to test
- `cqrs-query-generator` - Queries to test
- `domain-entity-generator` - Domain entities to test
- `integration-testing` - End-to-end tests
