#Requires -Version 5.1
<#
.SYNOPSIS
  Repo doğrulama: GameCompile, CoreTests, AtomSim, SweepV2 kapısı.
.PARAMETER Skip
  Virgülle ayrılmış adımlar: gamecompile, unitycompile, coretests, integration, atomsim, sweep
.PARAMETER Quick
  Sweep adımını atla.
#>
param(
    [string[]] $Skip = @(),
    [switch] $Quick
)

$ErrorActionPreference = 'Continue'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$OutDir = Join-Path $RepoRoot 'tools\verify-out'
if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}

function Test-StepSkipped {
    param([string] $Name)
    foreach ($s in $Skip) {
        if ($s.Trim().ToLowerInvariant() -eq $Name) { return $true }
    }
    return $false
}

function Get-PythonCommand {
    $py = Get-Command python -ErrorAction SilentlyContinue
    if ($py) { return 'python' }
    $py3 = Get-Command python3 -ErrorAction SilentlyContinue
    if ($py3) { return 'python3' }
    return $null
}

function Invoke-VerifyStep {
    param(
        [string] $Key,
        [string] $Title,
        [scriptblock] $Run,
        [scriptblock] $NoteFromLog
    )
    $logPath = Join-Path $OutDir "$Key.log"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $status = 'PASS'
    $note = ''
    try {
        $text = (& $Run *>&1 | Out-String)
        $exit = $LASTEXITCODE
        if ($null -eq $exit) { $exit = 0 }
        [System.IO.File]::WriteAllText($logPath, $text, (New-Object System.Text.UTF8Encoding $false))
        if ($exit -ne 0) { $status = 'FAIL' }
        if ($NoteFromLog) {
            $n = & $NoteFromLog $text $exit
            if ($n) {
                $note = $n
                if ($n -eq 'kapı satırı bulunamadı') { $status = 'FAIL' }
                if ($n -eq 'SKIP') { $status = 'SKIP' }
            }
        }
    } catch {
        $status = 'FAIL'
        $note = $_.Exception.Message
        [System.IO.File]::WriteAllText($logPath, $note, (New-Object System.Text.UTF8Encoding $false))
    }
    $sw.Stop()
    return [pscustomobject]@{
        Status = $status
        Step   = $Title
        Sec    = [math]::Round($sw.Elapsed.TotalSeconds, 1)
        Note   = $note
    }
}

Push-Location $RepoRoot
$results = @()

# (a) GameCompile
if (-not (Test-StepSkipped 'gamecompile')) {
    $py = Get-PythonCommand
    if (-not $py) {
        $results += [pscustomobject]@{ Status = 'FAIL'; Step = 'GameCompile'; Sec = 0; Note = 'python bulunamadı' }
    } else {
        $results += Invoke-VerifyStep -Key 'gamecompile' -Title 'GameCompile' -Run {
            & $py (Join-Path $RepoRoot 'tools\GameCompile\check.py')
        } -NoteFromLog {
            param($t, $code)
            if ($code -ne 0) { return "exit $code" }
            return 'derleme OK'
        }
    }
}

# (a2) UnityCompile (yerel Unity 6; SKIP CI'da değil)
if (-not (Test-StepSkipped 'unitycompile')) {
    $results += Invoke-VerifyStep -Key 'unitycompile' -Title 'UnityCompile' -Run {
        powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot 'tools\UnityCompile\check.ps1')
    } -NoteFromLog {
        param($t, $code)
        if ($t -match 'SKIP:\s*Unity 6 bulunamadı') { return 'SKIP' }
        if ($code -ne 0) { return "exit $code" }
        if ($t -match 'unity scripts compiled') { return ($t.Trim() -split "`n" | Select-Object -Last 1).Trim() }
        return 'derleme OK'
    }
}

# (b) CoreTests
if (-not (Test-StepSkipped 'coretests')) {
    $results += Invoke-VerifyStep -Key 'coretests' -Title 'CoreTests' -Run {
        dotnet test (Join-Path $RepoRoot 'tools\CoreTests') --nologo -v q
    } -NoteFromLog {
        param($t, $code)
        $passed = 0
        $failed = 0
        if ($t -match 'Passed!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+)') {
            $failed = [int]$Matches[1]
            $passed = [int]$Matches[2]
        } elseif ($t -match 'Başarısız:\s*(\d+),\s*Başarılı:\s*(\d+)') {
            $failed = [int]$Matches[1]
            $passed = [int]$Matches[2]
        } elseif ($t -match 'Toplam:\s*(\d+)') {
            $passed = [int]$Matches[1]
            if ($t -match 'Başarısız:\s*(\d+)') { $failed = [int]$Matches[1] }
            elseif ($t -match 'Failed:\s*(\d+)') { $failed = [int]$Matches[1] }
        } elseif ($t -match 'Passed:\s*(\d+).*Failed:\s*(\d+)') {
            $passed = [int]$Matches[1]
            $failed = [int]$Matches[2]
        }
        $sum = "Passed $passed, Failed $failed"
        if ($code -ne 0) { return "$sum (exit $code)" }
        return $sum
    }
}

