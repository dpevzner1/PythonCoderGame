# Python Coder Game

Python Coder Game is a Windows desktop arcade learning game for beginner Python. It teaches syntax and programming flow through slow, deliberate typing missions, visual code stacking, compile-style replays, boss debugging battles, and instructor-facing telemetry.

The project was inspired by the existing WPM-style trainer at `C:\Users\demit\Documents\Antigrav\WPMtrainer`, but that folder is a read-only reference. This app is implemented separately as a C# WinForms executable. Python curriculum structure and teaching logic were guided by the DevOpsAgent reference at `C:\Users\demit\Documents\Antigrav\DevOpsAgent`, also used as read-only reference.

## Core Game Loop

1. The player registers or logs in as an operator.
2. The dashboard loads the player profile, score, upgrades, mission history, and telemetry summary.
3. `Deploy` starts the earliest incomplete mission in sequence. If the learner skipped a mission or has a gap, Deploy loads that gap first.
4. Python code rises slowly in the center lane.
5. The player types the current line in the bottom `USER INPUT RAIL`.
6. Correct lines move into the left `PYTHON COMPILER STACK` with indentation preserved, making the code look like it is assembling in a code viewer.
7. The right panel explains the current line, the term being taught, and how the syntax is used.
8. After all lines are typed, the compile/data-flow screen plays a slow line-by-line replay of how Python scans code and how data changes.
9. The player can repeat, save/edit, continue, or exit the compile screen.

## Learning Pace

The game is intentionally slow. The goal is learning Python, not high-speed typing.

- Level speed multiplier: `0.10`.
- Scroll speed multiplier: `0.06`.
- Rising code moves at a crawl so students can read the line explanation while typing.
- Compile/data-flow replay is also deliberately slow, with visible scanning and typewriter-style runtime trace text.

## Screens

### Boot / BIOS

The app starts with a retro-cyberpunk BIOS sequence. It establishes the arcade fiction of a Python learning terminal and confirms that the keyboard matrix, tokenizer, compiler stack visualizer, mission registry, telemetry database, and audio core are online.

The boot can be skipped by pressing any key or clicking.

### Operator Registry / Login

The operator access screen supports both command-style input and keyboard shortcuts:

- `(R) Register`
- `(L) Login`
- `(O) Operators`
- `(H) Help`

Registered users are stored locally. Each user has a callsign, rank, XP, score, scrap tokens, upgrades, and persistent progress history.

### Dashboard

The dashboard is the main hub.

- `Deploy` loads the next incomplete mission in order.
- `Mission Select` shows every mission and lets completed missions be replayed.
- `Hardware Lab` opens upgrades.
- `Profile` opens telemetry dashboards and exports.
- `Music` toggles playback.
- `Next Track` advances the current music track.
- `Logout` ends the current telemetry session and returns to operator access.

The dashboard also shows high-level operator telemetry such as XP, score, completed missions, best WPM, practice minutes, accuracy trend, and concept mastery snapshot.

### Mission Select

Mission Select lists the full curriculum.

- Completed missions are color coded green.
- Boss/debug missions are color coded orange.
- Incomplete missions remain available.
- Completed missions are still selectable for replay.

This screen is for manual selection. `Deploy` is for sequence continuity.

### Active Mission

The active mission screen has four main regions.

The left panel is the `PYTHON COMPILER STACK`. Correctly entered lines stack here with syntax coloring and indentation preserved.

The center lane contains the rising target code. Normal missions show the target line with live character comparison: green for matching typed characters and red for wrong typed characters.

The bottom field is the `USER INPUT RAIL`. This is the only place the student types, keeping rising text visually separate from entered text.

The right panel is the line explainer. It shows the concept, explanation, and usage for the current line.

### Help Overlay

Typing `-help` pauses the game and opens the current mission syntax reference. It shows terms, syntax examples, explanations, and usage patterns relevant to the current level.

The help menu is framed clearly and shows `PRESS ESC TO CLOSE`.

### Compile + Data Flow Demo

At the end of each mission, the compile screen demonstrates what the code does.

The left panel performs a compiler scan. Each line is visibly consumed by a transparent scan/progress overlay while remaining readable.

The right panel shows a runtime trace. Each row explains the effect of the corresponding line:

- assignment enters memory,
- `print()` sends data to the output console,
- conditions evaluate true or false,
- branches run or skip,
- loops move through short conveyor-belt iterations,
- functions receive parameters and return values.

Console output appears in a responsive pill that expands or wraps to fit the full text instead of truncating.

Compile screen shortcuts:

- `(R) Repeat`
- `(S) Save/Edit`
- `(C) Continue` or `(C) Finish`
- `(E) Exit`

`Save/Edit` writes the mission code to `Saved Missions` in the app folder and opens it in Notepad when possible.

### Hardware Lab

The Hardware Lab is the upgrade screen. Scrap tokens can be spent on upgrade categories such as CPU, GPU, NVMe, and RAM. Upgrades affect score and play feel while preserving the slow learning-oriented pace.

### Operator Profile

The profile screen is the instructor/student analytics hub. It includes dashboard controls for:

- view selection,
- date range,
- current student vs all students,
- export format.

Available dashboard views include:

- Overview
- Concepts
- Errors
- Sessions
- Plain Tables
- Export

## Boss Battles

Boss battles are simple debugging recap rounds.

Every boss is based on the five previous learning missions. This keeps the battle educational instead of surprising the learner with unrelated syntax.

Boss battle flow:

1. Header shows `A VIRUS HAS CORRUPTED THE CODE`.
2. A corrupted orange snippet is shown.
3. The correct answer is not shown.
4. The learner types the repaired code in the input rail.
5. The virus health bar sits below the corrupted code.
6. Each corrected snippet removes one section of the virus health bar.
7. A 60-second virus timer appears in the upper right of the active code screen.
8. If the timer reaches zero, the virus wins and the boss restarts.
9. When the learner submits an incorrect repair, the game simulates a compile attempt and highlights four characters around the likely error area for three seconds.

Bosses are intentionally beginner-friendly. They focus on one repair at a time, such as:

- missing quote,
- missing colon,
- wrong boolean capitalization,
- wrong operator,
- missing parentheses,
- wrong list/dictionary syntax,
- incorrect loop keyword,
- short loop range correction.

Loop missions and boss loop snippets avoid long waits. Loop values stay small, with no loop condition exceeding the intended beginner-friendly range.

## Scoring And Feedback

Correct code entry gives arcade-style positive feedback:

- score increase,
- combo increase,
- floating `+points`,
- success audio cue.

Mistyped characters are penalized immediately, without requiring Enter. Each live typo shows a bright red arcade-style penalty and subtracts a small number of points. The learner can still correct the line and earn points for completing it correctly.

Wrong submitted lines also record an error event for telemetry and give feedback about what likely went wrong.

## Curriculum

The curriculum now uses 50 learning missions plus 10 boss recaps. Each section contains five learning missions followed by one boss battle that corrupts code from those five lessons.

The sections are:

- Section 1: code order, `print()`, comments, strings, and syntax symbols.
- Section 2: text variables, integers, floats, booleans, and `None`.
- Section 3: readable names, string joining, f-strings, math expressions, and reassignment.
- Section 4: `type()`, lists, indexes, append, and a mini inventory.
- Section 5: dictionaries, dictionary lookup, comparisons, `if`, and `if/else`.
- Section 6: equality, `elif`, `and`, `or`, and `not`.
- Section 7: `range()` loops, loop variables, list loops, accumulators, and short `while` loops.
- Section 8: defining functions, calling functions, parameters, return values, and status functions.
- Section 9: `input()`, `int()` conversion, imports, file path values, and settings dictionaries.
- Section 10: reading errors, `try/except`, simple checks, focused function design, and a final mini program.

Boss missions are generated from the five previous learning missions so each battle is a true recap.

### Day 2 Curriculum Improvements

Upcoming curriculum improvements are tracked in:

```text
MISSION_RECOMMENDATION.md
```

The Day 2 backlog preserves the current curriculum while capturing future refinements:

- review whether `type()` should move later,
- consider teaching comparisons before dictionaries,
- consider placing dictionaries after basic conditionals,
- add an explicit Python indentation mission,
- introduce `+=` only after expanded reassignment and accumulators,
- defer tuples until an intermediate follow-up,
- preserve five-level boss recap structure,
- keep loop visualizations short.

## Telemetry Capture

Telemetry is stored locally in SQLite at:

```text
%APPDATA%\PythonCoderGame\telemetry.db
```

User profile data is stored locally at:

```text
%APPDATA%\PythonCoderGame\users.json
```

Telemetry is designed for both student growth tracking and instructor review.

### Captured Tables

`sessions`

Tracks login/session engagement:

- session id,
- student id,
- start time,
- end time,
- last seen heartbeat,
- app version,
- curriculum version.

`mission_attempts`

Tracks mission-level progress:

- mission id,
- session id,
- student id,
- mission index,
- mission title,
- start and completion time,
- completion status,
- boss flag,
- help usage,
- save/edit usage,
- repeat usage,
- score,
- accuracy.

`line_attempts`

Tracks line-level learning:

- line index,
- concept,
- target code,
- typed code,
- correct/incorrect,
- first try,
- error count,
- duration,
- help before success.

`error_events`

Tracks syntax and typing error patterns:

- error type,
- expected code,
- actual typed code,
- character position,
- concept,
- timestamp.

Examples include missing colon, missing quote, missing parenthesis, wrong indent, wrong case, wrong operator, extra character, missing character, or token mismatch.

`help_events`

Tracks help usage:

- mission,
- concept,
- opened time,
- closed time,
- duration.

`compile_events`

Tracks compile screen actions:

- viewed time,
- duration,
- action taken: repeat, save/edit, continue, or exit.

`boss_attempts`

Tracks boss debugging performance:

- corrupted code,
- corrected code,
- diagnostic concept,
- first try success,
- attempts to fix,
- duration.

`understanding_events`

Tracks student self-checks when available:

- clear,
- review,
- stuck.

### Engagement Metrics

The game can report:

- total practice days,
- total sessions,
- total minutes,
- average minutes per practice day,
- sessions per day,
- minutes per day,
- missions attempted per day,
- accuracy by session/day.

### Learning Metrics

The game can report:

- overall mastery,
- syntax accuracy,
- concept mastery,
- first-try rate,
- help dependency,
- average line duration,
- error pattern frequency,
- understanding score,

Boss battles are not reported as a separate metric family. Boss repairs are normal line attempts tied to the concept being reviewed, wrong repairs are normal error events, and timer failures are captured as `timeout` errors for the active concept. This means boss performance naturally affects concept mastery, syntax accuracy, error patterns, completion, and session trends.

## Profile Dashboards

The profile view visualizes telemetry in several ways.

Overview:

- metric cards for mastery, syntax accuracy, understanding, sessions, and engagement,
- line chart for accuracy trend,
- bar chart for top concept mastery.

Concepts:

- horizontal mastery bars for syntax concepts.

Errors:

- bar chart of common error patterns.

Sessions:

- accuracy timeline,
- minutes per practice day,
- sessions per practice day.

Plain Tables:

- instructor-readable concept rows,
- attempts,
- correct count,
- errors,
- help usage,
- mastery,
- boss outcome cards.

Export:

- CSV,
- JSON,
- PDF.

Exports use the selected date range and scope. The scope can be current student or all students.

## Reports

Reports are written to:

```text
dist\Reports
```

CSV exports are useful for spreadsheet analysis. JSON exports preserve the structured telemetry snapshot. PDF exports include summary values and simple graph visuals such as bars and trends.

Available date scopes:

- 7 days,
- 30 days,
- 90 days,
- all time.

## Audio

Music is packaged into the published app under:

```text
dist\Music Resources
```

Runtime music controls are available from the bottom navigation:

- `Music`
- `Next Track`

Correct code entry and typo penalties use arcade-style sound feedback.

## Build

From the project directory:

```powershell
dotnet build -c Release
```

Publish the Windows executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\win-x64
```

The current distributable build is copied to:

```text
dist\PythonCoderGame.exe
```

The executable is self-contained for Windows x64.

## Installer / Release Pack

The Windows installer lives in:

```text
INSTALLER
```

Build the full release pack with:

```powershell
.\INSTALLER\build-installer.ps1
```

This command updates both the main game distribution and the installer media:

```text
dist\PythonCoderGame.exe
INSTALLER\Output\PythonCoderGame.Setup.exe
```

The installer is a cyberpunk arcade themed guided setup wizard. It supports fresh install, update/reinstall, clean install, and uninstall. It writes Windows registry entries under:

```text
HKLM\SOFTWARE\PythonCoderGame
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PythonCoderGame
```

Normal update/reinstall preserves student profiles and telemetry in:

```text
%APPDATA%\PythonCoderGame
```

Clean install removes that local student data only when explicitly selected.

Release rule: whenever an app update is published, run `.\INSTALLER\build-installer.ps1` so both `dist` and `INSTALLER\Output` are refreshed together.

## Important Project Boundaries

Do not modify these reference projects:

```text
C:\Users\demit\Documents\Antigrav\WPMtrainer
C:\Users\demit\Documents\Antigrav\DevOpsAgent
C:\Users\demit\Documents\Antigrav\NetworkMonitorTool
```

This project owns the implementation in:

```text
C:\Users\demit\Documents\Antigrav\PythonCoderGame
```
