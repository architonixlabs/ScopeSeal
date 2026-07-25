#requires -Version 5.1
<#
  deploy.ps1 — deploy ScopeSeal to the remote Ubuntu host over an SSH Docker context.

  Usage:
    .\deploy.ps1 [dev|prod] [up|down|logs|ps|restart|build|config]
  Defaults: dev up

  dev  -> compose project scopeseal-dev,  env .env.dev,  nginx 8110/8112/8113, api 8111
  prod -> compose project scopeseal-prod, env .env.prod, nginx 9110/9112/9113, api 9111
#>
[CmdletBinding()]
param(
    [ValidateSet('dev','prod')]              [string]$Env = 'dev',
    [ValidateSet('up','down','logs','ps','restart','build','config')] [string]$Action = 'up'
)

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$confPath = Join-Path $root 'deploy\remote.conf'
if (-not (Test-Path $confPath)) {
    Write-Host "[ERROR] deploy/remote.conf not found. Copy deploy/remote.conf.example to deploy/remote.conf." -ForegroundColor Red
    exit 1
}
$conf = @{}
Get-Content $confPath | ForEach-Object {
    $line = $_.Trim()
    if ($line -and -not $line.StartsWith('#') -and $line.Contains('=')) {
        $k,$v = $line.Split('=',2); $conf[$k.Trim()] = $v.Trim()
    }
}
foreach ($k in 'REMOTE_HOST','REMOTE_USER','DOCKER_CONTEXT','PROJECT_SLUG') {
    if (-not $conf[$k]) { Write-Host "[ERROR] $k missing in deploy/remote.conf" -ForegroundColor Red; exit 1 }
}
$DOCKER_CONTEXT = $conf['DOCKER_CONTEXT']
$SLUG = $conf['PROJECT_SLUG']

$project  = "$SLUG-$Env"
$envFile  = ".env.$Env"
if (-not (Test-Path (Join-Path $root $envFile))) {
    Write-Host "[ERROR] $envFile not found. Copy $envFile.example to $envFile and fill it in." -ForegroundColor Red
    exit 1
}

$ctxExists = (& docker context ls --format '{{.Name}}') -contains $DOCKER_CONTEXT
if (-not $ctxExists) {
    Write-Host "[setup] Creating Docker context '$DOCKER_CONTEXT' -> ssh://$($conf['REMOTE_USER'])@$($conf['REMOTE_HOST'])" -ForegroundColor Cyan
    & docker context create $DOCKER_CONTEXT --docker "host=ssh://$($conf['REMOTE_USER'])@$($conf['REMOTE_HOST'])" | Out-Null
}

$files = @('-f','docker-compose.yml')
if ($Env -eq 'prod') { $files += @('-f','docker-compose.prod.yml') }

if ($Env -eq 'prod' -and $Action -in @('up','restart','build')) {
    Write-Host "About to run '$Action' on PROD ($project) on $($conf['REMOTE_HOST'])." -ForegroundColor Yellow
    $typed = Read-Host "Type the project slug '$SLUG' to confirm"
    if ($typed -ne $SLUG) { Write-Host "Aborted." -ForegroundColor Red; exit 1 }
}

$env:ENV_FILE = $envFile
$env:DOCKER_CONTEXT = $DOCKER_CONTEXT

$base = @('compose','--env-file',$envFile) + $files + @('-p',$project)

Write-Host "[$Env] docker --context $DOCKER_CONTEXT compose -p $project  ($Action)" -ForegroundColor Cyan

switch ($Action) {
    'up'      { & docker --context $DOCKER_CONTEXT @base up -d --build; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; & docker --context $DOCKER_CONTEXT @base ps }
    'build'   { & docker --context $DOCKER_CONTEXT @base build }
    'down'    { & docker --context $DOCKER_CONTEXT @base down }
    'restart' { & docker --context $DOCKER_CONTEXT @base restart }
    'logs'    { & docker --context $DOCKER_CONTEXT @base logs -f --tail 100 }
    'ps'      { & docker --context $DOCKER_CONTEXT @base ps }
    'config'  { & docker --context $DOCKER_CONTEXT @base config }
}
exit $LASTEXITCODE
