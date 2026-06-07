# Telemetry And Reporting Plan

The app uses local SQLite telemetry at:

```text
%APPDATA%\PythonCoderGame\telemetry.db
```

The model follows a DevOps-style observability pipeline:

```text
raw learning events -> session/mission summaries -> dashboard views -> filtered exports
```

## Captured Data

- sessions
- mission attempts
- line attempts
- error classifications
- boss/debug attempts
- compile-screen actions
- save/edit and repeat actions

## Profile Dashboards

The current user profile exposes switchable dashboard tabs:

- Overview
- Concepts
- Errors
- Sessions
- Export

The date range can be switched between:

- 7 days
- 30 days
- 90 days

## Meaningful Graphs

- Metric cards for mastery, syntax accuracy, boss first-try rate, and practice days.
- Line chart for accuracy growth across sessions.
- Horizontal bar charts for concept mastery.
- Horizontal bar charts for top error patterns.
- Session bar chart for missions per practice day.

## Export Formats

Exports are generated from the selected current profile and date range:

- CSV: concept mastery spreadsheet
- JSON: full structured telemetry snapshot
- PDF: instructor-readable report with text summary and concept mastery bars

Reports are saved beside the executable:

```text
Reports\
```

## Instructor-Grade Improvements To Add Next

- Class/group selector.
- Student roster dashboard.
- Class concept heatmap.
- Prediction-question telemetry.
- Export report bundle as ZIP.
- PDF charts for error trends and session timelines.
