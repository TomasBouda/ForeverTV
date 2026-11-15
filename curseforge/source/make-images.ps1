param([string]$Shot, [string]$OutDir, [string]$Icon, [string]$Frame)
# -Frame: optional PNG of a video frame for the screen instead of the original stage. Such output may contain other
# people's work; write it to curseforge/local/ (git-ignored), never into the repository.
# Builds the CurseForge gallery picture (TV with an original 80s music-video stand-in on the screen) and the summary banner.
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
function Col([int]$a, [int]$r, [int]$gr, [int]$b) { [Drawing.Color]::FromArgb($a, $r, $gr, $b) }
function Brush($c) { [Drawing.SolidBrush]::new($c) }
function Pt([double]$x, [double]$y) { [Drawing.PointF]::new([float]$x, [float]$y) }

# --- the screen picture: an original 80s stage, not a frame of any real video ---
function New-Screen([int]$sw, [int]$sh) {
    $bmp = [Drawing.Bitmap]::new($sw, $sh)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'
    $horizon = [int]($sh * 0.62)
    $sky = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Rectangle]::new(0, 0, $sw, $horizon), (Col 255 24 8 58), (Col 255 214 54 132), [float]90)
    $g.FillRectangle($sky, 0, 0, $sw, $horizon)
    $sr = [int]($sh * 0.30); $sunX = [int]($sw * 0.5) - $sr; $sunY = $horizon - [int]($sr * 1.35)
    $sun = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Rectangle]::new($sunX, $sunY, $sr * 2, $sr * 2), (Col 255 255 214 90), (Col 255 255 70 120), [float]90)
    $g.SetClip([Drawing.Rectangle]::new(0, 0, $sw, $horizon))
    $g.FillEllipse($sun, $sunX, $sunY, $sr * 2, $sr * 2)
    $stripe = Brush (Col 255 150 30 110)
    for ($i = 0; $i -lt 6; $i++) { $g.FillRectangle($stripe, $sunX, $sunY + [int]($sr * (1.05 + $i * 0.16)), $sr * 2, 3 + $i * 2) }
    $g.ResetClip()
    $g.FillRectangle((Brush (Col 255 18 4 40)), 0, $horizon, $sw, $sh - $horizon)
    $pen = [Drawing.Pen]::new((Col 230 255 60 200), [float]2)
    $cx = $sw / 2
    for ($i = -12; $i -le 12; $i++) { $g.DrawLine($pen, (Pt ($cx + $i * 18) $horizon), (Pt ($cx + $i * 120) $sh)) }
    for ($i = 0; $i -lt 9; $i++) { $y = $horizon + [Math]::Pow($i / 8.0, 2) * ($sh - $horizon); $g.DrawLine($pen, (Pt 0 $y), (Pt $sw $y)) }
    $g.DrawLine([Drawing.Pen]::new((Col 255 120 240 255), [float]3), (Pt 0 $horizon), (Pt $sw $horizon))
    $beam = Brush (Col 40 255 255 255)
    $g.FillPolygon($beam, [Drawing.PointF[]]@((Pt ($sw * 0.15) 0), (Pt ($sw * 0.20) 0), (Pt ($sw * 0.56) ($sh * 0.92)), (Pt ($sw * 0.40) ($sh * 0.92))))
    $g.FillPolygon($beam, [Drawing.PointF[]]@((Pt ($sw * 0.80) 0), (Pt ($sw * 0.85) 0), (Pt ($sw * 0.60) ($sh * 0.92)), (Pt ($sw * 0.44) ($sh * 0.92))))
    $chrome = [Drawing.Pen]::new((Col 255 225 225 235), [float]5)
    # an original goblin crooner in silhouette: pointy ears, quiff, long nose, long coat
    $u = $sh / 100.0; $c = $sw * 0.5 - 6 * $u
    $body = [Drawing.Drawing2D.GraphicsPath]::new()
    $body.AddRectangle([Drawing.RectangleF]::new([float]($c - 6 * $u), [float](70 * $u), [float](4 * $u), [float](20 * $u)))
    $body.AddRectangle([Drawing.RectangleF]::new([float]($c + 2 * $u), [float](70 * $u), [float](4 * $u), [float](20 * $u)))
    $body.AddPolygon([Drawing.PointF[]]@((Pt ($c - 9 * $u) (42 * $u)), (Pt ($c + 9 * $u) (42 * $u)), (Pt ($c + 12 * $u) (76 * $u)), (Pt ($c - 12 * $u) (76 * $u))))
    $body.AddEllipse([float]($c - 6 * $u), [float](27 * $u), [float](12 * $u), [float](14 * $u))
    $body.AddPolygon([Drawing.PointF[]]@((Pt ($c - 5 * $u) (31 * $u)), (Pt ($c - 17 * $u) (25 * $u)), (Pt ($c - 5 * $u) (37 * $u))))
    $body.AddPolygon([Drawing.PointF[]]@((Pt ($c + 5 * $u) (31 * $u)), (Pt ($c + 15 * $u) (24 * $u)), (Pt ($c + 5 * $u) (36 * $u))))
    $body.AddEllipse([float]($c - 5 * $u), [float](21 * $u), [float](13 * $u), [float](8 * $u))
    $body.AddPolygon([Drawing.PointF[]]@((Pt ($c + 5 * $u) (33 * $u)), (Pt ($c + 11 * $u) (36 * $u)), (Pt ($c + 5 * $u) (37 * $u))))
    $body.FillMode = 'Winding'
    # rim light first, the dark fill on top, so only the outer edge glows
    $mx = $c + 13 * $u; $top = 40 * $u; $base = $sh * 0.90
    $rim = [Drawing.Pen]::new((Col 220 255 110 220), [float]4); $rim.LineJoin = 'Round'
    $g.DrawPath($rim, $body)
    $rimArm = [Drawing.Pen]::new((Col 220 255 110 220), [float](3 * $u + 4)); $rimArm.StartCap = 'Round'; $rimArm.EndCap = 'Round'
    $g.DrawLine($rimArm, (Pt ($c + 6 * $u) (46 * $u)), (Pt ($mx - 2 * $u) ($top + 1 * $u)))
    $g.FillPath((Brush (Col 255 22 6 34)), $body)
    $arm = [Drawing.Pen]::new((Col 255 22 6 34), [float](3 * $u)); $arm.StartCap = 'Round'; $arm.EndCap = 'Round'
    $g.DrawLine($arm, (Pt ($c + 6 * $u) (46 * $u)), (Pt ($mx - 2 * $u) ($top + 1 * $u)))
    $g.DrawLine($chrome, (Pt $mx $top), (Pt $mx $base))
    $g.DrawLine($chrome, (Pt ($mx - 34) $base), (Pt ($mx + 34) $base))
    $g.FillEllipse((Brush (Col 255 40 40 48)), [float]($mx - 13), [float]($top - 26), [float]26, [float]34)
    $g.DrawEllipse([Drawing.Pen]::new((Col 255 200 200 210), [float]2), [float]($mx - 13), [float]($top - 26), [float]26, [float]34)
    $noteFont = [Drawing.Font]::new('Segoe UI Symbol', [float]($sh * 0.09), [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
    $noteBrush = Brush (Col 230 120 240 255)
    $g.DrawString([string][char]0x266A, $noteFont, $noteBrush, (Pt ($sw * 0.28) ($sh * 0.22)))
    $g.DrawString([string][char]0x266B, $noteFont, $noteBrush, (Pt ($sw * 0.66) ($sh * 0.14)))
    if ($Frame) {
        # a user-supplied frame, pillarbox removed, cropped to the screen aspect from the top
        $fr = [Drawing.Image]::FromFile($Frame)
        $vx = 346; $vw = 1214
        $vh = [int]($vw * $sh / $sw)
        $g.DrawImage($fr, [Drawing.Rectangle]::new(0, 0, $sw, $sh), [Drawing.Rectangle]::new($vx, 0, $vw, $vh), [Drawing.GraphicsUnit]::Pixel)
        $fr.Dispose()
    }
    $g.FillRectangle((Brush (Col 150 0 0 0)), 0, 0, $sw, [int]($sh * 0.11))
    $tf = [Drawing.Font]::new('Segoe UI', [float]($sh * 0.05), [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
    $g.DrawString('Never Gonna Give You Up', $tf, [Drawing.Brushes]::White, (Pt ($sh * 0.03) ($sh * 0.025)))
    $barY = [float]($sh - $sh * 0.06)
    $g.FillRectangle((Brush (Col 160 255 255 255)), [float]0, $barY, [float]$sw, [float]4)
    $g.FillRectangle((Brush (Col 255 230 30 40)), [float]0, $barY, [float]($sw * 0.21), [float]4)
    $g.FillEllipse((Brush (Col 255 230 30 40)), [float]($sw * 0.21 - 6), [float]($barY - 4), [float]12, [float]12)
    $g.Dispose()
    return $bmp
}

# --- TV with the picture on its screen ---
$src = [Drawing.Image]::FromFile($Shot)
$crop = [Drawing.Rectangle]::new(560, 170, 840, 630)
$tv = [Drawing.Bitmap]::new($crop.Width, $crop.Height)
$g = [Drawing.Graphics]::FromImage($tv)
$g.DrawImage($src, [Drawing.Rectangle]::new(0, 0, $crop.Width, $crop.Height), $crop, [Drawing.GraphicsUnit]::Pixel)
# screen interior in the 1920x1080 render: x 636..1284, y 354..729 (covers the corner markers, as the video window does)
$screen = New-Screen 648 375 | Select-Object -Last 1
$g.DrawImage($screen, 636 - $crop.X, 354 - $crop.Y, 648, 375)
$g.Dispose(); $src.Dispose()
$tv.Save((Join-Path $OutDir 'screenshots\01-tv-playing.png'), [Drawing.Imaging.ImageFormat]::Png)

# --- summary banner, 2000x568 like the other kits ---
$BW = 2000; $BH = 568
$ban = [Drawing.Bitmap]::new($BW, $BH)
$g = [Drawing.Graphics]::FromImage($ban)
$g.SmoothingMode = 'AntiAlias'; $g.InterpolationMode = 'HighQualityBicubic'; $g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([Drawing.Color]::Black)
$glow = [Drawing.Drawing2D.GraphicsPath]::new()
$glow.AddEllipse(-200, -300, 1500, 1200)
$pgb = [Drawing.Drawing2D.PathGradientBrush]::new($glow)
$pgb.CenterColor = (Col 255 46 30 8); $pgb.SurroundColors = [Drawing.Color[]]@((Col 0 0 0 0))
# plain black like the other kits; the TV render has a black background
$scale = ($BH - 24) / $tv.Height
$tw = [int]($tv.Width * $scale); $th = [int]($tv.Height * $scale)
$g.DrawImage($tv, 60, [int](($BH - $th) / 2), $tw, $th)
$logoImg = [Drawing.Image]::FromFile($Icon)
$ix = 60 + $tw + 80
$g.DrawImage($logoImg, $ix, 70, 150, 150)
$gold = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Rectangle]::new($ix, 240, 900, 130), (Col 255 255 226 140), (Col 255 196 128 40), [float]90)
$g.DrawString('ForeverTV', [Drawing.Font]::new('Georgia', [float]128, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel), $gold, (Pt ($ix - 10) 225))
$g.DrawString('YouTube on a goblin TV inside WoW Forever', [Drawing.Font]::new('Segoe UI', [float]44, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel), (Brush (Col 255 236 222 196)), (Pt $ix 385))
$g.DrawString('One click from your browser. Clicks go through to the game.', [Drawing.Font]::new('Segoe UI', [float]32, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel), (Brush (Col 255 170 160 140)), (Pt $ix 450))
$g.Dispose(); $logoImg.Dispose()
$ban.Save((Join-Path $OutDir 'summary.png'), [Drawing.Imaging.ImageFormat]::Png)
$tv.Dispose(); $ban.Dispose()
'done'



