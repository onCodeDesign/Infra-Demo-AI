---
name: unit-testing
description: "Write, refactor, and review unit tests using xUnit and NSubstitute. Target trustworthy, maintainable, readable tests; AAA structure; isolate dependencies; prefer state-based
  testing; avoid brittle interaction tests; enforce clear naming and minimal noise."
version: 1.0.0
language: C#
framework: .NET 10.0
dependencies: xUnit, NSubstitute, FluentAssertions
pattern: Arrange-Act-Assert, Test Doubles, Mocks and Stubs
---

# Unit Testing Expert Skill

## Quick Reference

**Test Structure:** Arrange-Act-Assert (blank lines separate sections)
**Naming:** `[Method]_[Scenario]_[ExpectedBehavior]`
**SUT Variable:** Always `target`
**Mocks:** ONE per test (verify interactions)
**Stubs:** Many OK (return data)
**Fake Classes:** `Fake{Interface}`, variables: `{name}Stub` or `{name}Mock`

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

## Test Structure (AAA Pattern)

### Arrange-Act-Assert

```csharp
[Test]
public void Withdraw_ValidAmount_DecreasesBalance()
{
    // Arrange - Set up test data and dependencies
    var account = new BankAccount(100);
    var withdrawAmount = 50;
    
    // Act - Execute the method under test
    account.Withdraw(withdrawAmount);
    
    // Assert - Verify the expected outcome
    Assert.AreEqual(50, account.Balance);
}
```

### Key Points

- Keep each section visually separated (blank lines)
- Avoid separating comments in favour of blank lines
- Arrange should be comprehensive but not excessive
- Act should usually be a single line
- Assert should verify one logical concept

## Stubs vs Mocks

Both stubs and mocks are fakes used to isolate the system under test (SUT) from its dependencies, but they serve different purposes.

### Stubs

**Purpose**: Provide controllable indirect input to the system under test

**Characteristics**:

- Never fail a test
- Used to simulate scenarios
- Return predetermined values
- Don't verify interactions

**Example**:

```csharp
// Stub returns fake data
IDataProvider stubProvider = Substitute.For();
stubProvider.GetData().Returns(fakeData);

// Test uses stub but doesn't verify it was called
var result = systemUnderTest.Process();
Assert.IsTrue(result.IsValid);
```

### Mocks

**Purpose**: Verify that the system under test interacts correctly with dependencies

**Characteristics**:

- CAN fail a test
- Used to verify behavior
- Assert against method calls
- Check interaction occurred correctly

**Example**:

```csharp
// Mock verifies interaction
IEmailService mockEmail = Substitute.For();

systemUnderTest.ProcessOrder(order);

// Verify the interaction happened
mockEmail.Received().SendEmail("user@example.com", Arg.Any());
```

### Golden Rule

**Only ONE mock per test** - Everything else should be stubs

- If you're verifying multiple mocks, you're testing multiple things
- Multiple stubs are fine - they're just setup

## Naming Convention

### Test Naming Template

```
[MethodUnderTest]_[Scenario]_[ExpectedBehavior]
```

**Examples:**

- `IsValidFileName_BadExtension_ReturnsFalse()`
- `Add_NegativeNumber_ThrowsException()`
- `ProcessOrder_InvalidUser_SendsNotificationEmail()`

**Guidelines:**

- Use underscores to separate the three parts
- Be descriptive and specific about the scenario
- Avoid technical jargon in favor of business language
- Make the expected behavior crystal clear

### Naming Convention for system under test (SUT)

- Always use `target` as the variable name for the SUT instance in tests

**There can be only one `target` per test.**

### Naming Convention for Fakes

**Class Names:**

- Use `Fake{InterfaceName}` preffix for handwritten test doubles that can act as both stub and mock
- Example: `FakeRepository`, `FakeUnitOfWork`, `FakePersonService`

**Variable Names in Tests:**

- Use `{name}Stub` suffix when the fake provides indirect input (returns data)
- Use `{name}Mock` suffix when the fake is verified in assertions (checks interactions)
- **A fake variable in a test is either a stub OR a mock, never both**

