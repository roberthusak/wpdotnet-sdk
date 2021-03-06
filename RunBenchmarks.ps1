param (
    [string[]] $configs,
    [string] $logFile = "benchmarks.log",
    [switch] $build = $false,
    [switch] $trace = $false,
    [switch] $benchmarks = $false,
    [switch] $stats = $false,
    [switch] $buildPeachpie = $false,
    [string] $peachpiePath = $null,
    [string] $peachpieConfig = "Release",
    [ValidateSet("wordpress", "bench")][string] $targetProject = "wordpress"
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

if ($buildPeachpie) {
    if (-not $peachpiePath) {
        Write-Output "Specify -peachpiePath to compile Peachpie"
        exit
    }

    Push-Location $peachpiePath

    Write-Output "Building Peachpie and updating packages..." | Tee-Object $logFile -Append

    Remove-Item .nugs/*.nupkg
    Remove-Item .nugs/*.snupkg

    & dotnet build -c $peachpieConfig | Out-File $logFile -Append
    if (-not $?) {
        Write-Output "Error compiling Peachpie"
        exit
    }

    & build/update-cache.ps1 | Out-File $logFile -Append
    if (-not $?) {
        Write-Output "Error updating Peachpie packages"
        exit
    }

    Write-Output "OK"

    Pop-Location
}

if ($build) {
    Write-Output "Building configurations:" | Tee-Object $logFile -Append
    Push-Location $targetProject
    
    $success = $True
    foreach ($config in $configs) {
        Write-Output $config | Tee-Object $logFile -Append
        & dotnet build -c $config /p:IsPackable=false /p:EnableCallCounting=$stats /p:EnableCallTracing=$trace | Out-File $logFile -Append
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

    if ($targetProject -eq "wordpress") {
        Push-Location "PeachPied.WordPress.Benchmarks"

        & dotnet build -c Release --no-dependencies | Out-File $logFile -Append

        $filters = @()
        if ($configs.Contains("Release")) {
            $filters += "*.Release"
        }
        $optimizationConfigs = ($configs | Where-Object { $_ -ne "Release"})
        if ($optimizationConfigs.Count -gt 0) {
            $filters += "*.Optimization"
            $env:WpBenchmark_Optimizations = $optimizationConfigs
        }

        & dotnet run -c Release --no-build -- --join --filter $filters

        CheckProofFiles

        Pop-Location
    } elseif ($targetProject -eq "bench") {
        Push-Location "StatsCommon"
        & dotnet build -c Release --no-dependencies | Out-File $logFile -Append
        Pop-Location   

        Push-Location "PeachPied.PhpBenchmarks.Runner"

        & dotnet build -c Release --no-dependencies | Out-File $logFile -Append

        New-Item -ItemType "directory" -Name "results" -Force | Out-Null
        $benchLogFile = "results/bench_" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log"
        & dotnet run -c Release --no-build -- $configs | Tee-Object $benchLogFile | Tee-Object $logFile -Append

        Pop-Location
    }
}

if ($stats) {
    if ($targetProject -ne "wordpress") {
        Write-Output "Stats are only supported for WordPress"
        exit
    }

    Write-Output "Measuring statistics:" | Tee-Object $logFile -Append

    Push-Location "StatsCommon"
    & dotnet build -c Release --no-dependencies | Out-File $logFile -Append
    Pop-Location    

    Push-Location "PeachPied.WordPress.StatsRunner"
    & dotnet build -c Release --no-dependencies | Out-File $logFile -Append
    Pop-Location    

    Push-Location "PeachPied.WordPress.Stats"
    $statsLogFile = "results/stats_" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log"

    & dotnet build -c Release --no-dependencies | Out-File $logFile -Append
    $flags = If ($trace) { @("-trace") } else { @() }
    & dotnet run -c Release --no-build -- $flags $configs | Tee-Object $statsLogFile | Tee-Object $logFile -Append

    CheckProofFiles

    Pop-Location
}
