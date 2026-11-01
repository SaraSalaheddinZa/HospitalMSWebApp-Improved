param([ValidateSet('preview','seed','cleanup-preview','cleanup')][string]$Action='preview')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
# Do not infer consent from Web.config or turn on opt-in flags on the caller's behalf.
if($env:HOSPITALMS_ENVIRONMENT -ne 'Development' -or $env:HOSPITALMS_DEMO_OPT_IN -ne 'SYNTHETIC_DEVELOPMENT_ONLY') { throw 'Set the explicit Development and synthetic opt-in environment flags described in DEMO_SETUP.md.' }
[xml]$taskConfig=Get-Content -LiteralPath (Join-Path $taskRoot 'Web.config')
if(($taskConfig.configuration.appSettings.add | Where-Object key -eq 'EnvironmentName').value -ne 'Development') { throw 'The web application is not configured for Development.' }
if(-not $env:HOSPITALMS_CONNECTIONSTRING){throw 'Provide the local development database through HOSPITALMS_CONNECTIONSTRING. The tool also validates the exact development target.'}
Push-Location $taskRoot
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /out:bin\SyntheticDemo.exe /r:bin\HospitalMSWebApp.dll /r:System.Transactions.dll /r:System.Data.dll /r:System.Web.dll Tools\SyntheticDemo.cs
    if($LASTEXITCODE -ne 0){throw 'Build the application and fix demo tool compilation before continuing.'}
    & .\bin\SyntheticDemo.exe $Action
    if($LASTEXITCODE -ne 0){throw 'Synthetic command failed; no partial dataset was committed.'}
} finally { Pop-Location }
