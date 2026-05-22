---
name: design-review-md-report
description: "Generate structured detailed design review reports in Markdown format. Produces a quality checklist assessment, severity-grouped remarks, and a verdict."
version: 1.0.0
output_format: Markdown
---

# Design Review Markdown Report Skill

## Purpose

Generates a structured, portable detailed design review report as a **single Markdown artifact** that can be committed to the repository for traceability.

## When to Use

- After completing a detailed design review with the `detailed-designer` agent
- When a persistent review artifact is needed for traceability
- When the review output must be saved to a specific folder passed via the prompt

## Required Inputs

- **issueId**: GitHub issue number being reviewed
- **issueTitle**: Title of the issue (from GitHub or the design document header)
- **detailedDesignPath**: Relative path to the reviewed detailed design document
- **reportFolder**: Destination folder for the report file — **must be passed in the prompt**; ask the user if missing
- **model**: Name of the AI model performing the review (e.g., `Claude Sonnet 4.5`)

## Output File

**Path:** `{reportFolder}/{issueId}-detailed-design-review_{timestamp}.md`

**Timestamp format:** `yyyyMMdd-HHmm` (e.g., `20260522-1430`)

Create `{reportFolder}` if it does not exist.

## Markdown Report Template

Use the following template **exactly**. Replace all `{placeholders}` with actual values. Omit subsections that have no content (e.g., if Blockers count is 0, omit that subsection entirely) but always keep all top-level `##` sections.

---

```markdown
# Detailed Design Review Report

| Field | Value |
|-------|-------|
| **Issue** | #{issueId} — {issueTitle} |
| **Reviewed Document** | `{detailedDesignPath}` |
| **Date** | {date} |
| **Reviewer** | AI Detailed Designer - {model} |

## Verdict

**{APPROVE | REQUEST CHANGES | APPROVE WITH SUGGESTIONS}**

{1-2 sentence justification for the verdict.}

## Summary

{2-4 sentence overview of the detailed design quality, completeness, and adherence to architecture constraints.}

## Quality Checklist Assessment

| Check | Status |
|-------|--------|
| All interfaces have explicit signatures | {✅ \| ❌ \| ⚠️} |
| All DTOs have validation attributes | {✅ \| ❌ \| ⚠️} |
| All exceptions are documented | {✅ \| ❌ \| ⚠️} |
| Error handling is specified | {✅ \| ❌ \| ⚠️} |
| Cross-cutting concerns are addressed | {✅ \| ❌ \| ⚠️} |
| Edge cases are handled | {✅ \| ❌ \| ⚠️} |
| Test strategy is complete | {✅ \| ❌ \| ⚠️} |
| Implementation plan is actionable | {✅ \| ❌ \| ⚠️} |
| No actual implementation code (specs only) | {✅ \| ❌ \| ⚠️} |
| Architecture dependency rules respected | {✅ \| ❌ \| ⚠️} |

## Metrics

| Severity | Count |
|----------|-------|
| 🔴 Blocker | {count} |
| 🟡 Warning | {count} |
| 🟢 Suggestion | {count} |
| ℹ️ Note | {count} |
| **Total** | **{total}** |

## Remarks

### 🔴 Blockers

> Omit this subsection if count is 0

#### R{number}: {short title}

- **Section:** {Section heading in the detailed design document}
- **Dimension:** {Completeness | Clarity | Quality Bar | Consistency | Testability | Conciseness | Architecture Compliance}
- **Description:** {What is missing or wrong and why it matters}
- **Suggested Fix:** {Direction for resolution — no full code}

---

### 🟡 Warnings

> Omit this subsection if count is 0

#### R{number}: {short title}

- **Section:** {Section heading}
- **Dimension:** {dimension}
- **Description:** {description}
- **Suggested Fix:** {direction}

---

### 🟢 Suggestions

> Omit this subsection if count is 0

#### R{number}: {short title}

- **Section:** {Section heading}
- **Dimension:** {dimension}
- **Description:** {description}

---

### ℹ️ Notes

> Omit this subsection if count is 0

#### R{number}: {short title}

- **Section:** {Section heading}
- **Description:** {observation}

---

## Remarks Index

| # | Severity | Dimension | Section | Title |
|---|----------|-----------|---------|-------|
| R1 | 🔴 | Architecture Compliance | {section} | {title} |
| R2 | 🟡 | Completeness | {section} | {title} |
| ... | ... | ... | ... | ... |

## Action Items

{- If REQUEST CHANGES: enumerate the blockers to resolve before implementation can begin}
{- If APPROVE WITH SUGGESTIONS: list the suggestions worth considering}
{- If APPROVE: state readiness for handover to implementation}
```

---

## Formatting Rules

### General
- Remark numbers (`R1`, `R2`, ...) are sequential across all severity levels, ordered blockers-first
- Every remark in the detail sections must appear in the **Remarks Index** table
- Use the exact section heading from the reviewed document in the **Section** field

### Severity Assignment

| Severity | Criteria |
|----------|----------|
| 🔴 **Blocker** | Architecture rule violation, missing required contract (interface/DTO/exception), ambiguity that blocks implementation, cross-module dependency violation |
| 🟡 **Warning** | Quality concern with potential runtime impact, incomplete error handling specification, missing test for a designed scenario, minor design drift |
| 🟢 **Suggestion** | Style improvement, naming refinement, optional additional test, non-blocking clarity improvement |
| ℹ️ **Note** | Positive observation, context for future work, informational only |

## Edge Cases

### No Remarks Found
- Set verdict to `APPROVE`
- State in Summary that the design is complete with no issues found
- Omit all severity subsections under Remarks
- Keep the Remarks Index with a single row: `| — | — | — | — | No remarks |`

### Missing Design Document
- Add a 🔴 Blocker: `R1: Reviewed document not found`
- Mark all Quality Checklist rows as `❌`
- Set verdict to `REQUEST CHANGES`

## After Saving the Report

- Commit with message: `[AI:det-des, HUMAN:-, MODEL: {model}] docs: Add detailed design review for #{issueId}`
- Post a chat summary with: verdict, total remarks count, and the relative path to the saved file
