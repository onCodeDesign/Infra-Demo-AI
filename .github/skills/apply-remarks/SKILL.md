---
name: apply-remarks
description: "Process code review remarks with anti-oscillation safeguards. Classify remarks, apply targeted fixes, produce a cumulative decisions ledger to prevent review loops."
version: 1.0.0
---

# Apply Remarks Skill

## Purpose

Provides the rules, classification logic, anti-oscillation safeguards, and decisions ledger format used by the coder agent in **Mode 3: APPLY REMARKS**. This skill ensures review remarks are processed systematically and that repeated review iterations converge instead of oscillating.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| `reviewReportPath` | Yes | Path to the review report (e.g., `docs/code-reviews/{issueId}-code-review_{timestamp}.md`) |
| `priorDecisionsPath` | No | Path to an existing decisions ledger from a prior iteration. Default: `docs/code-reviews/{issueId}-decisions.md` |
| Design documents | Yes | High-level and detailed design — used to validate whether a remark aligns with the design |

## Workflow

1. **Load context** → Read review report, design documents, and prior decisions ledger (if exists)
2. **Classify remarks** → For each remark, determine action: APPLY, REJECT, or DEFER (see Classification Rules)
3. **Check for oscillation** → Before applying any remark, check against prior decisions (see Anti-Oscillation Rules)
4. **Apply accepted remarks** → Minimal, targeted edits per remark — one logical change at a time
5. **Build + Test** → After each batch of related remarks, run build and tests
6. **Produce decisions ledger** → Append iteration to the Decisions Ledger (see format below)
7. **Commit** → Include applied remark IDs in commit message (e.g., `Applied R1, R3, R5`)

## Remark Classification Rules

Each remark from the review report is classified into exactly one action:

| Action | When |
|--------|------|
| **APPLY** | Remark is correct, improves quality, and does not contradict the design or a prior decision |
| **REJECT** | Remark contradicts the design, contradicts a prior decision (oscillation), is subjective/stylistic, or the fix would break something else |
| **DEFER** | Remark is valid but out of scope for this issue, or requires design clarification |

### Rejection Reasons

Use one of these codes when rejecting a remark:

| Code | Meaning |
|------|---------|
| `DESIGN_CONFLICT` | Remark asks for something that contradicts the approved design |
| `OSCILLATION_PREVENTED` | Applying this remark would reverse a prior decision |
| `SUBJECTIVE` | Remark is style/preference with no quality or correctness basis |
| `WOULD_BREAK` | Applying the remark would break build, tests, or other functionality |
| `OUT_OF_SCOPE` | Valid concern but not related to the current issue |

## Anti-Oscillation Rules

These rules prevent the review loop from cycling indefinitely:

1. **Prior decisions are binding** — If the prior decisions ledger contains a decision (APPLIED or REJECTED) for a remark targeting the same file + location + concern, that decision stands. Do NOT reverse it unless the new remark provides a **correctness argument** (bug, architecture violation, or design non-conformance) that the prior decision lacked.

2. **Semantic equivalence** — Two remarks are considered equivalent if they target the same code location and address the same concern, even if worded differently. Match by: file path + line range + dimension (e.g., "Code Quality", "Architecture Compliance").

3. **Contradiction detection** — If a new remark asks to undo or reverse a change that was APPLIED in a prior iteration, REJECT it with reason `OSCILLATION_PREVENTED` and reference the prior decision ID.

4. **Escalation** — If you detect 2+ oscillation conflicts in a single iteration, STOP processing and request human intervention. Output the conflicting decisions for review.

5. **New concerns only** — Remarks that address genuinely new issues (not present in prior iterations) are processed normally regardless of prior decisions.

## Decisions Ledger

### Location

Save to `docs/code-reviews/{issueId}-decisions.md`. If the file already exists from a prior iteration, **append** the new iteration — do NOT overwrite prior iterations.

### Format

```markdown
# Decisions Ledger — Issue #{issueId}

## Iteration {N} — {timestamp}

**Review report:** `{reviewReportPath}`
**Prior decisions:** `{priorDecisionsPath | "none"}`

### Applied

| ID | Remark | File | Line(s) | Rationale |
|----|--------|------|---------|----------|
| D{N}.{seq} | R{id}: {short title} | `{file}` | {lines} | {1-sentence why applied} |

### Rejected

| ID | Remark | File | Line(s) | Reason | Rationale |
|----|--------|------|---------|--------|----------|
| D{N}.{seq} | R{id}: {short title} | `{file}` | {lines} | {DESIGN_CONFLICT \| OSCILLATION_PREVENTED \| SUBJECTIVE \| WOULD_BREAK \| OUT_OF_SCOPE} | {1-sentence justification} |

### Deferred

| ID | Remark | File | Line(s) | Rationale |
|----|--------|------|---------|----------|
| D{N}.{seq} | R{id}: {short title} | `{file}` | {lines} | {1-sentence why deferred} |

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
