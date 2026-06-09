# Python Coder Game GitHub Export

This folder is a GitHub-ready export of Python Coder Game.

## Layout

```text
source
installer
SHA256SUMS.csv
```

- `source` contains the open source C# WinForms game source, curriculum, telemetry code, music/assets, installer source, documentation, and MIT license.
- `installer` contains the installable Windows package and a portable `dist` copy.
- `SHA256SUMS.csv` contains SHA-256 hashes and sizes for integrity checks.

## File Size Rule

Every file in this export is kept below 200 MB for smoother upload. Large binaries are split into `.part###` files.

Reassemble the installer, embedded payload zip, and portable EXE with:

```powershell
.\installer\reassemble-installer.ps1
```

## Build From Source

From `source`:

```powershell
dotnet build -c Release
.\INSTALLER\build-installer.ps1
```

The source is provided under the MIT License. See `source\LICENSE`.
