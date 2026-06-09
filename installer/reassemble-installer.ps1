$ErrorActionPreference = "Stop"

function Join-Parts([string]$BaseName, [string]$Directory) {
    $parts = Get-ChildItem -LiteralPath $Directory -Filter "$BaseName.part*" | Sort-Object Name
    if (-not $parts) {
        return
    }

    $target = Join-Path $Directory $BaseName
    if (Test-Path $target) {
        Remove-Item -LiteralPath $target -Force
    }

    $out = [IO.File]::Create($target)
    try {
        foreach ($part in $parts) {
            $in = [IO.File]::OpenRead($part.FullName)
            try {
                $in.CopyTo($out)
            }
            finally {
                $in.Dispose()
            }
        }
    }
    finally {
        $out.Dispose()
    }

    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash
    Write-Host "$BaseName SHA256 $hash"
}

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Join-Parts "PythonCoderGame.Setup.exe" $here
Join-Parts "PythonCoderGamePayload.zip" $here
Join-Parts "PythonCoderGame.exe" (Join-Path $here "portable-dist")
