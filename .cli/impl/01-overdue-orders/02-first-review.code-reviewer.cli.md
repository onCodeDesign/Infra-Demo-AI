> copilot --agent=code-reviewer `
> --model=claude-sonnet-4.6 `
> --allow-all-tools `
> --autopilot `
> -p "Review the implementation for issue #1. When finished output a review report in markdown format.
> Context: Mode=IMPLEMENT+UNIT_TESTS,
> Files=Modules/Contracts/Sales/CustomerOverdueOrdersData.cs, Modules/Contracts/Sales/ICustomerService.cs, Modules/Sales/Sales.Services/CustomerService.cs, Modules/Sales/Sales.ConsoleCommands/ShowCustomersWithOverdueOrdersConsoleCommand.cs, Modules/Sales/Sales.Services.UnitTests/Sales.Services.UnitTests.csproj, Modules/Sales/Sales.Services.UnitTests/xunit.runner.json, Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs, Modules/Sales/Sales.Services/Sales.Services.csproj, AppInfraDemo.sln,
> Build=PASS,
> Tests=PASS,
> Deviations=NONE,
> Commits=33220c49085a1de911b9363f13eb25cae15258d4, 263d3b04e95c14cb427a2a050033e3cd8c6cb460, 415764b6c6bb408f8c410aadab163a62c40c25c5"
● List directory docs
  └ 1 file found

✗ Get issue details
  └ MCP server 'github-mcp-server': McpError: MCP error 0: failed to get issue: GET
    https://api.github.com/repos/DevAgenticAI/Infra-Demo-AI/issues/1: 404 Not Found []

● List directory docs\workitems
  └ 2 files found

● Read 1-detailed-design.md
  │ docs\workitems\1-detailed-design.md
  └ 209 lines read

● Read 1-design.md
  │ docs\workitems\1-design.md
  └ 108 lines read

● Read ShowCustomersWithOverdueOrdersConsoleCommand.cs
  │ Modules\Sales\Sales.ConsoleCommands\ShowCustomersWithOverdueOrdersConsoleCommand.cs
  └ 29 lines read

● Read CustomerOverdueOrdersData.cs
  │ Modules\Contracts\Sales\CustomerOverdueOrdersData.cs
  └ 8 lines read

● Read CustomerServiceTests.cs
  │ Modules\Sales\Sales.Services.UnitTests\CustomerServiceTests.cs
  └ 213 lines read

● Read ICustomerService.cs
  │ Modules\Contracts\Sales\ICustomerService.cs
  └ 12 lines read

● Read CustomerService.cs
  │ Modules\Sales\Sales.Services\CustomerService.cs
  └ 85 lines read

● Read Sales.Services.csproj
  │ Modules\Sales\Sales.Services\Sales.Services.csproj
  └ 22 lines read

● Read Sales.Services.UnitTests.csproj
  │ Modules\Sales\Sales.Services.UnitTests\Sales.Services.UnitTests.csproj
  └ 38 lines read

● Check if decisions ledger exists (shell)
  │ Test-Path "C:\D\Projects\DevAgenticAI\Infra-Demo-AI\src.worktrees\cli\feature-1-t6\docs\code-rev
  │ iews\1-decisions.md"
  └ 2 lines...

● Read xunit.runner.json
  │ Modules\Sales\Sales.Services.UnitTests\xunit.runner.json
  └ 4 lines read

● Check commit info (shell)
  │ cd "C:\D\Projects\DevAgenticAI\Infra-Demo-AI\src.worktrees\cli\feature-1-t6" && git --no-pager
  │ log --oneline 33220c49085a1de911b9363f13eb25cae15258d4 263d3b04e95c14cb427a2a050033e3cd8c6cb460
  │ 415764b6c6bb408f8c410aadab163a62c40c25c5 2>&1 | head -20
  └ 7 lines...

● Check commit stats (shell)
  │ cd "C:\D\Projects\DevAgenticAI\Infra-Demo-AI\src.worktrees\cli\feature-1-t6" && git --no-pager
  │ show 33220c49085a1de911b9363f13eb25cae15258d4 --stat 2>&1; git --no-pager show
  │ 263d3b04e95c14cb427a2a050033e3cd8c6cb460 --stat 2>&1; git --no-pager show
  │ 415764b6c6bb408f8c410aadab163a62c40c25c5 --stat 2>&1
  └ 20 lines...

● List directory Modules\Sales\Sales.DataModel
  └ 5 files found

