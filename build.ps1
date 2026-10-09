param(
    [string]$GameDir,
    [string]$GodotExe,
    [string]$ArkBaseDll,
    [switch]$NoInstall,
    [switch]$Online
)

$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$project = Get-ChildItem -LiteralPath $projectDir -Filter '*.csproj' |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $project) { throw 'No project file found.' }
$name = [System.IO.Path]::GetFileNameWithoutExtension($project)
$buildDir = Join-Path $projectDir '.godot\build'
$logDir = Join-Path $buildDir 'logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

function Invoke-BuildStep {
    param([string]$Stage, [string]$Executable, [string[]]$Arguments)
    $log = Join-Path $logDir "$Stage.log"
    Write-Host "[$name][$Stage] Running..."
    # Keep native stderr in the stage log; success is determined by exit code.
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & $Executable @Arguments *> $log
        $code = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    if ($code -ne 0) {
        Get-Content -LiteralPath $log -Tail 40 | ForEach-Object { Write-Host $_ }
        throw "[$name][$Stage] FAILED (exit $code). Full log: $log"
    }
    Write-Host "[$name][$Stage] OK"
}

$environmentNames = @(
    'NUGET_PACKAGES', 'DOTNET_CLI_HOME', 'APPDATA',
    'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_ADD_GLOBAL_TOOLS_TO_PATH',
    'DOTNET_GENERATE_ASPNET_CERTIFICATE'
)
$savedEnvironment = @{}
foreach ($key in $environmentNames) {
    $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}
$stage = 'preflight'
Push-Location $projectDir
try {
    $localProps = Join-Path $projectDir 'local.props'
    if (-not (Test-Path -LiteralPath $localProps)) {
        throw 'local.props is missing. Copy local.props.template and set Sts2Dir and GodotExe.'
    }
    [xml]$props = Get-Content -LiteralPath $localProps -Raw -Encoding UTF8
    if (-not $GameDir) { $GameDir = [string]$props.Project.PropertyGroup.Sts2Dir }
    if (-not $GodotExe) { $GodotExe = [string]$props.Project.PropertyGroup.GodotExe }
    $gameAssembly = Join-Path $GameDir 'data_sts2_windows_x86_64\sts2.dll'
    if (-not (Test-Path -LiteralPath $gameAssembly)) { throw "Game assembly not found: $gameAssembly" }
    if (-not (Test-Path -LiteralPath $GodotExe)) { throw "Godot executable not found: $GodotExe" }
    if ($ArkBaseDll -and -not (Test-Path -LiteralPath $ArkBaseDll)) {
        throw "ArkBase dependency not found: $ArkBaseDll"
    }
    if (-not $env:NUGET_PACKAGES) {
        $env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget\packages'
    }
    if (-not (Test-Path -LiteralPath $env:NUGET_PACKAGES)) {
        throw "NuGet cache not found: $env:NUGET_PACKAGES. See BUILD.md for dependency setup."
    }
    $env:DOTNET_CLI_HOME = Join-Path $buildDir 'dotnet-home'
    $env:APPDATA = Join-Path $buildDir 'appdata'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME, $env:APPDATA -Force | Out-Null

    $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
    Invoke-BuildStep -Stage 'sdk' -Executable $dotnet -Arguments @('--version')
    $properties = @("-p:Sts2Dir=$GameDir", '-p:RitsuLibAutoCopy=false')
    if ($ArkBaseDll) { $properties += "-p:ArkBaseDll=$ArkBaseDll" }

    $stage = 'restore'
    $source = $env:NUGET_PACKAGES
    $audit = 'false'
    if ($Online) {
        $source = 'https://api.nuget.org/v3/index.json'
        $audit = 'true'
    }
    Write-Host "[$name] Dependency source: $source"
    $restoreArgs = @('restore', $project, '--source', $source, "-p:NuGetAudit=$audit") + $properties
    Invoke-BuildStep -Stage $stage -Executable $dotnet -Arguments $restoreArgs

    $stage = 'compile'
    $compileArgs = @('build', $project, '--no-restore', '-p:CopyModOnBuild=false', '-p:RunPckExport=false') + $properties
    Invoke-BuildStep -Stage $stage -Executable $dotnet -Arguments $compileArgs

    $stage = 'export'
    $pack = Join-Path $buildDir "$name-$PID.pck"
    if (Test-Path -LiteralPath $pack) { Remove-Item -LiteralPath $pack -Force }
    Invoke-BuildStep -Stage $stage -Executable $GodotExe -Arguments @(
        '--headless', '--quiet', '--path', $projectDir, '--export-pack', 'Windows Desktop', $pack
    )
    if (-not (Test-Path -LiteralPath $pack) -or (Get-Item -LiteralPath $pack).Length -eq 0) {
        throw "Godot did not produce a non-empty PCK. See $logDir\export.log"
    }
    if (Select-String -LiteralPath (Join-Path $logDir 'export.log') -SimpleMatch 'Failed to read the root certificate store' -Quiet) {
        Write-Host "[$name] Godot certificate diagnostic recorded in export.log; export returned success and produced a PCK."
    }

    $stage = 'package'
    $dist = Join-Path $projectDir 'dist'
    $package = Join-Path $dist $name
    New-Item -ItemType Directory -Path $package -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $projectDir ".godot\mono\temp\bin\Debug\$name.dll") -Destination $package -Force
    Copy-Item -LiteralPath (Join-Path $projectDir "$name.json") -Destination $package -Force
    Copy-Item -LiteralPath $pack -Destination (Join-Path $package "$name.pck") -Force
    Remove-Item -LiteralPath $pack -Force
    $version = (Get-Content -LiteralPath (Join-Path $projectDir "$name.json") -Raw -Encoding UTF8 | ConvertFrom-Json).version
    $zip = Join-Path $dist "$name-$version.zip"
    Compress-Archive -LiteralPath $package -DestinationPath $zip -CompressionLevel Optimal -Force
    Write-Host "[$name][package] OK: $zip"

    if (-not $NoInstall) {
        $stage = 'install'
        if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) {
            throw "BUILD SUCCEEDED, INSTALL PENDING: close SlayTheSpire2 and rerun. Package: $package"
        }
        $destination = Join-Path $GameDir "mods\$name"
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        $files = @(Get-ChildItem -LiteralPath $package -File)
        foreach ($file in $files) {
            $target = Join-Path $destination $file.Name
            if (Test-Path -LiteralPath $target) {
                $handle = [System.IO.File]::Open($target, 'Open', 'ReadWrite', 'None')
                $handle.Dispose()
            }
        }
        foreach ($file in $files) {
            $target = Join-Path $destination $file.Name
            Copy-Item -LiteralPath $file.FullName -Destination $target -Force
            if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {
                throw "Installed file differs from package: $target"
            }
        }
        Write-Host "[SUCCESS] $name BUILT AND INSTALLED: $destination"
    }
    else {
        Write-Host "[SUCCESS] $name BUILT AND PACKAGED (installation not requested): $package"
    }
    Write-Host "[$name] Logs: $logDir"
}
catch {
    throw "[$name][$stage] $($_.Exception.Message)"
}
finally {
    Pop-Location
    foreach ($key in $environmentNames) {
        [Environment]::SetEnvironmentVariable($key, $savedEnvironment[$key], 'Process')
    }
}
