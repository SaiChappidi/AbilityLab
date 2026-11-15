$ErrorActionPreference='Stop'
$kitArtRoot=Join-Path $PSScriptRoot '../Assets/Resources/Dex'
New-Item -ItemType Directory -Path (Join-Path $kitArtRoot 'Abilities') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $kitArtRoot 'ChampionData') -Force | Out-Null
$kitArtVersion='16.22.1'
$assetSources=@()
$iconSets=@{Ahri=@('AhriQ','AhriW','AhriE','AhriR'); Morgana=@('MorganaQ','MorganaW','MorganaE','MorganaR'); Ezreal=@('EzrealQ','EzrealW','EzrealE','EzrealR'); Mel=@('MelQ','MelW','MelE','MelR')}
foreach($kitChampion in @('Ahri','Morgana','Ezreal','Mel')) {
    $icons=@($iconSets[$kitChampion] | ForEach-Object {"$_.png"})
    for($slot=0;$slot -lt $icons.Count;$slot++) {
        $icon=$icons[$slot]
        if($icon -notmatch '^[A-Za-z0-9_.-]+\.png$'){throw 'Unexpected asset filename.'}
        $kind=if($slot -eq 4){'passive'}else{'spell'}
        $uri="https://ddragon.leagueoflegends.com/cdn/$kitArtVersion/img/$kind/$icon"
        $destination=Join-Path $kitArtRoot "Abilities/${kitChampion}_$slot.png"
        if(!(Test-Path -LiteralPath $destination)){try{Invoke-WebRequest -Uri $uri -OutFile $destination -TimeoutSec 15}catch{Write-Warning "Unavailable official icon: $uri";continue}}
        $assetSources+="$kitChampion ability $slot`: $uri"
    }
    if($kitChampion -eq 'Mel') {
        $portraitUrl="https://ddragon.leagueoflegends.com/cdn/$kitArtVersion/img/champion/Mel.png"
        $portraitFile=Join-Path $kitArtRoot 'Portraits/Mel.png'
        if(!(Test-Path -LiteralPath $portraitFile)){Invoke-WebRequest -Uri $portraitUrl -OutFile $portraitFile -TimeoutSec 30}
        $assetSources+="Mel portrait: $portraitUrl"
    }
}
$assetSources | Set-Content -LiteralPath (Join-Path $kitArtRoot 'Abilities/Sources.txt') -Encoding UTF8
Write-Output 'Downloaded official kit icons and Mel portrait. Existing portraits were preserved.'
