[CmdletBinding()]
param(
    [string] $Unity
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (!$Unity) {
    $versionFile = Join-Path $project 'ProjectSettings/ProjectVersion.txt'
    $version = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s+(\S+)$'
    if (!$version) { throw "Cannot read the editor version from '$versionFile'." }
    $Unity = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$($version.Matches[0].Groups[1].Value)\Editor\Unity.exe"
}
if (!(Test-Path -LiteralPath $Unity -PathType Leaf)) {
    throw "Unity Editor was not found at '$Unity'. Pass -Unity with the full path to Unity.exe."
}
$Unity = (Resolve-Path -LiteralPath $Unity).Path

$results = New-Item -ItemType Directory -Force -Path (Join-Path $project 'TestResults')

foreach ($mode in 'EditMode', 'PlayMode') {
    $report = Join-Path $results.FullName "$mode.xml"
    $log = Join-Path $results.FullName "$mode.log"
    if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
    $arguments = "-batchmode -runTests -testPlatform $mode -projectPath `"$project`" -testResults `"$report`" -logFile `"$log`""
    $run = Start-Process -FilePath $Unity -ArgumentList $arguments -WorkingDirectory $project -WindowStyle Hidden -PassThru
    # Wait only for this Editor, not for persistent helpers such as the licensing client.
    $run.WaitForExit()
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
