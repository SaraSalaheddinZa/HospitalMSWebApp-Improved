# Anonymous integration checks only; no real credentials, accounts or recovery requests.
$ErrorActionPreference='Stop'
$base='http://localhost:53677'
$script:checks=0
function Assert-Public([bool]$ok,[string]$message){if(!$ok){throw $message};$script:checks++;Write-Host "PASS: $message"}
function Fields([string]$html){
    $values=@{}
    foreach($input in [regex]::Matches($html,'<input[^>]+>')){
        if($input.Value -notmatch 'type="hidden"'){continue}
        $name=[regex]::Match($input.Value,'name="([^"]+)"').Groups[1].Value
        $value=[regex]::Match($input.Value,'value="([^"]*)"').Groups[1].Value
        if($name){$values[$name]=[Net.WebUtility]::HtmlDecode($value)}
    }
    return $values
}
foreach($audience in @('role=Patient','access=staff')){
    $session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $url="$base/Views/login.aspx?$audience"
    $html=(Invoke-WebRequest $url -WebSession $session -UseBasicParsing).Content
    Assert-Public ([regex]::Matches($html,'<form\b').Count -eq 1) "$audience has exactly one real server form"
    Assert-Public ($html -match '__EVENTVALIDATION' -and $html -match '__VIEWSTATE') "$audience retains protected Web Forms submission fields"
    Assert-Public ($html -notmatch 'preview only|Visual prototype|<canvas|heart-motion|form-action') "$audience has no prototype submit blocker or heart renderer"
    foreach($role in @('Admin','Doctor','Receptionist','Laboratorian','Nurse','Pharmacist','Patient')){
        Assert-Public ($html.Contains('value="'+$role+'"')) "$audience retains canonical $role option"
    }
    $values=Fields $html;$values['loginBtn']='Sign In';$values['emailTb']='';$values['passwordTb']='';$values['roleCb']=if($audience -eq 'role=Patient'){'Patient'}else{'Doctor'}
    $result=(Invoke-WebRequest $url -Method Post -Body $values -WebSession $session -UseBasicParsing).Content
    Assert-Public ($result -match 'Enter your email address and password\.') "$audience rejects an empty submission server-side without JavaScript"
    Assert-Public ($result -match 'class="feedback error"' -and $result -match 'tabindex="-1"') "$audience exposes focusable server error feedback"
    Assert-Public ($result.Contains('selected="selected" value="'+$values['roleCb']+'"')) "$audience preserves selected account role after validation"
    $values=Fields $result;$values['loginBtn']='Sign In';$values['emailTb']='not-an-email';$values['passwordTb']='PublicValidationOnly';$values['roleCb']='Doctor'
    $result=(Invoke-WebRequest $url -Method Post -Body $values -WebSession $session -UseBasicParsing).Content
    Assert-Public ($result -match 'Enter a valid email address\.') "$audience rejects malformed email server-side"
    Assert-Public ($result -notmatch 'PublicValidationOnly') "$audience never echoes a submitted password"
    $values=Fields $result;$values['loginBtn']='Sign In';$values['emailTb']='public-validation@example.invalid';$values['passwordTb']='PublicValidationOnly';$values['roleCb']=''
    $result=(Invoke-WebRequest $url -Method Post -Body $values -WebSession $session -UseBasicParsing).Content
    Assert-Public ($result -match 'Select your role\.') "$audience rejects missing role before authentication"
}
foreach($file in @('sculpture-architecture-light.webp','sculpture-architecture-dark.webp','sculpture-silk-sheer-light.webp','sculpture-silk-sheer-dark.webp')){
    $response=Invoke-WebRequest "$base/Assets/Public/LivingThread/$file" -UseBasicParsing
    Assert-Public ($response.StatusCode -eq 200 -and $response.Headers['Content-Type'] -match 'image/webp') "Approved image serves correctly: $file"
}
foreach($path in @('ForgotPassword.aspx','ResetPassword.aspx','ContactAdmin.aspx','HelpCenter.aspx')){
    $response=Invoke-WebRequest "$base/Views/$path" -UseBasicParsing
    Assert-Public ($response.StatusCode -eq 200 -and $response.Content -notmatch 'Server Error in') "Existing recovery/help route remains available: $path"
}
Write-Host "PublicLogin: $script:checks assertions passed. No account authentication or data mutation attempted."
