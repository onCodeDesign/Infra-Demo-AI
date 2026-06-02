# Decisions Ledger — Issue #1

## Iteration 1 — 2026-05-22T18:49:48+03:00

**Review report:** `.cFabric\workflow\1\1-detailed-design-review_20260522-1821.md`
**Prior decisions:** none

### Applied

| ID | Remark | Target | Location | Rationale |
|----|--------|--------|----------|-----------|
| D1.1 | R1: Implementation plan Phase 2 contains query/data-access pseudocode | `docs/workitems/1-detailed-design.md` | Implementation Plan — Phase 2: Service Implementation | Algorithmic sub-bullets cross into implementation detail; replaced with a single task referencing the interface contract, per design instructions prohibiting data access patterns and LINQ. |
| D1.2 | R2: Console command test scope is conditional and ambiguous | `docs/workitems/1-detailed-design.md` | Test Strategy — Unit Tests / Implementation Plan — Phase 4 | Removed the "if present" conditional from the test class heading; added an explicit prerequisite task in Phase 4 to verify/create the test project, eliminating inconsistency between the two sections. |
| D1.3 | R4: Console output format not specified | `docs/workitems/1-detailed-design.md` | Implementation Plan — Phase 3: Console Command | Added a one-line format sample `"{CustomerName} | Overdue: {OverdueOrderCount} | Oldest: {OldestOverdueDueDate:d}"` to remove implementation ambiguity. |

### Rejected

| ID | Remark | Target | Location | Reason | Rationale |
|----|--------|--------|----------|--------|-----------|
| D1.4 | R3: `OldestOverdueDueDate` uses `DateTime` — consider `DateOnly` | `docs/workitems/1-detailed-design.md` | Module-Level Contracts — DTOs | `DESIGN_CONFLICT` | The existing entity `SalesOrderHeader.DueDate` is `DateTime`; aligning the DTO to `DateOnly` would introduce an asymmetric type conversion at the mapping boundary, adding complexity without a correctness benefit. |

### Deferred

_None._

### Oscillation Conflicts

_None detected._

---

## Iteration 2 — 2026-05-22T19:18:11+03:00

**Review report:** `.cFabric\workflow\1\1-detailed-design-review_20260522-1852.md`
**Prior decisions:** `.cFabric\workflow\1\1-detailed-design-decisions.md` (Iteration 1)

### Applied

| ID | Remark | Target | Location | Rationale |
|----|--------|--------|----------|-----------|
| D2.1 | R1: `OverdueCustomerSummary` declared as `sealed class` instead of `sealed record` | `docs/workitems/1-detailed-design.md` | Module-Level Contracts > DTOs | Changed to `sealed record` to align with the DTO convention prescribed by the detailed designer specification; `required`/`init` properties carry over unchanged. |
| D2.2 | R2: DTO name differs from the approved architecture design | `docs/workitems/1-detailed-design.md` | Module-Level Contracts > DTOs | Added an inline `> Note:` before the DTO definition acknowledging the rename from `OverdueCustomerData` with a brief rationale, closing the traceability gap. |
| D2.3 | R3: Console command test project creation not surfaced as a prerequisite | `docs/workitems/1-detailed-design.md` | Implementation Plan > Phase 1 / Phase 4 | Moved the project-creation task from Phase 4 to Phase 1 so the test scaffold is available before the console command is written; removed redundant step from Phase 4. Prior decision D1.2 addressed conditional scope (different concern). |
| D2.4 | R4: Adding a method to `ICustomerService` will break existing mocks | `docs/workitems/1-detailed-design.md` | Implementation Plan > Phase 2: Service Implementation | Added an inline Note in Phase 2 warning implementers to update affected `Substitute.For<ICustomerService>()` test setups after extending the interface. |

### Rejected

_None._

### Deferred

| ID | Remark | Target | Location | Rationale |
|----|--------|--------|----------|-----------|
| D2.5 | R5: `CustomerData` existing DTO pattern does not use `required`/`init` | `docs/workitems/1-detailed-design.md` | Module-Level Contracts > DTOs | OUT_OF_SCOPE — The note itself states this is not a concern for this feature; legacy DTO clean-up belongs in a separate issue. |

### Oscillation Conflicts

_None detected.__
