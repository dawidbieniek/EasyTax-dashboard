<#
.SYNOPSIS
    Creates the package icons in src/EasyTax.App/Assets.

.DESCRIPTION
    Without parameters it draws a plain placeholder (white "ET" on a blue tile) so the package builds.
    When the real icon exists, run it again with the picture:

        powershell tools/New-Icons.ps1 -Source path\to\icon.png

    The source should be a square PNG of at least 512 x 512 pixels (1024 is better). It is scaled into every size the manifest
    asks for: tiles and the app icon in all required sizes, the wide tile and the splash screen with the icon centred on a
    transparent background (BackgroundColor in the manifest decides what shows behind it).
#>
[CmdletBinding()]
param(
    [string]$Source
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assets = Join-Path (Split-Path -Parent $PSScriptRoot) 'src/EasyTax.App/Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

function New-Canvas([int]$width, [int]$height) {
    $bitmap = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'HighQuality'
    $graphics.InterpolationMode = 'HighQualityBicubic'
    $graphics.PixelOffsetMode = 'HighQuality'
    $graphics.TextRenderingHint = 'AntiAliasGridFit'
    $graphics.Clear([System.Drawing.Color]::Transparent)
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

# The icon on its own: either the supplied picture or the placeholder, drawn at the requested square size.
function Get-Square([int]$size) {
    $canvas = New-Canvas $size $size
    $g = $canvas.Graphics

    if ($Source) {
        $picture = [System.Drawing.Image]::FromFile((Resolve-Path $Source))
        $g.DrawImage($picture, 0, 0, $size, $size)
        $picture.Dispose()
    }
    else {
        $radius = [int]($size * 0.22)
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $d = $radius * 2
        $path.AddArc(0, 0, $d, $d, 180, 90)
        $path.AddArc($size - $d - 1, 0, $d, $d, 270, 90)
        $path.AddArc($size - $d - 1, $size - $d - 1, $d, $d, 0, 90)
        $path.AddArc(0, $size - $d - 1, $d, $d, 90, 90)
        $path.CloseFigure()
        $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 15, 108, 189))), $path)

        $font = New-Object System.Drawing.Font 'Segoe UI Semibold', ([single]($size * 0.42)), ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = 'Center'
        $format.LineAlignment = 'Center'
        $g.DrawString('ET', $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF 0, 0, $size, $size), $format)
    }

    $g.Dispose()
    return $canvas.Bitmap
}

function Save-Square([string]$name, [int]$size) {
    $bitmap = Get-Square $size
    $bitmap.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

# The icon centred on a wider transparent canvas (wide tile, splash screen).
function Save-Wide([string]$name, [int]$width, [int]$height) {
    $canvas = New-Canvas $width $height
    $side = [int]($height * 0.8)
    $icon = Get-Square $side
    $canvas.Graphics.DrawImage($icon, [int](($width - $side) / 2), [int](($height - $side) / 2), $side, $side)
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Bitmap.Dispose()
    $icon.Dispose()
}

Save-Square 'Square44x44Logo.scale-200.png' 88
Save-Square 'Square44x44Logo.targetsize-24_altform-unplated.png' 24
Save-Square 'Square44x44Logo.targetsize-48_altform-unplated.png' 48
Save-Square 'Square150x150Logo.scale-200.png' 300
Save-Square 'StoreLogo.png' 50
Save-Square 'LockScreenLogo.scale-200.png' 48
Save-Wide 'Wide310x150Logo.scale-200.png' 620 300
Save-Wide 'SplashScreen.scale-200.png' 1240 600

Write-Host ("Icons written to $assets (" + $(if ($Source) { "from $Source" } else { 'placeholder' }) + ').')
