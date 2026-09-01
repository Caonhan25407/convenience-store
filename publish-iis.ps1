param(
    [string]$SiteUrl = 'http://127.0.0.1:8081'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectDirectory = [System.IO.Path]::GetFullPath($PSScriptRoot)
$frontendDirectory = Join-Path $projectDirectory 'frontend'
$frontendDist = Join-Path $frontendDirectory 'dist'
$backendProject = Join-Path $projectDirectory 'backend\StoreWeb.Api.csproj'
$publishDirectory = Join-Path $projectDirectory 'publish'
$backupRoot = Join-Path $projectDirectory 'publish-backups'
$backupDirectory = Join-Path $backupRoot (Get-Date -Format 'yyyyMMdd-HHmmss')
$temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$stagingDirectory = Join-Path $temporaryRoot ('storeweb-iis-' + [System.Guid]::NewGuid().ToString('N'))
$appOfflinePath = Join-Path $publishDirectory 'app_offline.htm'
$envFile = Join-Path $projectDirectory '.env'
$siteBaseUrl = $SiteUrl.TrimEnd('/')

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Parent,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path).TrimEnd([char[]]@('\', '/'))
    $resolvedParent = [System.IO.Path]::GetFullPath($Parent).TrimEnd([char[]]@('\', '/'))
    $parentPrefix = $resolvedParent + [System.IO.Path]::DirectorySeparatorChar

    if (-not $resolvedPath.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to use $Label outside its expected parent: $resolvedPath"
    }

    return $resolvedPath
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$FailureMessage
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage (exit code $LASTEXITCODE)"
    }
}

function Read-DotEnv {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Environment file was not found: $Path"
    }

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) {
            continue
        }

        $separatorIndex = $line.IndexOf('=')
        if ($separatorIndex -lt 1) {
            continue
        }

        $name = $line.Substring(0, $separatorIndex).Trim()
        $value = $line.Substring($separatorIndex + 1).Trim()
        if ($value.Length -ge 2) {
            $firstCharacter = $value.Substring(0, 1)
            $lastCharacter = $value.Substring($value.Length - 1, 1)
            if (($firstCharacter -eq '"' -and $lastCharacter -eq '"') -or
                ($firstCharacter -eq "'" -and $lastCharacter -eq "'")) {
                $value = $value.Substring(1, $value.Length - 2)
            }
        }

        $values[$name] = $value
    }

    return $values
}

function Quote-ConnectionValue {
    param([Parameter(Mandatory = $true)][string]$Value)

    return '"' + $Value.Replace('"', '""') + '"'
}

function Set-WebConfigEnvironment {
    param(
        [Parameter(Mandatory = $true)][string]$WebConfigPath,
        [Parameter(Mandatory = $true)][string]$ConnectionString
    )

    [xml]$webConfig = Get-Content -LiteralPath $WebConfigPath -Raw
    $aspNetCoreNodes = $webConfig.SelectNodes('/configuration/location/system.webServer/aspNetCore | /configuration/system.webServer/aspNetCore')
    if ($null -eq $aspNetCoreNodes -or $aspNetCoreNodes.Count -ne 1) {
        throw 'The generated web.config must contain exactly one aspNetCore element.'
    }
    $aspNetCore = $aspNetCoreNodes.Item(0)

    $environmentVariables = $aspNetCore.SelectSingleNode('environmentVariables')
    if ($null -eq $environmentVariables) {
        $environmentVariables = $webConfig.CreateElement('environmentVariables')
        [void]$aspNetCore.AppendChild($environmentVariables)
    }

    $settings = [ordered]@{
        'ASPNETCORE_ENVIRONMENT' = 'Development'
        'Auth__CookieName' = 'StoreWeb.Iis.Auth'
        'ConnectionStrings__DefaultConnection' = $ConnectionString
    }

    foreach ($setting in $settings.GetEnumerator()) {
        $existing = $environmentVariables.SelectSingleNode("environmentVariable[@name='$($setting.Key)']")
        if ($null -eq $existing) {
            $existing = $webConfig.CreateElement('environmentVariable')
            [void]$environmentVariables.AppendChild($existing)
        }

        $existing.SetAttribute('name', $setting.Key)
        $existing.SetAttribute('value', $setting.Value)
    }

    $webConfig.Save($WebConfigPath)
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        throw "Source directory was not found: $Source"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | Copy-Item -Destination $Destination -Recurse -Force
}

