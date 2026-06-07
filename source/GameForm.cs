using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace PythonCoderGame;

internal enum AppScreen
{
    Boot,
    Auth,
    Dashboard,
    MissionSelect,
    Game,
    Upgrades,
    Profile
}

internal sealed class GameForm : Form
{
    private const double LevelSpeedMultiplier = 0.10;
    private const double ScrollSpeedMultiplier = 0.06;
    private const double BaseScrollPixelsPerSecond = 24.0;

    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly TextBox _input = new();
    private readonly List<(Rectangle Rect, string Action)> _hotspots = [];
    private readonly List<string> _terminal = [];
    private readonly List<string> _bootLines = [];
    private readonly List<string> _completedLines = [];
    private readonly List<FloatingText> _floatingTexts = [];
    private readonly Font _mono = new("Consolas", 13.5f);
    private readonly Font _monoSmall = new("Consolas", 10.5f);
    private readonly Font _monoBold = new("Consolas", 13.5f, FontStyle.Bold);
    private readonly Font _ui = new("Segoe UI", 10.5f);
    private readonly Font _uiBold = new("Segoe UI Semibold", 11.5f, FontStyle.Bold);
    private readonly Font _title = new("Segoe UI Semibold", 24f, FontStyle.Bold);
    private readonly Font _hero = new("Segoe UI Semibold", 34f, FontStyle.Bold);
    private readonly Font _missionSectionTitle = new("Segoe UI Semibold", 19f, FontStyle.Bold);
    private readonly AudioSystem _audio = new();
    private readonly ComboBox _profileViewSelect = new();
    private readonly ComboBox _profileRangeSelect = new();
    private readonly ComboBox _profileScopeSelect = new();

    private AppScreen _screen = AppScreen.Boot;
    private UserProfile? _user;
    private ScoreEngine _score = new(UpgradeSystem.GetEffects(null));
    private int _authStep;
    private string _firstName = "";
    private string _lastName = "";
    private int _lessonIndex;
    private int _lineIndex;
    private double _snippetY;
    private DateTime _lastTick = DateTime.Now;
    private bool _paused;
    private bool _showHelp;
    private bool _showCompile;
    private string _status = "";
    private string _feedback = "";
    private double _compileElapsed;
    private string _sessionId = "";
    private string _missionAttemptId = "";
    private DateTime _lineStartedUtc;
    private int _lineAttempts;
    private bool _lineUsedHelp;
    private int _bossAttempts;
    private double _bossTimeRemaining;
    private DateTime _bossHintUntilUtc;
    private int _bossHintStart = -1;
    private int _bossHintLength;
    private string _lastLiveInputText = "";
    private bool _suppressLiveTypoPenalty;
    private string _profileView = "overview";
    private string _reportRange = "30";
    private string _reportScope = "student";
    private DateTime? _helpOpenedUtc;
    private string _helpConceptWhenOpened = "";
    private DateTime _lastTelemetryTouchUtc = DateTime.MinValue;
    private double _bootElapsed;
    private int _bootVisibleLines;
    private bool _bootComplete;
    private int _missionSelectScroll;
    private int _missionSelectMaxScroll;
    private int _missionSelectContentHeight;
    private int _missionSelectViewportHeight;
    private Rectangle _missionSelectScrollbarTrack = Rectangle.Empty;
    private bool _missionSelectDraggingScrollbar;
    private int _missionSelectScrollbarDragOffset;

    public GameForm()
    {
        Text = "Python Coder Game // Nexus Learning Interface";
        MinimumSize = new Size(1220, 760);
        Size = new Size(1420, 860);
        DoubleBuffered = true;
        BackColor = Palette.Bg;
        KeyPreview = true;
        TelemetryStore.Initialize();

        _input.Font = _mono;
        _input.BackColor = Color.FromArgb(5, 9, 12);
        _input.ForeColor = Palette.Text;
        _input.BorderStyle = BorderStyle.FixedSingle;
        _input.KeyDown += InputOnKeyDown;
        _input.MouseWheel += (_, e) =>
        {
            if (_screen == AppScreen.MissionSelect)
            {
                ScrollMissionSelect(e.Delta > 0 ? -150 : 150);
            }
        };
        _input.TextChanged += (_, _) =>
        {
            if (_screen == AppScreen.Game && !_paused && !_showHelp)
            {
                ApplyLiveTypingPenalty();
                _audio.KeyTick();
                Invalidate();
            }
        };
        Controls.Add(_input);
        ConfigureProfileCombo(_profileViewSelect, ["Overview", "Concepts", "Errors", "Sessions", "Plain Tables", "Export"]);
        ConfigureProfileCombo(_profileRangeSelect, ["7 Days", "30 Days", "90 Days", "All Time"]);
        ConfigureProfileCombo(_profileScopeSelect, ["Current Student", "All Students"]);
        _profileViewSelect.SelectedIndexChanged += (_, _) =>
        {
            _profileView = _profileViewSelect.SelectedItem?.ToString() switch
            {
                "Concepts" => "concepts",
                "Errors" => "errors",
                "Sessions" => "sessions",
                "Plain Tables" => "tables",
                "Export" => "export",
                _ => "overview"
            };
            Invalidate();
        };
        _profileRangeSelect.SelectedIndexChanged += (_, _) =>
        {
            _reportRange = _profileRangeSelect.SelectedItem?.ToString() switch
            {
                "7 Days" => "7",
                "90 Days" => "90",
                "All Time" => "all",
                _ => "30"
            };
            Invalidate();
        };
        _profileScopeSelect.SelectedIndexChanged += (_, _) =>
        {
            _reportScope = _profileScopeSelect.SelectedItem?.ToString() == "All Students" ? "all" : "student";
            Invalidate();
        };
        Controls.Add(_profileViewSelect);
        Controls.Add(_profileRangeSelect);
        Controls.Add(_profileScopeSelect);

        Resize += (_, _) => LayoutInput();
        Shown += (_, _) => _input.Focus();
        FormClosing += (_, _) => EndCurrentTelemetrySession();
        MouseClick += OnMouseClick;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        MouseWheel += OnGameMouseWheel;
        KeyDown += OnFormKeyDown;

        _timer.Interval = 16;
        _timer.Tick += (_, _) => TickGame();
        _timer.Start();

        StartBoot();
    }

