$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$modId = 'JanusSpire2'
$stableVersion = '0.111.0'
$currentVersion = '0.113.1'
$implProject = "$modId.Impl.csproj"
$manifestPath = Join-Path $root 'mod_manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.id -ne $modId -or $manifest.min_game_version -ne $stableVersion -or $manifest.has_pck -or !$manifest.has_dll) {
    throw "The manifest must declare $modId, min_game_version $stableVersion, has_dll true and has_pck false."
}
$version = ([version]$manifest.version.TrimStart('v', 'V')).ToString()
$configuration = $env:JANUS_CONFIGURATION
if ($configuration -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]*$') {
    throw "Invalid MSBuild configuration '$configuration'."
}
$godot = $env:JANUS_GODOT
$stableData = (Resolve-Path -LiteralPath $env:JANUS_STABLE_DATA).ProviderPath
$currentData = (Resolve-Path -LiteralPath $env:JANUS_CURRENT_DATA).ProviderPath
$currentDll = $env:JANUS_CURRENT_DLL
$gameRoot = $env:JANUS_CURRENT_GAME
$releasePath = Join-Path $gameRoot 'release_info.json'
if (!(Test-Path -LiteralPath $releasePath)) {
    $releasePath = Join-Path $gameRoot 'SlayTheSpire2.app/Contents/Resources/release_info.json'
}
$release = Get-Content -LiteralPath $releasePath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($release.version.TrimStart('v', 'V') -ne $currentVersion) {
    throw "Expected game $currentVersion at '$gameRoot', found '$($release.version)'."
}
foreach ($file in @('sts2.dll', 'GodotSharp.dll', '0Harmony.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $stableData $file) -PathType Leaf)) {
        throw "Missing $stableVersion reference '$file' in '$stableData'."
    }
}

function Invoke-Tool([string] $Executable, [string[]] $Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Executable failed with exit code $LASTEXITCODE." }
}

$env:IsInnerGodotExport = 'true'
$env:BuildBundle = 'false'
$env:MSBUILDDISABLENODEREUSE = '1'
$work = Join-Path $root "temp/bundle/$configuration"
$gitArgs = @('-c', "safe.directory=$root", '-C', $root)
$stableRef = $env:JANUS_STABLE_REF
if (!$stableRef) {
    $stableRef = '373fa9dce7e38d042e5fd60e4e53c5a5991473cd'
    foreach ($branch in @("refs/heads/$stableVersion", "refs/remotes/origin/$stableVersion")) {
        & git @gitArgs show-ref --verify --quiet $branch
        if ($LASTEXITCODE -eq 0) {
            $stableRef = $branch
            break
        }
    }
}
$commit = & git @gitArgs rev-parse --verify "$stableRef^{commit}"
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[a-f0-9]{40}$') {
    throw "Cannot resolve the $stableVersion source ref '$stableRef'."
}
Write-Host "Building game $stableVersion from $stableRef ($commit)."
$snapshot = Join-Path $work "source-$commit"
New-Item -ItemType Directory -Force -Path $work, $snapshot | Out-Null
New-Item -ItemType File -Force -Path (Join-Path (Split-Path -Parent $work) '.gdignore') | Out-Null
$archive = Join-Path $work "$commit.tar"
$ready = Join-Path $snapshot '.source-ready'
if (!(Test-Path -LiteralPath $ready)) {
    Invoke-Tool git ($gitArgs + @('archive', '--format=tar', "--output=$archive", $commit))
    Invoke-Tool tar @('-xf', $archive, '-C', $snapshot)
    Remove-Item -LiteralPath $archive
    New-Item -ItemType File -Path $ready | Out-Null
}
Copy-Item -LiteralPath (Join-Path $root $implProject), (Join-Path $root "$modId.Impl.sln"), $manifestPath -Destination $snapshot -Force
$entry = Join-Path $snapshot 'JanusSpire2Code/MainFile.cs'
$entrySource = [IO.File]::ReadAllText($entry)
$entrySource = $entrySource -replace '(?m)^\[ModInitializer\(nameof\(Initialize\)\)\]\r?\n', ''
[IO.File]::WriteAllText($entry, $entrySource)
$projectFile = Join-Path $snapshot 'project.godot'
$projectSettings = [IO.File]::ReadAllText($projectFile).Replace('project/assembly_name="JanusSpire2"', 'project/assembly_name="JanusSpire2.Impl"')
if ($projectSettings -notmatch '(?m)^import/use_multiple_threads=') {
    $projectSettings = $projectSettings.Replace('[editor]', "[editor]`n`nimport/use_multiple_threads=false")
}
[IO.File]::WriteAllText($projectFile, $projectSettings)
$preset = Join-Path $snapshot 'export_presets.cfg'
$presetText = [IO.File]::ReadAllText($preset)
if (!$presetText.Contains('exclude_filter="mod_manifest.json,')) {
    $presetText = $presetText -replace '(?m)^exclude_filter="', 'exclude_filter="mod_manifest.json,JanusSpire2.json,'
    [IO.File]::WriteAllText($preset, $presetText)
}

