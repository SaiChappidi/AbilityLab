param([ValidateSet('before','after')][string]$Phase)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$recordRoot = Join-Path $PSScriptRoot '../DexReports/codex-demo-backup'
$frames = Get-ChildItem -LiteralPath (Join-Path $recordRoot "$Phase-frames") -Filter '*.png' | Sort-Object Name
if ($frames.Count -ne 24) { throw 'Expected 24 actual captured frames.' }
$gifOutput = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($gifOutput)
$writer.Write([System.Text.Encoding]::ASCII.GetBytes('GIF89a'))
$writer.Write([uint16]960); $writer.Write([uint16]540)
$writer.Write([byte]0x70); $writer.Write([byte]0); $writer.Write([byte]0)
$writer.Write([byte[]]@(0x21,0xff,0x0b)); $writer.Write([System.Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0'))
$writer.Write([byte[]]@(3,1,0,0,0))
foreach ($frame in $frames) {
    $sourceBitmap = [System.Drawing.Bitmap]::new($frame.FullName)
    $bitmap = [System.Drawing.Bitmap]::new(960,540)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.DrawImage($sourceBitmap,0,0,960,540)
    $singleGif = New-Object System.IO.MemoryStream
    $bitmap.Save($singleGif,[System.Drawing.Imaging.ImageFormat]::Gif)
    $bytes = $singleGif.ToArray()
    $paletteSize = 3 * [int][Math]::Pow(2, (($bytes[10] -band 7) + 1))
    $palette = [byte[]]$bytes[13..(12+$paletteSize)]
    $cursor = 13+$paletteSize
    while ($bytes[$cursor] -eq 0x21) {
        $cursor += 2
        while ($bytes[$cursor] -gt 0) { $cursor += 1+$bytes[$cursor] }
        $cursor++
    }
    if ($bytes[$cursor] -ne 0x2c) { throw 'Unexpected single-frame GIF structure.' }
    $descriptor = [byte[]]$bytes[($cursor+1)..($cursor+9)]
    $imageStart = $cursor+10
    if (($descriptor[8] -band 0x80) -ne 0) { throw 'Unexpected existing local palette.' }
    $descriptor[8] = [byte](0x80 -bor ($bytes[10] -band 7))
    $dataEnd = $imageStart+1
    while ($bytes[$dataEnd] -gt 0) { $dataEnd += 1+$bytes[$dataEnd] }
    $writer.Write([byte[]]@(0x21,0xf9,4,4,25,0,0,0))
    $writer.Write([byte]0x2c); $writer.Write($descriptor); $writer.Write($palette)
    $writer.Write([byte[]]$bytes[$imageStart..$dataEnd])
    $singleGif.Dispose(); $graphics.Dispose(); $bitmap.Dispose(); $sourceBitmap.Dispose()
}
$writer.Write([byte]0x3b); $writer.Flush()
$destination = Join-Path $recordRoot "$Phase.gif"
[System.IO.File]::WriteAllBytes($destination,$gifOutput.ToArray())
$writer.Dispose(); $gifOutput.Dispose()
$probe = [System.Drawing.Image]::FromFile($destination)
$frameCount = $probe.GetFrameCount([System.Drawing.Imaging.FrameDimension]::Time)
$probe.Dispose()
if ($frameCount -ne 24) { throw "Encoded GIF has $frameCount frames; expected 24." }
Write-Output "Saved $destination ($frameCount frames, 6 seconds)."
