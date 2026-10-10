$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$command = $payload.tool_input.command
if (-not $command) { exit 0 }

$globalOptionsWithArgument = '-C', '-c', '--git-dir', '--work-tree', '--namespace'
$reason = $null

foreach ($segment in ($command -split '[;&|\r\n]+')) {
    $tokens = @([regex]::Matches($segment, '"[^"]*"|''[^'']*''|\S+') | ForEach-Object { $_.Value.Trim('"', "'") })
    $gitIndex = [array]::FindIndex($tokens, [Predicate[string]] { param($t) $t -match '(^|[\\/])git(\.exe)?$' })
    if ($gitIndex -lt 0) { continue }

    $i = $gitIndex + 1
    while ($i -lt $tokens.Count -and $tokens[$i].StartsWith('-')) {
        $i += if ($globalOptionsWithArgument -contains $tokens[$i]) { 2 } else { 1 }
    }
    if ($i -ge $tokens.Count) { continue }

    $subcommand = $tokens[$i]
    $rest = @($tokens | Select-Object -Skip ($i + 1))

    switch ($subcommand) {
        'push' { $reason = 'git push is not allowed' }
        'reset' { if ($rest -contains '--hard') { $reason = 'git reset --hard is not allowed' } }
        'branch' {
            $deletes = $rest | Where-Object { $_ -eq '--delete' -or $_ -cmatch '^-[A-Za-z]*[dD]' }
            if ($deletes) { $reason = 'deleting branches is not allowed' }
        }
    }
    if ($reason) { break }
}

if ($reason) {
    @{
        hookSpecificOutput = @{
            hookEventName            = 'PreToolUse'
            permissionDecision       = 'deny'
            permissionDecisionReason = "Blocked by orchestrator policy: $reason. Ask the user to run it manually."
        }
    } | ConvertTo-Json -Depth 4 -Compress
}
exit 0
