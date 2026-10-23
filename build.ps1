param(
  [ValidateSet('1.6','1.7','both')][string]$Version = 'both',
  [string]$RimWorldManagedDir,
  [string]$HarmonyAssemblyPath
)
$ErrorActionPreference = 'Stop'
if ($Version -eq 'both' -and ($RimWorldManagedDir -or $HarmonyAssemblyPath)) {
  throw 'Local references require a single Version; use the corresponding game/Harmony DLLs for each build.'
}
$versions = if ($Version -eq 'both') { @('1.6','1.7') } else { @($Version) }
foreach ($v in $versions) {
  $arguments = @('build', (Join-Path $PSScriptRoot 'Source/VCEFlourAlternatives.csproj'), '-c', 'Release', "-p:RimWorldVersion=$v", '-warnaserror')
  if ($RimWorldManagedDir) { $arguments += "-p:RimWorldManagedDir=$RimWorldManagedDir" }
  if ($HarmonyAssemblyPath) { $arguments += "-p:HarmonyAssemblyPath=$HarmonyAssemblyPath" }
  & dotnet @arguments
  if ($LASTEXITCODE -ne 0) { throw "Build failed: $v" }
  $framework = if ($v -eq '1.6') { 'net472' } else { 'net10.0' }
  $dll = Join-Path $PSScriptRoot "Source/bin/Release/$framework/VCEFlourAlternatives.dll"
  $target = Join-Path $PSScriptRoot "VCE-Flour-Alternatives/$v/Assemblies"
  New-Item -ItemType Directory -Force -Path $target | Out-Null
  Copy-Item $dll -Destination $target -Force
}