```csharp
// ✅ CORRECT: Variable named as stub (only returns data)
[Fact]
public void ProcessOrder_ValidRequest_CalculatesTotal()
{
    var calculatorStub = Substitute.For<IPriceCalculator>();
    calculatorStub.Calculate(Arg.Any<Order>()).Returns(150m);
    
    var result = target.ProcessOrder(order);
    
    Assert.Equal(150m, result.Total);  // Not verifying calculatorStub
}

// ✅ CORRECT: Variable named as mock (verified in assertion)
[Fact]
public void ProcessOrder_ValidRequest_SendsEmail()
{
    var emailMock = Substitute.For<IEmailService>();
    
    target.ProcessOrder(order);
    
    emailMock.Received(1).SendEmail(Arg.Any<string>());  // Verifying emailMock
}

// ✅ CORRECT: Handwritten fake class used as stub
[Fact]
public void ImportPersons_ExistingCustomers_ReturnsData()
{
    var repositoryStub = new FakeUnitOfWork([existingCustomers]);  // Class: Fake*, Variable: *Stub
    
    var result = target.ImportPersons();
    
    Assert.Equal(3, result.Count);  // Not verifying repositoryStub
}

// ✅ CORRECT: Same fake class used as mock in different test
[Fact]
public void ImportPersons_NewPerson_AddsCustomer()
{
    var repositoryMock = new FakeUnitOfWork([]);  // Class: Fake*, Variable: *Mock
    
    target.ImportPersons();
    
    Assert.Single(repositoryMock.GetAddedEntities<Customer>());  // Verifying repositoryMock
}

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

**Solitary Tests (Full Isolation):** 
Fake ALL dependencies, test SUT in complete isolation:

```csharp
[Fact]
public void PlaceOrder_ValidRequest_CalculatesTotal()
{
    var calculatorStub = Substitute.For<IPriceCalculator>();  // Faked
    var validatorStub = Substitute.For<IOrderValidator>();     // Faked
    var repositoryStub = Substitute.For<IRepository>();        // Faked
    
    calculatorStub.Calculate(Arg.Any<Order>()).Returns(150m);
    
    OrderService target = GetTarget(calculatorStub, validatorStub, repositoryStub);
    
    var result = target.PlaceOrder(order);
    
    Assert.Equal(150m, result.Total);
}
```

**Collaborative Tests (Partial Isolation):**
Use real collaborators when they're simple, fast, and stateless:

```csharp
[Fact]
public void PlaceOrder_ValidRequest_CalculatesTotal()
{
    var calculatorStub = new PriceCalculator();  // Real - it's just math
    var validatorStub = new OrderValidator();     // Real - no dependencies
    var repositoryStub = new FakeRepository();    // Fake - has state/I/O
    
    OrderService target = GetTarget(calculatorStub, validatorStub, repositoryStub);
    
    var result = target.PlaceOrder(order);
    
    Assert.Equal(150m, result.Total);
}
```

#### Guideline

**Default to solitary tests in this codebase because:**

- Services have many dependencies
- Repository pattern abstracts EF Core (always fake IRepository)
- Clear separation between layers

**Use sociable tests when:**

- Collaborator is a pure function (no state, no I/O)
- Collaborator is a simple value object
- You want to test interaction between two units

Use real collaborators when they're simple, fast, and stateless.

### When to Use Fake vs NSubstitute

**Use Handwritten Fakes When:**

- ✅ Dependency needs both stub AND mock behavior in same test
- ✅ Complex state tracking required (e.g., tracking added entities)
- ✅ Multiple tests need same fake implementation
- ✅ Need query methods to inspect state for assertions

```csharp
// Fake can act as both stub and mock

var stub = new FakeUnitOfWork([existingCustomers]);
stub.GetEntities<Customer>().Returns(...);  // Stub

// ... test code ...
var mock = new FakeUnitOfWork([existingCustomers]);
mock.GetAddedEntities<Customer>()  // Mock inspection
```

**Use NSubstitute When:**

- ✅ Simple stub behavior only (return fixed value)
- ✅ One-off verification needed
- ✅ No state tracking required

```csharp
var stub = Substitute.For<IEmailService>();
stub.SendEmail(Arg.Any<string>()).Returns(true);
```

**DataAccess Guideline:**

- Prefer handwritten Fakes for IUnitOfWork because they track entity changes.
- Prefer NSubstitute for IRepository stubs because it just returns data.

### `[Theory]` Usage Guidance (LOW PRIORITY)

When to use `[Theory]` vs multiple `[Fact]` tests:

**Good Use Case: Same Logic, Different Data**:

```csharp
// ✅ CORRECT: Testing same logic with different inputs
[Theory]
[InlineData(0, true)]
[InlineData(2, true)]
[InlineData(4, true)]
[InlineData(1, false)]
[InlineData(3, false)]
public void IsEven_Number_ReturnsCorrectResult(int number, bool expected)
{
    var result = calculator.IsEven(number);
    Assert.Equal(expected, result);
}
```

**Bad Use Case: Different Scenarios**:

```csharp
// ❌ WRONG: Different business scenarios, poor test names
[Theory]
[InlineData("valid@email.com", true)]
[InlineData("invalid", false)]
[InlineData("", false)]
[InlineData(null, false)]
public void Validate_Input_ReturnsResult(string input, bool expected)
{
    // Test name doesn't describe WHAT is being validated
    // Each case is a different scenario (not just different data)
}

// ✅ CORRECT: Separate tests with clear names
[Fact]
public void ValidateEmail_ValidFormat_ReturnsTrue() { ... }

[Fact]
public void ValidateEmail_MissingAtSign_ReturnsFalse() { ... }

[Fact]
public void ValidateEmail_EmptyString_ReturnsFalse() { ... }

