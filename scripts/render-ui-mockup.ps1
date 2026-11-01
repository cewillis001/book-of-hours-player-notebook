$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$out = Join-Path (Split-Path -Parent $PSScriptRoot) 'outputs'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$bmp = [Drawing.Bitmap]::new(1480, 1000)
$g = [Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
function Fill($hex,$x,$y,$w,$h) {
    $b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex))
    $g.FillRectangle($b,[single]$x,[single]$y,[single]$w,[single]$h); $b.Dispose()
}
function Words($value,$size,$hex,$x,$y,$family='Georgia') {
    $f=[Drawing.Font]::new($family,[single]$size,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
    $b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex))
    $g.DrawString($value,$f,$b,[single]$x,[single]$y); $f.Dispose(); $b.Dispose()
}
$g.Clear([Drawing.ColorTranslator]::FromHtml('#222b2a'))
Words 'PLAYER NOTEBOOK  /  INTERFACE STUDY' 17 '#c4b693' 60 23 'Segoe UI'
Fill '#161d1c' 68 72 1360 880
Fill '#3b302b' 60 60 1360 880
Fill '#b49a67' 70 70 1340 860
Fill '#3b302b' 72 72 1336 856
Words 'COMMONPLACE BOOK' 18 '#d0bd94' 96 90 'Segoe UI'
Words 'Untitled Notebook' 38 '#f4ead4' 96 122
Fill '#b49a67' 100 175 1015 1
Fill '#55473b' 1236 90 140 48
Words 'Close  ×' 24 '#f4ead4' 1255 99 'Segoe UI'
Fill '#e5d8bb' 80 200 300 656
Fill '#f5ecd8' 380 200 1020 656
Fill '#d4c4a1' 378 200 2 656
Fill '#ebe0c7' 380 200 12 656
Words 'ENTRIES' 20 '#615846' 106 229 'Segoe UI'
Words '2' 20 '#615846' 340 229 'Segoe UI'
Fill '#334f49' 100 279 260 78
Fill '#c4aa75' 100 279 4 78
Words 'A first thought' 24 '#fff6e0' 118 291
Words 'A place for observations…' 17 '#d3dece' 118 327 'Segoe UI'
Words 'Loose thoughts' 24 '#3c3830' 118 384
Words 'Untitled entry' 17 '#786e5b' 118 420 'Segoe UI'
Fill '#334f49' 100 784 260 50
Words '+  New entry' 23 '#fff6e0' 151 793 'Segoe UI'
Words 'ENTRY 01' 17 '#82745a' 434 230 'Segoe UI'
Words 'A first thought' 34 '#342f27' 434 270
Fill '#cabd9c' 438 324 904 1
Words 'A place for observations, questions, and things worth keeping.' 26 '#342f27' 434 355
Words 'The words are yours. The notebook simply keeps them.' 26 '#342f27' 434 400
Fill '#e2d6bb' 1370 350 7 395
Fill '#9c8d6b' 1370 350 7 125
Words 'Move up     Move down' 19 '#6d624e' 434 808 'Segoe UI'
Words 'Delete entry' 19 '#89544b' 1234 808 'Segoe UI'
Words 'Game paused · Save the game before quitting.' 18 '#d0bd94' 100 884 'Segoe UI'
Words 'Text size' 18 '#d0bd94' 1140 884 'Segoe UI'
Words 'A−     A+' 24 '#f4ead4' 1254 878 'Segoe UI'
$bmp.Save((Join-Path $out 'notebook-ui-direction.png'),[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
