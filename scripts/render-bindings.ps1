# Original geometric cover artwork. No game art is read, copied, or redistributed.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$destination = Join-Path (Split-Path -Parent $PSScriptRoot) 'mod/PlayerNotebook/images/books'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($decorated in @($false, $true)) {
    foreach ($spine in @($false, $true)) {
        $width = if ($spine) { 48 } else { 256 }
        $height = 384
        # Native BookManifestation sizes the physical object from texture pixels.
        # Keep the drawing coordinates readable, but render at normal shelf-book size.
        $bitmap = [System.Drawing.Bitmap]::new(($width / 2), ($height / 2))
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.ScaleTransform(0.5, 0.5)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.ColorTranslator]::FromHtml('#543623'))
        $edge = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#2e2018'), 5)
        $thread = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#ad8652'), 2)
        $gold = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#d4b36a'), 3)
        $graphics.DrawRectangle($edge, 3, 3, ($width - 6), ($height - 6))
        $graphics.DrawRectangle($thread, 10, 12, ($width - 20), ($height - 24))
        if ($spine) {
            foreach ($y in @(42, 52, 330, 340)) { $graphics.DrawLine($thread, 5, $y, ($width - 5), $y) }
        } else {
            $graphics.DrawLine($edge, 19, 6, 19, ($height - 6))
            foreach ($y in @(54, 324)) { $graphics.DrawLine($thread, 4, $y, 18, $y) }
        }
        if ($decorated) {
            $inset = if ($spine) { 16 } else { 30 }
            $graphics.DrawRectangle($gold, $inset, 28, ($width - 2 * $inset), ($height - 56))
            $cx = [single]($width / 2)
            $radius = if ($spine) { 9 } else { 35 }
            $points = [System.Drawing.PointF[]]@(
                [System.Drawing.PointF]::new($cx, (192 - $radius)),
                [System.Drawing.PointF]::new(($cx + $radius), 192),
                [System.Drawing.PointF]::new($cx, (192 + $radius)),
                [System.Drawing.PointF]::new(($cx - $radius), 192))
            $graphics.DrawPolygon($gold, $points)
        }
        $design = if ($decorated) { 'decorated' } else { 'plain' }
        $suffix = if ($spine) { '_' } else { '' }
        $bitmap.Save((Join-Path $destination "playernotebook.brown.$design$suffix.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $gold.Dispose(); $thread.Dispose(); $edge.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
    }
}
