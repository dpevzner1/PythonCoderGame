# Installer Package

This folder contains the GitHub-safe installer package for Python Coder Game.

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

The portable app can also be rebuilt from `portable-dist\PythonCoderGame.exe.part*` for quick testing without running the installer.
