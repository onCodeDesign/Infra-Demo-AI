# Detailed Design Review Report

| Field | Value |
|-------|-------|
| **Issue** | #1 — Show Customers with Overdue Orders |
| **Reviewed Document** | `docs/workitems/1-detailed-design.md` |
| **Date** | 2026-05-22 |
| **Reviewer** | AI Detailed Designer - Claude Sonnet 4.6 |

## Verdict

**APPROVE WITH SUGGESTIONS**

The design is clear, implementable, and architecturally compliant. One consistency warning with the DTO type declaration and two non-blocking suggestions are worth addressing before implementation begins.

## Summary

The detailed design is thorough and well-scoped for a read-only query feature. Contracts, data model decisions, cross-cutting concerns, and the test strategy are all correctly specified. The design stays within approved architecture boundaries, requires no new entities, and correctly identifies no fault contracts are warranted. Minor inconsistencies exist between the DTO type keyword choice and the specification convention, and between the DTO name used here and in the architecture design.

## Quality Checklist Assessment

| Check | Status |
|-------|--------|
| All interfaces have explicit signatures | ✅ |
| All DTOs have validation attributes | ✅ |
| All exceptions are documented | ✅ |
| Error handling is specified | ✅ |
| Cross-cutting concerns are addressed | ✅ |
| Edge cases are handled | ✅ |
| Test strategy is complete | ✅ |
| Implementation plan is actionable | ✅ |
| No actual implementation code (specs only) | ✅ |
| Architecture dependency rules respected | ✅ |

## Metrics

| Severity | Count |
|----------|-------|
| 🔴 Blocker | 0 |
| 🟡 Warning | 1 |
| 🟢 Suggestion | 2 |
| ℹ️ Note | 2 |
| **Total** | **5** |

## Remarks

### 🟡 Warnings

#### R1: `OverdueCustomerSummary` declared as `sealed class` instead of `sealed record`

- **Section:** Module-Level Contracts > DTOs
- **Dimension:** Consistency
- **Description:** The detailed designer specification guidelines prescribe `sealed record` for output DTOs. `sealed class` works correctly but forgoes value equality and auto-generated `ToString()`, and is inconsistent with the forward-looking DTO convention established in this agent's own specification examples. While existing contracts (`CustomerData`, `SalesOrderResult`) pre-date this convention and use plain `class`, new DTOs introduced by this issue should conform to the documented standard.
- **Suggested Fix:** Change `public sealed class OverdueCustomerSummary` to `public sealed record OverdueCustomerSummary`. The `required` keyword and `init` setters carry over to records unchanged.

---

### 🟢 Suggestions

#### R2: DTO name differs from the approved architecture design

- **Section:** Module-Level Contracts > DTOs
- **Dimension:** Consistency
- **Description:** The architecture design (`1-design.md`) refers to the new DTO as `OverdueCustomerData`. The detailed design introduces `OverdueCustomerSummary`. The rename is arguably clearer but creates a traceability gap between the two documents that reviewers and future maintainers may find confusing.
- **Suggested Fix:** Add a brief inline note in the DTOs section acknowledging the rename and the rationale (e.g., *"Renamed from `OverdueCustomerData` (architecture design) to `OverdueCustomerSummary` for clarity"*).

---

#### R3: Console command test project creation not surfaced as a prerequisite

- **Section:** Implementation Plan > Phase 4
- **Dimension:** Clarity
- **Description:** `Sales.ConsoleCommands.UnitTests` does not exist in the repository. The plan handles this conditionally in Phase 4 ("Verify project exists; create it if not"), but by that point the implementer has already written the console command (Phase 3) and is now discovering a missing test scaffold. A missing project is infrastructure work that is better called out up-front.
- **Suggested Fix:** Move the project-creation task to Phase 1 or Phase 3 so the test scaffold is in place before the console command tests are written.

---

### ℹ️ Notes

#### R4: Adding a method to `ICustomerService` will break existing mocks

- **Section:** Implementation Plan > Phase 2
- **Description:** `ICustomerService` is already mocked or substituted in `Sales.Services.UnitTests`. Extending the interface with `GetCustomersWithOverdueOrders()` will cause compile errors in any existing test that creates a manual stub or a `Substitute.For<ICustomerService>()` without configuring the new method. This is expected and easy to fix, but implementers should be aware upfront.

---

#### R5: `CustomerData` existing DTO pattern does not use `required`/`init`

- **Section:** Module-Level Contracts > DTOs
- **Description:** The existing `CustomerData` and `SalesOrderResult` DTOs use mutable `{ get; set; }` properties without `required`. The new `OverdueCustomerSummary` correctly uses `required` and `init`. This improvement is welcome; the inconsistency with legacy DTOs is not a concern for this feature but worth a future clean-up ticket.

---

## Remarks Index

| # | Severity | Dimension | Section | Title |
|---|----------|-----------|---------|-------|
| R1 | 🟡 | Consistency | Module-Level Contracts > DTOs | `OverdueCustomerSummary` declared as `sealed class` instead of `sealed record` |
| R2 | 🟢 | Consistency | Module-Level Contracts > DTOs | DTO name differs from the approved architecture design |
| R3 | 🟢 | Clarity | Implementation Plan > Phase 4 | Console command test project creation not surfaced as a prerequisite |
| R4 | ℹ️ | Clarity | Implementation Plan > Phase 2 | Adding a method to `ICustomerService` will break existing mocks |
| R5 | ℹ️ | Consistency | Module-Level Contracts > DTOs | `CustomerData` existing DTO pattern does not use `required`/`init` |

## Action Items

The following suggestions are worth addressing before handing off to implementation:

- **R1 (Warning — address before implementation):** Change `OverdueCustomerSummary` from `sealed class` to `sealed record` to align with the specified DTO convention.
- **R2 (Suggestion):** Add an inline note in the DTOs section acknowledging the rename from `OverdueCustomerData`.
- **R3 (Suggestion):** Move the `Sales.ConsoleCommands.UnitTests` project-creation task to an earlier phase in the implementation plan.
- **R4 (Note — implementation heads-up):** Expect compile errors in `Sales.Services.UnitTests` after extending `ICustomerService`; update any affected mock setups.
