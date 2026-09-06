$ErrorActionPreference = 'Stop'
$taskProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$taskValidationRoot = Join-Path $taskProjectRoot 'temp/diary-ui-validation'
$taskSceneRoot = Join-Path $taskProjectRoot 'JanusSpire2/scenes/screens'
New-Item -ItemType Directory -Force -Path $taskValidationRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'project.godot') -Destination (Join-Path $taskValidationRoot 'project.godot')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'check_layout.gd') -Destination (Join-Path $taskValidationRoot 'check_layout.gd')
$taskBinaryResources = @(
    'JanusSpire2/images/characters/char_select_bg_janus.jpg',
    'JanusSpire2/images/ui/diary/open_diary_spread.png',
    'JanusSpire2/images/ui/diary/diary_left_page_background.tres',
    'JanusSpire2/images/ui/diary/diary_right_page_background.tres',
    'JanusSpire2/images/ui/diary/page_arrow.png',
    'JanusSpire2/fonts/diary/SourceHanSerifSC-Medium.otf'
)
foreach ($taskResource in $taskBinaryResources) {
    $taskResourceTarget = Join-Path $taskValidationRoot $taskResource
    New-Item -ItemType Directory -Force -Path (Split-Path $taskResourceTarget) | Out-Null
    Copy-Item -LiteralPath (Join-Path $taskProjectRoot $taskResource) -Destination $taskResourceTarget
}
foreach ($taskShader in @(
    'JanusSpire2/shaders/diary/hsv.gdshader',
    'JanusSpire2/shaders/diary/black_to_alpha.gdshader'
)) {
    $taskShaderTarget = Join-Path $taskValidationRoot $taskShader
    New-Item -ItemType Directory -Force -Path (Split-Path $taskShaderTarget) | Out-Null
    Copy-Item -LiteralPath (Join-Path $taskProjectRoot $taskShader) -Destination $taskShaderTarget
}
# This isolated engine harness checks the real scene layout/resources without starting
# a game. Strip only C# bindings that require the game's initialized singleton graph.
foreach ($taskScene in @('diary_card_page.tscn', 'diary_page_arrow.tscn', 'diary_card_pile_screen.tscn')) {
    $taskSceneText = Get-Content -Raw -LiteralPath (Join-Path $taskSceneRoot $taskScene)
    $taskSceneText = [regex]::Replace($taskSceneText, '(?m)^\[ext_resource type="Script"[^\r\n]*\r?\n', '')
    $taskSceneText = [regex]::Replace($taskSceneText, '(?m)^script = [^\r\n]*\r?\n', '')
    $taskSceneText = [regex]::Replace($taskSceneText, '(?m)^PointRight = [^\r\n]*\r?\n', '')
    $taskSceneTarget = Join-Path $taskValidationRoot "JanusSpire2/scenes/screens/$taskScene"
    New-Item -ItemType Directory -Force -Path (Split-Path $taskSceneTarget) | Out-Null
    [System.IO.File]::WriteAllText($taskSceneTarget, $taskSceneText, [System.Text.UTF8Encoding]::new($false))
}
Write-Output "Prepared isolated UI validation at $taskValidationRoot"
