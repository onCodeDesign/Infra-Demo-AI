# Copilot Instructions for This Repository

> These instructions guide GitHub Copilot when working with this code structure enforced by the Application Infrastructure. Focus: modular plugin architecture with strict boundary enforcement and strict dependencies rules.

---

## Project Identity
- **Style:** Clean Architecture with runtime-loadable plugin modules
- **Stack:** .NET 10, C#, EF Core 10, Microsoft.Extensions.* (DI, Hosting, Logging)
- **Key Patterns:** 
    - AppBoot plugin system with isolated LoadContexts per module
    - Hide external frameworks (EF Core) from business logic (Modules) via abstraction (IRepository/IUnitOfWork)
- **Modules:** `Sales`, `ProductsManagement`, `PersonsManagement`, `Notifications`, `Export`

---

## Architecture Overview

### Folder Structure & Boundaries
```
Infra/                           # Infrastructure framework (DO NOT MODIFY)
├─ AppBoot/                      # DI, plugin loading, app bootstrap, modules initialization
├─ DataAccess/                   # Hides EF Core via abstraction (IRepository/IUnitOfWork). The ONLY assembly allowed to reference EF Core
Modules/
├─ Contracts/                    # Pure interfaces/DTOs - NO dependencies, NO logic
├─ {Module}/                     # e.g., Sales, ProductsManagement
│  ├─ {Module}.Services/         # Business logic - [Service] attribute for DI
│  ├─ {Module}.DataModel/        # Entities only - NO logic, NO EF references
│  ├─ {Module}.DbContext/        # EF DbContext (DO NOT MODIFY - generated)
│  └─ {Module}.ConsoleCommands/  # Console commands via IConsoleCommand
UI/
└─ ConsoleUi/                    # Host app - references Contracts only
```

### Dependency Rules (STRICT)
1. `Contracts` → **ZERO** dependencies (pure interfaces/DTOs)
2. `*.Services` → References: `Contracts`, `DataAccess`, `*.DataModel` only
3. `*.DataModel` → NO logic (entities/value objects only), NO EF Core references
4. `UI/ConsoleUi` → References: `Contracts`, `AppBoot` only (NOT *.Services)
5. `*.DbContext` → EF-specific, uses `PrivateAssets=all` to hide EF from dependents
6. **NO cross-module references** → `Sales.*` cannot reference `ProductsManagement.*` directly - modules interact ONLY through `Contracts` interfaces

**If a change violates these, STOP and suggest an adapter/extension instead.**

---

## Critical Patterns

### 1) Service Registration
Use `[Service(typeof(IInterface), ServiceLifetime)]` from `AppBoot/DependencyInjection`:
```csharp
[Service(typeof(IOrderingService), ServiceLifetime.Transient)]
class OrderingService(IRepository repository) : IOrderingService
```
- **Always** register by interface, not concrete type
- Attribute discovered automatically by `ServiceRegistrationBehavior`
- Default lifetime is `Transient` if omitted

### 2) Data Access - Repository Pattern
**Read-only:** Inject `IRepository`, use `GetEntities<T>()`:
```csharp
var orders = repository.GetEntities<SalesOrderHeader>()
    .Where(o => o.Customer.LastName == name)
    .ToArray();
```

**Modify data:** Use `CreateUnitOfWork()` pattern:
```csharp
using (IUnitOfWork uof = repository.CreateUnitOfWork())
{
    var order = uof.GetEntities<Order>().First(o => o.Id == id);
    order.Status = 5;
    uof.SaveChanges();
}
```
- **Never** use `DbContext` directly in Services layer (enforced by project references)
- `IUnitOfWork` inherits from `IRepository`. It reads data to be modified and tracks changes for `SaveChanges()`.

### 3) Plugin Loading (Program.cs)
Modules are loaded dynamically at runtime:
```csharp
options
    .AddPlugin("Sales.Services", "Sales.DbContext", "Sales.ConsoleCommands")
    .AddPlugin("Notifications.Services");
```
- First parameter: primary assembly name (must have `EnableDynamicLoading=true`)
- Additional params: dependent assemblies in same LoadContext
- Specify all assemblies that are not referenced by any other assembly, grouped by module. Plugin == Module.
- One LoadContext is created for each defined plugin.
- Convention: `{ModuleName}.{AssemblySuffix}` matches folder structure. Note: the `Modules` and `Infra` folders are NOT part of the namespace, as it is a physical organization only.
- See `UI/ConsoleUi/Program.cs` for full bootstrap example

### 4) Module Initialization
Modules implement `IModule` for startup logic:
```csharp
[Service(typeof(IModule), ServiceLifetime.Singleton)]
class SalesServicesModule(INotificationService notificationService) : IModule
{
    public void Initialize(IHost host)
    {
        notificationService.NotifyAlive(this);
    }
}
```
- `Initialize()` called once at app startup, on `Main()` function

### 5) Entity Interceptors
Hook into EF lifecycle via `IEntityInterceptor<T>` or `IEntityInterceptor`:
- Registered automatically via `[Service]` attribute
- Applied by DataAccess layer (no direct EF SaveChanges calls)

#### 5.1.) Specific Entity Interceptor

- Use `IEntityInterceptor<T>` to register interceptors that will be applied to a specific entity type ONLY.
- Implement by inheriting from `EntityInterceptor<T>` and overriding methods like `OnSave()`, `OnDelete()`, etc.

