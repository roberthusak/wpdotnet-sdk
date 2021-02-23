param (
    [bool] $build,
    [string[]] $configs,
    [string] $logFile = "benchmarks.log"
)

if ($build) {
    Write-Output "Building configurations:"
    Push-Location "wordpress"
    
    $success = $True
    Clear-Content $logFile
    foreach ($config in $configs) {
        Write-Output $config
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

Write-Output "Benchmarking"
Push-Location "PeachPied.WordPress.Benchmarks"

& dotnet build -c Release --no-dependencies

$filters = $configs | ForEach-Object { "*." + $_ }
& dotnet run -c Release --no-build -- --join --filter $filters

Pop-Location
