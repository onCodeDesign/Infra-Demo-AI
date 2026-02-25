````skill
---
name: code-review-report
description: "Generate structured code review reports in Markdown format suitable for import into GitHub PRs or Azure DevOps pull requests. Produces per-file inline remarks and a summary that renders natively in PR comment threads."
version: 1.0.0
output_format: Markdown
platforms: GitHub, Azure DevOps
---

# Code Review Report Skill

## Purpose

Generates a structured, portable code review report in Markdown that:
1. Renders correctly when pasted as a **GitHub PR comment** or **Azure DevOps PR comment**
2. Can be saved as a persistent artifact in `docs/code-reviews/`
3. Contains per-file remarks with line references that reviewers can act on
4. Provides a machine-parseable remarks table for downstream automation

## When to Use

- After completing a code review with the `code-reviewer` agent
- When the review output must be shared in a pull request on GitHub or Azure DevOps
- When a persistent review artifact is needed for traceability

## Output Locations

| Output | Path |
|--------|------|
| Report file | `docs/code-reviews/{issueId}-review_{timestamp}.md` |
| Chat summary | Displayed inline in the agent conversation |

**Timestamp format:** `yyyyMMdd-HHmm` (e.g., `20260225-1430`)

## Report Template

Use the following template **exactly** as the structure for the report file. Replace all `{placeholders}` with actual values. Omit sections that have no content (e.g., if no blockers, omit the blockers subsection) but always keep the top-level sections.

---

```markdown
# Code Review Report

| Field | Value |
|-------|-------|
| **Issue** | #{issueId} — {issueTitle} |
| **Date** | {date} |
| **Reviewer** | AI Code Reviewer (GitHub Copilot) |
| **Build** | {✅ Pass \| ❌ Fail} |
| **Tests** | {✅ Pass \| ❌ Fail \| ⚠️ Partial} |

## Design Documents

| Document | Status |
|----------|--------|
| High-Level Design (`docs/workitems/{issueId}-design.md`) | {✅ Found \| ❌ Missing} |
| Detailed Design (`docs/workitems/{issueId}-detailed-design.md`) | {✅ Found \| ❌ Missing} |

## Verdict

**{APPROVE | REQUEST CHANGES | APPROVE WITH SUGGESTIONS}**

{1-2 sentence justification for the verdict}

## Summary

{2-4 sentence overview of the implementation quality. Is it faithful to the design? What are the main strengths and concerns?}

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

- **File:** `{relative/path/to/File.cs}`
- **Line(s):** {line or range}
- **Dimension:** {Design Conformance | Architecture Compliance | Code Quality | Test Quality | Scope & Hygiene}
- **Description:** {What is wrong and why it matters}
- **Suggested Fix:** {Direction for resolution — no full code}

---

### 🟡 Warnings

> Omit this subsection if count is 0

#### R{number}: {short title}

- **File:** `{relative/path/to/File.cs}`
- **Line(s):** {line or range}
- **Dimension:** {dimension}
- **Description:** {description}
- **Suggested Fix:** {direction}

---

### 🟢 Suggestions

> Omit this subsection if count is 0

#### R{number}: {short title}

- **File:** `{relative/path/to/File.cs}`
- **Line(s):** {line or range}
- **Dimension:** {dimension}
- **Description:** {description}

---

### ℹ️ Notes

> Omit this subsection if count is 0

#### R{number}: {short title}

- **File:** `{relative/path/to/File.cs}`
- **Description:** {observation}

---

## Remarks Index

| # | Severity | Dimension | File | Title |
|---|----------|-----------|------|-------|
| R1 | 🔴 | Design Conformance | `path/File.cs` | {title} |
| R2 | 🟡 | Architecture | `path/File.cs` | {title} |
| ... | ... | ... | ... | ... |

## Files Reviewed

| File | Role | Remarks |
|------|------|---------|
| `{relative/path/to/File.cs}` | {Contract \| Service \| Entity \| Interceptor \| Test \| Command \| Other} | {R1, R3 \| None} |

## Design Conformance Summary

| Design Component | Status | Remarks |
|-----------------|--------|---------|
| {Interface/DTO/Entity name from design} | {✅ Matches \| ⚠️ Drifted \| ❌ Missing \| ➕ Extra} | {Remark refs or "—"} |

## Next Steps

{Recommended actions based on the verdict:}
{- If REQUEST CHANGES: enumerate the blockers to resolve}
{- If APPROVE WITH SUGGESTIONS: list the suggestions worth considering}
{- If APPROVE: state readiness for merge}
```

