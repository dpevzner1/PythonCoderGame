using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace PythonCoderGame;

internal sealed record ConceptMetric(string Concept, int Attempts, int Correct, int FirstTry, int Errors, int HelpUses, double AvgDurationMs)
{
    public double Mastery => Attempts == 0 ? 0 : Math.Round((Correct * 0.45 + FirstTry * 0.30 + Math.Max(0, Attempts - HelpUses) * 0.15 + Math.Max(0, Attempts - Errors) * 0.10) / Attempts * 100, 1);
}

internal sealed record ErrorMetric(string ErrorType, int Count);
internal sealed record SessionMetric(DateTime StartedUtc, int Sessions, int Missions, double Minutes, double Accuracy, int Errors);
internal sealed record UnderstandingMetric(int Clear, int Review, int Stuck)
{
    public int Total => Clear + Review + Stuck;
    public double Score => Total == 0 ? 0 : Math.Round((Clear * 100.0 + Review * 55.0 + Stuck * 20.0) / Total, 1);
}

internal sealed class TelemetrySnapshot
{
    public string Callsign { get; init; } = "";
    public DateTime FromUtc { get; init; }
    public DateTime ToUtc { get; init; }
    public IReadOnlyList<ConceptMetric> Concepts { get; init; } = [];
    public IReadOnlyList<ErrorMetric> Errors { get; init; } = [];
    public IReadOnlyList<SessionMetric> Sessions { get; init; } = [];
    public UnderstandingMetric Understanding { get; init; } = new(0, 0, 0);
    public int EngagementDays => Sessions.Count(s => s.Sessions > 0);
    public int EngagementSessions => Sessions.Sum(s => s.Sessions);
    public double EngagementMinutes => Math.Round(Sessions.Sum(s => s.Minutes), 1);
    public double AverageMinutesPerDay => EngagementDays == 0 ? 0 : Math.Round(EngagementMinutes / EngagementDays, 1);
    public double OverallMastery => Concepts.Count == 0 ? 0 : Math.Round(Concepts.Average(c => c.Mastery), 1);
    public double SyntaxAccuracy => Concepts.Sum(c => c.Attempts) == 0 ? 100 : Math.Round(Concepts.Sum(c => c.Correct) * 100.0 / Concepts.Sum(c => c.Attempts), 1);
}

internal static class TelemetryStore
{
    private static readonly string StoreDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PythonCoderGame");
    private static readonly string DbPath = Path.Combine(StoreDir, "telemetry.db");

