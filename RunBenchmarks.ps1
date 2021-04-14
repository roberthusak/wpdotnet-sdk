param (
    [string[]] $configs,
    [string] $logFile = "benchmarks.log",
    [switch] $build = $false,
    [switch] $benchmarks = $false,
    [switch] $stats = $false
)

$logFile = [System.IO.Path]::GetFullPath($logFile)
$expectedOutput = Get-Content ".\Expected.html"

function CheckProofFiles() {
    foreach ($config in $configs) {
        $configOutput = Get-Content "./proofs/$config.html"
        if (Compare-Object -ReferenceObject $expectedOutput -DifferenceObject $configOutput) {
            Write-Output "Wrong output of configuration $config!" | Tee-Object $logFile -Append
        }
    }
}

if (Test-Path $logFile) {
    Clear-Content $logFile
}

if ($build) {
    Write-Output "Building configurations:" | Tee-Object $logFile -Append
    Push-Location "wordpress"
    
    $success = $True
    foreach ($config in $configs) {
        Write-Output $config | Tee-Object $logFile -Append
        & dotnet build -c $config /p:IsPackable=false | Out-File $logFile -Append
        if ($?) {
            Write-Output "OK"
        } else {
            $success = $False
            Write-Output "Error"
        }
    }

    Pop-Location

    if (-not $success) {
        exit
    }
}

if ($benchmarks) {
    Write-Output "Benchmarking:" | Tee-Object $logFile -Append
    Push-Location "PeachPied.WordPress.Benchmarks"

    & dotnet build -c Release --no-dependencies | Out-File $logFile -Append

    $filters = $configs | ForEach-Object { "*." + $_ }
    & dotnet run -c Release --no-build -- --join --filter $filters

    CheckProofFiles

    Pop-Location
}

if ($stats) {
    Write-Output "Measuring statistics:" | Tee-Object $logFile -Append

    Push-Location "PeachPied.WordPress.StatsRunner"
    & dotnet build -c Release --no-dependencies | Out-File $logFile -Append
    Pop-Location    

    Push-Location "PeachPied.WordPress.Stats"
    $statsLogFile = "results/stats_" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log"

    & dotnet build -c Release --no-dependencies | Out-File $logFile -Append
    & dotnet run -c Release --no-build -- $configs | Tee-Object $statsLogFile -Append | Tee-Object $logFile -Append

    CheckProofFiles

    Pop-Location
}