    private static void ConfigureProfileCombo(ComboBox combo, object[] items)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = Color.FromArgb(8, 13, 20);
        combo.ForeColor = Palette.Text;
        combo.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
        combo.Items.AddRange(items);
        combo.SelectedIndex = 0;
        combo.Visible = false;
    }

    private Lesson CurrentLesson => Curriculum.BeginnerLessons[_lessonIndex];
    private CodeLine CurrentLine => CurrentLesson.Lines[_lineIndex];
    private bool IsLessonComplete => _lineIndex >= CurrentLesson.Lines.Count;
    private string CurrentCorruptedLine => CorruptedLineFor(_lineIndex);

    private string CorruptedLineFor(int index)
    {
        if (index >= 0 && index < CurrentLesson.CorruptedLines.Count)
        {
            return CurrentLesson.CorruptedLines[index];
        }

        if (index >= 0 && index < CurrentLesson.Lines.Count)
        {
            return CurrentLesson.Lines[index].Text;
        }

        return CurrentLesson.Lines.LastOrDefault()?.Text ?? "";
    }

    private void StartBoot()
    {
        _screen = AppScreen.Boot;
        _bootElapsed = 0;
        _bootVisibleLines = 0;
        _bootComplete = false;
        _bootLines.Clear();
        _bootLines.AddRange([
            "RETRO CYBERPYTHON BIOS v0.9.7",
            "Copyright 2026 // Python Coder Systems",
            "",
            "MEMORY CHECK....................OK",
            "NEON DISPLAY BUS................OK",
            "KEYBOARD MATRIX.................OK",
            "PYTHON TOKENIZER................ONLINE",
            "COMPILER STACK VISUALIZER.......ONLINE",
            "MISSION REGISTRY................MOUNTED",
            "AUDIO CORE......................ARMED",
            "",
            "Loading operator access shell...",
            "Press any key or click to skip boot."
        ]);
        _audio.PlayForScreen(_screen);
        LayoutInput();
        Invalidate();
    }

    private void BootAuth()
    {
        _screen = AppScreen.Auth;
        _authStep = 0;
        _terminal.Clear();
        WriteTerminal("PYTHON CODER GAME // OPERATOR ACCESS");
        WriteTerminal("Type register, login, list, or help.");
        WriteTerminal("");
        _input.PlaceholderText = "AUTH>";
        _input.Clear();
        _audio.PlayForScreen(_screen);
        LayoutInput();
        Invalidate();
    }

    private void LayoutInput()
    {
        _input.Visible = _screen is AppScreen.Auth or AppScreen.Game;
        _input.Location = new Point(310, ClientSize.Height - 58);
        _input.Width = Math.Max(320, ClientSize.Width - 620);
        var showProfileControls = _screen == AppScreen.Profile;
        _profileViewSelect.Visible = showProfileControls;
        _profileRangeSelect.Visible = showProfileControls;
        _profileScopeSelect.Visible = showProfileControls;
        if (showProfileControls)
        {
            _profileViewSelect.SetBounds(52, 286, 170, 32);
            _profileRangeSelect.SetBounds(238, 286, 132, 32);
            _profileScopeSelect.SetBounds(386, 286, 156, 32);
        }
    }

    private void InputOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (HandleCompileShortcut(e))
        {
            return;
        }

        if (_screen == AppScreen.Boot)
        {
            CompleteBoot();
            e.SuppressKeyPress = true;
            return;
        }

        if (_screen == AppScreen.Auth && HandleAuthShortcut(e))
        {
            return;
        }

        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        var value = _input.Text;
        _input.Clear();

        if (_screen == AppScreen.Auth)
        {
            HandleAuthInput(value.Trim());
            return;
        }

        if (_screen == AppScreen.Game)
        {
            HandleGameInput(value);
        }
    }

    private void HandleAuthInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        WriteTerminal($"> {value}");

        if (_authStep == 1)
        {
            _firstName = value;
            _authStep = 2;
            WriteTerminal("Enter last name:");
        }
        else if (_authStep == 2)
        {
            _lastName = value;
            _authStep = 3;
            WriteTerminal("Choose callsign, 3-20 letters/numbers/underscore:");
        }
        else if (_authStep == 3)
        {
            if (!IsValidCallsign(value))
            {
                WriteTerminal("Invalid callsign. Use 3-20 letters, numbers, or underscore.");
            }
            else
            {
                try
                {
                    _user = ProfileStore.CreateUser(_firstName, _lastName, value);
                    EnterDashboard($"Operator {_user.Callsign} registered.");
                }
                catch (Exception ex)
                {
                    WriteTerminal(ex.Message);
                }
            }
        }
        else
        {
            switch (value.ToLowerInvariant())
            {
                case "register":
                    _authStep = 1;
                    WriteTerminal("NEW OPERATOR REGISTRATION");
                    WriteTerminal("Enter first name:");
                    break;
                case "login":
                    WriteTerminal("Enter callsign:");
                    _authStep = 4;
                    break;
                case "list":
                    var users = ProfileStore.LoadRegistry().Users;
                    WriteTerminal(users.Count == 0 ? "No registered operators yet." : string.Join(", ", users.Select(u => u.Callsign)));
                    break;
                case "help":
                    WriteTerminal("register: create profile");
                    WriteTerminal("login: load profile");
                    WriteTerminal("list: show registered operators");
                    break;
                default:
                    if (_authStep == 4)
                    {
                        _user = ProfileStore.LoadUser(value);
                        if (_user is null)
                        {
                            WriteTerminal("Operator not found. Type register to create one.");
                            _authStep = 0;
                        }
                        else
                        {
                            EnterDashboard($"Authenticated as {_user.Callsign}.");
                        }
                    }
                    else
                    {
                        WriteTerminal("Unknown command. Type help.");
                    }
                    break;
            }
        }

        Invalidate();
    }

    private bool HandleAuthShortcut(KeyEventArgs e)
    {
        if (e.Control || e.Alt || _authStep != 0)
        {
            return false;
        }

        var command = e.KeyCode switch
        {
            Keys.R => "register",
            Keys.L => "login",
            Keys.O => "list",
            Keys.H => "help",
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        e.SuppressKeyPress = true;
        HandleAuthInput(command);
        return true;
    }

    private static bool IsValidCallsign(string value)
    {
        return value.Length is >= 3 and <= 20 && value.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private void EnterDashboard(string message)
    {
        _screen = AppScreen.Dashboard;
        _status = message;
        if (_user is not null && string.IsNullOrWhiteSpace(_sessionId))
        {
            _sessionId = TelemetryStore.StartSession(_user.Callsign);
            _lastTelemetryTouchUtc = DateTime.UtcNow;
        }
        _input.PlaceholderText = "";
        _input.Clear();
        _audio.PlayForScreen(_screen);
        LayoutInput();
        Invalidate();
    }

    private void StartMission()
    {
        StartMissionAt(NextIncompleteLessonIndex());
    }

    private int NextIncompleteLessonIndex()
    {
        if (_user is null)
        {
            return 0;
        }

        var completed = TelemetryStore.CompletedMissionIndexes(_user.Callsign);
        for (var i = 0; i < Curriculum.BeginnerLessons.Count; i++)
        {
            if (!completed.Contains(i))
            {
                return i;
            }
        }

        return Curriculum.BeginnerLessons.Count - 1;
    }

    private void OpenMissionSelect()
    {
        _screen = AppScreen.MissionSelect;
        _status = "Select any available mission. Completed missions stay available for replay.";
        _missionSelectScroll = 0;
        _audio.PlayForScreen(_screen);
        LayoutInput();
        ActiveControl = null;
        Focus();
        Invalidate();
    }

    private void StartMissionAt(int lessonIndex)
    {
        _screen = AppScreen.Game;
        _lessonIndex = Math.Clamp(lessonIndex, 0, Curriculum.BeginnerLessons.Count - 1);
        _lineIndex = 0;
        _completedLines.Clear();
        _score = new ScoreEngine(UpgradeSystem.GetEffects(_user));
        _score.Reset();
        _missionAttemptId = _user is null ? "" : TelemetryStore.StartMission(_sessionId, _user.Callsign, _lessonIndex, CurrentLesson);
        _bossAttempts = 0;
        _bossTimeRemaining = CurrentLesson.IsBoss ? 60 : 0;
        _bossHintUntilUtc = DateTime.MinValue;
        _bossHintStart = -1;
        _bossHintLength = 0;
        _floatingTexts.Clear();
        _paused = false;
        _showHelp = false;
        _showCompile = false;
        _compileElapsed = 0;
        _feedback = "";
        _status = CurrentLesson.IsBoss
            ? "Boss match: repair five corrupted snippets before the virus timer reaches zero."
            : "Type the rising Python line exactly. Enter -help for syntax.";
        _audio.PlayForScreen(_screen, _lessonIndex);
        LayoutInput();
        ResetSnippet();
    }

    private void RepeatCurrentMission()
    {
        RecordCompileAction("repeat");
        if (!string.IsNullOrWhiteSpace(_missionAttemptId))
        {
            TelemetryStore.MarkMissionFlag(_missionAttemptId, "repeated");
        }
        var lesson = _lessonIndex;
        StartMissionAt(lesson);
        _status = "Mission repeated. Try it again with the data flow in mind.";
    }

    private void SaveAndEditCurrentMission()
    {
        RecordCompileAction("save_edit");
        if (!string.IsNullOrWhiteSpace(_missionAttemptId))
        {
            TelemetryStore.MarkMissionFlag(_missionAttemptId, "used_save_edit");
        }
        var saveDir = Path.Combine(AppContext.BaseDirectory, "Saved Missions");
        Directory.CreateDirectory(saveDir);
        var fileName = $"mission_{_lessonIndex + 1:00}_{SanitizeFileName(CurrentLesson.Title)}.py";
        var path = Path.Combine(saveDir, fileName);
        File.WriteAllLines(path, CurrentLesson.Lines.Select(line => line.Text));
        _status = $"Saved mission code for editing: {fileName}";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        catch
        {
            _status = $"Saved mission code at {path}";
        }
    }

    private void RecordCompileAction(string action)
    {
        if (!string.IsNullOrWhiteSpace(_missionAttemptId) && _user is not null)
        {
            TelemetryStore.RecordCompileAction(_missionAttemptId, _user.Callsign, _lessonIndex, action, (int)(_compileElapsed * 1000));
        }
    }

    private void EndCurrentTelemetrySession()
    {
        if (!string.IsNullOrWhiteSpace(_sessionId))
        {
            TelemetryStore.EndSession(_sessionId);
            _sessionId = "";
        }
    }

    private void RecordUnderstanding(string rating)
    {
        if (!string.IsNullOrWhiteSpace(_missionAttemptId) && _user is not null)
        {
            TelemetryStore.RecordUnderstanding(_missionAttemptId, _user.Callsign, _lessonIndex, CurrentLesson.Title, rating);
            _status = rating switch
            {
                "clear" => "Understanding check recorded: clear.",
                "review" => "Understanding check recorded: review again later.",
                "stuck" => "Understanding check recorded: needs support.",
                _ => _status
            };
        }
    }

    private void ToggleHelp()
    {
        if (_showHelp)
        {
            CloseHelp();
            return;
        }

        OpenHelp();
    }

    private void OpenHelp()
    {
        _showHelp = true;
        _paused = true;
        _status = "Help open. Type -help or press Esc to resume.";
        _helpOpenedUtc = DateTime.UtcNow;
        _helpConceptWhenOpened = IsLessonComplete ? CurrentLesson.Title : CurrentLine.Term;
    }

    private void CloseHelp()
    {
        if (!_showHelp)
        {
            return;
        }

        var opened = _helpOpenedUtc ?? DateTime.UtcNow;
        if (_user is not null && !string.IsNullOrWhiteSpace(_sessionId))
        {
            TelemetryStore.RecordHelpEvent(_sessionId, _user.Callsign, _lessonIndex, _helpConceptWhenOpened, opened, DateTime.UtcNow);
        }

        _showHelp = false;
        _paused = false;
        _status = "Help closed.";
        _helpOpenedUtc = null;
        _helpConceptWhenOpened = "";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray())
            .Replace(' ', '_')
            .ToLowerInvariant();
    }

    private void ResetSnippet()
    {
        _snippetY = ClientSize.Height - 126;
        _input.PlaceholderText = "type current line, or -help";
        _suppressLiveTypoPenalty = true;
        _input.Clear();
        _suppressLiveTypoPenalty = false;
        _lastLiveInputText = "";
        _lineStartedUtc = DateTime.UtcNow;
        _lineAttempts = 0;
        _lineUsedHelp = false;
        _input.Focus();
    }

    private void ApplyLiveTypingPenalty()
    {
        if (_suppressLiveTypoPenalty || _showCompile || IsLessonComplete)
        {
            _lastLiveInputText = _input.Text;
            return;
        }

        var typed = _input.Text;
        if (typed.Length <= _lastLiveInputText.Length)
        {
            _lastLiveInputText = typed;
            return;
        }

        const int livePenalty = 5;
        var target = CurrentLine.Text;
        var penaltyCount = 0;
        for (var i = _lastLiveInputText.Length; i < typed.Length; i++)
        {
            if (i >= target.Length || typed[i] != target[i])
            {
                penaltyCount++;
            }
        }

        if (penaltyCount > 0)
        {
            var penalty = penaltyCount * livePenalty;
            _score.ApplyLiveTypoPenalty(penalty);
            AddFloatingText($"-{penalty} TYPO", Palette.HotRed);
            _feedback = BuildCorrectionHint(typed, target);
        }

        _lastLiveInputText = typed;
    }

    private void HandleGameInput(string value)
    {
        if (value.Equals("-help", StringComparison.OrdinalIgnoreCase))
        {
            ToggleHelp();
            _lineUsedHelp = true;
            if (!string.IsNullOrWhiteSpace(_missionAttemptId))
            {
                TelemetryStore.MarkMissionFlag(_missionAttemptId, "used_help");
            }
            Invalidate();
            return;
        }

        if (_paused || _showHelp || IsLessonComplete)
        {
            return;
        }

        var scoreBefore = _score.Score;
        _lineAttempts++;
        var perfect = _score.SubmitLine(value, CurrentLine.Text);
        var scoreDelta = _score.Score - scoreBefore;
        if (!string.IsNullOrWhiteSpace(_missionAttemptId) && _user is not null)
        {
            TelemetryStore.RecordLine(
                _missionAttemptId,
                _user.Callsign,
                _lessonIndex,
                _lineIndex,
                CurrentLine,
                value,
                perfect,
                _lineAttempts == 1,
                (int)(DateTime.UtcNow - _lineStartedUtc).TotalMilliseconds,
                _lineUsedHelp);
        }
        if (perfect)
        {
            _completedLines.Add(CurrentLine.Text);
            _feedback = "Correct. The line locked into the code viewer.";
            AddFloatingText($"+{Math.Max(0, scoreDelta)}", Palette.Green);
            _audio.Success();
            AdvanceLine();
        }
        else
        {
            _feedback = BuildCorrectionHint(value, CurrentLine.Text);
            if (CurrentLesson.IsBoss)
            {
                _bossAttempts++;
                TriggerBossCompileHint(value);
            }
            AddFloatingText($"-{_score.LastPenalty} TYPO PENALTY", Palette.HotRed);
            _audio.Error();
            _suppressLiveTypoPenalty = true;
            _input.Text = value;
            _input.SelectAll();
            _suppressLiveTypoPenalty = false;
            _lastLiveInputText = value;
        }

        Invalidate();
    }

    private void TriggerBossCompileHint(string typed)
    {
        var diff = FirstDifference(typed, CurrentLine.Text);
        _bossHintStart = Math.Max(0, diff - 2);
        _bossHintLength = 4;
        _bossHintUntilUtc = DateTime.UtcNow.AddSeconds(3);
        _status = "COMPILE ERROR: virus scan highlights the likely damaged token.";
    }

    private static string BuildCorrectionHint(string typed, string target)
    {
        var max = Math.Min(typed.Length, target.Length);
        for (var i = 0; i < max; i++)
        {
            if (typed[i] != target[i])
            {
                return $"Character {i + 1}: expected {Readable(target[i])}, got {Readable(typed[i])}.";
            }
        }

        return typed.Length < target.Length ? "More characters needed." : "Extra characters at the end.";
    }

    private static string Readable(char c) => c == ' ' ? "space" : $"'{c}'";

    private static int FirstDifference(string typed, string target)
    {
        var max = Math.Min(typed.Length, target.Length);
        for (var i = 0; i < max; i++)
        {
            if (typed[i] != target[i])
            {
                return i;
            }
        }

        return max;
    }

    private void AdvanceLine()
    {
        _lineIndex++;
        if (_lineIndex < CurrentLesson.Lines.Count)
        {
            ResetSnippet();
            return;
        }

        ShowCompileDemo();
    }

    private void ShowCompileDemo()
    {
        _showCompile = true;
        _paused = true;
        _compileElapsed = 0;
        _status = $"Mission {_lessonIndex + 1} compiled. Watch the data flow, then continue.";
        if (!string.IsNullOrWhiteSpace(_missionAttemptId) && _user is not null)
        {
            TelemetryStore.CompleteMission(_missionAttemptId, _score.Score, _score.Accuracy);
            if (CurrentLesson.IsBoss)
            {
                TelemetryStore.RecordBoss(_missionAttemptId, _user.Callsign, _lessonIndex, CurrentLesson, _bossAttempts == 0, Math.Max(1, _bossAttempts + 1), (int)(DateTime.UtcNow - _lineStartedUtc).TotalMilliseconds);
            }
        }
        _input.Clear();
        _audio.Complete();
    }

    private void ContinueAfterCompile()
    {
        RecordCompileAction("continue");
        _showCompile = false;
        _lessonIndex++;
        if (_lessonIndex < Curriculum.BeginnerLessons.Count)
        {
            _lineIndex = 0;
            _completedLines.Clear();
            _status = "Next lesson loaded.";
            _audio.PlayForScreen(_screen, _lessonIndex);
            _paused = false;
            ResetSnippet();
            return;
        }

        CompleteMission();
    }

    private void ExitCompileScreen()
    {
        RecordCompileAction("exit");
        _showCompile = false;
        _paused = true;
        _screen = AppScreen.Dashboard;
        _status = "Exited compile replay. Mission result was saved.";
        _audio.PlayForScreen(_screen);
        LayoutInput();
    }

    private void CompleteMission()
    {
        if (_user is not null)
        {
            var tokens = _score.TokensEarned;
            var xp = _score.XpEarned;
            _user.ScrapTokens += tokens;
            _user.Xp += xp;
            _user.TotalScore += Math.Max(0, _score.Score);
            _user.MissionsCompleted++;
            _user.BestWpm = Math.Max(_user.BestWpm, _score.Wpm);
            _user.BestAccuracy = Math.Max(_user.BestAccuracy, _score.Accuracy);
            _user.Rank = Math.Clamp(1 + _user.Xp / 3500, 1, 5);
            ProfileStore.SaveUser(_user);
            _status = $"Mission complete. +{xp} XP, +{tokens} scrap tokens.";
        }

        _audio.Complete();
        _paused = true;
        _input.Clear();
    }

    private void TickGame()
    {
        var now = DateTime.Now;
        var elapsed = (now - _lastTick).TotalSeconds;
        _lastTick = now;

        if (_screen == AppScreen.Boot)
        {
            _bootElapsed += elapsed;
            _bootVisibleLines = Math.Min(_bootLines.Count, (int)(_bootElapsed * 5.5));
            if (!_bootComplete && _bootElapsed > 3.2)
            {
                CompleteBoot();
            }
            Invalidate();
            return;
        }

        if (_screen == AppScreen.Game && _showCompile)
        {
            _compileElapsed += elapsed;
        }

        if (_screen == AppScreen.Game && CurrentLesson.IsBoss && !_paused && !_showHelp && !_showCompile && !IsLessonComplete)
        {
            _bossTimeRemaining = Math.Max(0, _bossTimeRemaining - elapsed);
            if (_bossTimeRemaining <= 0)
            {
                _bossAttempts++;
                if (!string.IsNullOrWhiteSpace(_missionAttemptId) && _user is not null)
                {
                    TelemetryStore.RecordLineTimeout(_missionAttemptId, _user.Callsign, _lessonIndex, _lineIndex, CurrentLine, (int)(DateTime.UtcNow - _lineStartedUtc).TotalMilliseconds, _lineUsedHelp);
                }
                AddFloatingText("VIRUS WINS - RETRY", Palette.HotRed);
                StartMissionAt(_lessonIndex);
                _status = "Virus timer reached zero. Boss fight restarted.";
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(_sessionId) && (DateTime.UtcNow - _lastTelemetryTouchUtc).TotalSeconds >= 30)
        {
            TelemetryStore.TouchSession(_sessionId);
            _lastTelemetryTouchUtc = DateTime.UtcNow;
        }

        if (_screen == AppScreen.Game && !_paused && !_showHelp && !_showCompile && !IsLessonComplete)
        {
            var effects = UpgradeSystem.GetEffects(_user);
            var lessonFactor = 1.0 + (_lessonIndex * 0.22 * LevelSpeedMultiplier);
            _snippetY -= BaseScrollPixelsPerSecond * ScrollSpeedMultiplier * lessonFactor * effects.SpeedMod / effects.TimeMod * elapsed;
            if (_snippetY < 108)
            {
                _snippetY = ClientSize.Height - 126;
                _feedback = "Wrapped for study time. Keep reading the explanation.";
            }
        }

        for (var i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            _floatingTexts[i].Age += elapsed;
            _floatingTexts[i].Y -= (float)(52 * elapsed);
            if (_floatingTexts[i].Age > 1.15)
            {
                _floatingTexts.RemoveAt(i);
            }
        }

        Invalidate();
    }

    private void AddFloatingText(string text, Color color)
    {
        _floatingTexts.Add(new FloatingText
        {
            Text = text,
            Color = color,
            X = ClientSize.Width / 2f + 150,
            Y = (float)_snippetY - 64,
            Age = 0
        });
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (HandleCompileShortcut(e))
        {
            return;
        }

        if (_screen == AppScreen.Boot)
        {
            CompleteBoot();
            return;
        }

        if (_screen == AppScreen.MissionSelect && HandleMissionSelectKey(e))
        {
            return;
        }

        if (e.KeyCode != Keys.Escape)
        {
            return;
        }

        if (_screen == AppScreen.Game)
        {
            if (_showHelp)
            {
                CloseHelp();
            }
            else if (_showCompile)
            {
                EnterDashboard("Compile replay exited. Returned to dashboard.");
            }
            else if (_paused)
            {
                EnterDashboard("Mission paused and exited to dashboard.");
            }
            else
            {
                _paused = true;
                _status = "Mission paused. Press Esc again to return to dashboard.";
            }

            e.SuppressKeyPress = true;
            Invalidate();
        }
    }

    private void OnGameMouseWheel(object? sender, MouseEventArgs e)
    {
        if (_screen != AppScreen.MissionSelect)
        {
            return;
        }

        ScrollMissionSelect(e.Delta > 0 ? -150 : 150);
    }

    private bool HandleMissionSelectKey(KeyEventArgs e)
    {
        var amount = e.KeyCode switch
        {
            Keys.Down => 72,
            Keys.Up => -72,
            Keys.PageDown => 420,
            Keys.PageUp => -420,
            Keys.Home => -_missionSelectMaxScroll,
            Keys.End => _missionSelectMaxScroll,
            _ => 0
        };

        if (amount == 0)
        {
            return false;
        }

        e.SuppressKeyPress = true;
        ScrollMissionSelect(amount);
        return true;
    }

    private void ScrollMissionSelect(int delta)
    {
        _missionSelectScroll = Math.Clamp(_missionSelectScroll + delta, 0, Math.Max(0, _missionSelectMaxScroll));
        Invalidate();
    }

    private bool HandleCompileShortcut(KeyEventArgs e)
    {
        if (_screen != AppScreen.Game || !_showCompile || e.Control || e.Alt)
        {
            return false;
        }

        switch (e.KeyCode)
        {
            case Keys.R:
                e.SuppressKeyPress = true;
                RepeatCurrentMission();
                return true;
            case Keys.S:
                e.SuppressKeyPress = true;
                SaveAndEditCurrentMission();
                return true;
            case Keys.C:
                e.SuppressKeyPress = true;
                ContinueAfterCompile();
                return true;
            case Keys.E:
                e.SuppressKeyPress = true;
                ExitCompileScreen();
                return true;
            default:
                return false;
        }
    }

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (_screen == AppScreen.Boot)
        {
            CompleteBoot();
            return;
        }

        if (_screen == AppScreen.MissionSelect && _missionSelectScrollbarTrack.Contains(e.Location))
        {
            return;
        }

        foreach (var (rect, action) in _hotspots)
        {
            if (!rect.Contains(e.Location))
            {
                continue;
            }

            HandleAction(action);
            return;
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (_screen != AppScreen.MissionSelect || e.Button != MouseButtons.Left || _missionSelectMaxScroll <= 0)
        {
            return;
        }

        if (!_missionSelectScrollbarTrack.Contains(e.Location))
        {
            return;
        }

        var thumb = MissionSelectScrollbarThumb();
        if (thumb.Contains(e.Location))
        {
            _missionSelectDraggingScrollbar = true;
            _missionSelectScrollbarDragOffset = e.Y - thumb.Y;
            Capture = true;
        }
        else
        {
            SetMissionSelectScrollFromScrollbarY(e.Y - thumb.Height / 2);
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_missionSelectDraggingScrollbar)
        {
            return;
        }

        SetMissionSelectScrollFromScrollbarY(e.Y - _missionSelectScrollbarDragOffset);
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (!_missionSelectDraggingScrollbar)
        {
            return;
        }

        _missionSelectDraggingScrollbar = false;
        Capture = false;
    }

    private void HandleAction(string action)
    {
        if (action == "deploy") StartMission();
        else if (action.StartsWith("auth:", StringComparison.Ordinal) && _screen == AppScreen.Auth) HandleAuthInput(action[5..]);
        else if (action == "missionSelect") OpenMissionSelect();
        else if (action.StartsWith("mission:", StringComparison.Ordinal) && int.TryParse(action[8..], out var missionIndex)) StartMissionAt(missionIndex);
        else if (action == "upgrades") { _screen = AppScreen.Upgrades; _audio.PlayForScreen(_screen); LayoutInput(); }
        else if (action == "profile") { _screen = AppScreen.Profile; _profileView = "overview"; SyncProfileCombos(); _audio.PlayForScreen(_screen); LayoutInput(); }
        else if (action == "dashboard") { _screen = AppScreen.Dashboard; _audio.PlayForScreen(_screen); LayoutInput(); }
        else if (action == "logout") { EndCurrentTelemetrySession(); BootAuth(); }
        else if (action == "help") ToggleHelp();
        else if (action == "restart") StartMission();
        else if (action == "pause") _paused = !_paused;
        else if (action == "continueMission") ContinueAfterCompile();
        else if (action == "exitCompile") ExitCompileScreen();
        else if (action == "repeatMission") RepeatCurrentMission();
        else if (action == "saveEditMission") SaveAndEditCurrentMission();
        else if (action.StartsWith("understanding:", StringComparison.Ordinal)) RecordUnderstanding(action[14..]);
        else if (action == "music") _audio.Toggle(_screen, _lessonIndex);
        else if (action == "nextMusic") _audio.Next();
        else if (action.StartsWith("profile:", StringComparison.Ordinal)) { _profileView = action[8..]; SyncProfileCombos(); }
        else if (action.StartsWith("range:", StringComparison.Ordinal)) { _reportRange = action[6..]; SyncProfileCombos(); }
        else if (action.StartsWith("scope:", StringComparison.Ordinal)) { _reportScope = action[6..]; SyncProfileCombos(); }
        else if (action.StartsWith("export:", StringComparison.Ordinal)) ExportTelemetry(action[7..]);
        else if (action.StartsWith("buy:", StringComparison.Ordinal) && _user is not null)
        {
            _status = UpgradeSystem.Purchase(_user, action[4..]);
        }

        _input.Focus();
        Invalidate();
    }

    private void ExportTelemetry(string format)
    {
        if (_user is null) return;
        var snapshot = CurrentTelemetrySnapshot();
        var path = format switch
        {
            "csv" => TelemetryStore.ExportCsv(snapshot),
            "json" => TelemetryStore.ExportJson(snapshot),
            "pdf" => TelemetryStore.ExportPdf(snapshot),
            _ => ""
        };
        _status = string.IsNullOrWhiteSpace(path) ? "Export failed." : $"Exported {format.ToUpperInvariant()} report: {Path.GetFileName(path)}";
        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = Path.GetDirectoryName(path)!, UseShellExecute = true });
            }
            catch
            {
            }
        }
    }

    private void CompleteBoot()
    {
        if (_bootComplete)
        {
            return;
        }

        _bootComplete = true;
        BootAuth();
    }

    private void WriteTerminal(string line)
    {
        _terminal.Add(line);
        if (_terminal.Count > 18)
        {
            _terminal.RemoveAt(0);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        _hotspots.Clear();

        DrawShell(g);
        switch (_screen)
        {
            case AppScreen.Boot:
                DrawBoot(g);
                break;
            case AppScreen.Auth:
                DrawAuth(g);
                break;
            case AppScreen.Dashboard:
                DrawDashboard(g);
                break;
            case AppScreen.MissionSelect:
                DrawMissionSelect(g);
                break;
            case AppScreen.Game:
                DrawGame(g);
                break;
            case AppScreen.Upgrades:
                DrawUpgrades(g);
                break;
            case AppScreen.Profile:
                DrawProfile(g);
                break;
        }
    }

    private void DrawShell(Graphics g)
    {
        using var bg = new LinearGradientBrush(ClientRectangle, Palette.Bg, Palette.Bg2, 90);
        g.FillRectangle(bg, ClientRectangle);

        using var gridPen = new Pen(Palette.Grid);
        for (var y = 86; y < ClientSize.Height - 74; y += 32)
        {
            g.DrawLine(gridPen, 0, y, ClientSize.Width, y);
        }

        using var scanPen = new Pen(Color.FromArgb(18, Palette.Magenta));
        for (var y = 0; y < ClientSize.Height; y += 5)
        {
            g.DrawLine(scanPen, 0, y, ClientSize.Width, y);
        }

        using var titleBrush = new SolidBrush(Palette.Text);
        using var accentBrush = new SolidBrush(Palette.Cyan);
        g.DrawString("PYTHON CODER", _title, titleBrush, 24, 18);
        g.DrawString("LEARNING TERMINAL", _uiBold, accentBrush, 31, 58);

        if (_user is not null)
        {
            var right = $"{_user.Callsign} // {_user.RankName} // {_user.ScrapTokens} ST";
            var size = g.MeasureString(right, _uiBold);
            g.DrawString(right, _uiBold, titleBrush, ClientSize.Width - size.Width - 26, 26);
        }

        using var dim = new SolidBrush(Palette.Dim);
        var music = $"MUSIC: {_audio.CurrentTrack}";
        var musicSize = g.MeasureString(music, _monoSmall);
        g.DrawString(music, _monoSmall, dim, ClientSize.Width - musicSize.Width - 26, 56);
    }

    private void DrawBoot(Graphics g)
    {
        var rect = new Rectangle(128, 118, ClientSize.Width - 256, ClientSize.Height - 236);
        Panel(g, rect, Color.FromArgb(232, 5, 9, 15), Palette.Magenta);
        using var cyan = new SolidBrush(Palette.Cyan);
        using var green = new SolidBrush(Palette.Green);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        using var magenta = new SolidBrush(Palette.Magenta);

        g.DrawString("CYBERPYTHON BIOS", _hero, cyan, rect.X + 36, rect.Y + 28);
        g.DrawString("RETRO LEARNING OPERATING ENVIRONMENT", _uiBold, magenta, rect.X + 42, rect.Y + 88);

        var y = rect.Y + 142;
        for (var i = 0; i < _bootVisibleLines; i++)
        {
            var line = _bootLines[i];
            var brush = line.Contains("OK", StringComparison.Ordinal) || line.Contains("ONLINE", StringComparison.Ordinal) || line.Contains("ARMED", StringComparison.Ordinal)
                ? green
                : line.StartsWith("Press", StringComparison.Ordinal)
                    ? magenta
                    : string.IsNullOrWhiteSpace(line) ? dim : text;
            g.DrawString(line, _mono, brush, rect.X + 44, y);
            y += 28;
        }

        var progressWidth = rect.Width - 88;
        var progress = Math.Min(1f, (float)(_bootElapsed / 3.2));
        var bar = new Rectangle(rect.X + 44, rect.Bottom - 58, progressWidth, 12);
        using var barBack = new SolidBrush(Color.FromArgb(35, 20, 28, 38));
        using var barFill = new LinearGradientBrush(bar, Palette.Magenta, Palette.Cyan, 0f);
        g.FillRectangle(barBack, bar);
        g.FillRectangle(barFill, new Rectangle(bar.X, bar.Y, (int)(bar.Width * progress), bar.Height));
        g.DrawString("BOOT SEQUENCE", _monoSmall, dim, bar.X, bar.Y - 24);
    }

    private void DrawAuth(Graphics g)
    {
        var rect = new Rectangle(74, 112, ClientSize.Width - 148, ClientSize.Height - 210);
        Panel(g, rect, Color.FromArgb(236, 6, 10, 18), Palette.Cyan);
        using var cyan = new SolidBrush(Palette.Cyan);
        using var green = new SolidBrush(Palette.Green);
        using var magenta = new SolidBrush(Palette.Magenta);
        using var gold = new SolidBrush(Palette.Gold);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);

        g.DrawString("OPERATOR ACCESS STATION", _hero, cyan, rect.X + 34, rect.Y + 24);
        g.DrawString("Neon profile vault // local telemetry core // keyboard-first command shell", _ui, dim, rect.X + 40, rect.Y + 80);

        var left = new Rectangle(rect.X + 34, rect.Y + 124, (int)(rect.Width * 0.58), rect.Height - 184);
        var right = new Rectangle(left.Right + 24, left.Y, rect.Right - left.Right - 58, left.Height);
        Panel(g, left, Color.FromArgb(228, 5, 9, 15), Palette.Magenta);
        Panel(g, right, Color.FromArgb(228, 8, 12, 18), Palette.Gold);

        g.DrawString("COMMAND DECK", _uiBold, magenta, left.X + 18, left.Y + 16);
        var commandY = left.Y + 48;
        DrawAuthCommand(g, new Rectangle(left.X + 18, commandY, 154, 44), "(R) Register", "auth:register", Palette.Green);
        DrawAuthCommand(g, new Rectangle(left.X + 184, commandY, 132, 44), "(L) Login", "auth:login", Palette.Cyan);
        DrawAuthCommand(g, new Rectangle(left.X + 328, commandY, 158, 44), "(O) Operators", "auth:list", Palette.Gold);
        DrawAuthCommand(g, new Rectangle(left.X + 498, commandY, 118, 44), "(H) Help", "auth:help", Palette.Purple);

        var terminalRect = new Rectangle(left.X + 18, commandY + 68, left.Width - 36, left.Height - 94);
        using var terminalBack = new SolidBrush(Color.FromArgb(210, 2, 5, 9));
        using var terminalPen = new Pen(Color.FromArgb(130, Palette.Cyan));
        g.FillRoundedRectangle(terminalBack, terminalRect, 6);
        g.DrawRoundedRectangle(terminalPen, terminalRect, 6);
        g.DrawString("ACCESS SHELL", _monoSmall, cyan, terminalRect.X + 16, terminalRect.Y + 14);
        var y = terminalRect.Y + 46;
        foreach (var line in _terminal.TakeLast(Math.Max(1, (terminalRect.Height - 54) / 27)))
        {
            var brush = line.StartsWith(">", StringComparison.Ordinal) ? cyan : line.Contains("Operator", StringComparison.OrdinalIgnoreCase) ? gold : text;
            g.DrawString(line, _mono, brush, terminalRect.X + 18, y);
            y += 27;
        }

        DrawOperatorCard(g, right);
        g.DrawString("AUTH INPUT", _uiBold, dim, 206, ClientSize.Height - 52);
    }

    private void DrawAuthCommand(Graphics g, Rectangle rect, string label, string action, Color accent)
    {
        using var fill = new LinearGradientBrush(rect, Color.FromArgb(52, accent), Color.FromArgb(16, Palette.Bg), 0f);
        using var pen = new Pen(accent, 1.3f);
        using var brush = new SolidBrush(Palette.Text);
        g.FillRoundedRectangle(fill, rect, 6);
        g.DrawRoundedRectangle(pen, rect, 6);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, _uiBold, brush, rect, format);
        _hotspots.Add((rect, action));
    }

    private void DrawOperatorCard(Graphics g, Rectangle rect)
    {
        using var cyan = new SolidBrush(Palette.Cyan);
        using var green = new SolidBrush(Palette.Green);
        using var gold = new SolidBrush(Palette.Gold);
        using var magenta = new SolidBrush(Palette.Magenta);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        using var grid = new Pen(Color.FromArgb(55, Palette.Grid));

        g.DrawString("OPERATOR ID PREVIEW", _uiBold, gold, rect.X + 18, rect.Y + 16);
        var card = new Rectangle(rect.X + 20, rect.Y + 58, rect.Width - 40, 176);
        using var cardFill = new LinearGradientBrush(card, Color.FromArgb(40, Palette.Cyan), Color.FromArgb(35, Palette.Magenta), 0f);
        using var cardPen = new Pen(Palette.Cyan, 1.2f);
        g.FillRoundedRectangle(cardFill, card, 8);
        g.DrawRoundedRectangle(cardPen, card, 8);

        for (var yy = card.Y + 12; yy < card.Bottom - 10; yy += 18)
        {
            g.DrawLine(grid, card.X + 12, yy, card.Right - 12, yy);
        }

        var displayName = _authStep switch
        {
            1 => "ENTER FIRST NAME",
            2 => $"{_firstName} _",
            3 => $"{_firstName} {_lastName}",
            4 => "LOGIN CALLSIGN",
            _ => "UNVERIFIED OPERATOR"
        };
        var callsign = _authStep == 3 ? "CALLSIGN PENDING" : "ACCESS PENDING";
        g.DrawString(displayName, _title, text, card.X + 20, card.Y + 24);
        g.DrawString(callsign, _mono, cyan, card.X + 22, card.Y + 72);
        g.DrawString("RANK: LOCKED", _monoSmall, gold, card.X + 22, card.Y + 112);
        g.DrawString("TELEMETRY: STANDBY", _monoSmall, green, card.X + 172, card.Y + 112);
        g.DrawString("LOCAL PROFILE VAULT", _monoSmall, magenta, card.X + 22, card.Bottom - 34);

        var registry = ProfileStore.LoadRegistry().Users;
        g.DrawString("PROFILE VAULT", _uiBold, cyan, rect.X + 20, card.Bottom + 28);
        g.DrawString($"{registry.Count} registered operator{(registry.Count == 1 ? "" : "s")}", _title, text, rect.X + 20, card.Bottom + 58);
        g.DrawString("Keyboard matrix online", _monoSmall, green, rect.X + 22, card.Bottom + 106);
        g.DrawString("Python core linked", _monoSmall, green, rect.X + 22, card.Bottom + 130);
        g.DrawString("Telemetry database ready", _monoSmall, green, rect.X + 22, card.Bottom + 154);

        var listY = card.Bottom + 196;
        g.DrawString("RECENT OPERATORS", _uiBold, gold, rect.X + 20, listY);
        listY += 32;
        foreach (var op in registry.TakeLast(5))
        {
            g.DrawString(op.Callsign, _monoSmall, text, rect.X + 24, listY);
            g.DrawString(op.RankName, _monoSmall, dim, rect.Right - 118, listY);
            listY += 24;
        }
    }

    private void DrawDashboard(Graphics g)
    {
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString($"Welcome back, {_user?.FirstName ?? "Operator"}.", _hero, text, 42, 116);
        g.DrawString(_status, _ui, dim, 48, 172);

        var top = 230;
        DrawCard(g, new Rectangle(48, top, 300, 170), "DEPLOY", "Load the next incomplete mission, including any missed gap.", "deploy", Palette.Green);
        DrawCard(g, new Rectangle(378, top, 300, 170), "MISSION SELECT", "Choose or replay any mission.", "missionSelect", Palette.Cyan);
        DrawCard(g, new Rectangle(708, top, 300, 170), "HARDWARE LAB", "Spend scrap tokens on upgrades.", "upgrades", Palette.Gold);
        DrawCard(g, new Rectangle(1038, top, 300, 170), "PROFILE", "View records, score, XP, and rank.", "profile", Palette.Purple);

        DrawStatsBand(g, new Rectangle(48, 430, ClientSize.Width - 96, ClientSize.Height - 520));
        DrawNav(g);
    }

    private void DrawMissionSelect(Graphics g)
    {
        DrawNav(g);
        if (_user is null) return;
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);

        g.DrawString("MISSION SELECT", _hero, text, 42, 116);
        g.DrawString("Curriculum map: five learning missions, then one orange boss recap. Use mouse wheel, arrows, Page Up/Down, Home, or End.", _ui, dim, 48, 176);

        var rect = new Rectangle(46, 220, ClientSize.Width - 92, ClientSize.Height - 306);
        Panel(g, rect, Color.FromArgb(224, 8, 12, 20), Palette.Cyan);

        var viewport = new Rectangle(rect.X + 20, rect.Y + 20, rect.Width - 74, rect.Height - 40);
        var completedIndexes = TelemetryStore.CompletedMissionIndexes(_user.Callsign);
        var sections = MissionSections().ToArray();
        var contentHeight = sections.Sum(s => MissionSectionHeight(s.Count)) + Math.Max(0, sections.Length - 1) * 20;
        _missionSelectMaxScroll = Math.Max(0, contentHeight - viewport.Height);
        _missionSelectScroll = Math.Clamp(_missionSelectScroll, 0, _missionSelectMaxScroll);

        var saved = g.Save();
        g.SetClip(viewport);
        var y = viewport.Y - _missionSelectScroll;
        foreach (var section in sections)
        {
            var sectionHeight = MissionSectionHeight(section.Count);
            var sectionRect = new Rectangle(viewport.X, y, viewport.Width, sectionHeight);
            DrawMissionSection(g, section.Number, section.Start, section.Count, sectionRect, viewport, completedIndexes);
            y += sectionHeight + 20;
        }
        g.Restore(saved);

        DrawMissionScrollbar(g, new Rectangle(rect.Right - 38, viewport.Y, 18, viewport.Height), contentHeight, viewport.Height);
    }

    private IEnumerable<(int Number, int Start, int Count)> MissionSections()
    {
        var start = 0;
        var number = 1;
        while (start < Curriculum.BeginnerLessons.Count)
        {
            var count = 0;
            while (start + count < Curriculum.BeginnerLessons.Count)
            {
                count++;
                if (Curriculum.BeginnerLessons[start + count - 1].IsBoss)
                {
                    break;
                }
            }

            yield return (number, start, count);
            start += count;
            number++;
        }
    }

    private static int MissionSectionHeight(int lessonCount) => 136 + lessonCount * 82 + Math.Max(0, lessonCount - 1) * 10 + 22;

    private void DrawMissionSection(Graphics g, int sectionNumber, int start, int count, Rectangle rect, Rectangle viewport, HashSet<int> completedIndexes)
    {
        if (rect.Bottom < viewport.Top || rect.Top > viewport.Bottom)
        {
            return;
        }

        var accent = SectionAccent(sectionNumber);
        using var panelBack = new LinearGradientBrush(rect, Color.FromArgb(232, 7, 12, 22), Color.FromArgb(232, 15, 9, 25), 0f);
        using var border = new Pen(accent, 1.4f);
        using var glow = new Pen(Color.FromArgb(70, accent), 4f);
        g.FillRoundedRectangle(panelBack, rect, 8);
        g.DrawRoundedRectangle(glow, rect, 8);
        g.DrawRoundedRectangle(border, rect, 8);

        using var titleBrush = new SolidBrush(Palette.Text);
        using var accentBrush = new SolidBrush(accent);
        using var dim = new SolidBrush(Palette.Dim);
        using var green = new SolidBrush(Palette.Green);
        var sectionLessons = Curriculum.BeginnerLessons.Skip(start).Take(count).ToArray();
        var completed = Enumerable.Range(start, count).Count(i => completedIndexes.Contains(i) || i < (_user?.MissionsCompleted ?? 0));
        var sectionTitle = SectionTitle(sectionNumber);
        var topics = string.Join(" // ", sectionLessons.Where(l => !l.IsBoss).SelectMany(l => l.Lines.Select(line => line.Term)).Distinct().Take(8));

        g.DrawString($"SECTION {sectionNumber:00}", _monoSmall, accentBrush, rect.X + 18, rect.Y + 14);
        DrawWrapped(g, sectionTitle, _missionSectionTitle, titleBrush, new RectangleF(rect.X + 18, rect.Y + 38, 370, 54));
        DrawWrapped(g, SectionDescription(sectionNumber), _ui, dim, new RectangleF(rect.X + 408, rect.Y + 18, rect.Width - 650, 44));
        DrawWrapped(g, $"Covers: {topics}", _monoSmall, dim, new RectangleF(rect.X + 408, rect.Y + 70, rect.Width - 650, 34));
        DrawStatusPill(g, new Rectangle(rect.Right - 196, rect.Y + 28, 156, 34), $"{completed}/{count} complete", completed == count ? Palette.Green : accent);

        var rowY = rect.Y + 128;
        for (var offset = 0; offset < count; offset++)
        {
            var lessonIndex = start + offset;
            var lesson = Curriculum.BeginnerLessons[lessonIndex];
            var row = new Rectangle(rect.X + 16, rowY, rect.Width - 32, 82);
            DrawMissionRow(g, sectionNumber, offset + 1, lessonIndex, lesson, row, viewport, completedIndexes.Contains(lessonIndex) || lessonIndex < (_user?.MissionsCompleted ?? 0));
            rowY += 92;
        }
    }

    private void DrawMissionRow(Graphics g, int sectionNumber, int levelNumber, int lessonIndex, Lesson lesson, Rectangle row, Rectangle viewport, bool completed)
    {
        if (row.Bottom < viewport.Top || row.Top > viewport.Bottom)
        {
            return;
        }

        var isBoss = lesson.IsBoss;
        var accent = isBoss ? Palette.Orange : completed ? Palette.Green : SectionAccent(sectionNumber);
        using var back = new LinearGradientBrush(row, Color.FromArgb(218, 5, 10, 18), Color.FromArgb(218, 12, 19, 31), 0f);
        using var border = new Pen(Color.FromArgb(isBoss ? 230 : 170, accent), isBoss ? 1.8f : 1.2f);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        using var accentBrush = new SolidBrush(accent);
        g.FillRoundedRectangle(back, row, 6);
        g.DrawRoundedRectangle(border, row, 6);

        var label = isBoss ? "BOSS BATTLE" : $"LEVEL {levelNumber:00}";
        DrawStatusPill(g, new Rectangle(row.X + 12, row.Y + 20, 124, 38), label, accent);

        var title = isBoss ? lesson.Title.ToUpperInvariant() : lesson.Title;
        DrawWrapped(g, title, _uiBold, text, new RectangleF(row.X + 154, row.Y + 12, row.Width - 570, 28));
        DrawWrapped(g, lesson.Goal, _ui, dim, new RectangleF(row.X + 154, row.Y + 43, row.Width - 570, 30));

        var topics = string.Join(", ", lesson.Lines.Select(l => l.Term).Distinct().Take(6));
        var topicRect = new Rectangle(row.Right - 386, row.Y + 14, 248, 54);
        using var topicBack = new SolidBrush(Color.FromArgb(120, 4, 8, 13));
        using var topicBorder = new Pen(Color.FromArgb(100, accent));
        g.FillRoundedRectangle(topicBack, topicRect, 5);
        g.DrawRoundedRectangle(topicBorder, topicRect, 5);
        DrawWrapped(g, topics, _monoSmall, accentBrush, new RectangleF(topicRect.X + 10, topicRect.Y + 7, topicRect.Width - 20, topicRect.Height - 10));

        DrawStatusPill(g, new Rectangle(row.Right - 116, row.Y + 24, 94, 34), completed ? "DONE" : "OPEN", completed ? Palette.Green : Palette.Cyan);
        _hotspots.Add((row, $"mission:{lessonIndex}"));
    }

    private void DrawMissionScrollbar(Graphics g, Rectangle track, int contentHeight, int viewportHeight)
    {
        _missionSelectScrollbarTrack = track;
        _missionSelectContentHeight = contentHeight;
        _missionSelectViewportHeight = viewportHeight;
        using var trackBrush = new SolidBrush(Color.FromArgb(130, 4, 8, 14));
        using var trackPen = new Pen(Color.FromArgb(110, Palette.Cyan));
        g.FillRoundedRectangle(trackBrush, track, 5);
        g.DrawRoundedRectangle(trackPen, track, 5);
        if (contentHeight <= viewportHeight)
        {
            using var full = new SolidBrush(Color.FromArgb(170, Palette.Green));
            g.FillRoundedRectangle(full, new Rectangle(track.X + 2, track.Y + 2, track.Width - 4, track.Height - 4), 4);
            return;
        }

        var thumb = MissionSelectScrollbarThumb();
        using var fill = new LinearGradientBrush(thumb, Palette.Cyan, Palette.Magenta, 90f);
        g.FillRoundedRectangle(fill, thumb, 4);
    }

    private Rectangle MissionSelectScrollbarThumb()
    {
        if (_missionSelectScrollbarTrack == Rectangle.Empty || _missionSelectContentHeight <= _missionSelectViewportHeight)
        {
            return Rectangle.Empty;
        }

        var track = _missionSelectScrollbarTrack;
        var thumbHeight = Math.Max(46, (int)(track.Height * (_missionSelectViewportHeight / (float)Math.Max(1, _missionSelectContentHeight))));
        var thumbTravel = Math.Max(1, track.Height - thumbHeight - 4);
        var thumbY = track.Y + 2 + (int)(thumbTravel * (_missionSelectScroll / (float)Math.Max(1, _missionSelectMaxScroll)));
        return new Rectangle(track.X + 2, thumbY, track.Width - 4, thumbHeight);
    }

    private void SetMissionSelectScrollFromScrollbarY(int thumbTop)
    {
        if (_missionSelectScrollbarTrack == Rectangle.Empty || _missionSelectMaxScroll <= 0)
        {
            return;
        }

        var thumb = MissionSelectScrollbarThumb();
        if (thumb == Rectangle.Empty)
        {
            return;
        }

        var track = _missionSelectScrollbarTrack;
        var thumbTravel = Math.Max(1, track.Height - thumb.Height - 4);
        var clampedTop = Math.Clamp(thumbTop, track.Y + 2, track.Y + 2 + thumbTravel);
        var percent = (clampedTop - (track.Y + 2)) / (float)thumbTravel;
        _missionSelectScroll = Math.Clamp((int)Math.Round(percent * _missionSelectMaxScroll), 0, _missionSelectMaxScroll);
        Invalidate();
    }

    private void DrawStatusPill(Graphics g, Rectangle rect, string label, Color accent)
    {
        using var back = new SolidBrush(Color.FromArgb(60, accent));
        using var border = new Pen(accent, 1.1f);
        using var brush = new SolidBrush(Palette.Text);
        g.FillRoundedRectangle(back, rect, 5);
        g.DrawRoundedRectangle(border, rect, 5);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        g.DrawString(label, _monoSmall, brush, rect, format);
    }

    private static Color SectionAccent(int sectionNumber) => ((sectionNumber - 1) % 6) switch
    {
        0 => Palette.Cyan,
        1 => Palette.Green,
        2 => Palette.Purple,
        3 => Palette.Gold,
        4 => Palette.Magenta,
        _ => Palette.Cyan
    };

    private static string SectionTitle(int sectionNumber) => sectionNumber switch
    {
        1 => "First Output And Simple Values",
        2 => "Readable Names And Text Building",
        3 => "Expressions And Ordered Data",
        4 => "Collections And Decision Gates",
        5 => "Logic And Loop Conveyors",
        6 => "Functions And Mini Programs",
        7 => "User Data And Imports",
        8 => "Debugging And Safe Code",
        9 => "Integrated Practice",
        10 => "Final Python Readiness",
        _ => "Python Skill Block"
    };

    private static string SectionDescription(int sectionNumber) => sectionNumber switch
    {
        1 => "Students learn code order, print output, comments, strings, and syntax symbols.",
        2 => "Students store text, integers, floats, booleans, and empty placeholder values.",
        3 => "Students practice naming, text composition, f-strings, math, and reassignment.",
        4 => "Students inspect types, build lists, index values, append items, and combine a mini inventory.",
        5 => "Students use dictionaries, lookups, comparisons, if branches, and if/else decisions.",
        6 => "Students compare equality, use elif, combine and/or/not, and reason about access logic.",
        7 => "Students trace short range loops, list loops, accumulators, and safe while loops.",
        8 => "Students define, call, parameterize, and return values from focused functions.",
        9 => "Students model input, conversion, imports, file path values, and settings data.",
        10 => "Students read errors, use try/except, write checks, design small functions, and complete a mini program.",
        _ => "Students review and combine previously introduced Python concepts."
    };

    private void DrawStatsBand(Graphics g, Rectangle rect)
    {
        Panel(g, rect, Color.FromArgb(220, 10, 16, 25), Palette.Cyan);
        if (_user is null) return;
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        var snapshot = CurrentTelemetrySnapshot();
        g.DrawString("OPERATOR TELEMETRY", _uiBold, dim, rect.X + 22, rect.Y + 20);
        g.DrawString($"{_user.Xp} XP", _title, text, rect.X + 22, rect.Y + 54);
        g.DrawString($"Total Score: {_user.TotalScore}", _uiBold, text, rect.X + 280, rect.Y + 64);
        g.DrawString($"Missions: {_user.MissionsCompleted}", _uiBold, text, rect.X + 520, rect.Y + 64);
        g.DrawString($"Best WPM: {_user.BestWpm}", _uiBold, text, rect.X + 760, rect.Y + 64);
        g.DrawString($"Minutes: {snapshot.EngagementMinutes:0.0}", _uiBold, text, rect.X + 980, rect.Y + 64);
        if (rect.Height > 180)
        {
            DrawLineChart(g, new Rectangle(rect.X + 24, rect.Y + 112, rect.Width / 2 - 44, rect.Height - 134), "Dashboard Trend: Accuracy By Session", snapshot.Sessions.Select(x => x.Accuracy).ToList(), Palette.Cyan);
            var concepts = snapshot.Concepts.Count == 0
                ? Curriculum.BeginnerLessons.Take(5).Select(l => (l.Lines.FirstOrDefault()?.Term ?? l.Title, 0.0)).ToList()
                : snapshot.Concepts.Take(5).Select(c => (c.Concept, c.Mastery)).ToList();
            DrawHorizontalBars(g, new Rectangle(rect.X + rect.Width / 2 + 10, rect.Y + 112, rect.Width / 2 - 34, rect.Height - 134), "Concept Mastery Snapshot", concepts);
        }
    }

    private void DrawGame(Graphics g)
    {
        DrawButton(g, new Rectangle(26, ClientSize.Height - 58, 92, 36), "Dashboard", "dashboard");
        DrawButton(g, new Rectangle(126, ClientSize.Height - 58, 76, 36), "Pause", "pause");
        DrawButton(g, new Rectangle(210, ClientSize.Height - 58, 74, 36), "-help", "help");
        DrawButton(g, new Rectangle(ClientSize.Width - 116, ClientSize.Height - 58, 90, 36), "Restart", "restart");

        DrawHud(g);
        DrawInputRail(g);
        DrawCodeViewer(g);
        DrawLessonPanel(g);
        DrawRisingSnippet(g);
        DrawBossTimerPanel(g);
        DrawFloatingTexts(g);

        if (_showCompile) DrawCompileOverlay(g);
        else if (_showHelp) DrawHelpOverlay(g);
        else if (_paused) DrawPauseOverlay(g);
    }

    private void DrawHud(Graphics g)
    {
        var rect = new Rectangle(300, 92, ClientSize.Width - 616, 86);
        Panel(g, rect, Color.FromArgb(220, 8, 13, 21), Palette.Cyan);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString(CurrentLesson.Title, _uiBold, text, rect.X + 18, rect.Y + 14);
        using var statusBrush = new SolidBrush(CurrentLesson.IsBoss ? Palette.Orange : Palette.Dim);
        g.DrawString(CurrentLesson.IsBoss ? "A VIRUS HAS CORRUPTED THE CODE" : _status, _uiBold, statusBrush, rect.X + 18, rect.Y + 42);
        var stats = $"Score {_score.Score}   Combo {_score.Combo} x{_score.Multiplier:0.#}";
        var size = g.MeasureString(stats, _uiBold);
        g.DrawString(stats, _uiBold, text, rect.Right - size.Width - 18, rect.Y + 30);
    }

    private void DrawBossTimerPanel(Graphics g)
    {
        if (!CurrentLesson.IsBoss || _showCompile || _showHelp || _paused || IsLessonComplete)
        {
            return;
        }

        var seconds = Math.Max(0, (int)Math.Ceiling(_bossTimeRemaining));
        var critical = seconds <= 10;
        var lane = new Rectangle(300, 192, ClientSize.Width - 616, ClientSize.Height - 276);
        var rect = new Rectangle(lane.Right - 244, lane.Y + 12, 232, 58);
        using var back = new LinearGradientBrush(rect, Color.FromArgb(238, 22, 4, 22), Color.FromArgb(238, 55, 12, 7), 0f);
        using var border = new Pen(critical ? Palette.HotRed : Palette.Orange, critical ? 2.4f : 1.8f);
        using var glow = new Pen(Color.FromArgb(90, critical ? Palette.HotRed : Palette.Magenta), 5f);
        g.FillRoundedRectangle(back, rect, 7);
        g.DrawRoundedRectangle(glow, rect, 7);
        g.DrawRoundedRectangle(border, rect, 7);

        using var scanPen = new Pen(Color.FromArgb(42, Palette.Gold));
        for (var y = rect.Y + 7; y < rect.Bottom - 3; y += 7)
        {
            g.DrawLine(scanPen, rect.X + 8, y, rect.Right - 8, y);
        }

        var fill = Math.Clamp((float)_bossTimeRemaining / 60f, 0f, 1f);
        var bar = new Rectangle(rect.X + 12, rect.Bottom - 14, rect.Width - 24, 6);
        using var barBack = new SolidBrush(Color.FromArgb(160, 7, 5, 10));
        using var barFill = new LinearGradientBrush(bar, Palette.HotRed, Palette.Gold, 0f);
        g.FillRectangle(barBack, bar);
        g.FillRectangle(barFill, new Rectangle(bar.X, bar.Y, Math.Max(0, (int)(bar.Width * fill)), bar.Height));

        using var label = new SolidBrush(Palette.Orange);
        using var timeBrush = new SolidBrush(critical ? Palette.HotRed : Palette.Text);
        g.DrawString("VIRUS CLOCK", _monoSmall, label, rect.X + 14, rect.Y + 8);
        var value = $"{seconds:00}s";
        var valueSize = g.MeasureString(value, _title);
        g.DrawString(value, _title, timeBrush, rect.Right - valueSize.Width - 16, rect.Y + 12);
    }

    private void DrawCodeViewer(Graphics g)
    {
        var rect = new Rectangle(22, 104, 252, ClientSize.Height - 186);
        Panel(g, rect, Color.FromArgb(226, 7, 10, 16), Palette.Green);
        using var head = new SolidBrush(Palette.Green);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString("PYTHON COMPILER STACK", _uiBold, head, rect.X + 14, rect.Y + 14);
        g.DrawString("assembled_code.py", _monoSmall, dim, rect.X + 14, rect.Y + 36);
        var y = rect.Y + 66;
        for (var i = 0; i < _completedLines.Count; i++)
        {
            g.DrawString((i + 1).ToString().PadLeft(2), _monoSmall, dim, rect.X + 12, y + 2);
            DrawPythonHighlighted(g, _completedLines[i], _monoSmall, rect.X + 42, y);
            y += 24;
        }
    }

    private void DrawLessonPanel(Graphics g)
    {
        var rect = new Rectangle(ClientSize.Width - 286, 104, 264, ClientSize.Height - 186);
        Panel(g, rect, Color.FromArgb(226, 12, 10, 18), Palette.Gold);
        using var head = new SolidBrush(CurrentLesson.IsBoss ? Palette.Orange : Palette.Gold);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString(CurrentLesson.IsBoss ? "CODE MEDIC MODE" : "LINE EXPLAINER", _uiBold, head, rect.X + 14, rect.Y + 14);
        if (!IsLessonComplete)
        {
            DrawWrapped(g, CurrentLine.Term, _uiBold, head, new RectangleF(rect.X + 14, rect.Y + 54, rect.Width - 28, 32));
            DrawWrapped(g, CurrentLine.Explanation, _ui, text, new RectangleF(rect.X + 14, rect.Y + 90, rect.Width - 28, 132));
            g.DrawString("Usage", _uiBold, head, rect.X + 14, rect.Y + 244);
            DrawWrapped(g, CurrentLine.Usage, _ui, dim, new RectangleF(rect.X + 14, rect.Y + 274, rect.Width - 28, 92));
        }
        DrawWrapped(g, _feedback, _ui, text, new RectangleF(rect.X + 14, rect.Bottom - 108, rect.Width - 28, 88));
    }

    private void DrawRisingSnippet(Graphics g)
    {
        if (IsLessonComplete) return;
        var lane = new Rectangle(300, 192, ClientSize.Width - 616, ClientSize.Height - 276);
        using var pen = new Pen(Palette.Grid);
        g.DrawRectangle(pen, lane);

        using var laneLabel = new SolidBrush(Palette.Dim);
        g.DrawString("CENTER LANE: RISING TARGET CODE", _monoSmall, laneLabel, lane.X + 12, lane.Y + 10);

        var rectHeight = CurrentLesson.IsBoss ? 160 : 84;
        var rect = new RectangleF(lane.X + 36, (float)_snippetY - rectHeight / 2f, lane.Width - 72, rectHeight);
        using var brush = new LinearGradientBrush(rect, Color.FromArgb(236, 12, 25, 36), CurrentLesson.IsBoss ? Color.FromArgb(236, 72, 34, 8) : Color.FromArgb(236, 31, 18, 50), 0f);
        using var border = new Pen(CurrentLesson.IsBoss ? Palette.Orange : Palette.Cyan, 2);
        g.FillRoundedRectangle(brush, rect, 8);
        g.DrawRoundedRectangle(border, rect, 8);
        if (CurrentLesson.IsBoss)
        {
            DrawBossCorruptionFrame(g, rect);
        }

        using var labelBrush = new SolidBrush(CurrentLesson.IsBoss ? Palette.Orange : Palette.Magenta);
        g.DrawString(CurrentLesson.IsBoss ? "A VIRUS HAS CORRUPTED THE CODE" : "TARGET LINE", _monoSmall, labelBrush, rect.X + 18, rect.Y + 10);
        if (CurrentLesson.IsBoss)
        {
            DrawBossCorruptedCode(g, CurrentCorruptedLine, _monoBold, rect.X + 18, rect.Y + 40);
            DrawBossHealthBar(g, new Rectangle((int)rect.X + 18, (int)rect.Y + 84, (int)rect.Width - 36, 26));
            using var compileBrush = new SolidBrush(DateTime.UtcNow < _bossHintUntilUtc ? Palette.HotRed : Palette.Orange);
            g.DrawString(DateTime.UtcNow < _bossHintUntilUtc ? "COMPILE ERROR HINT ACTIVE" : "TYPE THE REPAIRED CODE IN THE INPUT RAIL", _monoSmall, compileBrush, rect.X + 18, rect.Y + 122);
        }
        else
        {
            DrawPythonCompared(g, CurrentLine.Text, _input.Text, _monoBold, rect.X + 18, rect.Y + 36);
        }

        if (!string.IsNullOrEmpty(_input.Text))
        {
            using var hint = new SolidBrush(Palette.Dim);
            g.DrawString("green = matching typed character   red = wrong typed character", _monoSmall, hint, rect.X + 18, rect.Bottom + 8);
        }
    }

    private void DrawInputRail(Graphics g)
    {
        var rect = new Rectangle(300, ClientSize.Height - 70, ClientSize.Width - 616, 56);
        using var back = new SolidBrush(Color.FromArgb(210, 6, 9, 13));
        using var border = new Pen(Palette.Magenta, 1.4f);
        g.FillRoundedRectangle(back, rect, 8);
        g.DrawRoundedRectangle(border, rect, 8);
        using var label = new SolidBrush(Palette.Magenta);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString("USER INPUT RAIL", _monoSmall, label, rect.X + 14, rect.Y + 8);
        g.DrawString("typing happens in the bottom field only", _monoSmall, dim, rect.Right - 278, rect.Y + 8);
    }

    private void DrawBossCorruptedCode(Graphics g, string code, Font font, float x, float y)
    {
        var cursor = x;
        var charWidth = GetMonoCharWidth(g, font);
        var hintActive = DateTime.UtcNow < _bossHintUntilUtc && _bossHintStart >= 0;
        for (var i = 0; i < code.Length; i++)
        {
            if (hintActive && i >= _bossHintStart && i < _bossHintStart + _bossHintLength)
            {
                using var back = new SolidBrush(Color.FromArgb(120, Palette.HotRed));
                g.FillRectangle(back, cursor - 1, y - 2, charWidth + 2, font.Height + 2);
            }

            DrawCodeText(g, code[i].ToString(), font, Palette.Orange, cursor, y);
            cursor += charWidth;
        }
    }

    private void DrawBossCorruptionFrame(Graphics g, RectangleF rect)
    {
        using var hot = new Pen(Color.FromArgb(135, Palette.HotRed), 1.2f);
        using var magenta = new Pen(Color.FromArgb(105, Palette.Magenta), 1f);
        using var gold = new Pen(Color.FromArgb(115, Palette.Gold), 1f);

        var left = rect.X + 10;
        var right = rect.Right - 10;
        var top = rect.Y + 30;
        var bottom = rect.Bottom - 12;
        for (var y = top; y < bottom; y += 13)
        {
            g.DrawLine(magenta, left, y, right, y);
        }

        g.DrawLine(hot, rect.X + 14, rect.Y + 28, rect.X + 46, rect.Y + 28);
        g.DrawLine(hot, rect.Right - 46, rect.Y + 28, rect.Right - 14, rect.Y + 28);
        g.DrawLine(hot, rect.X + 14, rect.Bottom - 18, rect.X + 46, rect.Bottom - 18);
        g.DrawLine(hot, rect.Right - 46, rect.Bottom - 18, rect.Right - 14, rect.Bottom - 18);

        for (var i = 0; i < 4; i++)
        {
            var x = rect.X + 18 + i * 34;
            g.DrawLine(gold, x, rect.Bottom - 34, x + 18, rect.Bottom - 34);
            g.DrawLine(gold, rect.Right - 18 - i * 34, rect.Y + 46, rect.Right - 36 - i * 34, rect.Y + 46);
        }
    }

    private void DrawBossHealthBar(Graphics g, Rectangle rect)
    {
        var total = Math.Max(1, CurrentLesson.Lines.Count);
        var remaining = Math.Max(0, total - _lineIndex);
        var pct = remaining / (float)total;
        using var back = new SolidBrush(Color.FromArgb(230, 15, 4, 10));
        using var border = new Pen(Palette.HotRed, 1.5f);
        g.FillRoundedRectangle(back, rect, 5);
        g.DrawRoundedRectangle(border, rect, 5);

        var labelRect = new Rectangle(rect.X + 10, rect.Y - 24, 260, 20);
        using var label = new SolidBrush(Palette.HotRed);
        using var labelBack = new SolidBrush(Color.FromArgb(180, 8, 4, 12));
        g.FillRoundedRectangle(labelBack, new Rectangle(rect.X + 4, rect.Y - 27, 132, 22), 4);
        g.DrawString("VIRUS HEALTH", _monoSmall, label, labelRect);

        var inner = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
        var fillWidth = pct <= 0 ? 0 : Math.Max(1, (int)(inner.Width * pct));
        if (fillWidth > 0)
        {
            using var fill = new LinearGradientBrush(new Rectangle(inner.X, inner.Y, fillWidth, inner.Height), Palette.HotRed, Palette.Orange, 0f);
            g.FillRectangle(fill, inner.X, inner.Y, fillWidth, inner.Height);
        }

        using var tickPen = new Pen(Color.FromArgb(180, Palette.Gold));
        for (var i = 1; i < total; i++)
        {
            var x = inner.X + i * inner.Width / total;
            g.DrawLine(tickPen, x, inner.Y, x, inner.Bottom);
        }
    }

    private void DrawFloatingTexts(Graphics g)
    {
        foreach (var item in _floatingTexts)
        {
            var alpha = Math.Clamp(1.0 - item.Age / 1.15, 0, 1);
            using var brush = new SolidBrush(Color.FromArgb((int)(255 * alpha), item.Color));
            g.DrawString(item.Text, _title, brush, item.X, item.Y);
        }
    }

    private void DrawCompileOverlay(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(218, 0, 0, 0));
        g.FillRectangle(shade, ClientRectangle);

        var rect = new Rectangle(82, 96, ClientSize.Width - 164, ClientSize.Height - 184);
        Panel(g, rect, Color.FromArgb(248, 5, 8, 16), Palette.Green);

        using var cyan = new SolidBrush(Palette.Cyan);
        using var green = new SolidBrush(Palette.Green);
        using var magenta = new SolidBrush(Palette.Magenta);
        using var gold = new SolidBrush(Palette.Gold);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);

        g.DrawString(CurrentLesson.IsBoss ? "VIRUS RECOVERY SCAN" : "COMPILE + DATA FLOW DEMO", _title, CurrentLesson.IsBoss ? gold : green, rect.X + 26, rect.Y + 20);
        g.DrawString(CurrentLesson.Title, _uiBold, cyan, rect.X + 32, rect.Y + 58);
        g.DrawString(CurrentLesson.IsBoss ? "Detect -> Diagnose -> Patch -> Re-run. The repaired line proves the previous three lessons stuck." : "Python scans the code, creates memory, follows control flow, then produces output.", _ui, dim, rect.X + 32, rect.Y + 84);

        var footerTop = rect.Bottom - 118;
        var codeRect = new Rectangle(rect.X + 26, rect.Y + 126, Math.Max(260, Math.Min(390, rect.Width / 3)), Math.Max(180, footerTop - rect.Y - 142));
        var traceRect = new Rectangle(codeRect.Right + 24, rect.Y + 126, Math.Max(320, rect.Right - codeRect.Right - 50), codeRect.Height);
        Panel(g, codeRect, Color.FromArgb(232, 7, 10, 16), Palette.Cyan);
        Panel(g, traceRect, Color.FromArgb(232, 9, 12, 20), Palette.Magenta);

        g.DrawString(CurrentLesson.IsBoss ? "INFECTED CODE SCAN" : "COMPILER SCAN", _uiBold, CurrentLesson.IsBoss ? gold : cyan, codeRect.X + 16, codeRect.Y + 14);
        const double compileLineRate = 0.36;
        const double typeCharsPerSecond = 18.0;
        var rowCount = Math.Max(CurrentLesson.Lines.Count, CurrentLesson.Trace.Count);
        var rowTop = codeRect.Y + 52;
        var rowGap = 10;
        var availableRowsHeight = Math.Max(86, codeRect.Bottom - rowTop - 18);
        var idealRowHeight = (availableRowsHeight - rowGap * Math.Max(0, rowCount - 1)) / Math.Max(1, rowCount);
        var rowHeight = Math.Clamp(idealRowHeight, 86, 104);
        var rowsFit = Math.Max(1, (availableRowsHeight + rowGap) / (rowHeight + rowGap));
        var scanLine = Math.Min(CurrentLesson.Lines.Count - 1, (int)(_compileElapsed * compileLineRate));
        var visibleSteps = Math.Min(CurrentLesson.Trace.Count, Math.Max(1, (int)(_compileElapsed * compileLineRate) + 1));
        var activeRow = Math.Max(scanLine, visibleSteps - 1);
        var firstRow = Math.Clamp(activeRow - rowsFit + 1, 0, Math.Max(0, rowCount - rowsFit));
        var lastRow = Math.Min(rowCount, firstRow + rowsFit);
        for (var i = firstRow; i < Math.Min(CurrentLesson.Lines.Count, lastRow); i++)
        {
            var visibleIndex = i - firstRow;
            var rowRect = new Rectangle(codeRect.X + 12, rowTop + visibleIndex * (rowHeight + rowGap), codeRect.Width - 24, rowHeight);
            var lineY = rowRect.Y + rowRect.Height / 2 - 9;
            var lineProgress = Math.Clamp((float)((_compileElapsed * compileLineRate) - i), 0f, 1f);
            using var rowBack = new SolidBrush(i == scanLine ? Color.FromArgb(42, Palette.Cyan) : Color.FromArgb(22, Palette.Cyan));
            g.FillRoundedRectangle(rowBack, rowRect, 5);
            if (lineProgress > 0)
            {
                var progressWidth = Math.Max(1, (int)Math.Ceiling((rowRect.Width - 46) * lineProgress));
                var progressRect = new Rectangle(rowRect.X + 34, rowRect.Y, progressWidth, rowRect.Height);
                using var progressBrush = new LinearGradientBrush(progressRect, Color.FromArgb(28, Palette.Magenta), Color.FromArgb(58, Palette.Cyan), 0f);
                g.FillRoundedRectangle(progressBrush, progressRect, 5);
                using var terminalBrush = new SolidBrush(Color.FromArgb(150, Palette.Cyan));
                g.DrawString(">", _monoBold, terminalBrush, Math.Min(progressRect.Right + 2, rowRect.Right - 18), lineY - 2);
            }

            if (i == scanLine)
            {
                using var scanPen = new Pen(Palette.Cyan, 2);
                var sweepX = rowRect.X + 10 + (int)(((Math.Sin(_compileElapsed * 6) + 1) / 2) * (rowRect.Width - 22));
                g.DrawLine(scanPen, sweepX, rowRect.Y + 6, sweepX, rowRect.Bottom - 6);
            }

            g.DrawString((i + 1).ToString().PadLeft(2), _monoSmall, dim, rowRect.X + 8, lineY);
            if (CurrentLesson.IsBoss)
            {
                var corruptedLine = CorruptedLineFor(i);
                DrawCodeText(g, corruptedLine, _monoSmall, Palette.Orange, rowRect.X + 42, lineY);
                DrawCodeText(g, "  ->  ", _monoSmall, Palette.Dim, rowRect.X + 42 + corruptedLine.Length * GetMonoCharWidth(g, _monoSmall), lineY);
                DrawPythonHighlighted(g, CurrentLesson.Lines[i].Text, _monoSmall, rowRect.X + 88 + corruptedLine.Length * GetMonoCharWidth(g, _monoSmall), lineY);
            }
            else
            {
                DrawPythonHighlighted(g, CurrentLesson.Lines[i].Text, _monoSmall, rowRect.X + 42, lineY);
            }
        }

        g.DrawString("RUNTIME TRACE", _uiBold, magenta, traceRect.X + 18, traceRect.Y + 14);
        for (var i = Math.Max(firstRow, 0); i < Math.Min(visibleSteps, lastRow); i++)
        {
            var step = CurrentLesson.Trace[i];
            var active = i == visibleSteps - 1;
            var stepElapsed = Math.Max(0, _compileElapsed - i / compileLineRate);
            var visibleIndex = i - firstRow;
            var stepRect = new Rectangle(traceRect.X + 16, rowTop + visibleIndex * (rowHeight + rowGap), traceRect.Width - 32, rowHeight);
            using var stepBack = new SolidBrush(active ? Color.FromArgb(42, Palette.Magenta) : Color.FromArgb(24, Palette.Cyan));
            using var stepPen = new Pen(TraceColor(step.Kind), active ? 2f : 1f);
            g.FillRoundedRectangle(stepBack, stepRect, 6);
            g.DrawRoundedRectangle(stepPen, stepRect, 6);

            if (i < CurrentLesson.Lines.Count)
            {
                using var connector = new Pen(Color.FromArgb(active ? 130 : 60, TraceColor(step.Kind)), active ? 1.6f : 1f);
                var connectorY = stepRect.Y + stepRect.Height / 2;
                g.DrawLine(connector, codeRect.Right + 3, connectorY, traceRect.X - 7, connectorY);
            }

            var leftLaneX = stepRect.X + 120;
            var leftLaneWidth = stepRect.Right - leftLaneX - 18;
            var contentY = stepRect.Y + Math.Max(10, (stepRect.Height - 72) / 2);
            DrawDataChip(g, new Rectangle(stepRect.X + 10, contentY, 98, 24), step.Kind.ToString().ToUpperInvariant(), TraceColor(step.Kind));
            DrawWrapped(g, TypeReveal(step.Title, stepElapsed, typeCharsPerSecond), _uiBold, text, new RectangleF(leftLaneX, contentY - 1, leftLaneWidth, 24));
            DrawWrapped(g, TypeReveal(step.Detail, Math.Max(0, stepElapsed - 0.45), typeCharsPerSecond), _ui, dim, new RectangleF(leftLaneX, contentY + 27, leftLaneWidth, 28));
            if (!string.IsNullOrWhiteSpace(step.DataBefore))
            {
                DrawWrapped(g, TypeReveal(step.DataBefore, Math.Max(0, stepElapsed - 0.9), typeCharsPerSecond), _monoSmall, gold, new RectangleF(leftLaneX, Math.Min(stepRect.Bottom - 58, contentY + 56), leftLaneWidth, 22));
            }

            if (!string.IsNullOrWhiteSpace(step.DataAfter))
            {
                var typedData = TypeReveal(step.DataAfter, Math.Max(0, stepElapsed - 1.25), typeCharsPerSecond);
                DrawDataChipToFit(g, stepRect, typedData, TraceColor(step.Kind));
            }
        }

        var progress = CurrentLesson.Trace.Count == 0 ? 1 : Math.Min(1f, (float)(_compileElapsed / Math.Max(2.8, CurrentLesson.Trace.Count / compileLineRate + 1.6)));
        var actionLeft = rect.Right - 610;
        var barWidth = Math.Max(120, actionLeft - (rect.X + 30) - 24);
        var bar = new Rectangle(rect.X + 30, rect.Bottom - 48, barWidth, 12);
        using var barBack = new SolidBrush(Color.FromArgb(60, 20, 25, 38));
        using var barFill = new LinearGradientBrush(bar, Palette.Magenta, Palette.Green, 0f);
        g.FillRectangle(barBack, bar);
        g.FillRectangle(barFill, new Rectangle(bar.X, bar.Y, (int)(bar.Width * progress), bar.Height));
        g.DrawString("DATA FLOW REPLAY", _monoSmall, dim, bar.X, bar.Y - 24);

        DrawButton(g, new Rectangle(rect.Right - 610, rect.Bottom - 62, 122, 42), "(R) Repeat", "repeatMission");
        DrawButton(g, new Rectangle(rect.Right - 478, rect.Bottom - 62, 132, 42), "(S) Save/Edit", "saveEditMission");
        DrawButton(g, new Rectangle(rect.Right - 336, rect.Bottom - 62, 152, 42), _lessonIndex == Curriculum.BeginnerLessons.Count - 1 ? "(C) Finish" : "(C) Continue", "continueMission");
        DrawButton(g, new Rectangle(rect.Right - 174, rect.Bottom - 62, 144, 42), "(E) Exit", "exitCompile");
    }

    private static Color TraceColor(TraceKind kind) => kind switch
    {
        TraceKind.Assign => Palette.Cyan,
        TraceKind.Print or TraceKind.Output => Palette.Gold,
        TraceKind.Compare => Palette.Purple,
        TraceKind.BranchTaken => Palette.Green,
        TraceKind.BranchSkipped => Palette.Red,
        TraceKind.Loop => Palette.Magenta,
        TraceKind.FunctionCall or TraceKind.Return => Palette.Green,
        _ => Palette.Cyan
    };

    private static string TypeReveal(string value, double elapsedSeconds, double charsPerSecond)
    {
        if (string.IsNullOrEmpty(value) || elapsedSeconds <= 0)
        {
            return "";
        }

        var count = Math.Clamp((int)(elapsedSeconds * charsPerSecond), 0, value.Length);
        return value[..count];
    }

    private void DrawDataChip(Graphics g, Rectangle rect, string label, Color color)
    {
        using var back = new SolidBrush(Color.FromArgb(38, color));
        using var pen = new Pen(color, 1f);
        using var brush = new SolidBrush(Palette.Text);
        g.FillRoundedRectangle(back, rect, 5);
        g.DrawRoundedRectangle(pen, rect, 5);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(label, _monoSmall, brush, rect, format);
    }

    private void DrawDataChipToFit(Graphics g, Rectangle stepRect, string label, Color color)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        var maxWidth = Math.Max(160, stepRect.Width - 138);
        var measured = g.MeasureString(label, _monoSmall);
        var chipWidth = Math.Min(maxWidth, Math.Max(190, (int)Math.Ceiling(measured.Width) + 34));
        var needsWrap = chipWidth >= maxWidth && measured.Width + 34 > maxWidth;
        var chipHeight = needsWrap ? 40 : 26;
        var rect = new Rectangle(stepRect.Right - chipWidth - 12, stepRect.Bottom - chipHeight - 10, chipWidth, chipHeight);

        using var back = new SolidBrush(Color.FromArgb(38, color));
        using var pen = new Pen(color, 1f);
        using var brush = new SolidBrush(Palette.Text);
        g.FillRoundedRectangle(back, rect, 5);
        g.DrawRoundedRectangle(pen, rect, 5);

        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.None
        };
        if (!needsWrap)
        {
            format.FormatFlags = StringFormatFlags.NoWrap;
        }

        var textRect = new RectangleF(rect.X + 10, rect.Y + 3, rect.Width - 20, rect.Height - 6);
        g.DrawString(label, _monoSmall, brush, textRect, format);
    }

    private void DrawHelpOverlay(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(190, 0, 0, 0));
        g.FillRectangle(shade, ClientRectangle);

        var rect = new Rectangle(150, 112, ClientSize.Width - 300, ClientSize.Height - 224);
        Panel(g, rect, Color.FromArgb(246, 5, 9, 15), Palette.Cyan);
        var inner = new Rectangle(rect.X + 18, rect.Y + 76, rect.Width - 36, rect.Height - 132);
        using var innerBack = new SolidBrush(Color.FromArgb(235, 9, 14, 22));
        using var innerPen = new Pen(Color.FromArgb(130, Palette.Magenta), 1.2f);
        g.FillRoundedRectangle(innerBack, inner, 6);
        g.DrawRoundedRectangle(innerPen, inner, 6);

        using var head = new SolidBrush(Palette.Cyan);
        using var text = new SolidBrush(Palette.Text);
        using var gold = new SolidBrush(Palette.Gold);
        using var magenta = new SolidBrush(Palette.Magenta);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString("CURRENT LEVEL SYNTAX", _title, head, rect.X + 28, rect.Y + 22);
        g.DrawString("Review the syntax available in this mission.", _ui, dim, rect.X + 32, rect.Y + 58);
        g.DrawString("PRESS ESC TO CLOSE", _uiBold, magenta, rect.Right - 176, rect.Y + 32);

        var y = inner.Y + 18;
        var colW = (inner.Width - 34) / 2;
        for (var i = 0; i < CurrentLesson.Help.Count; i++)
        {
            var x = inner.X + 16 + (i >= 8 ? colW + 18 : 0);
            if (i == 8) y = inner.Y + 18;
            var item = CurrentLesson.Help[i];
            g.DrawString(item.Term, _uiBold, gold, x, y);
            g.DrawString(item.Text, _monoSmall, text, x + 105, y + 2);
            y += 40;
        }

        using var footerPen = new Pen(Color.FromArgb(120, Palette.Cyan), 1f);
        g.DrawLine(footerPen, rect.X + 24, rect.Bottom - 42, rect.Right - 24, rect.Bottom - 42);
        g.DrawString("Type -help again or press Esc to resume the mission.", _uiBold, magenta, rect.X + 28, rect.Bottom - 30);
    }

    private void DrawPauseOverlay(Graphics g)
    {
        var rect = new Rectangle(ClientSize.Width / 2 - 220, ClientSize.Height / 2 - 76, 440, 132);
        Panel(g, rect, Color.FromArgb(8, 12, 14), Palette.Gold);
        using var head = new SolidBrush(Palette.Gold);
        using var text = new SolidBrush(Palette.Text);
        g.DrawString(IsLessonComplete ? "Mission Complete" : "Paused", _title, head, rect.X + 24, rect.Y + 22);
        g.DrawString(_status, _ui, text, rect.X + 28, rect.Y + 72);
    }

    private void DrawUpgrades(Graphics g)
    {
        DrawNav(g);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString("HARDWARE LAB", _hero, text, 42, 116);
        g.DrawString($"{_status}   Scrap Tokens: {_user?.ScrapTokens ?? 0}", _ui, dim, 48, 172);

        var y = 224;
        foreach (var cat in UpgradeSystem.Categories)
        {
            var rect = new Rectangle(48, y, ClientSize.Width - 96, 112);
            Panel(g, rect, Color.FromArgb(220, 12, 17, 26), Palette.Cyan);
            var current = _user is null ? null : UpgradeSystem.Current(_user, cat.Id);
            var next = _user is null ? null : UpgradeSystem.Next(_user, cat.Id);
            g.DrawString(cat.Name, _title, text, rect.X + 20, rect.Y + 15);
            g.DrawString(cat.Description, _ui, dim, rect.X + 170, rect.Y + 22);
            g.DrawString($"Current: {current?.Name ?? "Base"} // {current?.Effect ?? ""}", _uiBold, text, rect.X + 170, rect.Y + 58);
            DrawButton(g, new Rectangle(rect.Right - 184, rect.Y + 38, 154, 38), next is null ? "MAXED" : $"Buy {next.Cost} ST", $"buy:{cat.Id}");
            y += 128;
        }
    }

    private void DrawProfile(Graphics g)
    {
        DrawNav(g);
        if (_user is null) return;
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        var snapshot = CurrentTelemetrySnapshot();
        g.DrawString("OPERATOR PROFILE", _hero, text, 42, 116);
        g.DrawString($"{_user.FirstName} {_user.LastName} // {_user.Callsign}", _title, text, 48, 184);
        var rangeLabel = _reportRange == "all" ? "All Time" : $"Last {_reportRange} days";
        var scopeLabel = _reportScope == "all" ? "All Students" : "Current Student";
        g.DrawString($"Rank: {_user.RankName}   XP: {_user.Xp}   Scrap Tokens: {_user.ScrapTokens}   Range: {rangeLabel}   Scope: {scopeLabel}", _uiBold, dim, 52, 238);
        g.DrawString("Dashboard", _monoSmall, dim, 54, 266);
        g.DrawString("Range", _monoSmall, dim, 240, 266);
        g.DrawString("Scope", _monoSmall, dim, 388, 266);

        DrawProfileTabs(g, 560, 284);
        var content = new Rectangle(48, 340, ClientSize.Width - 96, ClientSize.Height - 426);
        Panel(g, content, Color.FromArgb(224, 8, 12, 20), Palette.Cyan);

        if (_profileView == "overview") DrawTelemetryOverview(g, content, snapshot);
        else if (_profileView == "concepts") DrawConceptDashboard(g, content, snapshot);
        else if (_profileView == "errors") DrawErrorDashboard(g, content, snapshot);
        else if (_profileView == "sessions") DrawSessionDashboard(g, content, snapshot);
        else if (_profileView == "tables") DrawTableDashboard(g, content, snapshot);
        else DrawExportDashboard(g, content, snapshot);
    }

    private TelemetrySnapshot CurrentTelemetrySnapshot()
    {
        var from = _reportRange == "all"
            ? DateTime.UtcNow.AddYears(-20)
            : DateTime.UtcNow.AddDays(-(int.TryParse(_reportRange, out var parsed) ? parsed : 30));
        if (_user is null)
        {
            return new TelemetrySnapshot();
        }

        try
        {
            return _reportScope == "all"
                ? TelemetryStore.SnapshotAll(from, DateTime.UtcNow.AddDays(1))
                : TelemetryStore.Snapshot(_user.Callsign, from, DateTime.UtcNow.AddDays(1));
        }
        catch (Exception ex)
        {
            _status = $"Telemetry dashboard unavailable: {ex.Message}";
            return new TelemetrySnapshot { Callsign = _reportScope == "all" ? "ALL_STUDENTS" : _user.Callsign, FromUtc = from, ToUtc = DateTime.UtcNow.AddDays(1) };
        }
    }

    private void DrawProfileTabs(Graphics g, int x, int y)
    {
        DrawButton(g, new Rectangle(x, y, 86, 34), "Student", "scope:student");
        DrawButton(g, new Rectangle(x + 94, y, 86, 34), "Class", "scope:all");
        DrawButton(g, new Rectangle(x + 194, y, 86, 34), "7 Days", "range:7");
        DrawButton(g, new Rectangle(x + 288, y, 94, 34), "30 Days", "range:30");
        DrawButton(g, new Rectangle(x + 390, y, 94, 34), "90 Days", "range:90");
        DrawButton(g, new Rectangle(x + 492, y, 92, 34), "All Time", "range:all");
    }

    private void SyncProfileCombos()
    {
        _profileViewSelect.SelectedItem = _profileView switch
        {
            "concepts" => "Concepts",
            "errors" => "Errors",
            "sessions" => "Sessions",
            "tables" => "Plain Tables",
            "export" => "Export",
            _ => "Overview"
        };
        _profileRangeSelect.SelectedItem = _reportRange switch
        {
            "7" => "7 Days",
            "90" => "90 Days",
            "all" => "All Time",
            _ => "30 Days"
        };
        _profileScopeSelect.SelectedItem = _reportScope == "all" ? "All Students" : "Current Student";
    }

    private void DrawTelemetryOverview(Graphics g, Rectangle rect, TelemetrySnapshot s)
    {
        DrawMetricCard(g, new Rectangle(rect.X + 22, rect.Y + 22, 198, 100), "Overall Mastery", $"{s.OverallMastery:0.0}%", Palette.Green);
        DrawMetricCard(g, new Rectangle(rect.X + 238, rect.Y + 22, 198, 100), "Syntax Accuracy", $"{s.SyntaxAccuracy:0.0}%", Palette.Cyan);
        DrawMetricCard(g, new Rectangle(rect.X + 454, rect.Y + 22, 198, 100), "Understanding", $"{s.Understanding.Score:0.0}%", Palette.Gold);
        DrawMetricCard(g, new Rectangle(rect.X + 670, rect.Y + 22, 198, 100), "Sessions", $"{s.EngagementSessions}", Palette.Orange);
        DrawMetricCard(g, new Rectangle(rect.X + 886, rect.Y + 22, 198, 100), "Engagement", $"{s.EngagementDays}d / {s.EngagementMinutes:0}m", Palette.Purple);
        DrawLineChart(g, new Rectangle(rect.X + 24, rect.Y + 150, rect.Width - 48, 150), "Growth Trend: Accuracy By Session", s.Sessions.Select(x => x.Accuracy).ToList(), Palette.Cyan);
        DrawHorizontalBars(g, new Rectangle(rect.X + 24, rect.Y + 330, rect.Width - 48, rect.Height - 360), "Top Concept Mastery", s.Concepts.Take(8).Select(c => (c.Concept, c.Mastery)).ToList());
    }

    private void DrawConceptDashboard(Graphics g, Rectangle rect, TelemetrySnapshot s)
    {
        DrawHorizontalBars(g, new Rectangle(rect.X + 24, rect.Y + 24, rect.Width - 48, rect.Height - 48), "Concept Mastery Heat Bars", s.Concepts.Select(c => (c.Concept, c.Mastery)).ToList());
    }

    private void DrawErrorDashboard(Graphics g, Rectangle rect, TelemetrySnapshot s)
    {
        var max = Math.Max(1, s.Errors.Select(e => e.Count).DefaultIfEmpty(1).Max());
        DrawHorizontalBars(g, new Rectangle(rect.X + 24, rect.Y + 24, rect.Width - 48, rect.Height - 48), "Top Error Patterns", s.Errors.Select(e => (e.ErrorType, e.Count * 100.0 / max)).ToList());
    }

    private void DrawSessionDashboard(Graphics g, Rectangle rect, TelemetrySnapshot s)
    {
        DrawLineChart(g, new Rectangle(rect.X + 24, rect.Y + 24, rect.Width - 48, 170), "Session Accuracy Timeline", s.Sessions.Select(x => x.Accuracy).ToList(), Palette.Green);
        DrawHorizontalBars(g, new Rectangle(rect.X + 24, rect.Y + 230, rect.Width / 2 - 40, rect.Height - 260), "Minutes Per Practice Day", s.Sessions.Select(x => (x.StartedUtc.ToString("MM-dd"), x.Minutes)).ToList());
        DrawHorizontalBars(g, new Rectangle(rect.X + rect.Width / 2 + 12, rect.Y + 230, rect.Width / 2 - 44, rect.Height - 260), "Sessions Per Practice Day", s.Sessions.Select(x => (x.StartedUtc.ToString("MM-dd"), (double)x.Sessions)).ToList());
    }

    private void DrawTableDashboard(Graphics g, Rectangle rect, TelemetrySnapshot s)
    {
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        using var cyanPen = new Pen(Color.FromArgb(120, Palette.Cyan));
        g.DrawString("PLAIN TABLE REVIEW", _title, text, rect.X + 24, rect.Y + 24);
        g.DrawString("Instructor-friendly rows for quick review before export.", _ui, dim, rect.X + 28, rect.Y + 68);

        var table = new Rectangle(rect.X + 24, rect.Y + 108, rect.Width - 48, Math.Min(300, rect.Height - 140));
        g.DrawRectangle(cyanPen, table);
        var headers = new[] { "Concept", "Attempts", "Correct", "Errors", "Help", "Mastery" };
        var widths = new[] { 280, 96, 96, 96, 96, 96 };
        var x = table.X + 10;
        for (var i = 0; i < headers.Length; i++)
        {
            g.DrawString(headers[i], _monoSmall, text, x, table.Y + 10);
            x += widths[i];
        }

        var y = table.Y + 38;
        foreach (var c in s.Concepts.Take(Math.Max(1, (table.Height - 48) / 28)))
        {
            x = table.X + 10;
            var cells = new[] { c.Concept, c.Attempts.ToString(), c.Correct.ToString(), c.Errors.ToString(), c.HelpUses.ToString(), $"{c.Mastery:0.0}%" };
            for (var i = 0; i < cells.Length; i++)
            {
                g.DrawString(cells[i], _monoSmall, i == 0 ? dim : text, x, y);
                x += widths[i];
            }
            y += 28;
        }

        var errorBox = new Rectangle(rect.X + 24, table.Bottom + 26, rect.Width / 2 - 36, rect.Bottom - table.Bottom - 56);
        var engagementBox = new Rectangle(errorBox.Right + 24, errorBox.Y, rect.Width / 2 - 36, errorBox.Height);
        DrawHorizontalBars(g, errorBox, "Error Table Summary", s.Errors.Select(e => (e.ErrorType, (double)e.Count)).ToList());
        DrawMetricCard(g, new Rectangle(engagementBox.X, engagementBox.Y + 8, 220, 100), "Practice Days", $"{s.EngagementDays}", Palette.Orange);
        DrawMetricCard(g, new Rectangle(engagementBox.X + 238, engagementBox.Y + 8, 220, 100), "Sessions", $"{s.EngagementSessions}", Palette.Cyan);
        DrawMetricCard(g, new Rectangle(engagementBox.X, engagementBox.Y + 128, 220, 100), "Minutes", $"{s.EngagementMinutes:0.0}", Palette.Gold);
        DrawMetricCard(g, new Rectangle(engagementBox.X + 238, engagementBox.Y + 128, 220, 100), "Avg Min/Day", $"{s.AverageMinutesPerDay:0.0}", Palette.Green);
    }

    private void DrawExportDashboard(Graphics g, Rectangle rect, TelemetrySnapshot s)
    {
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString("EXPORT REPORTS", _title, text, rect.X + 24, rect.Y + 24);
        g.DrawString("Exports use the selected date range and current profile. PDF includes summary visuals and bar charts.", _ui, dim, rect.X + 28, rect.Y + 68);
        DrawButton(g, new Rectangle(rect.X + 30, rect.Y + 120, 150, 42), "Export CSV", "export:csv");
        DrawButton(g, new Rectangle(rect.X + 200, rect.Y + 120, 150, 42), "Export JSON", "export:json");
        DrawButton(g, new Rectangle(rect.X + 370, rect.Y + 120, 150, 42), "Export PDF", "export:pdf");
        DrawTelemetryOverview(g, new Rectangle(rect.X + 10, rect.Y + 190, rect.Width - 20, rect.Height - 210), s);
    }

    private void DrawNav(Graphics g)
    {
        DrawButton(g, new Rectangle(34, ClientSize.Height - 58, 104, 36), "Dashboard", "dashboard");
        DrawButton(g, new Rectangle(146, ClientSize.Height - 58, 92, 36), "Deploy", "deploy");
        DrawButton(g, new Rectangle(246, ClientSize.Height - 58, 122, 36), "Missions", "missionSelect");
        DrawButton(g, new Rectangle(376, ClientSize.Height - 58, 104, 36), "Upgrades", "upgrades");
        DrawButton(g, new Rectangle(488, ClientSize.Height - 58, 88, 36), "Profile", "profile");
        DrawButton(g, new Rectangle(584, ClientSize.Height - 58, 82, 36), "Music", "music");
        DrawButton(g, new Rectangle(674, ClientSize.Height - 58, 94, 36), "Next Track", "nextMusic");
        DrawButton(g, new Rectangle(ClientSize.Width - 112, ClientSize.Height - 58, 84, 36), "Logout", "logout");
    }

    private void DrawCard(Graphics g, Rectangle rect, string title, string body, string action, Color accent)
    {
        Panel(g, rect, Color.FromArgb(224, 12, 17, 27), accent);
        using var head = new SolidBrush(accent);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString(title, _title, head, rect.X + 20, rect.Y + 20);
        DrawWrapped(g, body, _ui, dim, new RectangleF(rect.X + 24, rect.Y + 76, rect.Width - 48, 54));
        _hotspots.Add((rect, action));
    }

    private void DrawMetricCard(Graphics g, Rectangle rect, string label, string value, Color accent)
    {
        Panel(g, rect, Color.FromArgb(218, 10, 15, 26), accent);
        using var accentBrush = new SolidBrush(accent);
        using var text = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString(label.ToUpperInvariant(), _monoSmall, dim, rect.X + 14, rect.Y + 14);
        g.DrawString(value, _title, accentBrush, rect.X + 14, rect.Y + 42);
    }

    private void DrawHorizontalBars(Graphics g, Rectangle rect, string title, IReadOnlyList<(string Label, double Value)> values)
    {
        using var titleBrush = new SolidBrush(Palette.Text);
        using var dim = new SolidBrush(Palette.Dim);
        g.DrawString(title, _uiBold, titleBrush, rect.X, rect.Y);
        var y = rect.Y + 36;
        foreach (var (label, value) in values.Take(Math.Max(1, (rect.Height - 40) / 32)))
        {
            var pct = Math.Clamp(value, 0, 100);
            g.DrawString(label, _monoSmall, dim, rect.X, y + 2);
            var bar = new Rectangle(rect.X + 190, y + 2, rect.Width - 280, 18);
            using var back = new SolidBrush(Color.FromArgb(45, Palette.Grid));
            using var fill = new LinearGradientBrush(bar, Palette.Magenta, pct >= 75 ? Palette.Green : pct >= 50 ? Palette.Gold : Palette.Red, 0f);
            g.FillRectangle(back, bar);
            g.FillRectangle(fill, new Rectangle(bar.X, bar.Y, (int)(bar.Width * pct / 100.0), bar.Height));
            g.DrawString($"{value:0.0}", _monoSmall, titleBrush, bar.Right + 12, y);
            y += 32;
        }
    }

    private void DrawLineChart(Graphics g, Rectangle rect, string title, IReadOnlyList<double> values, Color accent)
    {
        using var text = new SolidBrush(Palette.Text);
        using var dimPen = new Pen(Color.FromArgb(80, Palette.Grid));
        using var accentPen = new Pen(accent, 2.4f);
        g.DrawString(title, _uiBold, text, rect.X, rect.Y);
        var plot = new Rectangle(rect.X, rect.Y + 34, rect.Width, rect.Height - 42);
        g.DrawRectangle(dimPen, plot);
        for (var i = 1; i < 4; i++)
        {
            var y = plot.Y + i * plot.Height / 4;
            g.DrawLine(dimPen, plot.X, y, plot.Right, y);
        }

        if (values.Count < 2)
        {
            using var dim = new SolidBrush(Palette.Dim);
            g.DrawString("Not enough session data yet.", _ui, dim, plot.X + 12, plot.Y + 20);
            return;
        }

        var points = values.Select((v, i) =>
        {
            var x = plot.X + i * plot.Width / Math.Max(1, values.Count - 1);
            var y = plot.Bottom - (int)(Math.Clamp(v, 0, 100) / 100.0 * plot.Height);
            return new Point(x, y);
        }).ToArray();
        g.DrawLines(accentPen, points);
        using var dot = new SolidBrush(accent);
        foreach (var p in points) g.FillEllipse(dot, p.X - 3, p.Y - 3, 6, 6);
    }

    private void DrawButton(Graphics g, Rectangle rect, string label, string action)
    {
        using var brush = new LinearGradientBrush(rect, Color.FromArgb(35, 14, 24, 34), Color.FromArgb(35, 34, 14, 44), 0f);
        using var pen = new Pen(Palette.Cyan);
        g.FillRoundedRectangle(brush, rect, 6);
        g.DrawRoundedRectangle(pen, rect, 6);
        using var text = new SolidBrush(Palette.Text);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, _uiBold, text, rect, format);
        _hotspots.Add((rect, action));
    }

    private static void Panel(Graphics g, Rectangle rect, Color fill, Color border)
    {
        using var brush = new SolidBrush(fill);
        using var glow = new Pen(Color.FromArgb(80, border), 5f);
        using var pen = new Pen(border, 1.3f);
        g.FillRoundedRectangle(brush, rect, 8);
        g.DrawRoundedRectangle(glow, rect, 8);
        g.DrawRoundedRectangle(pen, rect, 8);
    }

    private static void DrawWrapped(Graphics g, string value, Font font, Brush brush, RectangleF rect)
    {
        using var format = new StringFormat { Trimming = StringTrimming.EllipsisWord };
        g.DrawString(value, font, brush, rect, format);
    }

    private static void DrawPythonHighlighted(Graphics g, string code, Font font, float x, float y)
    {
        var cursor = x;
        var charWidth = GetMonoCharWidth(g, font);
        foreach (var token in PythonSyntax.Tokenize(code))
        {
            foreach (var ch in token.Text)
            {
                if (ch != ' ')
                {
                    DrawCodeText(g, ch.ToString(), font, token.Color, cursor, y);
                }

                cursor += charWidth;
            }
        }
    }

    private static void DrawPythonCompared(Graphics g, string target, string typed, Font font, float x, float y)
    {
        var cursor = x;
        var charWidth = GetMonoCharWidth(g, font);
        for (var i = 0; i < target.Length; i++)
        {
            var expected = target[i].ToString();
            var color = i >= typed.Length
                ? PythonSyntax.ColorForCharacterContext(target, i)
                : typed[i] == target[i]
                    ? Palette.Green
                    : Palette.Red;

            DrawCodeText(g, expected, font, color, cursor, y);
            cursor += charWidth;
        }

        if (typed.Length > target.Length)
        {
            var extra = typed[target.Length..];
            DrawCodeText(g, extra, font, Palette.Red, cursor, y);
        }
    }

    private static float GetMonoCharWidth(Graphics g, Font font)
    {
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;
        var tenCells = TextRenderer.MeasureText("0000000000", font, Size.Empty, flags).Width / 10f;
        return Math.Max(tenCells, font.Size * 0.62f);
    }

    private static void DrawCodeText(Graphics g, string text, Font font, Color color, float x, float y)
    {
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;
        TextRenderer.DrawText(g, text, font, new Point((int)Math.Round(x), (int)Math.Round(y)), color, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _input.Dispose();
            _mono.Dispose();
            _monoSmall.Dispose();
            _monoBold.Dispose();
            _ui.Dispose();
            _uiBold.Dispose();
            _title.Dispose();
            _hero.Dispose();
            _audio.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal sealed record SyntaxToken(string Text, Color Color);

internal sealed class FloatingText
{
    public string Text { get; set; } = "";
    public Color Color { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public double Age { get; set; }
}

internal static class PythonSyntax
{
    private static readonly HashSet<string> Keywords =
    [
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class",
        "continue", "def", "elif", "else", "except", "finally", "for", "from", "global",
        "if", "import", "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise",
        "return", "try", "while", "with", "yield"
    ];

    private static readonly HashSet<string> Builtins =
    [
        "print", "len", "range", "str", "int", "float", "bool", "list", "dict", "set",
        "tuple", "sum", "min", "max", "input"
    ];

    public static IEnumerable<SyntaxToken> Tokenize(string code)
    {
        for (var i = 0; i < code.Length;)
        {
            var c = code[i];

            if (c == '#')
            {
                yield return new SyntaxToken(code[i..], Palette.Comment);
                yield break;
            }

            if (c is '"' or '\'')
            {
                var quote = c;
                var start = i++;
                while (i < code.Length)
                {
                    if (code[i] == '\\' && i + 1 < code.Length)
                    {
                        i += 2;
                        continue;
                    }

                    if (code[i++] == quote)
                    {
                        break;
                    }
                }

                yield return new SyntaxToken(code[start..i], Palette.String);
                continue;
            }

            if (char.IsDigit(c))
            {
                var start = i++;
                while (i < code.Length && (char.IsDigit(code[i]) || code[i] == '.'))
                {
                    i++;
                }

                yield return new SyntaxToken(code[start..i], Palette.Number);
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = i++;
                while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
                {
                    i++;
                }

                var word = code[start..i];
                var color = Keywords.Contains(word)
                    ? Palette.Keyword
                    : Builtins.Contains(word)
                        ? Palette.Builtin
                        : Palette.Text;
                yield return new SyntaxToken(word, color);
                continue;
            }

            if ("=+-*/%<>!()[]{}:,. ".Contains(c))
            {
                yield return new SyntaxToken(c.ToString(), char.IsWhiteSpace(c) ? Palette.Text : Palette.Operator);
                i++;
                continue;
            }

            yield return new SyntaxToken(c.ToString(), Palette.Text);
            i++;
        }
    }

    public static Color ColorForCharacterContext(string code, int index)
    {
        var cursor = 0;
        foreach (var token in Tokenize(code))
        {
            var next = cursor + token.Text.Length;
            if (index >= cursor && index < next)
            {
                return token.Color;
            }

            cursor = next;
        }

        return Palette.Text;
    }
}

internal static class Palette
{
    public static readonly Color Bg = Color.FromArgb(5, 7, 15);
    public static readonly Color Bg2 = Color.FromArgb(22, 8, 32);
    public static readonly Color Grid = Color.FromArgb(34, 27, 72);
    public static readonly Color Text = Color.FromArgb(238, 248, 255);
    public static readonly Color Dim = Color.FromArgb(139, 157, 183);
    public static readonly Color Cyan = Color.FromArgb(42, 245, 255);
    public static readonly Color Green = Color.FromArgb(82, 255, 154);
    public static readonly Color Gold = Color.FromArgb(255, 207, 86);
    public static readonly Color Purple = Color.FromArgb(188, 102, 255);
    public static readonly Color Magenta = Color.FromArgb(255, 54, 171);
    public static readonly Color Orange = Color.FromArgb(255, 151, 45);
    public static readonly Color Red = Color.FromArgb(255, 69, 101);
    public static readonly Color HotRed = Color.FromArgb(255, 24, 48);
    public static readonly Color Keyword = Color.FromArgb(42, 245, 255);
    public static readonly Color Builtin = Color.FromArgb(82, 255, 154);
    public static readonly Color String = Color.FromArgb(255, 207, 86);
    public static readonly Color Number = Color.FromArgb(188, 102, 255);
    public static readonly Color Comment = Color.FromArgb(101, 190, 132);
    public static readonly Color Operator = Color.FromArgb(255, 54, 171);
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using var path = CreateRoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF bounds, float radius)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using var path = CreateRoundedPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedPath(RectangleF bounds, float radius)
    {
        var safeRadius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f);
        var diameter = safeRadius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
