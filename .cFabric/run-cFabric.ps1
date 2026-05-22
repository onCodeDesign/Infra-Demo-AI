$exePath = "c:\D\Projects\DevAgenticAI\CodeFrabric\src\CodeFabric\bin\Debug\net10.0\CodeFabric.exe"

$scriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Path

Push-Location $scriptFolder
try {
    & $exePath @args
}
finally {
    Pop-Location
}