function Clear-DirectoryContentsExcept {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][string]$PreservePath
    )

    $resolvedDirectory = [System.IO.Path]::GetFullPath($Directory)
    $resolvedPreservePath = Assert-ChildPath -Path $PreservePath -Parent $resolvedDirectory -Label 'preserved deployment file'

    foreach ($item in Get-ChildItem -LiteralPath $resolvedDirectory -Force) {
        $resolvedItemPath = Assert-ChildPath -Path $item.FullName -Parent $resolvedDirectory -Label 'deployment item'
        if ($resolvedItemPath.Equals($resolvedPreservePath, [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to remove a reparse point from the IIS package: $resolvedItemPath"
        }

        Remove-Item -LiteralPath $resolvedItemPath -Recurse -Force
    }
}

function Get-HttpStatus {
    param([Parameter(Mandatory = $true)][string]$Uri)

    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -Method Get -TimeoutSec 5
        return [int]$response.StatusCode
    }
    catch {
        if ($null -ne $_.Exception.Response -and $null -ne $_.Exception.Response.StatusCode) {
            return [int]$_.Exception.Response.StatusCode
        }

        return $null
    }
}

function Wait-ForHealthySite {
    param([Parameter(Mandatory = $true)][string]$BaseUrl)

    $healthUrl = "$BaseUrl/api/health"
    for ($attempt = 1; $attempt -le 20; $attempt++) {
        if ((Get-HttpStatus -Uri $healthUrl) -eq 200) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "IIS did not become healthy at $healthUrl within 20 seconds."
}

function Wait-ForApplicationFilesUnlocked {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [int]$TimeoutSeconds = 45
    )

    $candidateFiles = Get-ChildItem -LiteralPath $Directory -File -Force |
        Where-Object { $_.Extension -in @('.dll', '.exe') }

    for ($attempt = 1; $attempt -le $TimeoutSeconds; $attempt++) {
        $lockedFile = $null

        foreach ($candidateFile in $candidateFiles) {
            $stream = $null
            try {
                $stream = [System.IO.File]::Open(
                    $candidateFile.FullName,
                    [System.IO.FileMode]::Open,
                    [System.IO.FileAccess]::ReadWrite,
                    [System.IO.FileShare]::None
                )
            }
            catch [System.IO.IOException] {
                $lockedFile = $candidateFile.FullName
                break
            }
            finally {
                if ($null -ne $stream) {
                    $stream.Dispose()
                }
            }
        }

        if ($null -eq $lockedFile) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "IIS did not release its application files within $TimeoutSeconds seconds. Last locked file: $lockedFile"
}

$publishDirectory = Assert-ChildPath -Path $publishDirectory -Parent $projectDirectory -Label 'publish directory'
$backupRoot = Assert-ChildPath -Path $backupRoot -Parent $projectDirectory -Label 'backup root'
$backupDirectory = Assert-ChildPath -Path $backupDirectory -Parent $backupRoot -Label 'backup directory'
$stagingDirectory = Assert-ChildPath -Path $stagingDirectory -Parent $temporaryRoot -Label 'staging directory'
$appOfflinePath = Assert-ChildPath -Path $appOfflinePath -Parent $publishDirectory -Label 'app_offline file'

if (-not (Test-Path -LiteralPath $publishDirectory -PathType Container)) {
    throw "The existing IIS package was not found: $publishDirectory"
}

if (Test-Path -LiteralPath $appOfflinePath) {
    throw "The IIS package is already offline. Resolve the existing marker before deploying: $appOfflinePath"
}

$environment = Read-DotEnv -Path $envFile
$requiredVariables = @('POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD', 'POSTGRES_PORT')
foreach ($variableName in $requiredVariables) {
    if (-not $environment.ContainsKey($variableName) -or [string]::IsNullOrWhiteSpace($environment[$variableName])) {
        throw "Required value $variableName is missing from .env."
    }
}

$connectionString = 'Host=127.0.0.1;Port={0};Database={1};Username={2};Password={3}' -f @(
    $environment['POSTGRES_PORT'],
    (Quote-ConnectionValue -Value $environment['POSTGRES_DB']),
    (Quote-ConnectionValue -Value $environment['POSTGRES_USER']),
    (Quote-ConnectionValue -Value $environment['POSTGRES_PASSWORD'])
)

$offlineCreatedByThisRun = $false
$liveMutationStarted = $false
$backupCreated = $false
$leaveOffline = $false

try {
    Write-Host 'Building the Vue frontend...'
    Push-Location $frontendDirectory
    try {
        Invoke-NativeCommand -FilePath 'npm' -Arguments @('run', 'build') -FailureMessage 'Frontend build failed'
    }
    finally {
        Pop-Location
    }

    Write-Host 'Publishing ASP.NET Core to a staging directory...'
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
    Invoke-NativeCommand -FilePath 'dotnet' -Arguments @(
        'publish', $backendProject, '-c', 'Release', '-o', $stagingDirectory
    ) -FailureMessage 'Backend publish failed'

    Write-Host 'Adding the frontend build to the staged IIS package...'
    Copy-DirectoryContents -Source $frontendDist -Destination (Join-Path $stagingDirectory 'wwwroot')
    Set-WebConfigEnvironment -WebConfigPath (Join-Path $stagingDirectory 'web.config') -ConnectionString $connectionString

    $requiredArtifacts = @(
        (Join-Path $stagingDirectory 'StoreWeb.Api.dll'),
        (Join-Path $stagingDirectory 'web.config'),
        (Join-Path $stagingDirectory 'wwwroot\index.html'),
        (Join-Path $stagingDirectory 'wwwroot\assets')
    )
    foreach ($artifact in $requiredArtifacts) {
        if (-not (Test-Path -LiteralPath $artifact)) {
            throw "The staged IIS package is incomplete: $artifact"
        }
    }

    if (Test-Path -LiteralPath $appOfflinePath) {
        throw "The IIS package became offline before deployment started: $appOfflinePath"
    }

    Write-Host "Backing up the current IIS package to: $backupDirectory"
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    Copy-DirectoryContents -Source $publishDirectory -Destination $backupDirectory
    $backupCreated = $true

    [System.IO.File]::WriteAllText(
        $appOfflinePath,
        '<!doctype html><html><body><h1>Dang cap nhat he thong...</h1></body></html>'
    )
    $offlineCreatedByThisRun = $true
    [void](Get-HttpStatus -Uri $siteBaseUrl)
    Wait-ForApplicationFilesUnlocked -Directory $publishDirectory

    Write-Host 'Deploying the verified package to the IIS site...'
    $liveMutationStarted = $true
    Clear-DirectoryContentsExcept -Directory $publishDirectory -PreservePath $appOfflinePath
    Copy-DirectoryContents -Source $stagingDirectory -Destination $publishDirectory
    Remove-Item -LiteralPath $appOfflinePath -Force

    Write-Host 'Waiting for IIS to start the updated application...'
    Wait-ForHealthySite -BaseUrl $siteBaseUrl

    foreach ($route in @('/', '/login', '/register', '/store')) {
        $status = Get-HttpStatus -Uri ($siteBaseUrl + $route)
        if ($status -ne 200) {
            throw "IIS route $route returned HTTP $status instead of 200."
        }
    }

    $unauthenticatedStatus = Get-HttpStatus -Uri "$siteBaseUrl/api/auth/me"
    if ($unauthenticatedStatus -ne 401) {
        throw "The current authentication API was not detected (HTTP $unauthenticatedStatus)."
    }

    Write-Host "IIS deployment completed successfully: $siteBaseUrl"
    if ($backupCreated) {
        Write-Host "Rollback backup: $backupDirectory"
    }
}
catch {
    $originalError = $_
    Write-Warning "IIS deployment failed: $($originalError.Exception.Message)"

    if ($liveMutationStarted -and $backupCreated) {
        try {
            Write-Warning 'Restoring the previous IIS package...'
            if (-not (Test-Path -LiteralPath $appOfflinePath -PathType Leaf)) {
                [System.IO.File]::WriteAllText(
                    $appOfflinePath,
                    '<!doctype html><html><body><h1>Dang khoi phuc he thong...</h1></body></html>'
                )
            }
            [void](Get-HttpStatus -Uri $siteBaseUrl)
            Wait-ForApplicationFilesUnlocked -Directory $publishDirectory
            Clear-DirectoryContentsExcept -Directory $publishDirectory -PreservePath $appOfflinePath
            Copy-DirectoryContents -Source $backupDirectory -Destination $publishDirectory
            Remove-Item -LiteralPath $appOfflinePath -Force
            Wait-ForHealthySite -BaseUrl $siteBaseUrl
            Write-Warning 'The previous IIS package was restored successfully.'
        }
        catch {
            $leaveOffline = $true
            Write-Warning "Automatic rollback also failed: $($_.Exception.Message)"
            Write-Warning "The IIS application was left offline at: $appOfflinePath"
        }
    }
    elseif ($offlineCreatedByThisRun) {
        Write-Warning 'No live IIS files were changed; the existing package will be restarted.'
    }

    throw $originalError
}
finally {
    if ($offlineCreatedByThisRun -and -not $leaveOffline -and
        (Test-Path -LiteralPath $appOfflinePath -PathType Leaf)) {
        Remove-Item -LiteralPath $appOfflinePath -Force
    }

    if (Test-Path -LiteralPath $stagingDirectory -PathType Container) {
        Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
    }
}
