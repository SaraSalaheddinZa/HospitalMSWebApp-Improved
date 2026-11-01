# Run locally in your own PowerShell console. No credentials are written to disk.
param([switch]$CheckMigrationIdempotence)
$ErrorActionPreference='Stop'
$roles=@('Admin','Doctor','Receptionist','Laboratorian','Nurse','Pharmacist','Patient')
$prior=@{}
$log=Join-Path $env:TEMP ('guardian-approved-integration-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
try {
    foreach($role in $roles) {
        $key='HOSPITALMS_TEST_'+$role.ToUpperInvariant()+'_PASSWORD'
        $prior[$key]=[Environment]::GetEnvironmentVariable($key,'Process')
        if($prior[$key]) { continue }
        $secret=Read-Host "Current development $role password (private; Enter if unknown)" -AsSecureString
        if($secret.Length -eq 0){$secret.Dispose();Write-Host "BLOCKED: $role credential not provided; no guess/reset/unlock will occur.";continue}
        $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
        try { [Environment]::SetEnvironmentVariable($key,[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer),'Process') }
        finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer);$secret.Dispose() }
    }
    # The first credential-dependent suite verifies hashes before any HTTP sign-in.
    & (Join-Path $PSScriptRoot '..\Tests\Verify-Guardian.ps1') -CheckMigrationIdempotence:$CheckMigrationIdempotence -AllowMissingTestCredentials *> $log
    Get-Content -LiteralPath $log | Where-Object {$_ -match 'BLOCKED:|PARTIAL AGGREGATE:|FULL AGGREGATE PASS:'} | Write-Host
    Write-Host "Verification finished (see status above; blocked does not mean passed). Non-secret log: $log"
} catch {
    Write-Host "Verification did not complete. Non-secret log: $log"
    throw
} finally {
    foreach($key in $prior.Keys) { [Environment]::SetEnvironmentVariable($key,$prior[$key],'Process') }
}
