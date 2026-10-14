$files = @(
'C:/Users/johin/kk_krita_v1/src/cs/KK_KritaBridge.dll',
'E:/Users/johin/Code_Library_Old/Koikatsu BetterRepack RX22/BepInEx/core/BepInEx.dll',
'E:/Users/johin/Code_Library_Old/Koikatsu BetterRepack RX22/CharaStudio_Data/Managed/UnityEngine.dll'
)
foreach ($f in $files) {
  if (Test-Path $f) {
    $a = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($f)
    Write-Output "$f => $($a.ImageRuntimeVersion)"
  } else {
    Write-Output "$f MISSING"
  }
}
