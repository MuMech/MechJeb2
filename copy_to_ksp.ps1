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

$localizationSource = "$repo\Localization"

foreach ($pluginDir in $pluginDirs) {
    if (!(Test-Path $pluginDir)) {
        Write-Warning "Missing plugin folder: $pluginDir"
        continue
    }

    Write-Host "`nCopying DLLs to: $pluginDir"

    foreach ($file in $files) {
        if (Test-Path $file) {
            Copy-Item $file $pluginDir -Force
            Write-Host "Copied: $(Split-Path $file -Leaf)"
        } else {
            Write-Warning "Missing: $file"
        }
    }

    # pluginDir = ...\GameData\MechJeb2\Plugins
    # mechJebDir = ...\GameData\MechJeb2
    $mechJebDir = Split-Path $pluginDir -Parent
    $localizationDest = Join-Path $mechJebDir "Localization"

    if (Test-Path $localizationSource) {
        if (!(Test-Path $localizationDest)) {
            New-Item -ItemType Directory -Path $localizationDest | Out-Null
            Write-Host "Created localization folder: $localizationDest"
        }

        Write-Host "Copying localization files to: $localizationDest"
        Copy-Item "$localizationSource\*.cfg" $localizationDest -Force
        Write-Host "Copied localization .cfg files"
    } else {
        Write-Warning "Missing localization source folder: $localizationSource"
    }
}

Write-Host "`nDone copying MechJeb runtime DLLs and localization files."