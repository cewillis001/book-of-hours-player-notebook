param(
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\Book of Hours',
    [string]$FixtureSave,
    [switch]$Launch,
    [switch]$NewGame,
    [int]$Width,
    [int]$Height
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (Get-Process bh -ErrorAction SilentlyContinue) { throw 'Exit Book of Hours before preparing the isolated runtime test.' }
$root = Join-Path $repo $(if ($NewGame) { 'scratch\runtime-newgame-saves' } else { 'scratch\runtime-test-saves' })
New-Item -ItemType Directory -Force $root | Out-Null
Set-Content -LiteralPath (Join-Path $root '.playernotebook-test') -Value 'Isolated development saves only.'
if ($FixtureSave) {
    if (Test-Path -LiteralPath (Join-Path $root 'AUTOSAVE.json')) { throw 'Test autosave exists; inspect before replacing it.' }
    Copy-Item -LiteralPath $FixtureSave -Destination (Join-Path $root 'AUTOSAVE.json')
}
dotnet build (Join-Path $repo 'src\PlayerNotebook\PlayerNotebook.csproj') -c Release "/p:GameDir=$GameDir" /p:DefineConstants=NOTEBOOK_TESTING -o (Join-Path $repo 'scratch\test-build')
if ($LASTEXITCODE) { throw 'Test build failed' }
$modDir = Join-Path $env:USERPROFILE 'AppData\LocalLow\Weather Factory\Book of Hours\mods\PlayerNotebook'
if (!(Test-Path -LiteralPath (Join-Path $modDir 'synopsis.json'))) { throw 'Install prototype package first.' }
Copy-Item -LiteralPath (Join-Path $repo 'scratch\test-build\PlayerNotebook.dll') -Destination (Join-Path $modDir 'dll\PlayerNotebook.dll') -Force
Write-Output "Test-only DLL installed. Save root: $root"
if ($Launch) {
    $start = [System.Diagnostics.ProcessStartInfo]::new((Join-Path $GameDir 'bh.exe'))
    $start.WorkingDirectory = $GameDir
    $start.UseShellExecute = $false
    $start.Arguments = '-screen-fullscreen 0'
    if ($Width -gt 0 -and $Height -gt 0) { $start.Arguments += " -screen-width $Width -screen-height $Height" }
    $start.EnvironmentVariables['PLAYERNOTEBOOK_TEST_ROOT'] = $root
    if ($Width -gt 0 -and $Height -gt 0) {
        $start.EnvironmentVariables['PLAYERNOTEBOOK_TEST_WIDTH'] = $Width.ToString()
        $start.EnvironmentVariables['PLAYERNOTEBOOK_TEST_HEIGHT'] = $Height.ToString()
    }
    if ($NewGame) { $start.EnvironmentVariables['PLAYERNOTEBOOK_TEST_NEW_GAME'] = '1' }
    [System.Diagnostics.Process]::Start($start) | Out-Null
}
