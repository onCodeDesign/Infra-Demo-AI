# Decisions Ledger — Issue #1

## Iteration 1 — 2026-05-24

**Review report:** `docs/code-reviews/1-code-review_20260524-1238.md`  
**Prior decisions:** none

### Applied

| ID | Remark | Target | Location | Rationale |
|----|--------|--------|----------|-----------|
| D1.1 | R1: `CustomerService` missing explicit `internal` modifier | `Modules/Sales/Sales.Services/CustomerService.cs` | L12 | Architecture convention in `copilot-instructions.md` mandates explicit `internal` on all services |
| D1.2 | R2: XML doc comments on `OverdueCustomerSummary` restate the obvious | `Modules/Contracts/Sales/OverdueCustomerSummary.cs` | L5-L16 | Project convention: "Only comment code that needs clarification" — property names are fully self-documenting |
| D1.3 | R3: Ordering test asserts names only, not date values driving the sort | `Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs` | L87-L111 | Adding `OldestOverdueDueDate` to the assertion pins the causal link and prevents a name-based sort from producing a false green |

### Rejected

| ID | Remark | Target | Location | Reason | Rationale |
|----|--------|--------|----------|--------|-----------|
| D1.4 | R4: HLD diagram shows `GetEntities<Customer>()` vs actual `GetEntities<SalesOrderHeader>()` | `docs/workitems/1-design.md` | Sequence diagram | OUT_OF_SCOPE | Informational note only; reviewer confirmed no code change needed — diagram is a minor HLD inaccuracy, not a defect |
| D1.5 | R5: `FakeRepository` is hand-rolled rather than NSubstitute | `Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs` | `FakeRepository` class | OUT_OF_SCOPE | Positive confirmation note — reviewer confirmed the hand-rolled fake is the correct choice; no change warranted |

---

## Iteration 2 — 2026-05-24

**Review report:** `docs/code-reviews/1-code-review_20260524-1323.md`  
**Prior decisions:** `.cFabric/workflow/1/1-code-review-decisions.md` (Iteration 1)

### Applied

| ID | Remark | Target | Location | Rationale |
|----|--------|--------|----------|-----------|
| D2.1 | R1: Cosmetic separator comment in test helper section | `Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs` | L179 | Project convention "Only comment code that needs clarification" — the separator is decorative and provides no clarification |

### Rejected

| ID | Remark | Target | Location | Reason | Rationale |
|----|--------|--------|----------|--------|-----------|
| D2.2 | R2: Out-of-scope files verified clean | `Modules/Sales/Sales.ConsoleCommands/OverdueCustomersConsoleCommand.cs` | N/A | OUT_OF_SCOPE | Informational note only confirming already-reviewed out-of-scope files are clean; no code change warranted |
