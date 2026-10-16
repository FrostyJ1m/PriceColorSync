param([Parameter(Mandatory=$true)][string]$ClientPath)
$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$clientDir = (Resolve-Path -LiteralPath $ClientPath).Path
$managedDir = Join-Path $clientDir 'EscapeFromTarkov_Data/Managed'
$coreDir = Join-Path $clientDir 'BepInEx/core'
$compilerPath = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDir = Join-Path $projectDir 'build'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$references = @(
    "$coreDir/BepInEx.dll", "$coreDir/0Harmony.dll",
    "$managedDir/Assembly-CSharp.dll", "$managedDir/UnityEngine.dll", "$managedDir/UnityEngine.CoreModule.dll",
    "$managedDir/UnityEngine.UI.dll", "$managedDir/UnityEngine.UIModule.dll",
    "$managedDir/ItemTemplate.Types.dll", "$managedDir/ItemComponent.Types.dll",
    "$managedDir/Sirenix.Serialization.dll", "$clientDir/BepInEx/plugins/spt/spt-reflection.dll",
    "$managedDir/netstandard.dll",
    "$managedDir/Comfort.dll",
    "$clientDir/BepInEx/plugins/SwiftXP.ShowMeTheMoney.dll"
)
$compilerArgs = @('/nologo','/target:library','/optimize+',('/out:' + (Join-Path $outputDir 'SPT.Diagnostics.PriceColorSync.dll')))
$compilerArgs += $references | ForEach-Object { '/reference:' + $_ }
$compilerArgs += (Join-Path $projectDir 'InventorySimulationScope.cs')
$compilerArgs += @((Join-Path $projectDir 'TierPolicy.cs'), (Join-Path $projectDir 'BackgroundWorkQueue.cs'), (Join-Path $projectDir 'ItemPreparationQueue.cs'), (Join-Path $projectDir 'MenuRefreshPolicy.cs'), (Join-Path $projectDir 'PriceColorSync.cs'))
& $compilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Plugin compilation failed' }
& $compilerPath /nologo /target:exe (('/out:' + (Join-Path $outputDir 'TierPolicyTests.exe'))) (Join-Path $projectDir 'TierPolicy.cs') (Join-Path $projectDir 'BackgroundWorkQueue.cs') (Join-Path $projectDir 'TierPolicyTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& (Join-Path $outputDir 'TierPolicyTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Policy tests failed' }
& $compilerPath /nologo /target:exe (('/out:' + (Join-Path $outputDir 'MenuRefreshPolicyTests.exe'))) (Join-Path $projectDir 'MenuRefreshPolicy.cs') (Join-Path $projectDir 'MenuRefreshPolicyTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Menu refresh test compilation failed' }
& (Join-Path $outputDir 'MenuRefreshPolicyTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Menu refresh policy tests failed' }
& $compilerPath /nologo /target:exe (('/out:' + (Join-Path $outputDir 'ItemPreparationQueueTests.exe'))) (Join-Path $projectDir 'InventorySimulationScope.cs') (Join-Path $projectDir 'ItemPreparationQueue.cs') (Join-Path $projectDir 'ItemPreparationQueueTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Item preparation test compilation failed' }
& (Join-Path $outputDir 'ItemPreparationQueueTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Item preparation tests failed' }
& $compilerPath /nologo /target:exe (('/out:' + (Join-Path $outputDir 'InventorySimulationTests.exe'))) (Join-Path $projectDir 'InventorySimulationScope.cs') (Join-Path $projectDir 'ItemPreparationQueue.cs') (Join-Path $projectDir 'InventorySimulationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Inventory simulation test compilation failed' }
& (Join-Path $outputDir 'InventorySimulationTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Inventory simulation tests failed' }
