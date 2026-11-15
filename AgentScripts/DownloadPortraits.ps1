$ErrorActionPreference = 'Stop'
$portraitRoot = Join-Path $PSScriptRoot '../Assets/Resources/Dex/Portraits'
New-Item -ItemType Directory -Path $portraitRoot -Force | Out-Null
$versions = Invoke-RestMethod -Uri 'https://ddragon.leagueoflegends.com/api/versions.json' -TimeoutSec 30
$portraitVersion = $versions[0]
$sources = @()
foreach ($championName in @('Ahri','Morgana','Ezreal')) {
    $portraitUri = "https://ddragon.leagueoflegends.com/cdn/$portraitVersion/img/champion/$championName.png"
    $portraitPath = Join-Path $portraitRoot "$championName.png"
    if (Test-Path -LiteralPath $portraitPath) { throw "Refusing to overwrite existing asset $portraitPath" }
    Invoke-WebRequest -Uri $portraitUri -OutFile $portraitPath -TimeoutSec 30
    $sources += "$championName`: $portraitUri"
}
$sources | Set-Content -LiteralPath (Join-Path $portraitRoot 'Sources.txt') -Encoding UTF8
Write-Output "Downloaded three official champion portraits from Data Dragon $portraitVersion."
