---
name: unit-testing
description: "Write, refactor, and review unit tests using xUnit and NSubstitute. Target trustworthy, maintainable, readable tests; AAA structure; isolate dependencies; prefer state-based testing; avoid brittle interaction tests; enforce clear naming and minimal noise."
version: 1.0.0
language: C#
framework: .NET 10.0
dependencies: xUnit, NSubstitute, FluentAssertions
pattern: Arrange-Act-Assert, Test Doubles, Mocks and Stubs
---

# Unit Testing Skill

## Resources
Consult `CHEATSHEET.md` at the points marked below.

## Quick Reference

**Test Structure:** Arrange-Act-Assert (blank lines separate sections)
**Naming:** `[MethodUnderTest]_[TestScenario]_[ExpectedBehavior]`
**SUT Variable:** Always `target`
**Mocks:** ONE per test (verify interactions)
**Stubs:** Many OK (return data)
**Fake Classes:** `Fake{Interface}` -  Never shared, always are private into the test class
**Fake Variables:** `{name}Stub` (returns data), `{name}Mock` (verified in assertions)
**Assertion syntax:** → CHEATSHEET.md `## FluentAssertions`
**Test Projects (.csproj):** When you need to create a test project → CHEATSHEET.md `## Test Project File Template`
**Fake Test Double:** When you need to create a fake test double → CHEATSHEET.md `## Template: Fake Test Double (Stub + Mock)`
**Approved Packages:** When reviewing a test project → CHEATSHEET.md `## Approved Packages`

## Core Principles

### The Three Pillars of Good Tests

 1. **Trustworthiness** - Tests that developers believe in and accept with confidence
 2. **Maintainability** - Tests that are easy to change without breaking
 3. **Readability** - Tests that clearly communicate their intent

### What Makes a Good Unit Test

A unit test should be:

- **Fast** - Executes quickly (milliseconds, not seconds)
- **Isolated** - Can run independently in any order
- **Repeatable** - Produces consistent results
- **Self-validating** - Pass/fail is automatic, no manual inspection
- **Timely** - Written at the right time (ideally before or with production code)
- **Focused** - Tests one behavior or concept. Has at most one reason to fail

## Critical Rules

1. **One logical concept per test** — A test has one reason to fail
2. **Test behavior, not implementation** - Focus on outcomes
3. **Zero duplicated code in tests** - Remove duplication using private helpers with suggestive names.
4. **Test edge cases** - Empty, null, boundaries
5. **Fast tests** - No I/O, no database
6. **Independent tests** - No shared state
7. **Use GetTarget helpers** - Simplify SUT creation and reduce setup duplication
8. **Use helpers for asserts** - Encapsulate complex assertions for readability
9. **Do not couple test classes** - Helper classes or methods should be private to the test class, never shared.
10. **Never assert by index** — `collection[0].Property` is wrong; use `Contains`, `BeEquivalentTo`, or `ContainSingle(predicate)`
11. **FluentAssertions vs xUnit Assertions** - Use FluentAssertions for collections/objects, xUnit Assert for simple scalars/booleans
12. **Predicate assertions over chained property checks** — Use `Assert.Contains(list, x => x.Name == "John" && x.Age == 30)` not separate `Assert.Equal` calls on each property


## Test Structure (AAA Pattern)

### Arrange-Act-Assert

```csharp
[Fact]
public void Withdraw_ValidAmount_DecreasesBalance()
{
    var account = new BankAccount(100);
    var withdrawAmount = 50;
    
    account.Withdraw(withdrawAmount);
    
    Assert.Equal(50, account.Balance);
}
```

### Key Points

- Keep each section visually separated (blank lines)
- Avoid separating comments in favour of blank lines
- Act and Assert should usually be a single line
- Assert should verify one logical concept
- Use Assert helper methods with logical expressions rather than multiple primitive assertions

## Collection Assertions — Required Patterns

**Rule: Never access a collection by index in an assertion.**

### Single item — use Contains with predicate
```csharp
// ✅
Assert.Contains(repo.GetAddedEntities<Customer>(),
    c => c.FirstName == "John" && c.LastName == "Doe");

// ❌ — brittle, order-dependent
Assert.Equal("John", repo.GetAddedEntities<Customer>()[0].FirstName);
Assert.Equal("Doe",  repo.GetAddedEntities<Customer>()[0].LastName);
```

