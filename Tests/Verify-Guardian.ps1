param([switch]$CheckMigrationIdempotence,[switch]$WithoutPrivateCredentials,[switch]$AllowMissingTestCredentials)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
$priorPartial=[Environment]::GetEnvironmentVariable('HOSPITALMS_ALLOW_PARTIAL_CREDENTIAL_TESTS','Process')
$partialChecks=$WithoutPrivateCredentials.IsPresent
if($AllowMissingTestCredentials){[Environment]::SetEnvironmentVariable('HOSPITALMS_ALLOW_PARTIAL_CREDENTIAL_TESTS','1','Process')}
Push-Location $repoRoot
try {
    $buildTool='C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
    $compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $connection=New-Object System.Data.SqlClient.SqlConnection 'Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True'
    $connection.Open()
    function Read-PreservationSnapshot {
        $command=$connection.CreateCommand()
        # Fingerprints are regression checks, not a substitute for backups. Exclude only
        # authentication/access telemetry intentionally changed by HTTP verification.
        $command.CommandText="SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) AS TableName FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name IN('dbo','gh') AND t.name NOT IN('AuditEvents','LoginLogTbl','UserAccounts','sysdiagrams') ORDER BY s.name,t.name"
        $tables=New-Object System.Data.DataTable
        $tables.Load($command.ExecuteReader())
        $queries=@($tables.Rows | ForEach-Object { "SELECT '$($_.TableName)' AS Item,COUNT_BIG(*) AS Rows,CONVERT(bigint,CHECKSUM_AGG(BINARY_CHECKSUM(*))) AS Fingerprint FROM $($_.TableName)" })
        $queries += "SELECT 'User account identities and credentials',COUNT_BIG(*),CONVERT(bigint,CHECKSUM_AGG(BINARY_CHECKSUM(UserId,LegacyUserId,StaffMemberId,PatientId,DisplayName,Email,NormalizedEmail,PasswordHash,RoleName,IsActive,AuthVersion,FailedLoginCount,LockedUntil))) FROM gh.UserAccounts"
        $command.CommandText=$queries -join ' UNION ALL '
        $table=New-Object System.Data.DataTable
        $table.Load($command.ExecuteReader())
        return ($table.Rows | ForEach-Object { "$($_.Item):$($_.Rows):$($_.Fingerprint)" }) -join '|'
    }
    $before=Read-PreservationSnapshot
    if($CheckMigrationIdempotence) {
        foreach($migration in Get-ChildItem -LiteralPath 'Database\Migrations' -Filter '*.sql' | Sort-Object Name) {
            Write-Host "Checking migration $($migration.Name)"
            & sqlcmd -S '.\SQLEXP2017' -d 'HospitalMS-Sara7593-Wasan7639' -E -b -i $migration.FullName
            if($LASTEXITCODE-ne 0){throw "Migration failed: $($migration.Name)"}
        }
    }
    & $buildTool HospitalMSWebApp.sln /t:Rebuild /p:Configuration=Debug /v:minimal
    if($LASTEXITCODE-ne 0){throw 'Debug rebuild failed'}
    if(!(Test-Path -LiteralPath 'bin\roslyn\csc.exe') -or !(Test-Path -LiteralPath 'bin\roslyn\Microsoft.CodeAnalysis.CSharp.dll')) {
        throw 'ASP.NET runtime compiler is absent after the Debug rebuild'
    }
    & $compiler /nologo /out:bin\SyntheticDemo.exe /r:bin\HospitalMSWebApp.dll /r:System.Transactions.dll /r:System.Data.dll /r:System.Web.dll .\Tools\SyntheticDemo.cs
    if($LASTEXITCODE-ne 0){throw 'Synthetic demo tool compilation failed'}
    $suites=@('WorkflowIntegration','WorkflowGuards','AppointmentLifecycle','Reporting','SupplierReceipts','StockReconciliation','LaboratoryAmendments','ErrorHandling','StaffDirectories','StaffProfiles','AuthenticationGuards','PasswordRecovery','MedicalTeam','PatientExperience','StaffTaskExperience','SyntheticData','WorkspaceRender','HttpRegression')
    foreach($name in $suites) {
        & $compiler /nologo "/out:bin\$name.exe" /r:bin\HospitalMSWebApp.dll /r:bin\SyntheticDemo.exe /r:System.Transactions.dll /r:System.Data.dll /r:System.Web.dll ".\Tests\$name.cs"
        if($LASTEXITCODE-ne 0){throw "Test compilation failed: $name"}
    }
    # Compile everything before HTTP tests: changes to bin recycle ASP.NET sessions.
    # Fail fast on runtime/page compilation errors before long database fixture suites.
    foreach($page in @('/Views/Guest/Home.aspx','/Views/login.aspx','/Views/ForgotPassword.aspx')) {
        $request=[System.Net.HttpWebRequest]::Create('http://localhost:53677'+$page)
        $request.Timeout=30000
        $response=$null
        try {
            $response=$request.GetResponse()
            if([int]$response.StatusCode-ne 200){throw "Runtime preflight failed: $page"}
        } finally { if($response){$response.Dispose()} }
    }
    Write-Host 'Runtime preflight succeeded: homepage, sign-in and password recovery'
    Write-Host 'Running PublicTheme (isolated JavaScript tests)'
    & node .\Tests\PublicTheme.js
    if($LASTEXITCODE-ne 0){throw 'Test failed: PublicTheme'}
    & (Join-Path $PSScriptRoot 'HomepageRegression.ps1')
    & (Join-Path $PSScriptRoot 'PublicLogin.ps1')
    foreach($name in $suites) {
        if($WithoutPrivateCredentials -and $name -in @('WorkflowIntegration','HttpRegression')) {
            Write-Host "BLOCKED (not passed): $name requires private HOSPITALMS_TEST_<ROLE>_PASSWORD variables"
            continue
        }
        Write-Host "Running $name"
        # Capture native output directly. Windows PowerShell can turn redirected stderr
        # into a terminating ErrorRecord before the actual assertion reaches the log.
        $process=New-Object System.Diagnostics.Process
        try {
            $process.StartInfo.FileName=Join-Path $repoRoot "bin\$name.exe"
            $process.StartInfo.WorkingDirectory=$repoRoot
            $process.StartInfo.UseShellExecute=$false
            $process.StartInfo.CreateNoWindow=$true
            $process.StartInfo.RedirectStandardOutput=$true
            $process.StartInfo.RedirectStandardError=$true
            $null=$process.Start()
            # Drain both streams asynchronously to avoid a full-buffer deadlock.
            $standardOutput=$process.StandardOutput.ReadToEndAsync()
            $standardError=$process.StandardError.ReadToEndAsync()
            $process.WaitForExit()
            $output=$standardOutput.GetAwaiter().GetResult()
            $errorOutput=$standardError.GetAwaiter().GetResult()
            if($output){Write-Host $output.TrimEnd()}
            if($output -match 'BLOCKED:'){$partialChecks=$true}
            if($errorOutput){Write-Host $errorOutput.TrimEnd()}
            if($process.ExitCode-ne 0){throw "Test failed: $name (exit $($process.ExitCode))"}
        } finally { $process.Dispose() }
    }
    & sqlcmd -S '.\SQLEXP2017' -d 'HospitalMS-Sara7593-Wasan7639' -E -b -i Tests\DatabaseIntegrity.sql
    if($LASTEXITCODE-ne 0){throw 'Database integrity checks failed'}
    $after=Read-PreservationSnapshot
    if($before-ne $after){throw 'A preserved table or account credential snapshot changed; inspect test residue or concurrent application activity'}
    Write-Host 'PASS: clinical, administrative, historical and account credential snapshots preserved (access/login telemetry excluded)'
    if($partialChecks){Write-Host 'PARTIAL AGGREGATE: executed checks passed, but private credential-dependent checks remain BLOCKED; this is not a full regression pass.'}
    else{Write-Host 'FULL AGGREGATE PASS: all registered suites and preservation checks completed.'}
} finally {
    [Environment]::SetEnvironmentVariable('HOSPITALMS_ALLOW_PARTIAL_CREDENTIAL_TESTS',$priorPartial,'Process')
    if($connection){$connection.Dispose()}
    Pop-Location
}
