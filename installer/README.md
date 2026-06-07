# Installer Package

This folder contains the GitHub-safe installer package for Python Coder Game.

## Reassemble Setup EXE

Large package files are split into parts so every file remains below the GitHub export cap.

Run:

```powershell
.\reassemble-installer.ps1
```

Expected output:

```text
PythonCoderGame.Setup.exe
PythonCoderGamePayload.zip
portable-dist\PythonCoderGame.exe
```

Then launch `PythonCoderGame.Setup.exe` to install, update/reinstall, clean install, or uninstall.

## Portable App

The `portable-dist` folder contains the published app directly:

```text
portable-dist\PythonCoderGame.exe
```

Run `reassemble-installer.ps1` first to rebuild the portable executable from its parts. This is useful for quick testing without running the installer.

## Integrity

Use the export-level `SHA256SUMS.csv` to verify all files.

After reassembly, the script prints the SHA-256 hash of `PythonCoderGame.Setup.exe` so it can be compared with the source installer if needed.