    public static void Initialize()
    {
        Directory.CreateDirectory(StoreDir);
        using var db = Open();
        Execute(db, """
            CREATE TABLE IF NOT EXISTS sessions (
                session_id TEXT PRIMARY KEY,
                student_id TEXT NOT NULL,
                started_utc TEXT NOT NULL,
                ended_utc TEXT,
                last_seen_utc TEXT,
                app_version TEXT,
                curriculum_version TEXT
            );
            CREATE TABLE IF NOT EXISTS mission_attempts (
                mission_attempt_id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL,
                student_id TEXT NOT NULL,
                mission_index INTEGER NOT NULL,
                mission_title TEXT NOT NULL,
                started_utc TEXT NOT NULL,
                completed_utc TEXT,
                completed INTEGER NOT NULL DEFAULT 0,
                is_boss INTEGER NOT NULL DEFAULT 0,
                used_help INTEGER NOT NULL DEFAULT 0,
                used_save_edit INTEGER NOT NULL DEFAULT 0,
                repeated INTEGER NOT NULL DEFAULT 0,
                score INTEGER NOT NULL DEFAULT 0,
                accuracy REAL NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS line_attempts (
                line_attempt_id TEXT PRIMARY KEY,
                mission_attempt_id TEXT NOT NULL,
                student_id TEXT NOT NULL,
                mission_index INTEGER NOT NULL,
                line_index INTEGER NOT NULL,
                concept TEXT NOT NULL,
                target_code TEXT NOT NULL,
                typed_code TEXT NOT NULL,
                correct INTEGER NOT NULL,
                first_try INTEGER NOT NULL,
                error_count INTEGER NOT NULL,
                duration_ms INTEGER NOT NULL,
                used_help_before_success INTEGER NOT NULL,
                timestamp_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS error_events (
                error_id TEXT PRIMARY KEY,
                line_attempt_id TEXT NOT NULL,
                student_id TEXT NOT NULL,
                mission_index INTEGER NOT NULL,
                concept TEXT NOT NULL,
                error_type TEXT NOT NULL,
                expected TEXT,
                actual TEXT,
                character_position INTEGER,
                timestamp_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS help_events (
                help_id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL,
                student_id TEXT NOT NULL,
                mission_index INTEGER NOT NULL,
                concept TEXT NOT NULL,
                opened_utc TEXT NOT NULL,
                closed_utc TEXT,
                duration_ms INTEGER
            );
            CREATE TABLE IF NOT EXISTS compile_events (
                compile_event_id TEXT PRIMARY KEY,
                mission_attempt_id TEXT NOT NULL,
                student_id TEXT NOT NULL,
                mission_index INTEGER NOT NULL,
                viewed_utc TEXT NOT NULL,
                duration_ms INTEGER,
                action_taken TEXT
            );
            CREATE TABLE IF NOT EXISTS boss_attempts (
                boss_attempt_id TEXT PRIMARY KEY,
                mission_attempt_id TEXT NOT NULL,
                student_id TEXT NOT NULL,
                mission_index INTEGER NOT NULL,
                corrupted_code TEXT NOT NULL,
                corrected_code TEXT NOT NULL,
                diagnostic_concept TEXT NOT NULL,
                fixed_first_try INTEGER NOT NULL,
                attempts_to_fix INTEGER NOT NULL,
                duration_ms INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS understanding_events (
                understanding_event_id TEXT PRIMARY KEY,
                mission_attempt_id TEXT NOT NULL,
                student_id TEXT NOT NULL,
                mission_index INTEGER NOT NULL,
                concept TEXT NOT NULL,
                rating TEXT NOT NULL,
                timestamp_utc TEXT NOT NULL
            );
            """);
        AddColumnIfMissing(db, "sessions", "last_seen_utc", "TEXT");
    }

    public static string StartSession(string studentId)
    {
        Initialize();
        var id = Guid.NewGuid().ToString("N");
        using var db = Open();
        var now = Now();
        Execute(db, """
            INSERT INTO sessions (session_id,student_id,started_utc,ended_utc,last_seen_utc,app_version,curriculum_version)
            VALUES ($id,$student,$start,NULL,$seen,$app,$curr)
            """, ("$id", id), ("$student", studentId), ("$start", now), ("$seen", now), ("$app", Application.ProductVersion), ("$curr", "python-coder-v1"));
        return id;
    }

