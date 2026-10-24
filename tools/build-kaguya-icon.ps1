$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$source = [Drawing.Bitmap]::FromFile((Join-Path $root 'assets\kaguya\idle.png'))
try {
    # A full-body sprite becomes unreadably narrow at tray sizes. Frame the head
    # and shoulders using the artwork's visible width, excluding transparent margins.
    $left = $source.Width; $top = $source.Height; $right = -1; $bottom = -1
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $source.Width; $x++) {
            if ($source.GetPixel($x, $y).A -gt 24) {
                $left = [Math]::Min($left, $x); $top = [Math]::Min($top, $y)
                $right = [Math]::Max($right, $x); $bottom = [Math]::Max($bottom, $y)
            }
        }
    }
    if ($right -lt $left) { throw 'Character artwork is empty' }
    $portraitSize = [Math]::Min($right - $left + 1, $bottom - $top + 1)
    $portrait = [Drawing.RectangleF]::new($left, $top, $portraitSize, $portraitSize)
    $frames = @()
    $sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
    foreach ($size in $sizes) {
        $bitmap = [Drawing.Bitmap]::new($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        $stream = [IO.MemoryStream]::new()
        try {
            $graphics.Clear([Drawing.Color]::Transparent)
            $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $inset = [Math]::Max(0.5, $size * 0.04)
            $destination = [Drawing.RectangleF]::new($inset, $inset, $size - 2 * $inset, $size - 2 * $inset)
            $graphics.DrawImage($source, $destination, $portrait, [Drawing.GraphicsUnit]::Pixel)
            if ($size -eq 256) {
                $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
            } else {
                # Small shell icons use a DIB and an explicit transparency mask.
                $frameWriter = [IO.BinaryWriter]::new($stream, [Text.Encoding]::UTF8, $true)
                try {
                    $maskStride = [int]([Math]::Ceiling($size / 32.0) * 4)
                    $frameWriter.Write([uint32]40)
                    $frameWriter.Write([int32]$size)
                    $frameWriter.Write([int32]($size * 2))
                    $frameWriter.Write([uint16]1)
                    $frameWriter.Write([uint16]32)
                    $frameWriter.Write([uint32]0)
                    $frameWriter.Write([uint32]($size * $size * 4 + $maskStride * $size))
                    for ($field = 0; $field -lt 4; $field++) { $frameWriter.Write([uint32]0) }
                    for ($y = $size - 1; $y -ge 0; $y--) {
                        for ($x = 0; $x -lt $size; $x++) {
                            $pixel = $bitmap.GetPixel($x, $y)
                            $frameWriter.Write([byte]$pixel.B)
                            $frameWriter.Write([byte]$pixel.G)
                            $frameWriter.Write([byte]$pixel.R)
                            $frameWriter.Write([byte]$pixel.A)
                        }
                    }
                    for ($y = $size - 1; $y -ge 0; $y--) {
                        $mask = [byte[]]::new($maskStride)
                        for ($x = 0; $x -lt $size; $x++) {
                            if ($bitmap.GetPixel($x, $y).A -eq 0) {
                                $index = [int][Math]::Floor($x / 8.0)
                                $mask[$index] = $mask[$index] -bor (0x80 -shr ($x % 8))
                            }
                        }
                        $frameWriter.Write($mask)
                    }
                } finally { $frameWriter.Dispose() }
            }
            $frames += ,$stream.ToArray()
        } finally {
            $stream.Dispose()
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
    $writer = [IO.BinaryWriter]::new([IO.File]::Create((Join-Path $root 'assets\kaguya.ico')))
    try {
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($index = 0; $index -lt $sizes.Count; $index++) {
            $dimension = $sizes[$index] % 256
            $writer.Write([byte]$dimension)
            $writer.Write([byte]$dimension)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]$frames[$index].Length)
            $writer.Write([uint32]$offset)
            $offset += $frames[$index].Length
        }
        foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
    } finally { $writer.Dispose() }
    Write-Output 'Kaguya icon generated from assets/kaguya/idle.png (16-256 px).'
} finally { $source.Dispose() }
