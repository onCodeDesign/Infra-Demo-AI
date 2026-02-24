---
name: add-module-plugin
description: "Step-by-step guide for adding a new module as a plugin: folder structure, plugin registration in Program.cs, module initialization, and build-order dependencies in AppInfraDemo.sln."
version: 1.0.0
language: C#
framework: .NET 10.0
---

# Add Module as Plugin Skill

## Overview

A **plugin** is one isolated `AssemblyLoadContext`. 
It has one primary assembly (the first argument in `.AddPlugin(...)`) and zero or more co-loaded assemblies (additional arguments in `.AddPlugin(...)` that are not referenced by any other assembly). 
Logically, may contain more **modules** if multiple `IModule` implementations are present, but we follow a one-plugin-per-module convention for simplicity. 
Follow the steps below in order when adding a new module.

---

## Step 1 — Folder & Project Structure

Create the standard sub-projects under `Modules/{Module}/`:

```
Modules/{Module}/
├─ {Module}.DataModel/        # Entities only — no logic, no EF references
├─ {Module}.Services/         # Business logic — [Service] attribute for DI
├─ {Module}.DbContext/        # EF DbContext (scaffolded/generated)
└─ {Module}.ConsoleCommands/  # Optional — IConsoleCommand implementations
```

Project configuration rules:
- `{Module}.Services`, `{Module}.ConsoleCommands` → set `<EnableDynamicLoading>true</EnableDynamicLoading>`
- `{Module}.DbContext` → use `<PrivateAssets>all</PrivateAssets>` on EF packages (prevents leaking EF to Services)
- `{Module}.DataModel` → standard class library, no special flags

---

## Step 2 — Plugin Registration in Program.cs

Register the new module in `UI/ConsoleUi/Program.cs` via `.AddPlugin()`:

```csharp
options
    .AddPlugin("Sales.Services", "Sales.DbContext", "Sales.ConsoleCommands")
    .AddPlugin("Notifications.Services")
    .AddPlugin("{Module}.Services", "{Module}.DbContext", "{Module}.ConsoleCommands"); // new
```

Rules:
- **First argument** — primary assembly name; must have `<EnableDynamicLoading>true</EnableDynamicLoading>`
- **Additional arguments** — all co-loaded assemblies in the same `LoadContext` that are not referenced by any other assembly; must also have `<EnableDynamicLoading>true</EnableDynamicLoading>`
- One `LoadContext` is created per `.AddPlugin(...)`
- Naming convention: `{ModuleName}.{AssemblySuffix}` — `Modules/` and `Infra/` folders are physical only, not part of the namespace

---

## Step 3 — Module Initialization

Implement `IModule` in `{Module}.Services` for startup logic:

```csharp
[Service(typeof(IModule), ServiceLifetime.Singleton)]
internal sealed class {Module}ServicesModule(INotificationService notificationService) : IModule
{
    public void Initialize(IHost host)
    {
        notificationService.NotifyAlive(this);
    }
}
```

- `Initialize()` is called once at app startup from `Main()`
- Keep it lightweight — wire up cross-module notifications, warm caches, etc.
- Inject only `Contracts` interfaces (no cross-module service types)

### Initialization Order

By default the order of `IModule.Initialize()` calls is non-deterministic.
To control the order use `[Priority(int)]` attribute from `AppBoot/DependencyInjection` on the `IModule` implementation.

---

## Step 4 — Build-Order Dependencies in AppInfraDemo.sln

Plugin assemblies have `<EnableDynamicLoading>true</EnableDynamicLoading>` and are **not** referenced directly, so `dotnet build AppInfraDemo.sln` skips them unless build-order dependencies are declared explicitly.

Add `ProjectSection(ProjectDependencies) = postProject` blocks inside the relevant `Project(...)` entries in `AppInfraDemo.sln`.

### Rules

- The **primary** plugin assembly must be declared as a Project Dependency of `ConsoleUi`
- Each co-loaded assembly (the additional params in `.AddPlugin(...)`) must be declared as a Project Dependency of the **primary** assembly

### Pattern

**`ConsoleUi` → primary plugin assemblies**

```
Project("{FAE04EC0-...}") = "ConsoleUi", "UI\ConsoleUi\ConsoleUi.csproj", "{GUID-ConsoleUi}"
    ProjectSection(ProjectDependencies) = postProject
        {GUID-Sales.Services}                = {GUID-Sales.Services}
        {GUID-Notifications.Services}        = {GUID-Notifications.Services}
        {GUID-ProductsManagement.Services}   = {GUID-ProductsManagement.Services}
        {GUID-PersonsManagement.Services}    = {GUID-PersonsManagement.Services}
        {GUID-Export.Services}               = {GUID-Export.Services}
    EndProjectSection
EndProject
```

**Primary plugin assembly → its co-loaded assemblies**

```
Project("{FAE04EC0-...}") = "Sales.Services", "Modules\Sales\Sales.Services\Sales.Services.csproj", "{GUID-Sales.Services}"
    ProjectSection(ProjectDependencies) = postProject
        {GUID-Sales.DbContext}          = {GUID-Sales.DbContext}
        {GUID-Sales.ConsoleCommands}    = {GUID-Sales.ConsoleCommands}
    EndProjectSection
EndProject
```

If a plugin has no co-loaded assemblies (e.g., `.AddPlugin("Notifications.Services")`), no `ProjectDependencies` block is needed on that project.

### Finding GUIDs

GUIDs are on the `Project(...)` line of each entry in `AppInfraDemo.sln`:

```
Project("{FAE04EC0-...}") = "Sales.DbContext", "Modules\Sales\Sales.DbContext\...", "{8ECFDB0C-9146-4C51-B8AF-3DC696492DAE}"
                                                                                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                                                                     use this GUID in ProjectDependencies
```

### Via Visual Studio UI

Right-click the solution → **Project Build Dependencies** → select the dependant project and tick its dependencies. This writes the same `ProjectSection(ProjectDependencies)` blocks automatically.
