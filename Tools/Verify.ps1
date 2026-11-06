[CmdletBinding()]
param(
    [string] $Unity = "$env:ProgramFiles\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe"
)

$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Unity -PathType Leaf)) {
    throw "Unity Editor was not found at '$Unity'. Pass -Unity with the full path to Unity.exe."
}

$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$results = New-Item -ItemType Directory -Force -Path (Join-Path $project 'TestResults')

foreach ($mode in 'EditMode', 'PlayMode') {
    $report = Join-Path $results.FullName "$mode.xml"
    $log = Join-Path $results.FullName "$mode.log"
    if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
    $arguments = "-batchmode -runTests -testPlatform $mode -projectPath `"$project`" -testResults `"$report`" -logFile `"$log`""
    $run = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -Wait
    if ($run.ExitCode -ne 0 -or !(Test-Path -LiteralPath $report)) {
        throw "$mode did not finish successfully (exit $($run.ExitCode)). See $log"
    }
    [xml] $xml = Get-Content -Raw -LiteralPath $report
    $suite = $xml.'test-run'
    if (!$suite -or [int]$suite.passed -lt 1 -or $suite.result -ne 'Passed' -or [int]$suite.passed -ne [int]$suite.total) {
        throw "$mode reported missing, failed or skipped tests. See $report and $log"
    }
    Write-Output "$mode passed $($suite.passed) tests."
}
