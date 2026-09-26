param(
    [string]$FmodCli = 'D:\FMOD SoundSystem\FMOD Studio 2.03.14\fmodstudiocl.exe'
)
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'Janus.fspro'
& $FmodCli -build -banks Janus -platforms Desktop -export-guids -log-dir (Join-Path $PSScriptRoot 'Logs') $projectPath
if ($LASTEXITCODE -ne 0) { throw "FMOD build failed: $LASTEXITCODE" }
$resourceDirectory = Join-Path (Split-Path $PSScriptRoot -Parent) 'JanusSpire2\sfx'
$mappings = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Build\GUIDs.txt') |
    Where-Object { $_ -match '\} (bank:/Janus|event:/sfx/janus/[^\s]+)$' })
if ($mappings.Count -ne 6) { throw 'Expected one Janus bank and five Janus events.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Build\desktop\Janus.bank') -Destination (Join-Path $resourceDirectory 'Janus.bank') -Force
[System.IO.File]::WriteAllLines((Join-Path $resourceDirectory 'Janus.guids.txt'), $mappings, [System.Text.UTF8Encoding]::new($false))
Write-Output 'Published Janus.bank and Janus-only GUID mappings. Master banks remain authoring-only.'
