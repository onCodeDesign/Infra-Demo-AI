# Detailed Design Review Report

| Field | Value |
|-------|-------|
| **Issue** | #1 — Show Customers with Overdue Orders |
| **Reviewed Document** | `docs/workitems/1-detailed-design.md` |
| **Date** | 2026-05-22 |
| **Reviewer** | AI Detailed Designer - Claude Sonnet 4.6 |

## Verdict

**APPROVE WITH SUGGESTIONS**

The design is complete, well-structured, and ready for implementation. Two warnings and two suggestions are raised; none block implementation, but addressing the warnings will improve document quality and reduce developer ambiguity.

## Summary

The detailed design correctly extends `ICustomerService` with a read-only query method, defines a clean output DTO, and thoroughly specifies error handling, logging, security, and test strategy. Architecture constraints are fully respected: no cross-module references, no direct DbContext usage, and the new DTO and interface extension are correctly placed in `Contracts.Sales`. One warning concerns query-level implementation details leaking into the implementation plan; a second concerns an ambiguous conditional on console command tests.

## Quality Checklist Assessment

| Check | Status |
|-------|--------|
| All interfaces have explicit signatures | ✅ |
| All DTOs have validation attributes | ✅ |
| All exceptions are documented | ✅ |
| Error handling is specified | ✅ |
| Cross-cutting concerns are addressed | ✅ |
| Edge cases are handled | ✅ |
| Test strategy is complete | ⚠️ |
| Implementation plan is actionable | ⚠️ |
| No actual implementation code (specs only) | ⚠️ |
| Architecture dependency rules respected | ✅ |

> **Note on DTOs**: `OverdueCustomerSummary` is an output-only DTO; using the C# `required` keyword rather than `[Required]` data-annotation attributes is correct for this case.

## Metrics

| Severity | Count |
|----------|-------|
| 🔴 Blocker | 0 |
| 🟡 Warning | 2 |
| 🟢 Suggestion | 2 |
| ℹ️ Note | 2 |
| **Total** | **6** |

## Remarks

### 🟡 Warnings

#### R1: Implementation plan Phase 2 contains query/data-access pseudocode

- **Section:** Implementation Plan — Phase 2: Service Implementation
- **Dimension:** Conciseness
- **Description:** The five sub-bullets under "Implement `GetCustomersWithOverdueOrders()`" describe the LINQ filtering, grouping, projection, and ordering steps at an algorithmic level (e.g., "Filter: any `SalesOrderHeader` where `DueDate < DateTime.Today` AND `Status` not in `{Shipped, Cancelled}`", "Project to `OverdueCustomerSummary` with `CustomerName = $"{FirstName} {LastName}"`"). This crosses from spec into implementation detail. The design instructions explicitly prohibit including "data access patterns" or "LINQ queries". The information is already derivable from the interface, DTO definitions, and requirements summary.
- **Suggested Fix:** Replace the five sub-bullets with a single task item: "Implement `GetCustomersWithOverdueOrders()` in `CustomerService` per the interface contract and requirements summary." The algorithm is unambiguous from the contract alone.

---

#### R2: Console command test scope is conditional and ambiguous

- **Section:** Test Strategy — Unit Tests (`OverdueCustomersConsoleCommandTests`)
- **Dimension:** Clarity
- **Description:** The test class is qualified with "(`Sales.ConsoleCommands` test project, if present)". This conditional leaves it unclear whether the test project must be created as part of this work item or is simply skipped. The implementation plan (Phase 4) lists "Implement `OverdueCustomersConsoleCommandTests`" without any conditional, creating an inconsistency. Developers reading the two sections will encounter conflicting guidance.
- **Suggested Fix:** Make the scope definitive: either (a) remove the conditional and mandate the test project be created as part of Phase 4, or (b) add a Phase 0 task "Verify whether `Sales.ConsoleCommands.UnitTests` project exists; create it if not" and retain the conditional only in the Test Strategy preamble, not inline with the test list.

---

### 🟢 Suggestions

#### R3: `OldestOverdueDueDate` uses `DateTime` — consider `DateOnly`

- **Section:** Module-Level Contracts — DTOs
- **Dimension:** Quality Bar
- **Description:** `OldestOverdueDueDate` represents a calendar date with no time component. Using `DateTime` (which carries a time-of-day that will always be midnight) is technically correct but semantically imprecise. `DateOnly` (available since .NET 6) communicates intent more clearly and prevents accidental time-of-day comparisons or formatting noise in the console output. The existing `SalesOrderHeader.DueDate` entity property type should be checked — if it is already `DateTime`, a note to that effect would justify the choice.

---

#### R4: Console output format not specified

- **Section:** Implementation Plan — Phase 3: Console Command
- **Dimension:** Clarity
- **Description:** The plan states "Write each `OverdueCustomerSummary` record to `IConsole`" but does not specify the display format (e.g., column labels, field order, separator character, alignment). Developers will make inconsistent choices independently. A one-line format sample (e.g., `"{CustomerName} | Overdue: {OverdueOrderCount} | Oldest: {OldestOverdueDueDate:d}"`) in the implementation plan would eliminate this ambiguity.

---

### ℹ️ Notes

#### R5: Document is clean and appropriately concise

- **Section:** (Overall)
- **Description:** The design makes consistent use of "Not Required" with brief justifications across Data Model, External Systems Integration, Internal API Contracts, Fault Contracts, and Idempotency. This is exactly the right level of brevity; no padding or over-explanation.

---

#### R6: Acceptance criteria are fully mapped

- **Section:** Test Strategy — Acceptance Criteria
- **Description:** All seven acceptance criteria from the GitHub issue are mapped to specific named tests. This traceability is well done and will simplify verification at review time.

---

## Remarks Index

| # | Severity | Dimension | Section | Title |
|---|----------|-----------|---------|-------|
| R1 | 🟡 | Conciseness | Implementation Plan — Phase 2: Service Implementation | Implementation plan Phase 2 contains query/data-access pseudocode |
| R2 | 🟡 | Clarity | Test Strategy — Unit Tests (`OverdueCustomersConsoleCommandTests`) | Console command test scope is conditional and ambiguous |
| R3 | 🟢 | Quality Bar | Module-Level Contracts — DTOs | `OldestOverdueDueDate` uses `DateTime` — consider `DateOnly` |
| R4 | 🟢 | Clarity | Implementation Plan — Phase 3: Console Command | Console output format not specified |
| R5 | ℹ️ | — | (Overall) | Document is clean and appropriately concise |
| R6 | ℹ️ | — | Test Strategy — Acceptance Criteria | Acceptance criteria are fully mapped |

## Action Items

Address the following before considering the design fully polished (neither blocks implementation):

1. **R1** — Strip query-algorithm sub-bullets from Phase 2 of the implementation plan; replace with a single task referencing the interface contract.
2. **R2** — Resolve the conditional on `OverdueCustomersConsoleCommandTests`: either mandate creation of the test project in Phase 4 or add an explicit prerequisite task to check/create it.

Suggestions R3 and R4 are worth considering during implementation; they do not require design document changes before work begins.
