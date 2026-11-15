param([Parameter(Mandatory=$true)][string]$RunDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$runRoot=(Resolve-Path -LiteralPath $RunDirectory).Path
foreach($phase in @('before','after')) {
    $frames=Get-ChildItem -LiteralPath (Join-Path $runRoot "$phase-frames") -Filter '*.png' | Sort-Object Name
    if($frames.Count -lt 3){throw "Insufficient actual $phase frames."}
    $stream=New-Object System.IO.MemoryStream; $writer=New-Object System.IO.BinaryWriter($stream)
    $writer.Write([Text.Encoding]::ASCII.GetBytes('GIF89a'));$writer.Write([uint16]960);$writer.Write([uint16]540);$writer.Write([byte[]]@(0x70,0,0,0x21,0xff,0x0b));$writer.Write([Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0'));$writer.Write([byte[]]@(3,1,0,0,0))
    foreach($frame in $frames) {
        $source=[Drawing.Bitmap]::new($frame.FullName);$bitmap=[Drawing.Bitmap]::new(960,540);$graphics=[Drawing.Graphics]::FromImage($bitmap);$graphics.DrawImage($source,0,0,960,540)
        $one=New-Object IO.MemoryStream;$bitmap.Save($one,[Drawing.Imaging.ImageFormat]::Gif);$bytes=$one.ToArray();$size=3*[int][Math]::Pow(2,(($bytes[10] -band 7)+1));$palette=[byte[]]$bytes[13..(12+$size)];$cursor=13+$size
        while($bytes[$cursor] -eq 0x21){$cursor+=2;while($bytes[$cursor] -gt 0){$cursor+=1+$bytes[$cursor]};$cursor++}
        if($bytes[$cursor] -ne 0x2c){throw 'Unexpected GIF frame encoding.'}
        $descriptor=[byte[]]$bytes[($cursor+1)..($cursor+9)];if(($descriptor[8] -band 0x80) -ne 0){throw 'Unexpected local palette.'};$descriptor[8]=[byte](0x80 -bor ($bytes[10] -band 7));$data=$cursor+10;$end=$data+1
        while($bytes[$end] -gt 0){$end+=1+$bytes[$end]}
        $writer.Write([byte[]]@(0x21,0xf9,4,4,25,0,0,0,0x2c));$writer.Write($descriptor);$writer.Write($palette);$writer.Write([byte[]]$bytes[$data..$end]);$one.Dispose();$graphics.Dispose();$bitmap.Dispose();$source.Dispose()
    }
    $writer.Write([byte]0x3b);$writer.Flush();$output=Join-Path $runRoot "$phase.gif";[IO.File]::WriteAllBytes($output,$stream.ToArray());$writer.Dispose();$stream.Dispose()
    $probe=[Drawing.Image]::FromFile($output);$count=$probe.GetFrameCount([Drawing.Imaging.FrameDimension]::Time);$probe.Dispose();if($count -ne $frames.Count){throw 'GIF frame validation failed.'}
    Write-Output "Encoded $phase replay: $count actual frames."
}