[Fact]
public void ValidateEmail_Null_ReturnsFalse() { ... }
```

#### Decision Rule

Use `[Theory]` when:

✅ Testing the same logical path with boundary values
✅ Test name describes behavior clearly even with parameters
✅ All cases test the same business rule

Use multiple `[Fact]` when:

✅ Each case represents a different scenario
✅ Different setup/arrange logic needed
✅ Test names need to describe different behaviors

## Isolation Framework Usage

### Best Practices

1. **Prefer stubs over mocks** - Only use mocks when testing interactions
2. **Avoid overspecification** - Don't verify implementation details
3. **Keep it simple** - Complex mock setups indicate design problems
4. **Use AAA syntax** - Arrange-Act-Assert is clearer than Record-Replay

## Frameworks, Tools and Patterns

Unit tests handlers:

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
- Group related tests together
- Use regions sparingly (prefer well-named methods)

### Test Method Organization

```csharp
[Fact]
public class LogAnalyzerTests
{
    // Tests grouped by method under test
    [Test]
    public void IsValid_BadExtension_ReturnsFalse() { ... }
    
    [Test]
    public void IsValid_GoodExtension_ReturnsTrue() { ... }

    // Helper methods at bottom
    private LogAnalyzer MakeAnalyzer() { ... }
}
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

### Common Anti-Patterns

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

// ❌ WRONG: Testing implementation details
mock.Verify(x => x.Method1());
mock.Verify(x => x.Method2());
mock.Verify(x => x.Method3());

// ✅ CORRECT: Testing outcome
var result = systemUnderTest.Execute();
Assert.IsTrue(result.WasSuccessful);

// ❌ WRONG: Testing entry point (method was called)
[Fact]
public void ProcessOrder_ValidOrder_CallsRepository()
{
    repository.Received(1).Save(Arg.Any<Order>());  // Testing implementation
}

// ✅ CORRECT: Testing exit point (state changed)
[Fact]
public void ProcessOrder_ValidOrder_OrderIsPersisted()
{
    var orders = repository.GetAll<Order>();
    orders.Should().ContainSingle(o => o.Id == expectedId);  // Testing outcome
}
```

### Maintainability Guidelines

#### 1. Avoid Logic in Tests

```csharp
// ❌ WRONG:
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

// ✅ CORRECT:
[Fact]
public void Process_EvenNumber_ReturnsTrue()
{
    var result = calculator.IsEven(4);
    Assert.IsTrue(result);
}
```

#### 2. Remove Duplication with Helper Methods

```csharp
// ❌ WRONG:
[Fact]
public void Test1()
{
    var obj = new ComplexObject();
    obj.Property1 = "value";
    obj.Property2 = 42;
    obj.Initialize();
    // test code
}

[Fact]
public void Test2()
{
    var obj = new ComplexObject();
    obj.Property1 = "value";
    obj.Property2 = 42;
    obj.Initialize();
    // test code
}

// ✅ CORRECT:
[Fact]
public void Test1()
{
    var obj = GetTarget();
    // test code
}

private ComplexObject GetTarget()
{
    var obj = new ComplexObject();
    obj.Property1 = "value";
    obj.Property2 = 42;
    obj.Initialize();
    return obj;
}
```

#### 3. Use Setup Methods Carefully

**Only put in Setup what ALL tests need**:

```csharp
[SetUp]
public void SetUp()
{
    // Only if EVERY test needs this
    _commonDependency = new CommonDependency();
}

#### 4. Avoid Overspecification

```csharp
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


// ❌ WRONG: Overspecified - any property change breaks test
result.Should().BeEquivalentTo(new Order
{
    Id = 123,
    Status = "Pending",
    CreatedDate = DateTime.Parse("2026-01-30"),
    ModifiedDate = DateTime.Parse("2026-01-30"),
    CreatedBy = "System",
    Version = 1
    // ... 20 more properties
});

// ✅ CORRECT: Test only relevant properties
result.Status.Should().Be("Pending");
result.Items.Should().HaveCount(3);
```

### Readability Guidelines

**Prioritize readability over DRY:**

1. **Test names are documentation** - Should be readable sentences
2. **Setup code should be visible** - Not hidden in [TestInitialize]
3. **Magic numbers are OK in tests** - Clarity > DRY
4. **Some duplication is acceptable** - Readability > reuse


```csharp
// ❌ WRONG: Over-abstracted, hard to understand
[Fact]
public void Test_Scenario1()
{
    var result = ExecuteTest(TestData.Scenario1);
    VerifyExpectations(result, Expected.Scenario1);
}

// ✅ CORRECT: Explicit and obvious
[Fact]
public void ProcessOrder_WithThreeItems_CalculatesTotalPrice()
{
    var order = new Order 
    { 
        Items = new[] 
        { 
            new Item { Price = 10 },
            new Item { Price = 20 },
            new Item { Price = 30 }
        }
    };
    
    var result = processor.ProcessOrder(order);
    
    Assert.Equal(60, result.TotalPrice);
}
```