```csharp
[Service(typeof(IEntityInterceptor<SalesOrderHeader>))]
class SalesOrderCalculationsInterceptor : EntityInterceptor<SalesOrderHeader>
{
    public void OnSave(IEntityEntry<SalesOrderHeader> entry, IUnitOfWork uof)
    {
        entry.Entity.TotalDue = entry.Entity.SubTotal + entry.Entity.TaxAmt;
    }
}
```

#### 5.2.) Global Entity Interceptor

- Use `IEntityInterceptor` to register interceptors that will be applied to ALL entities.
- Implement by inheriting from `GlobalEntityInterceptor` and overriding methods like `OnSave()`, `OnDelete()`, etc.

```csharp
[Service(typeof(IEntityInterceptor))]
internal sealed class AuditableInterceptor : GlobalEntityInterceptor<IAuditable>
{
    public override void OnSave(IEntityEntry<IAuditable> entry, IUnitOfWork unitOfWork)
    {
        if (entry.State.HasFlag(EntityEntryState.Added) || entry.State.HasFlag(EntityEntryState.Modified))
        {
            entry.Entity.ModifiedDate = DateTime.UtcNow;
        }
    }
}
```

---

## Build & Debug Workflow

### Building
```powershell
dotnet build                     # From repo root
dotnet run --project UI/ConsoleUi  # Run console app
```

### Plugin Build Dependencies
**Problem:** Plugin assemblies (e.g., `Sales.Services`) aren't referenced directly by host, so VS may not build them.

**Solution:** Add to Visual Studio build dependencies:
- Right-click solution → **Project Build Dependencies**
- Add plugin projects as dependencies of `UI/ConsoleUi`
- For multi-assembly plugins (e.g., `Sales.Services` + `Sales.DbContext`), add dependent asseblies as dependencies of the primary plugin assembly only (e.g., `Sales.Services`)

### Project Configuration
- Plugin assemblies which are dynamically loaded, as no other assemblies references them, have `<EnableDynamicLoading>true</EnableDynamicLoading>`
- `DbContext` projects because they hide EF use `<PrivateAssets>all</PrivateAssets>` for EF packages (prevents leaking to Services)
- Contracts has NO dependencies (`Contracts.csproj` is minimal)

---

## Coding Conventions
- **Async all the way:** No `.Result`/`.Wait()`, use `async`/`await` end-to-end
- **Nullability:** `<Nullable>enable</Nullable>` enforced - treat warnings as errors
- **Primary constructors:** Use for DI (e.g., `class Foo(IRepo repo) : IFoo`)
- **No comments:** Code should be self-documenting
- **Manual mapping:** Avoid AutoMapper - write explicit mapping code
- **Internal by default:** Mark services `internal` unless explicitly exported via `Contracts`
- **Namespaces:** Match folder structure, exclude `Modules`, `Infra` and `UI` from namespace as they are physical organization only

---

## Protected Areas (DO NOT MODIFY)
- `Infra/**` - Framework code, touch only via extension methods/adapters
- `*/DbContext/**` - EF-generated or scaffolded, modify via migrations
- `*.csproj` files - Avoid manual edits (managed by SDK/tooling)

If modification requested in these areas, suggest:
- Extension methods (for Infra)
- Partial classes (for DbContext entities if needed)
- Adapter pattern (for changing contracts)

---

## Response Style
When proposing changes:
1. **Plan first** if >2 files affected (numbered list)
2. **Quote paths** (e.g., `Modules/Sales/Sales.Services/OrderingService.cs`)
3. **Verify boundaries** (e.g., "Sales.Services → only refs Contracts/DataModel ✓")
4. **Ask ONE question** if context missing (not a list)

Example plan:
```
Plan
1. Add ICustomerService interface to Modules/Contracts/Sales/
2. Implement in Modules/Sales/Sales.Services/CustomerService.cs with [Service] attribute
3. Inject IRepository for read-only customer queries

Files:
- Modules/Contracts/Sales/ICustomerService.cs (new)
- Modules/Sales/Sales.Services/CustomerService.cs (new)

Checklist:
- [x] No cross-module dependencies
- [x] Uses IRepository (not DbContext)
- [x] Registered via [Service] attribute
```

---

## Quick Reference
| Task | Pattern |
|------|---------|
| Add service | `[Service(typeof(IFoo))]` on implementation in `*.Services/` |
| Read data | Inject `IRepository`, use `GetEntities<T>()` |
| Write data | `using var uof = repository.CreateUnitOfWork()` |
| Add module | Create folder under `Modules/`, add to `Program.cs` `.AddPlugin()` |
| Console command | Implement `IConsoleCommand` in `*.ConsoleCommands/` |
| Share types | Add to `Modules/Contracts/{Module}/` (interfaces/DTOs only) |

---

## Tests
- Unit tests in corresponded test project named as `{Assembly}.UnitTests` example: `Infra/AppBoot.UnitTests` (xUnit)
- Test plugin loading with `AssembliesLoaderTests.cs` examples
- Run: `dotnet test`

### Unit Tests Naming Convention
- `{MethodName}_{Scenario}_{ExpectedResult}` example: `CalculateTotal_OrderHasItems_ReturnsSum`

### Integration Tests Naming Convention
- `When{Scenario}_Then{ExpectedResult}` example: `WhenCreatingOrder_ThenOrderIsPersisted`
