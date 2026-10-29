$ErrorActionPreference = 'Stop'

function Test-StoragePorts {
    foreach ($port in 10000, 10001, 10002) {
        $client = [System.Net.Sockets.TcpClient]::new()
        try {
            if (-not $client.ConnectAsync('127.0.0.1', $port).Wait(500)) { return $false }
        }
        catch { return $false }
        finally { $client.Dispose() }
    }
    return $true
}

$settings = Get-Content (Join-Path $PSScriptRoot 'local.settings.json') -Raw | ConvertFrom-Json
$func = Get-Command func -ErrorAction Stop

if ($settings.Values.AzureWebJobsStorage -eq 'UseDevelopmentStorage=true' -and -not (Test-StoragePorts)) {
    $azurite = Get-Command azurite -ErrorAction SilentlyContinue
    if (-not $azurite) {
        throw 'Azurite is required. Install it with: npm install -g azurite'
    }

    $dataPath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Azurite'
    New-Item -ItemType Directory -Path $dataPath -Force | Out-Null
    $job = Start-Job -ArgumentList $azurite.Source, $dataPath -ScriptBlock {
        param($command, $location)
        & $command --location $location
    }

    try {
        $deadline = [DateTime]::UtcNow.AddSeconds(90)
        while (-not (Test-StoragePorts)) {
            if ($job.State -in 'Completed', 'Failed', 'Stopped') {
                Receive-Job $job
                throw 'Azurite exited before its storage endpoints became available.'
            }
            if ([DateTime]::UtcNow -ge $deadline) {
                throw 'Timed out waiting for Azurite on ports 10000, 10001, and 10002.'
            }
            Start-Sleep -Milliseconds 250
        }

        Push-Location $PSScriptRoot
        try { & $func.Source start --port 7031 }
        finally { Pop-Location }
    }
    finally {
        Stop-Job $job
        Remove-Job $job
    }
}
else {
    Push-Location $PSScriptRoot
    try { & $func.Source start --port 7031 }
    finally { Pop-Location }
}

exit $LASTEXITCODE
