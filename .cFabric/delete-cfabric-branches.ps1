# Deletes all local and remote branches starting with 'cFabric-'

$prefix = "cFabric-"

# Local branches
$localBranches = git branch --list "$prefix*" | ForEach-Object { $_.Trim() } | Where-Object { $_ -notlike "$prefix`demo*" }
foreach ($branch in $localBranches) {
    Write-Host "Deleting local branch: $branch"
    git branch -D $branch
}

# Remote branches (origin)
$remoteBranches = git --no-pager branch -r | Where-Object { $_ -match "origin/$prefix" -and $_ -notmatch "origin/${prefix}demo" } | ForEach-Object { $_.Trim() -replace "^origin/", "" }
foreach ($branch in $remoteBranches) {
    Write-Host "Deleting remote branch: $branch"
    git push origin --delete $branch
}

Write-Host "Done."