### Full collection match — use BeEquivalentTo
```csharp
// ✅ — order-insensitive, property-scoped
var expectedCustomers = new[]
{
    new Customer { FirstName = "John", LastName = "Doe" },
    new Customer { FirstName = "Jane", LastName = "Smith" }
};
repo.GetAddedEntities<Customer>().Should().BeEquivalentTo(expectedCustomers,
    options => options
        .Including(c => c.FirstName)
        .Including(c => c.LastName)
);

// ❌ — order-dependent, over-specifies all properties, breaks on any schema change
result.Should().BeEquivalentTo(new Customer { Id = 1, FirstName = "John", ... });
```

### Partial match on a single item
```csharp
// ✅
repo.GetAddedEntities<Customer>()
    .Should().ContainSingle(c => c.FirstName == "John");
```

### Multiple asserts on same result — collapse into Assert.True or helper
```csharp
// ✅ — one call, one failure message
Assert.True(result.TotalProcessed == 3 && result.Added == 2,
    "Expected 3 processed and 2 added.");

// Or as a private helper
private static void AssertImportResult(CustomerImportResult result, int processed, int added)
    => Assert.True(result.TotalProcessed == processed && result.Added == added,
        $"Expected {processed} processed, {added} added.");
```

## Stubs vs Mocks

**Stub** — provides controlled input to the SUT. Never fails a test.
**Mock** — verifies the SUT interacted correctly with a dependency. Can fail a test.
```csharp
// Stub — returns data, never verified
var calculatorStub = Substitute.For<ICalculator>();
calculatorStub.Calculate(Arg.Any<int>()).Returns(150m);

var result = target.ProcessOrder(order);

Assert.Equal(150m, result.Total);  // asserting result, not the stub

// Mock — interaction is the assertion
var emailMock = Substitute.For<IEmailService>();

target.ProcessOrder(order);

emailMock.Received(1).SendEmail(Arg.Any<string>());  // asserting the mock
```

**Golden Rule:** ONE mock per test — if you're verifying multiple mocks, you're testing multiple things.

## Naming Convention

### Test Naming Template

```
[MethodUnderTest]_[TestScenario]_[ExpectedBehavior]
```

**Examples:**

- `IsValidFileName_BadExtension_ReturnsFalse()`
- `Add_NegativeNumber_ThrowsException()`
- `ProcessOrder_InvalidUser_SendsNotificationEmail()`

**Guidelines:**

- Use underscores to separate the three parts
- Be descriptive and specific about the scenario
- Avoid "And" in the [TestScenario] or [ExpectedBehavior] - if you need "And", split into multiple tests
- Avoid technical jargon in favor of business language


### Naming Convention for system under test (SUT)

- Always use `target` as the variable name for the SUT instance in tests

**There can be only one `target` per test.**

### Naming Convention for Fakes

**Class Names:**

- Use `Fake{InterfaceName}` prefix for handwritten test doubles that can act as both stub and mock
- Example: `FakeRepository`, `FakeUnitOfWork`, `FakePersonService`

**Variable Names in Tests:**

- Use `{name}Stub` suffix when the fake provides indirect input (returns data)
- Use `{name}Mock` suffix when the fake is verified in assertions (checks interactions)
- **A fake variable in a test is either a stub OR a mock, never both**

```csharp
// ✅ CORRECT: Variable named as stub (only returns data)
var calculatorStub = GetCalculatorStub({InputData});
var target = GetTarget(calculatorStub);
// assert is not verifying calculatorStub, so "stub" is correct

// ✅ CORRECT: Variable named as mock (verified in assertion)
var emailMock = Substitute.For<IEmailService>();
// ... act on target...
emailMock.Received(1).SendEmail(Arg.Any<string>());  // Verifying emailMock

// ❌ WRONG: Generic naming doesn't show intent
var repository = Substitute.For<IRepository>();  // Is this a stub or mock?

// ❌ WRONG: Calling it "mock" but using as stub
var repositoryMock = Substitute.For<IRepository>();
repositoryMock.GetAll().Returns(data);
var result = service.Process();  // Not verifying repositoryMock - should be named repositoryStub
```

