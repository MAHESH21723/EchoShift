param([string]$EditorData = 'D:\UNITY\unity editor\6000.3.10f1\Editor\Data')
$ErrorActionPreference = 'Stop'
$echoProject = Split-Path -Parent $PSScriptRoot
$echoPlugin = Join-Path $echoProject 'Assets\Editor\Bootstrap'
New-Item -ItemType Directory -Path $echoPlugin -Force | Out-Null
$echoReferences = @(Get-ChildItem -LiteralPath (Join-Path $EditorData 'NetStandard\ref\2.1.0') -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' })
$echoReferences += Get-ChildItem -LiteralPath (Join-Path $EditorData 'NetStandard\compat\2.1.0\shims\netfx') -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$echoReferences += Get-ChildItem -LiteralPath (Join-Path $EditorData 'Managed\UnityEngine') -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$echoArguments = @('/nologo','/target:library','/nostdlib+','/langversion:9',('/out:"' + (Join-Path $echoPlugin 'EchoShift.PackageRepair.dll') + '"')) + $echoReferences + ('"' + (Join-Path $PSScriptRoot 'PackageRepair.cs') + '"')
$echoResponseFile = Join-Path $env:TEMP 'EchoShift-PackageRepair.rsp'
$echoArguments | Set-Content -LiteralPath $echoResponseFile
& (Join-Path $EditorData 'NetCoreRuntime\dotnet.exe') (Join-Path $EditorData 'DotNetSdkRoslyn\csc.dll') ('@' + $echoResponseFile)
exit $LASTEXITCODE
