param([string]$GameDir = 'D:\SteamLibrary\steamapps\common\Book of Hours')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Push-Location $repo
try {
    dotnet build src/PlayerNotebook/PlayerNotebook.csproj -c Release "/p:GameDir=$GameDir"
    if ($LASTEXITCODE) { throw 'Build failed' }
    dotnet run --project tests/PlayerNotebook.Checks -c Release
    if ($LASTEXITCODE) { throw 'Notebook checks failed' }
    dotnet run --project tests/PlayerNotebook.NativeChecks -c Release "/p:GameDir=$GameDir" -- "$GameDir\bh_Data\Managed"
    if ($LASTEXITCODE) { throw 'Native serialization check failed' }
    $stage = Join-Path $repo ('dist\stage-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force $stage | Out-Null
    Copy-Item -LiteralPath "$repo\mod\PlayerNotebook" -Destination $stage -Recurse
    $modDir = Join-Path $stage 'PlayerNotebook'
    New-Item -ItemType Directory -Force "$modDir\dll" | Out-Null
    Copy-Item -LiteralPath "$repo\src\PlayerNotebook\bin\Release\net472\PlayerNotebook.dll" -Destination "$modDir\dll"
    Copy-Item -LiteralPath "$repo\src\PlayerNotebook\bin\Release\net472\0Harmony.dll" -Destination "$modDir\dll"
    Copy-Item -LiteralPath "$repo\README.md" -Destination $modDir
    Copy-Item -LiteralPath "$repo\docs\manual-tests.md" -Destination $modDir
    Copy-Item -LiteralPath "$repo\docs\THIRD-PARTY-NOTICES.md" -Destination $modDir
    Copy-Item -LiteralPath "$repo\LICENSE" -Destination $modDir
    Copy-Item -LiteralPath "$repo\docs\ASSETS.md" -Destination $modDir
    Copy-Item -LiteralPath "$repo\LICENSES" -Destination $modDir -Recurse
    $zip = Join-Path $repo 'dist\PlayerNotebook-0.1.0-prototype.zip'
    Compress-Archive -LiteralPath $modDir -DestinationPath $zip -Force
    Write-Output "Prototype package: $zip"
    Write-Output 'Not installed. In-game verification is still required.'
} finally { Pop-Location }
