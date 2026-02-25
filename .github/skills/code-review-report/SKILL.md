---
name: code-review-report
description: "Generate structured code review reports in Markdown format suitable for import into GitHub PRs or Azure DevOps pull requests. Produces per-file inline remarks and a summary that renders natively in PR comment threads."
version: 2.0.0
output_format: Markdown + JSON
platforms: GitHub, Azure DevOps
---

# Code Review Report Skill

## Purpose

Generates a structured, portable code review report consisting of **two artifacts**:

1. **Markdown report** — human-readable, renders correctly as a GitHub or Azure DevOps PR comment; can be pasted directly or posted via `gh pr comment`
2. **JSON payload** — machine-ready structured data for programmatic import via `gh api` (GitHub) or `curl` (Azure DevOps REST API)

Both artifacts are saved to `docs/code-reviews/` and referenced in the chat summary.

## When to Use

- After completing a code review with the `code-reviewer` agent
- When the review output must be shared in a pull request on GitHub or Azure DevOps
- When a persistent review artifact is needed for traceability

## Output Files

| File | Path | Purpose |
|------|------|---------|
| Markdown report | `docs/code-reviews/{issueId}-review_{timestamp}.md` | Human-readable, paste into PR comment |
| JSON payload | `docs/code-reviews/{issueId}-review_{timestamp}.json` | Programmatic PR import (GitHub + Azure DevOps) |

**Timestamp format:** `yyyyMMdd-HHmm` (e.g., `20260225-1430`)

> Generate the JSON payload **only** when at least one remark has a resolvable file path. If all remarks are general (no file/line), produce only the Markdown report.

## Artifact 1 — Markdown Report Template

Use the following template **exactly** as the structure for the report file. Replace all `{placeholders}` with actual values. Omit sections that have no content (e.g., if no blockers, omit the blockers subsection) but always keep the top-level sections.

---

```markdown
# Code Review Report

| Field | Value |
|-------|-------|
| **Issue** | #{issueId} — {issueTitle} |
| **Date** | {date} |
| **Reviewer** | AI Code Reviewer - {Model} |
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

## Artifact 2 — JSON Payload Template

The JSON payload serves a dual purpose: it is the body for the **GitHub PR Review API** and contains all data needed to reconstruct **Azure DevOps PR threads**. The agent fills this structure from the collected remarks.

```json
{
  "_meta": {
    "issueId": "{issueId}",
    "issueTitle": "{issueTitle}",
    "date": "{date}",
    "timestamp": "{timestamp}",
    "markdownReport": "docs/code-reviews/{issueId}-review_{timestamp}.md"
  },
  "verdict": "{APPROVE|REQUEST_CHANGES|COMMENT}",
  "summary": "{2-4 sentence overview}",
  "body": "{full markdown summary block — same as the Markdown report's Summary + Metrics + Verdict sections}",
  "event": "{APPROVE|REQUEST_CHANGES|COMMENT}",
  "metrics": {
    "blockers": 0,
    "warnings": 0,
    "suggestions": 0,
    "notes": 0
  },
  "remarks": [
    {
      "id": "R1",
      "severity": "blocker",
      "dimension": "Architecture Compliance",
      "file": "Modules/Sales/Sales.Services/OrderingService.cs",
      "line": 42,
      "lineEnd": 42,
      "title": "{short title}",
      "description": "{full description}",
      "suggestedFix": "{direction — no full code}"
    }
  ]
}
```

### JSON Field Rules

| Field | Values | Notes |
|-------|--------|-------|
| `verdict` / `event` | `APPROVE`, `REQUEST_CHANGES`, `COMMENT` | `APPROVE WITH SUGGESTIONS` → use `COMMENT` |
| `severity` | `blocker`, `warning`, `suggestion`, `note` | Lowercase |
| `file` | Repo-root relative, forward slashes | e.g. `Modules/Sales/Sales.Services/Foo.cs` |
| `line` / `lineEnd` | Integer (1-based), or `null` if unknown | Set both to same value for single-line remarks |
| `suggestedFix` | Omit the field if remark is a `note` | |
| `body` | GFM Markdown string | Used as the top-level PR review body |

---

## CLI Import Commands

After the agent saves both files, output a **ready-to-run commands block** in the chat so the user can import the review into their PR immediately.

### GitHub — `gh` CLI

```bash
# Option A: Post the full Markdown report as a single PR comment
gh pr comment {PR_NUMBER} --body-file docs/code-reviews/{issueId}-review_{timestamp}.md

# Option B: Post as a formal PR review with inline file comments (uses GitHub Review API)
# Requires: GH_TOKEN with repo scope, jq installed
gh api repos/{OWNER}/{REPO}/pulls/{PR_NUMBER}/reviews \
  --method POST \
  --input docs/code-reviews/{issueId}-review_{timestamp}.json
```

> **Note for Option B:** The JSON payload's top-level structure matches the GitHub PR Review API.
> GitHub inline comments (`remarks[]`) require `path` (= `file`), `line`, and `body`.
> The `gh api --input` flag reads JSON directly from the file — no shell escaping needed.

### Azure DevOps — REST API via `curl`

```bash
# Post the full Markdown report as a PR thread (top-level comment)
curl -s -X POST \
  "https://dev.azure.com/{ORG}/{PROJECT}/_apis/git/repositories/{REPO}/pullRequests/{PR_ID}/threads?api-version=7.1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AZURE_DEVOPS_TOKEN" \
  --data-binary @- <<EOF
{
  "comments": [{"content": $(jq -Rs . < docs/code-reviews/{issueId}-review_{timestamp}.md), "commentType": 1}],
  "status": 1
}
EOF

# Post individual inline remarks from the JSON payload
# (run once per remark that has a non-null 'file' and 'line')
curl -s -X POST \
  "https://dev.azure.com/{ORG}/{PROJECT}/_apis/git/repositories/{REPO}/pullRequests/{PR_ID}/threads?api-version=7.1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AZURE_DEVOPS_TOKEN" \
  -d '{
    "comments": [{"content": "{R1 body}", "commentType": 1}],
    "threadContext": {
      "filePath": "/{file}",
      "rightFileStart": {"line": {line}, "offset": 1},
      "rightFileEnd": {"line": {lineEnd}, "offset": 1}
    },
    "status": 1
  }'
```

> The agent outputs **placeholder-filled** commands (with actual values substituted) so the user only needs to set environment variables (`OWNER`, `REPO`, `PR_NUMBER`, `AZURE_DEVOPS_TOKEN`) and run.

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