● Check code-reviews directory (shell)
  │ Test-Path
  │ "C:\D\Projects\DevAgenticAI\Infra-Demo-AI\src.worktrees\cli\feature-1-t6\docs\code-reviews" &&
  │ Get-ChildItem
  │ "C:\D\Projects\DevAgenticAI\Infra-Demo-AI\src.worktrees\cli\feature-1-t6\docs\code-reviews" 2>&1
  └ 7 lines...

● List directory Modules\Sales\Sales.DataModel\Values
  └ 1 file found

● Read SalesOrderHeaderStatusValues.cs
  │ Modules\Sales\Sales.DataModel\Values\SalesOrderHeaderStatusValues.cs
  └ 15 lines read

Now I have all context. Let me invoke the skill and generate the report.

● skill(code-review-md-report)

● Create code-reviews directory (shell)
  │ New-Item -ItemType Directory -Path
  │ "C:\D\Projects\DevAgenticAI\Infra-Demo-AI\src.worktrees\cli\feature-1-t6\docs\code-reviews"
  │ -Force | Out-Null; Write-Host "Created"
  └ 2 lines...

● Create 1-code-review_20260323-1034.md +157
  └ docs\code-reviews\1-code-review_20260323-1034.md

● Commit review report (shell)
  │ cd "C:\D\Projects\DevAgenticAI\Infra-Demo-AI\src.worktrees\cli\feature-1-t6" && git add
  │ docs/code-reviews/1-code-review_20260323-1034.md && git commit -m "[AI:CodeReviewer, HUMAN:-,
  │ MODEL: Claude Sonnet 4.6] (#1) Add code review report
  │
  │ Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  └ 4 lines...

---

# Code Review: Issue #1 — Show customers with overdue orders

**Design Documents:**
- High-Level: `docs/workitems/1-design.md` — ✅ Found
- Detailed: `docs/workitems/1-detailed-design.md` — ✅ Found

**Build:** ✅ | **Tests:** ✅
**Blockers:** 0 | **Warnings:** 2 | **Suggestions:** 3 | **Notes:** 1

## Verdict
**APPROVE WITH SUGGESTIONS**

The implementation is a faithful and complete realisation of the detailed design. All contracts, LINQ query logic, console command, and test strategy scenarios are correctly implemented. Two warnings must be addressed before merge.

## Action Items
1. **R1** — Remove `using System.Security.Cryptography.X509Certificates;` from `CustomerService.cs` (L2)
2. **R2** — Remove `.copilot-cli/1-overdue-orders.coder.cli.md` from the repository; consider adding `.copilot-cli/` to `.gitignore`
3. **R3** *(recommended)* — Add test `GetCustomersWithOverdueOrders_HandlesEmptyNameFields_UsesCustomerId` for the `$"Customer {CustomerID}"` fallback path
4. **R4, R5** *(optional)* — Type `GetTarget` as `IRepository`; rename `FakeRepository` → `RepositoryStub`

---

**Report saved:** `docs/code-reviews/1-code-review_20260323-1034.md`

```
HANDOFF_START
issue-id: 1
review-dimensions: design-conformance, architecture, code-quality, test-quality, scope-hygiene
build-status: PASS
test-status: PASS
verdict: APPROVED_WITH_REMARKS
remarks-count: 6
review-report-path: docs/code-reviews/1-code-review_20260323-1034.md
failing-tests-count: 0
failing-tests-list: NONE
files-reviewed: Modules/Contracts/Sales/CustomerOverdueOrdersData.cs, Modules/Contracts/Sales/ICustomerService.cs, Modules/Sales/Sales.Services/CustomerService.cs, Modules/Sales/Sales.ConsoleCommands/ShowCustomersWithOverdueOrdersConsoleCommand.cs, Modules/Sales/Sales.Services.UnitTests/CustomerServiceTests.cs, Modules/Sales/Sales.Services.UnitTests/Sales.Services.UnitTests.csproj, Modules/Sales/Sales.Services.UnitTests/xunit.runner.json, Modules/Sales/Sales.Services/Sales.Services.csproj, AppInfraDemo.sln
deviations-from-design: Detailed design silently downgraded method signature from async (HLD) to synchronous; consistent with existing interface methods and IRepository's synchronous IQueryable<T> surface — acceptable design refinement.
next-steps: Coder should fix R1 (unused import) and R2 (remove committed session artifact). R3–R5 are optional improvements. After R1+R2 are addressed, branch is ready to merge.
HANDOFF_END
```


Total usage est:        1 Premium request
API time spent:         3m 52s
Total session time:     4m 8s
Total code changes:     +157 -0
Breakdown by AI model:
 claude-sonnet-4.6        397.8k in, 12.3k out, 345.4k cached (Est. 1 Premium request)