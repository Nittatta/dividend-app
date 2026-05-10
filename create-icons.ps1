Add-Type -AssemblyName System.Drawing

$iconsDir = "C:\Users\drums\dividend-app\icons"
if (!(Test-Path $iconsDir)) {
    New-Item -ItemType Directory -Path $iconsDir
}

function Create-Icon {
    param([int]$size, [string]$outputPath)

    $bmp = New-Object System.Drawing.Bitmap([int]$size, [int]$size)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

    $g.Clear([System.Drawing.Color]::FromArgb(13, 13, 26))

    $margin = [int]($size * 0.08)
    $inner  = [int]($size - $margin * 2)
    $brush  = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 169, 110))
    $g.FillEllipse($brush, [int]$margin, [int]$margin, [int]$inner, [int]$inner)

    $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(13, 13, 26))
    $fontSize  = [float]($size * 0.38)
    $font      = New-Object System.Drawing.Font("Arial", $fontSize, [System.Drawing.FontStyle]::Bold)
    $sf        = New-Object System.Drawing.StringFormat
    $sf.Alignment     = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $drawRect = New-Object System.Drawing.RectangleF([float]$margin, [float]$margin, [float]$inner, [float]$inner)
    $g.DrawString("Y", $font, $textBrush, $drawRect, $sf)

    $bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Host "Done: $outputPath"
}

Create-Icon -size 192 -outputPath "$iconsDir\icon-192.png"
Create-Icon -size 512 -outputPath "$iconsDir\icon-512.png"
Write-Host "Complete!"