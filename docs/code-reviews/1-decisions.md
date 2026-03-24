# Decisions Ledger — Issue #1

## Iteration 1 — 2026-03-24

**Review report:** `docs/code-reviews/1-code-review_20260323-1034.md`
**Prior decisions:** none

### Applied

| ID | Remark | File | Line(s) | Rationale |
|----|--------|------|---------|-----------|
| D1.1 | R1: Unused `X509Certificates` import | `Modules/Sales/Sales.Services/CustomerService.cs` | L2 | Dead using directive removed to eliminate code noise and potential compiler warnings. |
| D1.2 | R2: Coder session artifact committed | `.copilot-cli/1-overdue-orders.coder.cli.md` | entire file | File removed via `git rm` and `.copilot-cli/` added to `.gitignore` to prevent recurrence. |
| D1.3 | R3: Missing test for `"Customer {CustomerID}"` fallback | `Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs` | after L162 | Added `GetCustomersWithOverdueOrders_HandlesEmptyNameFields_UsesCustomerId` covering the third name-construction step. |
| D1.4 | R4: `GetTarget` typed as concrete `FakeRepository` | `Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs` | L166 | Parameter type changed to `IRepository` to reduce coupling between factory helper and test double. |
| D1.5 | R5: `FakeRepository` naming convention | `Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs` | L197–L212 | Renamed `FakeRepository` to `RepositoryStub` and updated all references to align with project `Stub`/`Mock` suffix convention. |

### Rejected

| ID | Remark | File | Line(s) | Reason | Rationale |
|----|--------|------|---------|--------|-----------|
| D1.6 | R6: HLD async vs detailed design sync | `Modules/Contracts/Sales/ICustomerService.cs` | — | SUBJECTIVE | Informational note only; no code change required — already acknowledged in handoff context as an acceptable design refinement. |
