param(
    [Parameter(Mandatory)] [string] $BundlePath,
    [Parameter(Mandatory)] [string] $ModsPath,
    [Parameter(Mandatory)] [version] $StableGameVersion,
    [Parameter(Mandatory)] [version] $CurrentGameVersion
)

$ErrorActionPreference = 'Stop'
$modId = 'JanusSpire2'
$bundle = (Resolve-Path -LiteralPath $BundlePath).ProviderPath
$mods = [IO.Path]::GetFullPath($ModsPath)
$destination = Join-Path $mods $modId
$gameDirectory = Split-Path -Parent $mods
$gameExecutables = @('SlayTheSpire2.exe', 'SlayTheSpire2', 'SlayTheSpire2.x86_64') |
    ForEach-Object { Join-Path $gameDirectory $_ }

$stable = "bin/g$StableGameVersion"
$current = "bin/g$CurrentGameVersion"
$files = @("$modId.dll", "$stable/$modId.Impl.dll", "$stable/$modId.pck",
    "$current/$modId.Impl.dll", "$current/$modId.patch.pck", 'mod_manifest.json')
# Retire the old flat layout and any full pack superseded by the current patch.
$retiredFiles = @("$modId.json", "$modId.pck", "$current/$modId.pck")

foreach ($relative in $files) {
    $source = Join-Path $bundle $relative
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Missing bundle file '$source'."
    }
}
foreach ($directory in @($mods, $destination, (Join-Path $destination 'bin'),
        (Join-Path $destination $stable), (Join-Path $destination $current))) {
    if (Test-Path -LiteralPath $directory) {
        $item = Get-Item -LiteralPath $directory -Force
        if (!$item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Expected an ordinary mod directory at '$directory'."
        }
    }
}

# The game's recursive manifest scan must not see staged files or backups.
$transaction = Join-Path $gameDirectory (".$modId-install-" + [guid]::NewGuid().ToString('N'))
$staged = Join-Path $transaction 'new'
$previous = Join-Path $transaction 'previous'
$savedFiles = [Collections.Generic.List[string]]::new()
$installedFiles = [Collections.Generic.List[string]]::new()
$canCleanUp = $true
$installMutex = [Threading.Mutex]::new($false, "$modId.InstallBundle")
$ownsInstallMutex = $false
try {
    foreach ($relative in $files) {
        $target = Join-Path $staged $relative
        [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
        [IO.File]::Copy((Join-Path $bundle $relative), $target)
    }
    try {
        $ownsInstallMutex = $installMutex.WaitOne([TimeSpan]::FromSeconds(30))
    } catch [Threading.AbandonedMutexException] {
        $ownsInstallMutex = $true
    }
    if (!$ownsInstallMutex) {
        throw "Another $modId installation is in progress. Bundle built at $bundle."
    }
    if (Get-Process -Name SlayTheSpire2,SlayTheSpire2.x86_64 -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -in $gameExecutables }) {
        Write-Warning "Bundle built at $bundle. Installation skipped because Slay the Spire 2 is running. Close it and build again."
        return
    }

    $filesToReplace = @('mod_manifest.json') + $retiredFiles + @($files | Where-Object { $_ -ne 'mod_manifest.json' })
    foreach ($relative in $filesToReplace) {
        $target = Join-Path $destination $relative
        if (Test-Path -LiteralPath $target) {
            $item = Get-Item -LiteralPath $target -Force
            if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw "Expected an ordinary mod file at '$target'."
            }
            $backup = Join-Path $previous $relative
            [IO.Directory]::CreateDirectory((Split-Path -Parent $backup)) | Out-Null
            [IO.File]::Move($target, $backup)
            $savedFiles.Add($relative)
        }
    }
    foreach ($relative in $files) {
        $target = Join-Path $destination $relative
        [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
        [IO.File]::Move((Join-Path $staged $relative), $target)
        $installedFiles.Add($relative)
    }
} catch {
    $installError = $_
    $canCleanUp = $false
    try {
        foreach ($relative in $installedFiles) {
            [IO.File]::Delete((Join-Path $destination $relative))
        }
        for ($index = $savedFiles.Count - 1; $index -ge 0; $index--) {
            $relative = $savedFiles[$index]
            [IO.File]::Move((Join-Path $previous $relative), (Join-Path $destination $relative))
        }
        $canCleanUp = $true
    } catch {
        throw "Installation failed and could not be rolled back. Backup: $previous. $($_.Exception.GetBaseException().Message)"
    }
    $failure = $installError.Exception.GetBaseException()
    if ($env:OS -eq 'Windows_NT' -and ($failure.HResult -band 0xffff) -in 5, 32, 33) {
        Write-Warning "Bundle built at $bundle. Installation skipped: $($failure.Message) Close any program using the mod or check folder permissions, then build again."
        return
    }
    throw $installError
} finally {
    if ($ownsInstallMutex) {
        $installMutex.ReleaseMutex()
    }
    $installMutex.Dispose()
    if ($canCleanUp -and (Test-Path -LiteralPath $transaction)) {
        $resolved = (Resolve-Path -LiteralPath $transaction).ProviderPath
        if ($resolved -eq [IO.Path]::GetFullPath($transaction) -and
            !((Get-Item -LiteralPath $resolved -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
}
Write-Host "Installed mod bundle: $destination"
