---
name: apply-remarks
description: "Process review remarks with anti-oscillation safeguards. Classify remarks, apply targeted edits, produce a cumulative decisions ledger to prevent review loops. Works for code reviews, design reviews, and any review-driven workflow."
version: 1.0.0
---

# Apply Remarks Skill

## Purpose

Provides the rules, classification logic, anti-oscillation safeguards, and decisions ledger format used by any agent that applies remarks from a structured review report. This skill ensures review remarks are processed systematically and that repeated review iterations converge instead of oscillating.

The skill is **review-target-agnostic**: it works for code reviews (where remarks point at files and lines), design reviews (where remarks point at documents and sections), and any future review type.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| `reviewReportPath` | Yes | Path to the review report being processed |
| `priorDecisionsPath` | No | Path to an existing decisions ledger from a prior iteration. The invoking agent supplies a default appropriate to its domain |
| `decisionsLedgerPath` | Yes | Path where the new/updated decisions ledger will be saved. Supplied by the invoking agent (see Ledger Location) |
| `referenceDocuments` | Yes | One or more authoritative documents used to validate whether a remark is consistent with prior agreements (e.g., architecture design, detailed design, requirements, ADRs) |

## Workflow

1. **Load context** → Read review report, reference documents, and prior decisions ledger (if exists)
2. **Classify remarks** → For each remark, determine action: APPLY, REJECT, or DEFER (see Classification Rules)
3. **Check for oscillation** → Before applying any remark, check against prior decisions (see Anti-Oscillation Rules)
4. **Apply accepted remarks** → Minimal, targeted edits per remark — one logical change at a time
5. **Verify** → Run the verification appropriate to the target (e.g., build + test for code; render/lint check for docs). Skip if no verification applies.
6. **Produce decisions ledger** → Append iteration to the Decisions Ledger (see format below)
7. **Commit** → Include applied remark IDs in commit message (e.g., `Applied R1, R3, R5`)

## Remark Classification Rules

Each remark from the review report is classified into exactly one action:

| Action | When |
|--------|------|
| **APPLY** | Remark is correct, improves quality, and does not contradict the reference documents or a prior decision |
| **REJECT** | Remark contradicts a reference document, contradicts a prior decision (oscillation), is subjective/stylistic, or the fix would break something else |
| **DEFER** | Remark is valid but out of scope for this issue, or requires clarification from a higher-level document |

### Rejection Reasons

Use one of these codes when rejecting a remark:

| Code | Meaning |
|------|---------|
| `DESIGN_CONFLICT` | Remark asks for something that contradicts an approved reference document (architecture, detailed design, requirements) |
| `OSCILLATION_PREVENTED` | Applying this remark would reverse a prior decision |
| `SUBJECTIVE` | Remark is style/preference/subjective with no quality or correctness basis |
| `WOULD_BREAK` | Applying the remark would break verification (build, tests, doc render) or other functionality |
| `OUT_OF_SCOPE` | Valid concern but not related to the current issue |

Invoking agents MAY define additional domain-specific triggers for `DESIGN_CONFLICT` (e.g., the detailed-designer rejects remarks that demand implementation code in a spec document).

## Anti-Oscillation Rules

These rules prevent the review loop from cycling indefinitely:

1. **Prior decisions are binding** — If the prior decisions ledger contains a decision (APPLIED or REJECTED) for a remark targeting the same target + location + concern, that decision stands. Do NOT reverse it unless the new remark provides a **correctness argument** (bug, architecture violation, or reference-document non-conformance) that the prior decision lacked.

2. **Semantic equivalence** — Two remarks are considered equivalent if they target the same location and address the same concern, even if worded differently. Match by: target + location + dimension.

3. **Contradiction detection** — If a new remark asks to undo or reverse a change that was APPLIED in a prior iteration, REJECT it with reason `OSCILLATION_PREVENTED` and reference the prior decision ID.

4. **Escalation** — If you detect 2+ oscillation conflicts in a single iteration, STOP processing and request human intervention. Output the conflicting decisions for review.

5. **New concerns only** — Remarks that address genuinely new issues (not present in prior iterations) are processed normally regardless of prior decisions.

## Status update

When reviewing a document, if its status is "Awaiting Review", update it based on the verdict of the review, to "AI: Review Applied - {verdict} - iteration {N}" regardless of how the  remarks are classified.
If the status already contains "AI: Review Applied - {verdict} - iteration {M}", update it to "AI: Review Applied - {verdict} - iteration {N}" with N = M+1.

## Decisions Ledger

### Location

Save to the `decisionsLedgerPath` supplied by the invoking agent. If the file already exists from a prior iteration, **append** the new iteration — do NOT overwrite prior iterations.
When the path is not provided, use the defaults in below table.

File name conventions and default paths per invoking agent:

| Invoking agent | Default `decisionsLedgerPath` | File name |
|----------------|-----------------------------------|-----------------------------------|
| `coder` (code review remarks) | `docs/code-reviews/` | `{issueId}-code-review-decisions.md` |
| `detailed-designer` (design review remarks) | `docs/workitems/` | `{issueId}-detailed-design-decisions.md` |
| `architect` (architecture review remarks) | `docs/workitems/` | `{issueId}-design-decisions.md` |

### Locator Semantics

The ledger uses two generic locator columns:

- **Target** — *what* the remark points at. For code, this is a file path. For a design review, this is the document path.
- **Location** — *where inside the target*. For code, this is a line or line range (e.g., `L42`, `L42-L58`). For a design review, this is a section heading (e.g., `## Module-Level Contracts > ### Interfaces`).

### Format

```markdown
# Decisions Ledger — Issue #{issueId}

## Iteration {N} — {timestamp}

**Review report:** `{reviewReportPath}`
**Prior decisions:** `{priorDecisionsPath | "none"}`

### Applied

| ID | Remark | Target | Location | Rationale |
|----|--------|--------|----------|----------|
| D{N}.{seq} | R{id}: {short title} | `{target}` | {location} | {1-sentence why applied} |

### Rejected

| ID | Remark | Target | Location | Reason | Rationale |
|----|--------|--------|----------|--------|----------|
| D{N}.{seq} | R{id}: {short title} | `{target}` | {location} | {DESIGN_CONFLICT \| OSCILLATION_PREVENTED \| SUBJECTIVE \| WOULD_BREAK \| OUT_OF_SCOPE} | {1-sentence justification} |

### Deferred

| ID | Remark | Target | Location | Rationale |
|----|--------|--------|----------|----------|
| D{N}.{seq} | R{id}: {short title} | `{target}` | {location} | {1-sentence why deferred} |

### Oscillation Conflicts

> Omit this section if none detected

| New Remark | Contradicts Decision | Resolution |
|------------|---------------------|------------|
| R{id} | D{prev}.{seq} | REJECTED — prior decision stands |
```

### ID Scheme

- **Iteration number** `{N}`: starts at 1, increments per review-apply cycle
- **Sequence** `{seq}`: 1-based index within the iteration, across all actions
- **Example**: `D2.3` = Iteration 2, third decision processed