$staging = Join-Path $work ("staging-" + [guid]::NewGuid().ToString('N'))
$stableOut = Join-Path $staging "bin/g$stableVersion"
$currentOut = Join-Path $staging "bin/g$currentVersion"
$launcherBuild = Join-Path $work 'launcher-dll'
New-Item -ItemType Directory -Force -Path $stableOut, $currentOut | Out-Null

function Export-Pack([string] $Project, [string] $DataDir, [string] $RitsuVersion, [string] $Destination, [string] $Label, [string] $BasePack) {
    $env:Sts2DataDir = $DataDir
    $env:RitsuLibVersion = $RitsuVersion
    $log = Join-Path $work "godot-$Label.log"
    $arguments = @('--headless', '--editor', '--path', $Project)
    if ($BasePack) {
        $arguments += @('--patches', $BasePack, '--export-patch')
    } else {
        $arguments += '--export-pack'
    }
    $arguments += @('Windows Desktop', $Destination)
    & {
        $ErrorActionPreference = 'Continue'
        & $godot @arguments 2>&1
    } | ForEach-Object { $_.ToString() } | Set-Content -LiteralPath $log -Encoding UTF8
    if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $Destination -PathType Leaf) -or
        (Select-String -LiteralPath $log -Quiet -Pattern '^ERROR:|^SCRIPT ERROR:|^System\.\w+Exception:')) {
        throw "Godot export failed (exit $LASTEXITCODE). See $log."
    }
    & (Join-Path $root 'tools/Finalize-ModPack.ps1') -PackPath $Destination -Patch:([bool] $BasePack)
}

$replaced = $false
try {
    $stableProject = Join-Path $snapshot $implProject
    $stableBuildArgs = @('build', $stableProject, '-v:q', '-p:NuGetAudit=false',
        "-p:Sts2DataDir=$stableData", '-p:RitsuLibVersion=0.6.10')
    # Godot always reads the project's Debug output, even for a Release bundle.
    Invoke-Tool dotnet ($stableBuildArgs + @('-c', 'Debug'))
    if ($configuration -ne 'Debug') {
        Invoke-Tool dotnet ($stableBuildArgs + @('-c', $configuration))
        Invoke-Tool dotnet @('build', (Join-Path $root $implProject), '-c', 'Debug', '-v:q', '-p:NuGetAudit=false',
            "-p:Sts2DataDir=$currentData", '-p:RitsuLibVersion=0.6.12')
    }
    Invoke-Tool dotnet @('build', (Join-Path $PSScriptRoot 'Launcher/JanusSpire2.Launcher.csproj'), '-c', $configuration, '-v:q',
        "-p:Sts2DataDir=$stableData", "-p:Version=$version", '-o', $launcherBuild)
    $launcherDll = Join-Path $launcherBuild "$modId.dll"
    $stableDll = Join-Path $snapshot ".godot/mono/temp/bin/$configuration/$modId.Impl.dll"
    $launcherAssembly = [Reflection.AssemblyName]::GetAssemblyName($launcherDll)
    foreach ($path in @($stableDll, $currentDll)) {
        $identity = [Reflection.AssemblyName]::GetAssemblyName($path)
        if ($identity.Name -ne "$modId.Impl" -or $identity.Version -ne $launcherAssembly.Version) {
            throw "Implementation '$path' does not match the manifest version and assembly identity."
        }
    }
    Copy-Item -LiteralPath $stableDll -Destination $stableOut
    Copy-Item -LiteralPath $currentDll -Destination $currentOut
    Copy-Item -LiteralPath $launcherDll, $manifestPath -Destination $staging
    $basePack = Join-Path $stableOut "$modId.pck"
    Export-Pack $snapshot $stableData '0.6.10' $basePack "g$stableVersion"
    Export-Pack $root $currentData '0.6.12' (Join-Path $currentOut "$modId.patch.pck") "g$currentVersion" $basePack

    $bundle = Join-Path $work $modId
    $previous = Join-Path $work ("previous-" + [guid]::NewGuid().ToString('N'))
    if (Test-Path -LiteralPath $bundle) {
        [IO.Directory]::Move($bundle, $previous)
    }
    try {
        [IO.Directory]::Move($staging, $bundle)
        $replaced = $true
    } catch {
        if (Test-Path -LiteralPath $previous) { [IO.Directory]::Move($previous, $bundle) }
        throw
    }
    Write-Host "Mod bundle: $bundle"
    if ($env:JANUS_BUNDLE_MODS_PATH) {
        & (Join-Path $PSScriptRoot 'Install-Bundle.ps1') -BundlePath $bundle -ModsPath $env:JANUS_BUNDLE_MODS_PATH `
            -StableGameVersion $stableVersion -CurrentGameVersion $currentVersion
    }
} finally {
    $cleanup = @($staging)
    if ($replaced) { $cleanup += $previous }
    foreach ($path in $cleanup) {
        if ($path -and (Test-Path -LiteralPath $path)) {
            $resolved = (Resolve-Path -LiteralPath $path).ProviderPath
            if ((Split-Path -Parent $resolved) -eq (Resolve-Path -LiteralPath $work).ProviderPath -and
                !((Get-Item -LiteralPath $resolved -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                Remove-Item -LiteralPath $resolved -Recurse -Force
            }
        }
    }
}
