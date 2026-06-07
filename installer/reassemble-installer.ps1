$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Join-Parts {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath
    )

    $directory = Split-Path -Parent $BasePath
    $name = Split-Path -Leaf $BasePath
    $parts = Get-ChildItem -LiteralPath $directory -Filter "$name.part*" | Sort-Object Name

    if ($parts.Count -eq 0) {
        Write-Host "No parts found for $name; skipping."
        return
    }

    if (Test-Path $BasePath) {
        Remove-Item -LiteralPath $BasePath -Force
    }

    $target = [System.IO.File]::Create($BasePath)
    try {
        foreach ($part in $parts) {
            Write-Host "Appending $($part.Name)..."
            $source = [System.IO.File]::OpenRead($part.FullName)
            try {
                $source.CopyTo($target)
            }
            finally {
                $source.Dispose()
            }
        }
    }
    finally {
        $target.Dispose()
    }

    $hash = Get-FileHash -LiteralPath $BasePath -Algorithm SHA256
    Write-Host ""
    Write-Host "Reassembled: $BasePath"
    Write-Host "SHA256: $($hash.Hash)"
    Write-Host ""
}

Join-Parts (Join-Path $scriptRoot "PythonCoderGame.Setup.exe")
Join-Parts (Join-Path $scriptRoot "PythonCoderGamePayload.zip")
Join-Parts (Join-Path $scriptRoot "portable-dist\PythonCoderGame.exe")
