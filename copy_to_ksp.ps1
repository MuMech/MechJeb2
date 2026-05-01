$repo = $PSScriptRoot

$pluginDirs = @(
    "C:\KSP 1.12.5 MJ2 Test\Kerbal Space Program\GameData\MechJeb2\Plugins",
    "C:\KSP RP1 Sol\GameData\MechJeb2\Plugins"
)

$files = @(
    "$repo\MechJeb2\bin\Debug\MechJeb2.dll",
    "$repo\MechJebLib\bin\Debug\MechJebLib.dll",
    "$repo\MechJebLibBindings\bin\Debug\MechJebLibBindings.dll",
    "$repo\alglib\bin\Debug\alglib.dll",
    "$repo\packages\JetBrains.Annotations.2024.3.0\lib\net20\JetBrains.Annotations.dll"
)

foreach ($pluginDir in $pluginDirs) {
    if (!(Test-Path $pluginDir)) {
        Write-Warning "Missing plugin folder: $pluginDir"
        continue
    }

    Write-Host "`nCopying to: $pluginDir"

    foreach ($file in $files) {
        if (Test-Path $file) {
            Copy-Item $file $pluginDir -Force
            Write-Host "Copied: $(Split-Path $file -Leaf)"
        } else {
            Write-Warning "Missing: $file"
        }
    }
}

Write-Host "`nDone copying MechJeb runtime DLLs."