    public static void TouchSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        using var db = Open();
        Execute(db, "UPDATE sessions SET last_seen_utc=$seen WHERE session_id=$id AND ended_utc IS NULL", ("$seen", Now()), ("$id", sessionId));
    }

    public static void EndSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        using var db = Open();
        var now = Now();
        Execute(db, "UPDATE sessions SET ended_utc=$end,last_seen_utc=$end WHERE session_id=$id AND ended_utc IS NULL", ("$end", now), ("$id", sessionId));
    }

    public static string StartMission(string sessionId, string studentId, int missionIndex, Lesson lesson)
    {
        var id = Guid.NewGuid().ToString("N");
        using var db = Open();
        Execute(db, """
            INSERT INTO mission_attempts
            (mission_attempt_id,session_id,student_id,mission_index,mission_title,started_utc,is_boss)
            VALUES ($id,$session,$student,$idx,$title,$start,$boss)
            """, ("$id", id), ("$session", sessionId), ("$student", studentId), ("$idx", missionIndex), ("$title", lesson.Title), ("$start", Now()), ("$boss", lesson.IsBoss ? 1 : 0));
        return id;
    }

    public static void CompleteMission(string missionAttemptId, int score, double accuracy)
    {
        using var db = Open();
        Execute(db, "UPDATE mission_attempts SET completed=1, completed_utc=$end, score=$score, accuracy=$acc WHERE mission_attempt_id=$id",
            ("$end", Now()), ("$score", score), ("$acc", accuracy), ("$id", missionAttemptId));
    }

    public static HashSet<int> CompletedMissionIndexes(string studentId)
    {
        Initialize();
        using var db = Open();
        return Query(db, """
            SELECT DISTINCT mission_index
            FROM mission_attempts
            WHERE student_id=$student AND completed=1
            """, r => AsInt(r, 0), ("$student", studentId)).ToHashSet();
    }

    public static void MarkMissionFlag(string missionAttemptId, string flag)
    {
        if (flag is not ("used_help" or "used_save_edit" or "repeated")) return;
        using var db = Open();
        Execute(db, $"UPDATE mission_attempts SET {flag}=1 WHERE mission_attempt_id=$id", ("$id", missionAttemptId));
    }

    public static void RecordLine(string missionAttemptId, string studentId, int missionIndex, int lineIndex, CodeLine line, string typed, bool correct, bool firstTry, int durationMs, bool usedHelp)
    {
        var lineId = Guid.NewGuid().ToString("N");
        var error = correct ? "" : ClassifyError(typed, line.Text);
        using var db = Open();
        Execute(db, """
            INSERT INTO line_attempts VALUES
            ($id,$mission,$student,$missionIndex,$lineIndex,$concept,$target,$typed,$correct,$firstTry,$errors,$duration,$help,$ts)
            """, ("$id", lineId), ("$mission", missionAttemptId), ("$student", studentId), ("$missionIndex", missionIndex), ("$lineIndex", lineIndex),
            ("$concept", line.Term), ("$target", line.Text), ("$typed", typed), ("$correct", correct ? 1 : 0), ("$firstTry", firstTry ? 1 : 0),
            ("$errors", correct ? 0 : 1), ("$duration", durationMs), ("$help", usedHelp ? 1 : 0), ("$ts", Now()));
        if (!correct)
        {
            Execute(db, """
                INSERT INTO error_events VALUES ($id,$line,$student,$mission,$concept,$type,$expected,$actual,$pos,$ts)
                """, ("$id", Guid.NewGuid().ToString("N")), ("$line", lineId), ("$student", studentId), ("$mission", missionIndex), ("$concept", line.Term),
                ("$type", error), ("$expected", line.Text), ("$actual", typed), ("$pos", FirstDifference(typed, line.Text)), ("$ts", Now()));
        }
    }

    public static void RecordLineTimeout(string missionAttemptId, string studentId, int missionIndex, int lineIndex, CodeLine line, int durationMs, bool usedHelp)
    {
        var lineId = Guid.NewGuid().ToString("N");
        using var db = Open();
        Execute(db, """
            INSERT INTO line_attempts VALUES
            ($id,$mission,$student,$missionIndex,$lineIndex,$concept,$target,$typed,0,0,1,$duration,$help,$ts)
            """, ("$id", lineId), ("$mission", missionAttemptId), ("$student", studentId), ("$missionIndex", missionIndex), ("$lineIndex", lineIndex),
            ("$concept", line.Term), ("$target", line.Text), ("$typed", "[timeout]"), ("$duration", durationMs), ("$help", usedHelp ? 1 : 0), ("$ts", Now()));

        Execute(db, """
            INSERT INTO error_events VALUES ($id,$line,$student,$mission,$concept,$type,$expected,$actual,$pos,$ts)
            """, ("$id", Guid.NewGuid().ToString("N")), ("$line", lineId), ("$student", studentId), ("$mission", missionIndex), ("$concept", line.Term),
            ("$type", "timeout"), ("$expected", line.Text), ("$actual", "[timeout]"), ("$pos", 0), ("$ts", Now()));
    }

    public static void RecordBoss(string missionAttemptId, string studentId, int missionIndex, Lesson lesson, bool firstTry, int attempts, int durationMs)
    {
        using var db = Open();
        Execute(db, """
            INSERT INTO boss_attempts VALUES ($id,$mission,$student,$idx,$bad,$fixed,$diag,$first,$attempts,$duration)
            """, ("$id", Guid.NewGuid().ToString("N")), ("$mission", missionAttemptId), ("$student", studentId), ("$idx", missionIndex),
            ("$bad", lesson.CorruptedLines.FirstOrDefault() ?? ""), ("$fixed", lesson.Lines.FirstOrDefault()?.Text ?? ""), ("$diag", lesson.BossDiagnostic),
            ("$first", firstTry ? 1 : 0), ("$attempts", attempts), ("$duration", durationMs));
    }

    public static void RecordCompileAction(string missionAttemptId, string studentId, int missionIndex, string action, int durationMs)
    {
        using var db = Open();
        Execute(db, """
            INSERT INTO compile_events VALUES ($id,$mission,$student,$idx,$viewed,$duration,$action)
            """, ("$id", Guid.NewGuid().ToString("N")), ("$mission", missionAttemptId), ("$student", studentId), ("$idx", missionIndex),
            ("$viewed", Now()), ("$duration", durationMs), ("$action", action));
    }

    public static void RecordHelpEvent(string sessionId, string studentId, int missionIndex, string concept, DateTime openedUtc, DateTime closedUtc)
    {
        using var db = Open();
        Execute(db, """
            INSERT INTO help_events VALUES ($id,$session,$student,$idx,$concept,$opened,$closed,$duration)
            """, ("$id", Guid.NewGuid().ToString("N")), ("$session", sessionId), ("$student", studentId), ("$idx", missionIndex),
            ("$concept", concept), ("$opened", openedUtc.ToString("O")), ("$closed", closedUtc.ToString("O")),
            ("$duration", Math.Max(0, (int)(closedUtc - openedUtc).TotalMilliseconds)));
    }

    public static void RecordUnderstanding(string missionAttemptId, string studentId, int missionIndex, string concept, string rating)
    {
        if (rating is not ("clear" or "review" or "stuck")) return;
        using var db = Open();
        Execute(db, """
            INSERT INTO understanding_events VALUES ($id,$mission,$student,$idx,$concept,$rating,$ts)
            """, ("$id", Guid.NewGuid().ToString("N")), ("$mission", missionAttemptId), ("$student", studentId), ("$idx", missionIndex),
            ("$concept", concept), ("$rating", rating), ("$ts", Now()));
    }

    public static TelemetrySnapshot Snapshot(string studentId, DateTime fromUtc, DateTime toUtc)
    {
        return SnapshotFor(studentId, studentId, fromUtc, toUtc);
    }

    public static TelemetrySnapshot SnapshotAll(DateTime fromUtc, DateTime toUtc)
    {
        return SnapshotFor(null, "ALL_STUDENTS", fromUtc, toUtc);
    }

    private static TelemetrySnapshot SnapshotFor(string? studentId, string label, DateTime fromUtc, DateTime toUtc)
    {
        Initialize();
        using var db = Open();
        var studentClause = studentId is null ? "" : "student_id=$student AND ";
        var sessionClause = studentId is null ? "" : "s.student_id=$student AND ";
        var missionClause = studentId is null ? "" : "ma.student_id=$student AND ";
        (string Name, object? Value)[] args = studentId is null
            ? [("$from", fromUtc.ToString("O")), ("$to", toUtc.ToString("O"))]
            : [("$student", studentId), ("$from", fromUtc.ToString("O")), ("$to", toUtc.ToString("O"))];
        var concepts = Query(db, """
            SELECT concept, COUNT(*) attempts, SUM(correct) correct, SUM(first_try) first_try,
                   SUM(error_count) errors, SUM(used_help_before_success) help, AVG(duration_ms) avg_duration
            FROM line_attempts
            WHERE STUDENT_FILTER timestamp_utc BETWEEN $from AND $to
            GROUP BY concept ORDER BY attempts DESC, concept
            """.Replace("STUDENT_FILTER", studentClause), r => new ConceptMetric(r.GetString(0), r.GetInt32(1), AsInt(r, 2), AsInt(r, 3), AsInt(r, 4), AsInt(r, 5), AsDouble(r, 6)), args);

        var errors = Query(db, """
            SELECT error_type, COUNT(*) c FROM error_events
            WHERE STUDENT_FILTER timestamp_utc BETWEEN $from AND $to
            GROUP BY error_type ORDER BY c DESC LIMIT 8
            """.Replace("STUDENT_FILTER", studentClause), r => new ErrorMetric(r.GetString(0), r.GetInt32(1)), args);

        var sessions = Query(db, """
            WITH session_daily AS (
                SELECT date(s.started_utc) day,
                       COUNT(*) sessions,
                       SUM(
                           CASE
                               WHEN julianday(COALESCE(s.ended_utc, s.last_seen_utc, s.started_utc)) > julianday(s.started_utc)
                               THEN (julianday(COALESCE(s.ended_utc, s.last_seen_utc, s.started_utc)) - julianday(s.started_utc)) * 1440.0
                               ELSE 0
                           END
                       ) minutes
                FROM sessions s
                WHERE SESSION_FILTER s.started_utc BETWEEN $from AND $to
                GROUP BY day
            ),
            mission_daily AS (
                SELECT date(ma.started_utc) day,
                       COUNT(DISTINCT ma.mission_attempt_id) missions,
                       AVG(ma.accuracy) accuracy,
                       COALESCE(SUM(la.error_count),0) errors
                FROM mission_attempts ma
                LEFT JOIN line_attempts la ON la.mission_attempt_id=ma.mission_attempt_id
                WHERE MISSION_FILTER ma.started_utc BETWEEN $from AND $to
                GROUP BY day
            )
            SELECT sd.day, sd.sessions, COALESCE(md.missions,0), sd.minutes, COALESCE(md.accuracy,0), COALESCE(md.errors,0)
            FROM session_daily sd
            LEFT JOIN mission_daily md ON md.day=sd.day
            ORDER BY sd.day
            """.Replace("SESSION_FILTER", sessionClause).Replace("MISSION_FILTER", missionClause),
            r => new SessionMetric(DateTime.Parse(r.GetString(0)), AsInt(r, 1), AsInt(r, 2), Math.Round(AsDouble(r, 3), 1), Math.Round(AsDouble(r, 4), 1), AsInt(r, 5)),
            args);

        var understanding = Query(db, """
            SELECT COALESCE(SUM(CASE WHEN rating='clear' THEN 1 ELSE 0 END),0),
                   COALESCE(SUM(CASE WHEN rating='review' THEN 1 ELSE 0 END),0),
                   COALESCE(SUM(CASE WHEN rating='stuck' THEN 1 ELSE 0 END),0)
            FROM understanding_events
            WHERE STUDENT_FILTER timestamp_utc BETWEEN $from AND $to
            """.Replace("STUDENT_FILTER", studentClause), r => new UnderstandingMetric(AsInt(r, 0), AsInt(r, 1), AsInt(r, 2)), args).FirstOrDefault() ?? new UnderstandingMetric(0, 0, 0);

        return new TelemetrySnapshot { Callsign = label, FromUtc = fromUtc, ToUtc = toUtc, Concepts = concepts, Errors = errors, Sessions = sessions, Understanding = understanding };
    }

    public static string ExportCsv(TelemetrySnapshot snapshot)
    {
        var path = ReportPath(snapshot, "csv");
        var sb = new StringBuilder();
        sb.AppendLine("Python Coder Telemetry Export");
        sb.AppendLine($"Scope,{Esc(snapshot.Callsign)}");
        sb.AppendLine($"From,{snapshot.FromUtc:O}");
        sb.AppendLine($"To,{snapshot.ToUtc:O}");
        sb.AppendLine($"OverallMastery,{snapshot.OverallMastery:0.0}");
        sb.AppendLine($"SyntaxAccuracy,{snapshot.SyntaxAccuracy:0.0}");
        sb.AppendLine($"EngagementDays,{snapshot.EngagementDays}");
        sb.AppendLine($"EngagementSessions,{snapshot.EngagementSessions}");
        sb.AppendLine($"EngagementMinutes,{snapshot.EngagementMinutes:0.0}");
        sb.AppendLine($"AverageMinutesPerDay,{snapshot.AverageMinutesPerDay:0.0}");
        sb.AppendLine();
        sb.AppendLine("[Concept Mastery]");
        sb.AppendLine("Concept,Attempts,Correct,FirstTry,Errors,HelpUses,AverageDurationMs,Mastery");
        foreach (var c in snapshot.Concepts)
            sb.AppendLine($"{Esc(c.Concept)},{c.Attempts},{c.Correct},{c.FirstTry},{c.Errors},{c.HelpUses},{c.AvgDurationMs:0},{c.Mastery:0.0}");
        sb.AppendLine();
        sb.AppendLine("[Error Patterns]");
        sb.AppendLine("ErrorType,Count");
        foreach (var e in snapshot.Errors)
            sb.AppendLine($"{Esc(e.ErrorType)},{e.Count}");
        sb.AppendLine();
        sb.AppendLine("[Sessions]");
        sb.AppendLine("Date,Sessions,Missions,Minutes,Accuracy,Errors");
        foreach (var s in snapshot.Sessions)
            sb.AppendLine($"{s.StartedUtc:yyyy-MM-dd},{s.Sessions},{s.Missions},{s.Minutes:0.0},{s.Accuracy:0.0},{s.Errors}");
        sb.AppendLine();
        sb.AppendLine("[Understanding Self Checks]");
        sb.AppendLine("Clear,Review,Stuck,Score");
        sb.AppendLine($"{snapshot.Understanding.Clear},{snapshot.Understanding.Review},{snapshot.Understanding.Stuck},{snapshot.Understanding.Score:0.0}");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    public static string ExportJson(TelemetrySnapshot snapshot)
    {
        var path = ReportPath(snapshot, "json");
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    public static string ExportPdf(TelemetrySnapshot snapshot)
    {
        var path = ReportPath(snapshot, "pdf");
        SimplePdfReport.Write(path, snapshot);
        return path;
    }

    private static SqliteConnection Open()
    {
        var db = new SqliteConnection($"Data Source={DbPath}");
        db.Open();
        return db;
    }

    private static void Execute(SqliteConnection db, string sql, params (string Name, object? Value)[] args)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static void AddColumnIfMissing(SqliteConnection db, string table, string column, string definition)
    {
        var columns = Query(db, $"PRAGMA table_info({table})", r => r.GetString(1));
        if (columns.Any(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        Execute(db, $"ALTER TABLE {table} ADD COLUMN {column} {definition}");
    }

    private static List<T> Query<T>(SqliteConnection db, string sql, Func<SqliteDataReader, T> map, params (string Name, object? Value)[] args)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        using var reader = cmd.ExecuteReader();
        var results = new List<T>();
        while (reader.Read()) results.Add(map(reader));
        return results;
    }

    private static string ClassifyError(string typed, string target)
    {
        if (target.Contains(':') && !typed.Contains(':')) return "missing_colon";
        if (target.Count(c => c == '"') > typed.Count(c => c == '"')) return "missing_quote";
        if (target.Count(c => c == '(') > typed.Count(c => c == '(') || target.Count(c => c == ')') > typed.Count(c => c == ')')) return "missing_parenthesis";
        if (target.StartsWith("    ", StringComparison.Ordinal) && !typed.StartsWith("    ", StringComparison.Ordinal)) return "wrong_indent";
        if (target.Contains("True") && typed.Contains("true")) return "wrong_case";
        if (target.Contains("==") && typed.Contains(" = ")) return "wrong_operator";
        return typed.Length < target.Length ? "missing_character" : typed.Length > target.Length ? "extra_character" : "variable_or_token_mismatch";
    }

    private static int FirstDifference(string typed, string target)
    {
        var max = Math.Min(typed.Length, target.Length);
        for (var i = 0; i < max; i++) if (typed[i] != target[i]) return i;
        return max;
    }

    private static string Now() => DateTime.UtcNow.ToString("O");
    private static int AsInt(SqliteDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
    private static double AsDouble(SqliteDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToDouble(r.GetValue(i));
    private static string Esc(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    private static string ReportPath(TelemetrySnapshot snapshot, string ext)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Reports");
        Directory.CreateDirectory(dir);
        var safeName = new string(snapshot.Callsign.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch).ToArray());
        return Path.Combine(dir, $"{safeName}_{snapshot.FromUtc:yyyyMMdd}_{snapshot.ToUtc:yyyyMMdd}.{ext}");
    }
}

internal static class SimplePdfReport
{
    public static void Write(string path, TelemetrySnapshot s)
    {
        var content = new StringBuilder();
        Text(content, 48, 760, 18, $"Python Coder Progress Report - {s.Callsign}");
        Text(content, 48, 736, 10, $"Range: {s.FromUtc:yyyy-MM-dd} to {s.ToUtc:yyyy-MM-dd}");
        Text(content, 48, 716, 10, $"Overall Mastery: {s.OverallMastery:0.0}%");
        Text(content, 220, 716, 10, $"Syntax Accuracy: {s.SyntaxAccuracy:0.0}%");
        Text(content, 392, 716, 10, $"Practice Days: {s.EngagementDays}");
        Text(content, 48, 696, 10, $"Understanding Score: {s.Understanding.Score:0.0}%   Clear: {s.Understanding.Clear}   Review: {s.Understanding.Review}   Stuck: {s.Understanding.Stuck}");
        Text(content, 48, 678, 10, $"Engagement: {s.EngagementDays} days   {s.EngagementSessions} sessions   {s.EngagementMinutes:0.0} minutes   Avg {s.AverageMinutesPerDay:0.0} min/day");

        Text(content, 48, 642, 13, "Concept Mastery");
        var y = 616;
        foreach (var c in s.Concepts.Take(10))
        {
            Text(content, 48, y + 2, 8, $"{c.Concept} ({c.Correct}/{c.Attempts})");
            Bar(content, 190, y, 260, 10, c.Mastery, "0.16 0.96 1");
            Text(content, 462, y + 2, 8, $"{c.Mastery:0.0}%");
            y -= 22;
        }

        Text(content, 48, 390, 13, "Top Error Patterns");
        var maxError = Math.Max(1, s.Errors.Select(e => e.Count).DefaultIfEmpty(1).Max());
        y = 366;
        foreach (var e in s.Errors.Take(8))
        {
            Text(content, 48, y + 2, 8, e.ErrorType);
            Bar(content, 190, y, 220, 10, e.Count * 100.0 / maxError, "1 0.58 0.13");
            Text(content, 424, y + 2, 8, e.Count.ToString());
            y -= 22;
        }

        Text(content, 48, 188, 13, "Session Accuracy Trend");
        DrawTrend(content, 48, 54, 500, 112, s.Sessions.Select(x => x.Accuracy).ToList());

        var stream = content.ToString();
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
        };

        using var fs = File.Create(path);
        using var writer = new StreamWriter(fs, Encoding.ASCII);
        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            writer.Flush();
            offsets.Add(fs.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
        }
        writer.Flush();
        var xref = fs.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1)) writer.WriteLine($"{offset:0000000000} 00000 n ");
        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xref);
        writer.WriteLine("%%EOF");
    }

    private static void Text(StringBuilder content, int x, int y, int size, string value)
    {
        content.AppendLine($"BT /F1 {size} Tf {x} {y} Td ({PdfEsc(value)}) Tj ET");
    }

    private static void Bar(StringBuilder content, int x, int y, int width, int height, double value, string rgb)
    {
        var filled = Math.Max(2, width * Math.Clamp(value, 0, 100) / 100.0);
        content.AppendLine($"0.08 0.10 0.14 rg {x} {y} {width} {height} re f");
        content.AppendLine($"{rgb} rg {x} {y} {filled:0.0} {height} re f");
    }

    private static void DrawTrend(StringBuilder content, int x, int y, int width, int height, IReadOnlyList<double> values)
    {
        content.AppendLine($"0.18 0.22 0.30 RG {x} {y} {width} {height} re S");
        for (var i = 1; i < 4; i++)
        {
            var gy = y + i * height / 4;
            content.AppendLine($"0.18 0.22 0.30 RG {x} {gy} m {x + width} {gy} l S");
        }
        if (values.Count < 2)
        {
            Text(content, x + 12, y + height / 2, 9, "Not enough session data yet.");
            return;
        }

        var points = values.Select((v, i) =>
        {
            var px = x + i * width / Math.Max(1, values.Count - 1);
            var py = y + (int)(Math.Clamp(v, 0, 100) / 100.0 * height);
            return (X: px, Y: py);
        }).ToArray();
        content.AppendLine($"0.22 1 0.52 RG 2 w {points[0].X} {points[0].Y} m");
        foreach (var p in points.Skip(1)) content.AppendLine($"{p.X} {p.Y} l");
        content.AppendLine("S");
        foreach (var p in points) content.AppendLine($"0.22 1 0.52 rg {p.X - 2} {p.Y - 2} 4 4 re f");
    }

    private static string PdfEsc(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