# (c) IntegrationTests
if (-not (Test-StepSkipped 'integration')) {
    $results += Invoke-VerifyStep -Key 'integration' -Title 'IntegrationTests' -Run {
        dotnet test (Join-Path $RepoRoot 'tools\IntegrationTests') --nologo -v q
    } -NoteFromLog {
        param($t, $code)
        $passed = 0
        $failed = 0
        if ($t -match 'Passed!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+)') {
            $failed = [int]$Matches[1]
            $passed = [int]$Matches[2]
        } elseif ($t -match 'Başarısız:\s*(\d+),\s*Başarılı:\s*(\d+)') {
            $failed = [int]$Matches[1]
            $passed = [int]$Matches[2]
        } elseif ($t -match 'Toplam:\s*(\d+)') {
            $passed = [int]$Matches[1]
            if ($t -match 'Başarısız:\s*(\d+)') { $failed = [int]$Matches[1] }
            elseif ($t -match 'Failed:\s*(\d+)') { $failed = [int]$Matches[1] }
        } elseif ($t -match 'Passed:\s*(\d+).*Failed:\s*(\d+)') {
            $passed = [int]$Matches[1]
            $failed = [int]$Matches[2]
        }
        $sum = "Passed $passed, Failed $failed"
        if ($code -ne 0) { return "$sum (exit $code)" }
        return $sum
    }
}

# (d) AtomSim
if (-not (Test-StepSkipped 'atomsim')) {
    $results += Invoke-VerifyStep -Key 'atomsim' -Title 'AtomSim' -Run {
        dotnet run --project (Join-Path $RepoRoot 'tools\AtomSim')
    } -NoteFromLog {
        param($t, $code)
        if ($code -ne 0) { return "exit $code" }
        $literal = ($t -match '0 hata')
        $gate = ($t -match '(?m)^A\s+[^:]+:\s*0\s*$') -and ($t -match '(?m)^B\s+.*imza grubu:\s*0\s*$') -and ($t -match '(?m)^D\s+[^:]+:\s*0\s*$')
        if ($literal) { return '0 hata' }
        if ($gate) { return 'gramer kapı (A/B/D=0)' }
        return 'kapı satırı bulunamadı'
    }
}

# (e) SweepV2
$runSweep = (-not $Quick) -and (-not (Test-StepSkipped 'sweep'))
if ($runSweep) {
    $sweepOut = Join-Path $OutDir 'sweep'
    $results += Invoke-VerifyStep -Key 'sweep' -Title 'SweepV2' -Run {
        dotnet run -c Release --project (Join-Path $RepoRoot 'tools\SweepV2') -- `
            --all --gate --quiet `
            --compare (Join-Path $RepoRoot 'docs\play-sweep\pr35-final-4x.csv') `
            --out $sweepOut `
            --label verify
    } -NoteFromLog {
        param($t, $code)
        $ozet = Join-Path $sweepOut 'verify-ozet.md'
        $line = ''
        if (Test-Path $ozet) {
            $lines = Get-Content -Path $ozet -Encoding UTF8
            foreach ($l in $lines) {
                if ($l -match 'toplam|Toplam|1440|/\s*144') { $line = $l.Trim(); break }
            }
            if (-not $line -and $lines.Count -gt 0) {
                $line = ($lines | Select-Object -Last 3) -join ' | '
            }
        }
        if (-not $line) { $line = "exit $code" }
        if ($code -ne 0) { return "FAIL $line" }
        return $line
    }
}

Pop-Location

Write-Host ''
Write-Host 'VERIFY ÖZET'
Write-Host ('{0,-6} {1,-14} {2,6}  {3}' -f 'DURUM', 'ADIM', 'SN', 'NOT')
foreach ($r in $results) {
    Write-Host ('{0,-6} {1,-14} {2,6}  {3}' -f $r.Status, $r.Step, $r.Sec, $r.Note)
}

$anyFail = $false
foreach ($r in $results) {
    if ($r.Status -eq 'FAIL') { $anyFail = $true; break }
}
if ($anyFail) { exit 1 }
exit 0
