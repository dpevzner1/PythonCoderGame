# Python Coder Game Installer Harness

This folder builds the guided Windows setup executable for Python Coder Game.

The installer follows the same broad pattern used by the reference projects:

- a custom .NET Windows setup wizard,
- embedded application payload,
- guided install/update/reinstall/uninstall flow,
- Windows registry registration,
- Start Menu and desktop shortcuts,
- Add/Remove Programs uninstall entry,
- a clean-install option that can remove local AppData when explicitly selected.

Reference projects are read-only:

```text
C:\Users\demit\Documents\Antigrav\WPMtrainer
C:\Users\demit\Documents\Antigrav\NetworkMonitorTool
```

Do not edit those folders when maintaining this installer.

## Build The Installer

From the project root:

```powershell
.\INSTALLER\build-installer.ps1
```

The script performs the complete release pack:

1. Stops any running `PythonCoderGame` process.
2. Publishes the Windows x64 self-contained game.
3. Rebuilds the project-level `dist` folder.
4. Creates `INSTALLER\Payload\PythonCoderGamePayload.zip`.
5. Embeds that payload into the setup wizard.
6. Publishes the installer to `INSTALLER\Output`.

Final outputs:

```text
dist\PythonCoderGame.exe
INSTALLER\Output\PythonCoderGame.Setup.exe
```

## Installer Behavior

The installer requires administrator privileges because it writes to HKLM.

Registry keys:

```text
HKLM\SOFTWARE\PythonCoderGame
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PythonCoderGame
```

Default install location:

```text
C:\Program Files\Python Coder Game
```

Local student profile and telemetry data is stored separately:

```text
%APPDATA%\PythonCoderGame
```

Normal update/reinstall preserves student data. Clean install removes student data only when explicitly selected.

## Supported Operations

- Fresh install: installs files, shortcuts, registry keys, and uninstaller.
- Update/reinstall: overwrites application files and keeps student data.
- Clean install: overwrites application files and removes `%APPDATA%\PythonCoderGame`.
- Uninstall: removes shortcuts, registry keys, and install files.

During install, the setup executable copies itself into the install directory as:

```text
PythonCoderGame.Setup.exe
```

Windows uses that copy for Apps & Features uninstall.

## Release Rule

Whenever publishing an app update, update both:

```text
C:\Users\demit\Documents\Antigrav\PythonCoderGame\dist
C:\Users\demit\Documents\Antigrav\PythonCoderGame\INSTALLER\Output
```

Use `.\INSTALLER\build-installer.ps1` so the game executable, music resources, embedded payload, and installer all stay in sync.
