$ErrorActionPreference = "Stop"

$projectDll = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\bin\Debug\net10.0\quan-ly-muon-tra-sach-thu-vien.dll"))

$runningInstances = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
    Where-Object {
        $_.CommandLine -and
        $_.CommandLine.IndexOf($projectDll, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    }

foreach ($instance in $runningInstances) {
    Stop-Process -Id $instance.ProcessId -Force -ErrorAction Stop
}

$timeout = [System.Diagnostics.Stopwatch]::StartNew()
do {
    $remainingInstances = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
        Where-Object {
            $_.CommandLine -and
            $_.CommandLine.IndexOf($projectDll, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        }

    if (-not $remainingInstances) {
        exit 0
    }

    Start-Sleep -Milliseconds 100
} while ($timeout.Elapsed.TotalSeconds -lt 5)

throw "Ứng dụng vẫn đang chạy; không thể build an toàn: $projectDll"