---

## Formatting Rules

### General
- Use **relative paths** from the repository root for all file references (e.g., `Modules/Sales/Sales.Services/OrderingService.cs`)
- Remark numbers (`R1`, `R2`, ...) are sequential across all severity levels, ordered blockers-first
- Every remark in the detail sections must appear in the **Remarks Index** table
- Every file in the file list must appear in the **Files Reviewed** table, even if it has no remarks

### Severity Assignment

| Severity | Criteria |
|----------|----------|
| 🔴 **Blocker** | Architecture rule violation (from `copilot-instructions.md`), design contract breach, missing component from design, build/test failure caused by implementation |
| 🟡 **Warning** | Quality concern with potential runtime impact, minor design drift that changes behavior, incomplete error handling, missing test for a designed scenario |
| 🟢 **Suggestion** | Style improvement, naming refinement, optional optimization, additional test for undocumented edge case |
| ℹ️ **Note** | Positive observation, context for future work, informational only |

### Line References
- Single line: `L42`
- Range: `L42-L58`
- If line number cannot be determined, use `(general)` and explain the scope in the description

### GitHub PR Compatibility
The report is designed to render correctly when pasted as a GitHub PR comment:
- Tables use standard GitHub-flavored Markdown (GFM)
- Emoji severities (🔴🟡🟢ℹ️) render natively
- No HTML tags — pure Markdown only
- Collapsible sections are NOT used (inconsistent support across platforms)
- Code references use backtick-wrapped paths, not links (links break across forks)

### Azure DevOps PR Compatibility
- Azure DevOps supports GFM in PR comments
- Same Markdown renders correctly in both platforms
- Avoid Mermaid diagrams in the report (not supported in Azure DevOps PR comments)

## Chat Summary Format

In addition to saving the report file, output a **condensed summary** in the chat conversation. This is what the agent displays inline:

```
# Code Review: Issue #{id} — {title}

**Design Documents:**
- High-Level: docs/workitems/{id}-design.md — {found|MISSING}
- Detailed: docs/workitems/{id}-detailed-design.md — {found|MISSING}

**Build:** {✅|❌} | **Tests:** {✅|❌}
**Blockers:** {count} | **Warnings:** {count} | **Suggestions:** {count} | **Notes:** {count}

## Verdict
{APPROVE | REQUEST CHANGES | APPROVE WITH SUGGESTIONS}

## Summary
{2-4 sentence overview}

## Top Remarks
{List only 🔴 blockers and 🟡 warnings here, as bullet points:}
- **R1** 🔴 `path/File.cs` — {title}
- **R2** 🟡 `path/File.cs` — {title}

📄 Full report saved to: `docs/code-reviews/{issueId}-review_{timestamp}.md`
```

## Edge Cases

### No Remarks Found
If the review finds zero remarks across all dimensions:
- Set verdict to `APPROVE`
- In the Summary, state that the implementation matches the design with no issues found
- Omit the severity subsections under Remarks
- Keep the Remarks Index table with a single row: `| — | — | — | — | No remarks |`

### Missing Design Document
- Add a 🔴 Blocker remark: `R1: Missing design document`
- File: the missing document path
- In the Design Conformance Summary, mark all components as `⚠️ Cannot verify`
- Reduce confidence statement in the Summary

### Partial Review (subset of dimensions)
If the agent was asked to review only specific dimensions (e.g., "Review only architecture compliance"):
- Only include the requested dimension sections
- Add a note at the top of the report: `> ⚠️ Partial review — only {dimension} was assessed`
- Verdict reflects only the assessed dimensions

### Large File Lists (>20 files)
- Group the Files Reviewed table by module/folder
- Add a sub-header per module for readability
````
