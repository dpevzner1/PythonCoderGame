# Python Coder Game GitHub Export

This folder is a GitHub-ready export of Python Coder Game.

## Layout

```text
source
installer
SHA256SUMS.csv
```

- `source` contains the open source C# WinForms game source, curriculum, telemetry code, music assets, installer source, documentation, and MIT license.
- `installer` contains the installable Windows package and a portable `dist` copy.
- `SHA256SUMS.csv` contains SHA-256 hashes and sizes for integrity checks.

## File Size Rule

Every file in this export is kept below 200 MB for smoother upload.

Large binaries are split into parts so the export remains friendly for GitHub-style uploads:

```text
installer\PythonCoderGame.Setup.exe.part001
installer\PythonCoderGame.Setup.exe.part002
installer\PythonCoderGamePayload.zip.part001
installer\portable-dist\PythonCoderGame.exe.part001
```

Reassemble the installer, embedded payload zip, and portable EXE with:

```powershell
.\installer\reassemble-installer.ps1
```

That produces the original package files:

```text
installer\PythonCoderGame.Setup.exe
installer\PythonCoderGamePayload.zip
installer\portable-dist\PythonCoderGame.exe
```

## Source License

The source is provided under the MIT License. See:

```text
source\LICENSE
```

## Build From Source

From `source`:

```powershell
dotnet build -c Release
```

Build the app and installer release pack from `source`:

```powershell
.\INSTALLER\build-installer.ps1
```

## Installer Package

Installer contents live in:

```text
installer
```

The folder includes:

- split setup executable parts,
- split embedded installer payload zip parts,
- setup PDB,
- portable `dist` folder with split app EXE parts,
- reassembly script,
- installer package notes.

Normal installer update/reinstall preserves local student data in `%APPDATA%\PythonCoderGame`. Clean install removes local student data only when selected in the setup wizard.
