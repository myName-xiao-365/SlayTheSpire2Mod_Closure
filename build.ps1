param(
    [string]$GameDir,
    [string]$GodotExe,
    [switch]$NoInstall
)

$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$project = Get-ChildItem -LiteralPath $projectDir -Filter '*.csproj' |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $project) { throw 'No project file found.' }

$name = [System.IO.Path]::GetFileNameWithoutExtension($project)
[xml]$props = Get-Content -LiteralPath (Join-Path $projectDir 'local.props') -Raw
if (-not $GameDir) { $GameDir = $props.Project.PropertyGroup.Sts2Dir }
if (-not $GodotExe) { $GodotExe = $props.Project.PropertyGroup.GodotExe }
if (-not (Test-Path -LiteralPath $GameDir)) { throw "Game not found: $GameDir" }
if (-not (Test-Path -LiteralPath $GodotExe)) { throw "Godot not found: $GodotExe" }

if (-not $env:NUGET_PACKAGES) {
    $env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget\packages'
}
$env:APPDATA = Join-Path $env:TEMP "sts2-ark-$name-godot"
New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null

& dotnet restore $project --ignore-failed-sources "-p:Sts2Dir=$GameDir"
if ($LASTEXITCODE -ne 0) { throw "Restore failed: $name" }
& dotnet build $project --no-restore "-p:Sts2Dir=$GameDir" -p:CopyModOnBuild=false -p:RunPckExport=false
if ($LASTEXITCODE -ne 0) { throw "Build failed: $name" }

$pack = Join-Path $env:TEMP "$name-$PID.pck"
if (Test-Path -LiteralPath $pack) { Remove-Item -LiteralPath $pack -Force }
& $GodotExe --headless --quiet --path $projectDir --export-pack 'Windows Desktop' $pack
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $pack) -or
    (Get-Item -LiteralPath $pack).Length -eq 0) {
    throw "PCK export failed: $name"
}

$dist = Join-Path $projectDir 'dist'
$package = Join-Path $dist $name
New-Item -ItemType Directory -Path $package -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectDir ".godot\mono\temp\bin\Debug\$name.dll") -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $projectDir "$name.json") -Destination $package -Force
Copy-Item -LiteralPath $pack -Destination (Join-Path $package "$name.pck") -Force
Remove-Item -LiteralPath $pack -Force

$version = (Get-Content -LiteralPath (Join-Path $projectDir "$name.json") -Raw | ConvertFrom-Json).version
$zip = Join-Path $dist "$name-$version.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -LiteralPath $package -DestinationPath $zip -CompressionLevel Optimal

if (-not $NoInstall) {
    $destination = Join-Path $GameDir "mods\$name"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $package -File | Copy-Item -Destination $destination -Force
    Write-Host "Installed $name to $destination"
}

Write-Host "Ready to publish: $zip"