**Key Rule:** The suffix tells the reader HOW the fake is used in THIS test, not what it's capable of.

## Test Isolation Strategies

### Solitary Tests vs Collaborative Tests

**Default: Solitary (full isolation)** — fake ALL dependencies.
Use this codebase default because services have many dependencies, the repository pattern abstracts EF Core, and layers should stay cleanly separated.

**Exception: Collaborative (partial isolation)** — use real collaborators only when they have no dependencies (no I/O, or external services) or are simple value objects.

| | Solitary | Collaborative |
|---|---|---|
| `IRepository` | Always fake | Never real |
| `IUnitOfWork` | Always fake | Never real |
| Pure math / validators | Fake OK | Real OK |
| Value objects | Fake OK | Real OK |

```csharp
// Solitary — everything faked
var calculatorStub = Substitute.For<ICalculator>();
var repositoryStub = Substitute.For<IRepository>();
OrderService target = GetTarget(calculatorStub, repositoryStub);

// Collaborative — real only when no dependencies/stateless
var calculator = new PriceCalculator();  // Real - it's just math
var validator = new OrderValidator();     // Real - no dependencies
var repositoryStub = new FakeRepository();    // Fake - has I/O
OrderService target = GetTarget(calculator, validator, repositoryStub);
```

### Handwritten Fake vs NSubstitute

| Use | When |
|---|---|
| **Handwritten Fake** | Needs state tracking, query methods for assertions, or both stub + mock behavior |
| **NSubstitute** | Simple return value, one-off interaction check, no state needed |

```csharp
// Handwritten Fake — tracks state, inspectable in assertions
var repoMock = new FakeUnitOfWork(existingCustomers);
target.ImportPersons();
Assert.Contains(repoMock.GetAddedEntities(), c => c.FirstName == "John");

// NSubstitute — simple stub or one-off verify
var emailMock = Substitute.For<IEmailService>();
target.ProcessOrder(order);
emailMock.Received(1).SendEmail(Arg.Any<string>());
```

## Test Organization

### Project Structure

```markdown
src/
└── Modules/
    └── {Module}/
        └── {Module}.{Assembly}/
            └── {Class}.cs
        └── {Module}.{Assembly}.UnitTests/
            └── {Class}Tests.cs   
```

### File Organization

