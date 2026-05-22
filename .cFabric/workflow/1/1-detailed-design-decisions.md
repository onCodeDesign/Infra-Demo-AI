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
