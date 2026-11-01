param([string]$BaseUrl = 'http://localhost:53677')
$ErrorActionPreference = 'Stop'

# Read-only public-page checks. No account seeding, migrations or fixture data.
function Assert-Home([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    Write-Host "PASS: $Message"
}
function Get-Page([string]$Url) {
    $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 40
    Assert-Home ($response.StatusCode -eq 200) "HTTP 200: $Url"
    return $response.Content
}

$repoPath = Split-Path $PSScriptRoot -Parent
$config = [xml](Get-Content -LiteralPath (Join-Path $repoPath 'Web.config') -Raw)
$connectionString = [string]($config.configuration.connectionStrings.add | Where-Object { $_.name -eq 'HospitalDB' }).connectionString
$environmentKey = [string]($config.configuration.appSettings.add | Where-Object { $_.key -eq 'HospitalDbEnvironmentVariable' }).value
if ($environmentKey -and [Environment]::GetEnvironmentVariable($environmentKey)) {
    $connectionString = [Environment]::GetEnvironmentVariable($environmentKey)
}
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
try {
    $connection.Open()
    function Read-HomeSnapshot {
        $command = $connection.CreateCommand()
        $command.CommandText = "SELECT COUNT_BIG(*) AS Rows, CHECKSUM_AGG(BINARY_CHECKSUM(patientId,MedicalRecordNumber,patientName)) AS Fingerprint FROM dbo.patientTbl UNION ALL SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(UserId,Email,PasswordHash,RoleName,IsActive)) FROM gh.UserAccounts UNION ALL SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM gh.Departments"
        $table = New-Object System.Data.DataTable
        $table.Load($command.ExecuteReader())
        return ($table.Rows | ForEach-Object { "$($_.Rows):$($_.Fingerprint)" }) -join '|'
    }
    $before = Read-HomeSnapshot
    $homeUrl = "$($BaseUrl.TrimEnd('/'))/Views/Guest/Home.aspx"
    $html = Get-Page $homeUrl
    Assert-Home ($html -match 'Care, connected\.' -and $html -match 'Human at heart\.') 'Approved Living Thread identity renders'
    Assert-Home ($html -match 'connected hospital experience') 'Public page identifies the hospital'
    Assert-Home ($html -notmatch 'Trusted by 500|statPatients|statDoctors|statAppointments|counterPatients|testimonials|50\+ Doctors') 'No legacy fabricated metrics or testimonials'
    Assert-Home ([regex]::Matches($html, '<h1\b').Count -eq 1) 'One primary page heading'
    Assert-Home ($html -match 'href="../login.aspx\?role=Patient"' -and $html -match 'href="../login.aspx\?access=staff"') 'Patient and staff access preserved'
    Assert-Home ($html -notmatch 'Portal.aspx|dev\..+@guardianhealth.test|PasswordHash') 'No legacy portal links or internal credentials exposed'
    Assert-Home ($html -match 'settings are illustrative' -and $html -match 'not diagnostic information') 'Artwork is not represented as diagnostic information or actual hospital staff'

    $ids = @([regex]::Matches($html, '\bid="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
    Assert-Home (@($ids | Group-Object | Where-Object Count -gt 1).Count -eq 0) 'No duplicate HTML IDs'
    foreach ($anchor in [regex]::Matches($html, 'href="#([^"]+)"')) {
        Assert-Home ($ids -contains $anchor.Groups[1].Value) "Anchor target exists: $($anchor.Groups[1].Value)"
    }
    $departmentCommand = $connection.CreateCommand()
    $departmentCommand.CommandText = "SELECT DepartmentName FROM gh.Departments d WHERE IsActive=1 AND NOT EXISTS(SELECT 1 FROM gh.SyntheticDemoRecords s WHERE s.TableName='[gh].[Departments]' AND s.RecordId=d.DepartmentId) ORDER BY CASE DepartmentName WHEN 'General Medicine' THEN 0 WHEN 'Laboratory' THEN 1 WHEN 'Pharmacy' THEN 2 WHEN 'Emergency Department' THEN 3 ELSE 4 END,DepartmentName"
    $departments = New-Object System.Data.DataTable
    $departments.Load($departmentCommand.ExecuteReader())
    $renderedNames = @([regex]::Matches($html, "class='public-department-name'>(.*?)</span>") | ForEach-Object { [System.Net.WebUtility]::HtmlDecode($_.Groups[1].Value) })
    $databaseNames = @($departments.Rows | ForEach-Object { [string]$_.DepartmentName })
    Assert-Home (($renderedNames -join '|') -ceq ($databaseNames -join '|')) 'Department directory matches active non-synthetic database records exactly'
    if ($databaseNames.Count -eq 0) { Assert-Home ($html -match 'public-empty') 'Empty department directory has an explanatory state' }

    $links = @([regex]::Matches($html, '<a\b[^>]*href="([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Where-Object { -not $_.StartsWith('#') } | Sort-Object -Unique)
    foreach ($link in $links) {
        $url = New-Object System.Uri ([uri]$homeUrl), ([System.Net.WebUtility]::HtmlDecode($link))
        Assert-Home ($url.Authority -eq ([uri]$BaseUrl).Authority) "Navigation remains local: $link"
        $page = Get-Page $url.AbsoluteUri
        Assert-Home ($page -notmatch 'Server Error in') "Navigation target renders: $link"
        if ($link -eq '../login.aspx?role=Patient') { Assert-Home ($page -match 'selected="selected" value="Patient"') 'Patient CTA preselects the Patient role' }
        if ($link -eq '../login.aspx?access=staff') { Assert-Home ($page -match 'value="Doctor"' -and $page -match 'value="Admin"') 'Staff CTA retains staff role choices' }
    }
    foreach ($image in [regex]::Matches($html, '<img\b[^>]*>')) {
        Assert-Home ($image.Value -match 'alt="[^"]*"' -and $image.Value -match 'width="\d+"' -and $image.Value -match 'height="\d+"') 'Images declare alternative text (empty for decorative anatomy) and intrinsic dimensions'
    }
    foreach ($asset in [regex]::Matches($html, '(?:src|href)="(../../(?:Content|Scripts|Assets)/[^"]+)"')) {
        $assetUrl = New-Object System.Uri ([uri]$homeUrl), $asset.Groups[1].Value
        $null = Get-Page $assetUrl.AbsoluteUri
    }
    Assert-Home ((Get-Page "$($BaseUrl.TrimEnd('/'))/Default.aspx") -match 'heart-artistic-hybrid.png') 'Default route still opens the public landing page'
    $css = Get-Content -LiteralPath (Join-Path $repoPath 'Assets/Public/LivingThread/universe.css') -Raw
    $script = Get-Content -LiteralPath (Join-Path $repoPath 'Assets/Public/LivingThread/study.js') -Raw
    Assert-Home ($css -match 'prefers-reduced-motion' -and $script -match 'reduced.matches') 'CSS and interactive motion respect reduced-motion preferences'
    Assert-Home ($script -match 'root.dataset.static' -and $html -match 'data-motion-toggle') 'Continuous decorative motion has a user pause control'
    Assert-Home ($html -match 'heart-artistic-hybrid.png' -and $script -match 'heart-motion.js') 'Approved artistic heart and regional motion module are present'
    Assert-Home ((Read-HomeSnapshot) -ceq $before) 'Patient records, account credentials and departments preserved'
    Write-Host 'PASS: all homepage regression checks completed; no database writes performed'
} finally {
    if ($connection) { $connection.Dispose() }
}
