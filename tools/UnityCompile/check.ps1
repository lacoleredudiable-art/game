#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')).Path
$Proj = Join-Path $RepoRoot 'tools\UnityCompile\UnityCompile.csproj'

$unityData = $env:UNITY_EDITOR_DATA
if (-not $unityData) {
    $unityData = 'C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Data'
}

if (-not (Test-Path (Join-Path $unityData 'Managed\UnityEngine\UnityEngine.dll'))) {
    Write-Host 'SKIP: Unity 6 bulunamadı'
    exit 0
}

Push-Location $RepoRoot
$build = dotnet build $Proj --nologo -v q 2>&1 | Out-String
$exit = $LASTEXITCODE
Pop-Location

if ($exit -eq 0) {
    $count = (Get-ChildItem -Recurse (Join-Path $RepoRoot 'unity\Assets\Scripts') -Filter '*.cs').Count
    Write-Host "unity scripts compiled ($count files)"
    exit 0
}

$errors = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($line in $build.Split([char[]]@("`n", "`r"), [StringSplitOptions]::RemoveEmptyEntries)) {
    if ($line -match 'error CS') {
        $trim = $line.Trim()
        [void]$errors.Add($trim)
    }
}

foreach ($e in ($errors | Sort-Object)) {
    Write-Host $e
}
exit 1