- One test class per production class
- Name: `{ClassName}Tests`
- Group related tests together, by method under test then by scenario
- Keep helper methods at the bottom of the file

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
    // Assert on result
    [Fact]
    public void {Method}_{Scenario}_{ExpectedBehavior}()
    {
        var {inputItem} = Create{Entity}({testData});
        var target = GetTarget(new[] { {inputItem} }, new {EntityType}[0]);

        var result = target.{Method}();

        Assert.Equal({expectedValue}, result.{Property});
    }

    // Assert on mock (interaction/state)
    [Fact]
    public void {Method}_{Scenario}_{ExpectedBehavior}()
    {
        var repoMock = new Fake{Dependency}(new {EntityType}[0]);
        var {inputItem} = Create{Entity}({testData});
        var target = GetTarget(new[] { {inputItem} }, repoMock);

        target.{Method}();

        Assert.Contains(repoMock.Get{TrackedEntities}<{EntityType}>(),
            e => e.{Property1} == {expectedValue} && e.{Property2} == {expectedValue});
    }

    // Assert on full collection match
    [Fact]
    public void {Method}_{Scenario}_{ExpectedBehavior}()
    {
        var repoMock = new Fake{Dependency}(new {EntityType}[0]);
        var target = GetTarget(new[] { Create{Entity}({data1}), Create{Entity}({data2}) }, repoMock);

        target.{Method}();

        var expected = new[]
        {
            Create{Entity}({expectedData1}),
            Create{Entity}({expectedData2})
        };
        repoMock.Get{TrackedEntities}<{EntityType}>().Should().BeEquivalentTo(expected,
            options => options.Including(e => e.{Property1}).Including(e => e.{Property2})
        );
    }

    // ── Helpers ────────────────────────────────────────────────

    private static {Class} GetTarget({InputType}[] inputData, {DependencyDataType}[] dependencyData)
        => GetTarget(new Fake{Dependency1}(inputData), new Fake{Dependency2}(dependencyData));

    private static {Class} GetTarget({InputType}[] inputData, Fake{Dependency2} repoMock)
        => GetTarget(new Fake{Dependency1}(inputData), repoMock);

    private static {Class} GetTarget(Fake{Dependency1} dep1, Fake{Dependency2} dep2)
        => new {Class}(dep1, dep2);

    private static {EntityType} Create{Entity}({ParamType} param1, {ParamType} param2, {ParamType} param3)
        => new {EntityType} { {Property1} = param1, {Property2} = param2, {Property3} = param3 };

    private static void Assert{ConditionName}({ResultType} result)
        => Assert.True(result.{Property1} == {expectedValue} && result.{Property2} == {expectedValue},
            "Expected {description}.");
}
```

## Patterns to Follow

✅ CORRECT: Separate tests for separate expectations

```csharp
 [Fact]
 public void ImportPersonsAsCustomers_NewPerson_AddsCustomer()
 {
     var repoMock = new FakeUnitOfWork(new Customer[0]);
     var person = CreatePersonData(1, "John", "Doe", DateTime.UtcNow);
     var target = GetTarget(new[] { person }, repoMock);

     target.ImportPersonsAsCustomers();

     Assert.Contains(repoMock.GetAddedEntities<Customer>(),
         c => c.FirstName == "John" && c.LastName == "Doe");
 }

 [Fact]
 public void ImportPersonsAsCustomers_NewPerson_ReturnsResultWithAddedCustomer()
 {
     var person = CreatePersonData(1, "John", "Doe", DateTime.UtcNow);
     var target = GetTarget(new[] { person }, new Customer[0]);

     CustomerImportResult result = target.ImportPersonsAsCustomers();

     Assert.Equal(1, result.CustomersAdded);
 }
```

## Anti-Patterns to Avoid

```csharp
// ❌ WRONG: Testing implementation details
repository.Received(1).GetByIdAsync(entityId, CancellationToken);
repository.Received(1).Add(Arg.Any<Entity>());
// ... testing every single call

// ✅ CORRECT: Testing outcomes
result.IsSuccess.Should().BeTrue();
result.Value.Id.Should().NotBeEmpty();
```

```csharp
// ❌ WRONG: Shared mutable state
private Entity _sharedEntity;  // Modified by tests, causes flaky tests

// ✅ CORRECT: Fresh setup per test
[Fact]
public void Test()
{
    var entity = CreateEntity();  // Fresh instance
}
```

```csharp
// ❌ WRONG: Logic in tests
[Fact]
public void BadTest()
{
    for (int i = 0; i < 10; i++)
    {
        if (i % 2 == 0)
        {
            // test logic
        }
    }
}
```

## `[Theory]` vs `[Fact]`

Use `[Theory]` only when testing the **same logical path with different data values** (e.g. boundary checks, math).
Use separate `[Fact]` tests when each case represents a **different business scenario** with its own name and meaning.
```csharp
// ✅ [Theory] — same logic, different inputs
[Theory]
[InlineData(0, true)]
[InlineData(2, true)]
[InlineData(1, false)]
public void IsEven_Number_ReturnsCorrectResult(int number, bool expected)
{
    // ... arrange target ...
    var actual = target.IsEven(number);
    Assert.Equal(expected, actual);
}

// ❌ [Theory] misuse — different scenarios collapsed into one poor name
[Theory]
[InlineData("valid@email.com", true)]
[InlineData("", false)]
[InlineData(null, false)]
public void Validate_Input_ReturnsResult(string input, bool expected) { ... }

// ✅ Correct — separate [Fact] per scenario with meaningful names
[Fact]
public void ValidateEmail_ValidFormat_ReturnsTrue() { ... }

[Fact]
public void ValidateEmail_EmptyString_ReturnsFalse() { ... }

[Fact]
public void ValidateEmail_Null_ReturnsFalse() { ... }
```