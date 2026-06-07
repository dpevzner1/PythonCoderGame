namespace PythonCoderGame;

internal enum TraceKind
{
    Tokenize,
    Assign,
    Print,
    Compare,
    BranchTaken,
    BranchSkipped,
    Loop,
    FunctionCall,
    Return,
    Output
}

internal sealed record RuntimeTraceStep(
    TraceKind Kind,
    string Title,
    string Detail,
    string DataBefore,
    string DataAfter);

internal sealed record CodeLine(string Text, string Term, string Explanation, string Usage);

internal sealed record Lesson(
    string Title,
    string Goal,
    IReadOnlyList<CodeLine> Lines,
    IReadOnlyList<CodeLine> Help,
    IReadOnlyList<RuntimeTraceStep> Trace,
    string ExpectedOutput,
    bool IsBoss,
    IReadOnlyList<string> CorruptedLines,
    string BossDiagnostic);

internal static class Curriculum
{
    private static IReadOnlyList<CodeLine> CommonHelp { get; } =
    [
        new("# note", "comment", "A human note. Python skips it.", "# explain why"),
        new("print(value)", "print", "Displays information on screen.", "print(name)"),
        new("name = \"Ada\"", "variable", "A named storage spot for a value.", "name = value"),
        new("\"hello\"", "str", "Text data wrapped in quotes.", "message = \"hello\""),
        new("42", "int", "A whole number.", "count = 42"),
        new("3.14", "float", "A decimal number.", "price = 3.14"),
        new("True", "bool", "A yes/no value: True or False.", "is_ready = True"),
        new("None", "NoneType", "A placeholder meaning no value.", "result = None"),
        new("[1, 2, 3]", "list", "Ordered, changeable collection.", "items = [1, 2, 3]"),
        new("{\"a\": 1}", "dict", "Key/value collection.", "scores = {\"Ada\": 100}"),
        new("if x > 0:", "if", "Starts a conditional block.", "Use a colon and indent the next line."),
        new("for x in range(3):", "for", "Repeats an indented block.", "range(3) gives 0, 1, 2."),
        new("def add(a, b):", "def", "Defines a function.", "Indented lines become the function body."),
        new("return value", "return", "Sends a result out of a function.", "return total"),
        new("input(\"Name: \")", "input", "Receives text from the user.", "name = input(\"Name: \")"),
        new("import random", "import", "Loads tools from a module.", "import random"),
        new("try:", "try/except", "Starts protected code that might fail.", "Use except to handle a known problem.")
    ];

    public static IReadOnlyList<Lesson> BeginnerLessons { get; } = AddBosses(
    [
        Mission("Mission 01: Python Reads Top To Bottom", "See that Python executes one line, then the next.", [Line("print(\"Boot\")", "print()", "Shows text in the output terminal.", "Python runs this first."), Line("print(\"Ready\")", "order", "The second line runs after the first.", "Execution moves downward."), Line("print(\"Go\")", "order", "The third line runs last.", "Top-to-bottom order matters.")], "Boot\nReady\nGo"),
        Mission("Mission 02: Print Sends Output", "Use print() to show data to the user.", [Line("print(\"Hello\")", "print()", "print() sends a value to the output console.", "The text inside parentheses is displayed.")], "Hello"),
        Mission("Mission 03: Comments Are Notes", "Learn that comments help humans and are ignored by Python.", [Line("# launch note", "comment", "A comment starts with # and does not run.", "Use comments to explain intent."), Line("print(\"Go\")", "print()", "Only the print line creates output.", "Python skips the note and runs this line.")], "Go"),
        Mission("Mission 04: Strings Need Quotes", "Recognize text values by their quotes.", [Line("print(\"Ada\")", "string", "A string is text wrapped in quotes.", "Quotes tell Python this is data, not a name.")], "Ada"),
        Mission("Mission 05: Syntax Shapes", "Practice the symbols that make a print line valid.", [Line("print(\"Ready\")", "syntax", "Parentheses hold the value, and quotes hold the text.", "A tiny symbol change can break code.")], "Ready"),

        Mission("Mission 06: Store A Text Value", "Put text into a variable and read it later.", [Line("name = \"Ada\"", "variable", "A variable is a named memory slot.", "The left side names the slot; the right side provides the value."), Line("print(name)", "variable use", "No quotes means read the variable.", "print asks memory for name.")], "Ada"),
        Mission("Mission 07: Store A Whole Number", "Store and display an integer.", [Line("age = 12", "int", "An int is a whole number.", "Numbers do not need quotes."), Line("print(age)", "print int", "print can display numbers too.", "The number travels from memory to output.")], "12"),
        Mission("Mission 08: Store A Decimal", "Store and display a float.", [Line("height = 1.52", "float", "A float is a decimal number.", "Python uses a dot for decimals."), Line("print(height)", "print float", "The decimal value moves to output.", "The console shows the number.")], "1.52"),
        Mission("Mission 09: True Or False", "Store a boolean switch.", [Line("is_ready = True", "bool", "A bool is True or False.", "Python capitalizes True and False."), Line("print(is_ready)", "print bool", "Booleans can be displayed.", "The switch value moves to output.")], "True"),
        Mission("Mission 10: Empty Placeholder", "Use None when a value is intentionally empty.", [Line("favorite = None", "None", "None means no value yet.", "Use it as a placeholder."), Line("print(favorite)", "print None", "Printing None shows the placeholder.", "This helps while building code.")], "None"),

        Mission("Mission 11: Readable Names", "Practice Python's snake_case naming style.", [Line("player_score = 10", "snake_case", "Python names often use underscores between words.", "Readable names help humans understand code."), Line("print(player_score)", "variable use", "Read a named value.", "Names should describe their data.")], "10"),
        Mission("Mission 12: Join Text", "Combine two strings with plus.", [Line("greeting = \"Hello, \" + \"Ada\"", "concat", "+ can join strings.", "Both sides must be strings."), Line("print(greeting)", "print", "Display the combined string.", "The finished text is stored in memory.")], "Hello, Ada"),
        Mission("Mission 13: F-Strings", "Insert a variable into text.", [Line("name = \"Ada\"", "variable", "Store a name.", "The value waits in memory."), Line("message = f\"Hello, {name}\"", "f-string", "An f-string inserts variable values.", "Use f before the opening quote."), Line("print(message)", "print", "Display the formatted message.", "The final message prints.")], "Hello, Ada"),
        Mission("Mission 14: Math Expressions", "Let Python calculate before storing.", [Line("total = 4 + 6", "addition", "Python evaluates the right side first.", "4 + 6 becomes 10."), Line("print(total)", "print", "Display the result.", "The answer comes from memory.")], "10"),
        Mission("Mission 15: Update A Value", "Change a variable by reading its old value.", [Line("score = 10", "assign", "Create a score.", "Memory slot score receives 10."), Line("score = score + 5", "reassign", "Read old score, add 5, store new score.", "Variables can change."), Line("print(score)", "print", "Show the updated value.", "Output sees the new score.")], "15"),

        Mission("Mission 16: Type Awareness", "Ask Python what kind of value you have.", [Line("count = 3", "int", "Store a whole number.", "count is an integer."), Line("print(type(count))", "type()", "type() reports the value kind.", "Useful for debugging.")], "<class 'int'>"),
        Mission("Mission 17: Create A List", "Store several values in one ordered container.", [Line("colors = [\"red\", \"green\", \"blue\"]", "list", "A list stores ordered items.", "Square brackets create a list."), Line("print(colors)", "print list", "Print can display the list.", "Items stay in order.")], "['red', 'green', 'blue']"),
        Mission("Mission 18: List Indexes", "Read one item from a list.", [Line("colors = [\"red\", \"green\", \"blue\"]", "list", "Store three colors.", "Indexes start at 0."), Line("first = colors[0]", "index", "colors[0] gets the first item.", "0 points at red."), Line("print(first)", "print", "Display the selected item.", "Only red prints.")], "red"),
        Mission("Mission 19: Append To A List", "Add an item to the end of a list.", [Line("items = [\"key\"]", "list", "Start with one item.", "items has one slot."), Line("items.append(\"map\")", "append()", "append adds to the end.", "The list mutates."), Line("print(items)", "print", "Show the changed list.", "Now there are two items.")], "['key', 'map']"),
        Mission("Mission 20: Mini Inventory", "Build a tiny list-based inventory.", [Line("items = [\"key\", \"map\"]", "list", "Store two items.", "The list preserves order."), Line("first_item = items[0]", "index", "Index 0 reads the first item.", "The selected item enters memory."), Line("print(first_item)", "print", "Display one inventory item.", "Only the selected item prints.")], "key"),

        Mission("Mission 21: Create A Dictionary", "Store labeled key/value data.", [Line("profile = {\"name\": \"Ada\", \"score\": 10}", "dict", "A dictionary maps keys to values.", "Keys unlock values."), Line("print(profile)", "print dict", "Show the whole mapping.", "Both keys and values print.")], "{'name': 'Ada', 'score': 10}"),
        Mission("Mission 22: Dictionary Lookup", "Read one value by key.", [Line("profile = {\"name\": \"Ada\", \"score\": 10}", "dict", "Store a profile.", "Keys point to values."), Line("name = profile[\"name\"]", "lookup", "The name key returns Ada.", "Square brackets choose a key."), Line("print(name)", "print", "Display the selected value.", "Only Ada prints.")], "Ada"),
        Mission("Mission 23: First Comparison", "Create a True or False result from a comparison.", [Line("lives = 3", "assign", "Store lives.", "lives gets 3."), Line("has_lives = lives > 0", "comparison", "3 > 0 becomes True.", "The boolean result is stored."), Line("print(has_lives)", "print", "Display the boolean.", "True prints.")], "True"),
        Mission("Mission 24: First If", "Run code only when a condition is true.", [Line("is_ready = True", "bool", "Store readiness.", "True means yes."), Line("if is_ready:", "if", "The branch opens when True.", "Colon starts the block."), Line("    print(\"Launch\")", "indented block", "Indented code belongs to the if.", "Runs only when ready.")], "Launch"),
        Mission("Mission 25: If Else", "Choose between two paths.", [Line("age = 12", "assign", "Store age.", "age gets 12."), Line("if age >= 18:", "if", "Check adult condition.", "12 >= 18 is false."), Line("    print(\"Adult\")", "true path", "Skipped when false.", "This line does not run."), Line("else:", "else", "Fallback path.", "Runs when if is false."), Line("    print(\"Minor\")", "else body", "This branch runs.", "Minor prints.")], "Minor"),

        Mission("Mission 26: Equality Check", "Use == to compare values.", [Line("code = \"red\"", "string", "Store text for checking.", "The string goes into memory."), Line("is_match = code == \"red\"", "==", "== asks whether two values match.", "The answer becomes True."), Line("print(is_match)", "print", "Display the comparison result.", "True prints.")], "True"),
        Mission("Mission 27: If Elif Else", "Check several conditions in order.", [Line("score = 85", "assign", "Store score.", "score gets 85."), Line("if score >= 90:", "if", "Check A grade.", "False for 85."), Line("    grade = \"A\"", "skipped", "Skipped because first condition fails.", "No assignment."), Line("elif score >= 80:", "elif", "Check B grade.", "True for 85."), Line("    grade = \"B\"", "taken", "Stores B.", "This branch wins."), Line("else:", "else", "Skipped after a match.", "Only runs if no prior match."), Line("    grade = \"Keep practicing\"", "else body", "Skipped.", "No change."), Line("print(grade)", "print", "Display final grade.", "B prints.")], "B"),
        Mission("Mission 28: And Logic", "Require two conditions at once.", [Line("has_key = True", "bool", "Store key state.", "Player has a key."), Line("door_open = True", "bool", "Store door state.", "Door is open."), Line("can_enter = has_key and door_open", "and", "and requires both sides to be True.", "The result becomes True."), Line("print(can_enter)", "print", "Display access result.", "True prints.")], "True"),
        Mission("Mission 29: Or Logic", "Allow either condition to pass.", [Line("has_code = False", "bool", "No code is present.", "The first switch is off."), Line("has_badge = True", "bool", "Badge is present.", "The second switch is on."), Line("can_enter = has_code or has_badge", "or", "or passes if either side is True.", "The result becomes True."), Line("print(can_enter)", "print", "Display access result.", "True prints.")], "True"),
        Mission("Mission 30: Not Logic", "Flip a boolean condition.", [Line("door_locked = False", "bool", "Door is not locked.", "False goes into memory."), Line("can_enter = not door_locked", "not", "not flips False into True.", "The result is stored."), Line("print(can_enter)", "print", "Display access result.", "True prints.")], "True"),

        Mission("Mission 31: For Loop With Range", "Repeat code using range.", [Line("for number in range(3):", "for/range", "range(3) creates 0, 1, 2.", "Each value enters number."), Line("    print(number)", "loop body", "Runs once per value.", "Print fires three times.")], "0\n1\n2"),
        Mission("Mission 32: Loop Variable", "Watch the loop variable change each pass.", [Line("for step in range(4):", "loop variable", "step receives 0, 1, 2, then 3.", "The value changes each iteration."), Line("    print(step)", "loop body", "Print the current step.", "Four short outputs appear.")], "0\n1\n2\n3"),
        Mission("Mission 33: Loop Over A List", "Loop through collection items.", [Line("colors = [\"red\", \"green\"]", "list", "Store two colors.", "List has two slots."), Line("for color in colors:", "for", "Each list item enters color.", "Loop follows list order."), Line("    print(color)", "loop body", "Print each color.", "Runs twice.")], "red\ngreen"),
        Mission("Mission 34: Accumulator", "Add values into a running total.", [Line("total = 0", "accumulator", "Start total at zero.", "Memory slot is ready."), Line("for number in [1, 2, 3]:", "for list", "Loop over three numbers.", "Each number enters the loop."), Line("    total = total + number", "update", "Add each number to total.", "Total changes each iteration."), Line("print(total)", "print", "Show final total.", "6 prints.")], "6"),
        Mission("Mission 35: Short While Loop", "Repeat while a condition stays true.", [Line("count = 3", "assign", "Start a countdown.", "count gets 3."), Line("while count > 0:", "while", "Repeat while count is above zero.", "Condition checked before each loop."), Line("    print(count)", "loop body", "Print current count.", "Runs each pass."), Line("    count = count - 1", "update", "Move count toward stopping.", "Prevents infinite loop.")], "3\n2\n1"),

        Mission("Mission 36: Define A Function", "Create a reusable code block.", [Line("def greet():", "def", "Create a reusable function.", "The body waits until called."), Line("    print(\"Hello\")", "function body", "Indented code belongs to the function.", "This runs when greet is called.")], ""),
        Mission("Mission 37: Call A Function", "Run the reusable function.", [Line("def greet():", "def", "Create a reusable function.", "The body waits until called."), Line("    print(\"Hello\")", "function body", "Runs when greet is called.", "Indented under def."), Line("greet()", "call", "Call the function.", "Python jumps into the function body.")], "Hello"),
        Mission("Mission 38: Function Parameters", "Send data into a function.", [Line("def greet(name):", "parameters", "name receives the input value.", "Parameters are function memory slots."), Line("    print(name)", "function body", "Print the parameter.", "The sent-in value appears."), Line("greet(\"Ada\")", "call", "Send Ada into the function.", "Ada enters name.")], "Ada"),
        Mission("Mission 39: Return A Value", "Send data back out of a function.", [Line("def add(a, b):", "parameters", "a and b receive inputs.", "Function expects two values."), Line("    return a + b", "return", "Send a result back.", "a + b becomes the return value."), Line("result = add(2, 3)", "call", "2 and 3 enter the function.", "Returned value enters result."), Line("print(result)", "print", "Display result.", "5 prints.")], "5"),
        Mission("Mission 40: Status Function", "Combine function, if, return, and output.", [Line("def status(score):", "function", "Create a status function.", "score becomes a parameter."), Line("    if score >= 10:", "if", "Check if score passes.", "Branch gate opens for 12."), Line("        return \"pass\"", "return", "Return pass when true.", "This path runs."), Line("    return \"try again\"", "fallback", "Runs only if if did not return.", "Skipped here."), Line("result = status(12)", "call", "Send 12 into status.", "Returned text enters result."), Line("print(result)", "print", "Show final result.", "pass prints.")], "pass"),

        Mission("Mission 41: Ask For Input", "Learn that input() receives text from a user.", [Line("name = input(\"Name: \")", "input()", "input asks the user for text.", "The typed answer enters name."), Line("print(name)", "print", "Display the typed answer.", "The stored text prints.")], "student input"),
        Mission("Mission 42: Convert Input To Int", "Turn typed text into a number.", [Line("age_text = \"12\"", "string", "Input starts as text.", "This simulates text from input()."), Line("age = int(age_text)", "int()", "int() converts number text to an integer.", "The value becomes numeric."), Line("print(age)", "print", "Display the converted number.", "12 prints.")], "12"),
        Mission("Mission 43: Import A Module", "Load code from Python's standard library.", [Line("import random", "import", "import loads a module.", "The random tools become available."), Line("print(\"module ready\")", "print", "Confirm the module is loaded.", "The message prints.")], "module ready"),
        Mission("Mission 44: Read A File Name", "Use a variable to represent a file path.", [Line("path = \"notes.txt\"", "file path", "A file path is text that points to a file.", "The name is stored for later file work."), Line("print(path)", "print", "Display the path value.", "The file name prints.")], "notes.txt"),
        Mission("Mission 45: Write A Setting", "Represent a saved setting with a dictionary.", [Line("settings = {\"music\": True}", "dict", "A dictionary can store app settings.", "The key is music."), Line("print(settings[\"music\"])", "lookup", "Read the music setting.", "The boolean value prints.")], "True"),

        Mission("Mission 46: Read An Error", "Recognize that errors point near the problem.", [Line("expected = \"missing colon\"", "debug", "An error message names what Python wanted.", "Store the diagnosis as text."), Line("print(expected)", "print", "Display the diagnosis.", "This models reading an error.")], "missing colon"),
        Mission("Mission 47: Try Except", "Catch a problem without crashing.", [Line("try:", "try", "try starts code that might fail.", "Indented code is protected."), Line("    number = int(\"3\")", "int()", "Convert text to a number.", "This conversion succeeds."), Line("except ValueError:", "except", "except runs if conversion fails.", "Skipped here."), Line("    number = 0", "fallback", "Fallback value.", "Skipped here."), Line("print(number)", "print", "Display the result.", "3 prints.")], "3"),
        Mission("Mission 48: Simple Check", "Write code that checks expected output.", [Line("result = 2 + 3", "test value", "Compute a result.", "The value should be 5."), Line("print(result == 5)", "check", "A check compares result to expected.", "True means the check passed.")], "True"),
        Mission("Mission 49: Small Function Design", "Keep one job inside one function.", [Line("def double(number):", "function", "A focused function has one job.", "number is the input."), Line("    return number * 2", "return", "Return the doubled value.", "The caller receives it."), Line("print(double(4))", "call", "Call and print the result.", "8 prints.")], "8"),
        Mission("Mission 50: Final Mini Program", "Combine input-like data, logic, function, and output.", [Line("def badge(name, score):", "function", "Create a badge builder.", "name and score enter the function."), Line("    if score >= 10:", "if", "Check if score passes.", "The branch opens for 12."), Line("        return f\"{name}: pass\"", "return f-string", "Return formatted text.", "The function sends back a badge."), Line("    return f\"{name}: retry\"", "fallback", "Fallback text.", "Skipped here."), Line("print(badge(\"Ada\", 12))", "call", "Call the mini program.", "The badge prints.")], "Ada: pass")
    ]);

    private static Lesson Mission(string title, string goal, IReadOnlyList<CodeLine> lines, string output)
        => new(title, goal, lines, CommonHelp, AutoTrace(lines, output), output, false, [], "");

    private static IReadOnlyList<Lesson> AddBosses(IReadOnlyList<Lesson> missions)
    {
        var result = new List<Lesson>();
        for (var i = 0; i < missions.Count; i++)
        {
            result.Add(missions[i]);
            if ((i + 1) % 5 == 0)
            {
                result.Add(BossFor((i + 1) / 5, missions.Skip(Math.Max(0, i - 4)).Take(5).ToArray()));
            }
        }

        return result;
    }

    private static Lesson BossFor(int bossNumber, IReadOnlyList<Lesson> previous)
    {
        var title = $"Boss {bossNumber:00}: Debug Virus";
        var review = string.Join(", ", previous.Select(m => m.Title.Replace("Mission ", "M")));
        var repairs = previous.Select(m =>
        {
            var line = m.Lines.FirstOrDefault(l => !l.Text.TrimStart().StartsWith("#", StringComparison.Ordinal)) ?? m.Lines.First();
            var (corrupted, diagnostic) = Corrupt(line.Text);
            return Repair(corrupted, line.Text, line.Term, diagnostic, string.IsNullOrWhiteSpace(m.ExpectedOutput) ? "compiled" : m.ExpectedOutput.Replace("\n", " | ", StringComparison.Ordinal));
        }).ToArray();

        return Boss(title, review, repairs);
    }

    private static Lesson Boss(string title, string review, IReadOnlyList<BossRepair> repairs)
        => new(
            title,
            $"A virus corrupted five concepts from the previous learning rounds: {review}. Repair each orange snippet before the timer expires.",
            repairs.Select(r => Line(r.FixedLine, r.Term, r.Diagnostic, "Type the corrected code in the input rail. The answer is hidden during boss fights.")).ToArray(),
            CommonHelp,
            repairs.SelectMany((r, i) => new[]
            {
                Step(TraceKind.Tokenize, $"Virus node {i + 1}", "Compiler scanner reaches a corrupted snippet.", r.Corrupted, "status -> infected"),
                Step(TraceKind.Compare, "Diagnosis", r.Diagnostic, r.Corrupted, "repair target identified"),
                Step(TraceKind.Output, "Recovery test", "The repaired snippet compiles and produces the expected result.", r.FixedLine, $"Output: {r.Output}")
            }).ToArray(),
            string.Join("\n", repairs.Select(r => r.Output)),
            true,
            repairs.Select(r => r.Corrupted).ToArray(),
            $"Repair five snippets from: {review}");

    private static BossRepair Repair(string corrupted, string fixedLine, string term, string diagnostic, string output)
        => new(corrupted, fixedLine, term, diagnostic, output);

    private sealed record BossRepair(string Corrupted, string FixedLine, string Term, string Diagnostic, string Output);

    private static CodeLine Line(string text, string term, string explanation, string usage)
        => new(text, term, explanation, usage);

    private static IReadOnlyList<RuntimeTraceStep> AutoTrace(IReadOnlyList<CodeLine> lines, string output)
    {
        var trace = new List<RuntimeTraceStep>();
        var outputLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var printCount = lines.Count(l => l.Text.Trim().StartsWith("print", StringComparison.Ordinal));
        var printSeen = 0;
        var outputIndex = 0;
        foreach (var line in lines)
        {
            var text = line.Text.Trim();
            var kind = GuessKind(text);
            var shouldEmitPrintOutput = true;
            if (text.StartsWith("print", StringComparison.Ordinal))
            {
                printSeen++;
                shouldEmitPrintOutput = printSeen > Math.Max(0, printCount - outputLines.Length);
            }

            var after = DataAfter(text, outputLines, ref outputIndex, shouldEmitPrintOutput);
            trace.Add(Step(kind, line.Term, line.Explanation, "", after));
        }

        if (trace.Count == 0)
        {
            trace.Add(Step(TraceKind.Tokenize, "No operation", "No visible output is produced.", "", "compiled"));
        }

        return trace;
    }

    private static TraceKind GuessKind(string text)
    {
        if (text.StartsWith("#", StringComparison.Ordinal)) return TraceKind.Tokenize;
        if (text.StartsWith("print", StringComparison.Ordinal)) return TraceKind.Output;
        if (text.StartsWith("for ", StringComparison.Ordinal) || text.StartsWith("while ", StringComparison.Ordinal)) return TraceKind.Loop;
        if (text.StartsWith("if ", StringComparison.Ordinal) || text.StartsWith("elif ", StringComparison.Ordinal) || text.StartsWith("else", StringComparison.Ordinal)) return TraceKind.Compare;
        if (text.StartsWith("def ", StringComparison.Ordinal) || (text.EndsWith(")", StringComparison.Ordinal) && !text.Contains("=", StringComparison.Ordinal))) return TraceKind.FunctionCall;
        if (text.StartsWith("return ", StringComparison.Ordinal)) return TraceKind.Return;
        if (text.Contains("=", StringComparison.Ordinal)) return TraceKind.Assign;
        return TraceKind.Tokenize;
    }

    private static string DataAfter(string text, IReadOnlyList<string> outputLines, ref int outputIndex, bool shouldEmitPrintOutput)
    {
        if (text.StartsWith("#", StringComparison.Ordinal)) return "Python skips this note";
        if (text.StartsWith("print", StringComparison.Ordinal))
        {
            if (!shouldEmitPrintOutput)
            {
                return "output waits for the active path";
            }

            var value = outputIndex < outputLines.Count ? outputLines[outputIndex++] : "value";
            return $"Output: {value}";
        }

        var eq = text.IndexOf('=');
        if (eq > 0 && !text.StartsWith("if ", StringComparison.Ordinal) && !text.StartsWith("elif ", StringComparison.Ordinal))
        {
            return $"{text[..eq].Trim()} -> {text[(eq + 1)..].Trim()}";
        }

        if (text.StartsWith("return ", StringComparison.Ordinal)) return $"return -> {text["return ".Length..].Trim()}";
        if (text.StartsWith("for ", StringComparison.Ordinal)) return "loop values enter one at a time";
        if (text.StartsWith("while ", StringComparison.Ordinal)) return "condition is checked before each pass";
        if (text.StartsWith("if ", StringComparison.Ordinal) || text.StartsWith("elif ", StringComparison.Ordinal) || text.StartsWith("else", StringComparison.Ordinal)) return "branch decision";
        if (text.StartsWith("def ", StringComparison.Ordinal)) return "function stored for later";
        return "compiled";
    }

    private static (string Corrupted, string Diagnostic) Corrupt(string fixedLine)
    {
        var line = fixedLine;
        if (line.StartsWith("def ", StringComparison.Ordinal) || line.StartsWith("if ", StringComparison.Ordinal) || line.StartsWith("elif ", StringComparison.Ordinal) || line.StartsWith("else", StringComparison.Ordinal) || line.StartsWith("for ", StringComparison.Ordinal) || line.StartsWith("while ", StringComparison.Ordinal) || line.StartsWith("try", StringComparison.Ordinal) || line.StartsWith("except", StringComparison.Ordinal))
        {
            return (line.TrimEnd(':'), "Block headers need a colon so Python knows an indented block begins.");
        }
        if (line.Contains(" in ", StringComparison.Ordinal) && line.StartsWith("for ", StringComparison.Ordinal))
        {
            return (line.Replace(" in ", " of ", StringComparison.Ordinal), "Python loops use in to pull values from a sequence.");
        }
        if (line.Contains("True", StringComparison.Ordinal)) return (line.Replace("True", "true", StringComparison.Ordinal), "Python booleans are capitalized: True and False.");
        if (line.Contains("None", StringComparison.Ordinal)) return (line.Replace("None", "none", StringComparison.Ordinal), "None is capitalized in Python.");
        if (line.Contains("==", StringComparison.Ordinal)) return (line.Replace("==", "=", StringComparison.Ordinal), "Use == for comparison. A single = stores a value.");
        if (line.Contains(">=", StringComparison.Ordinal)) return (line.Replace(">=", "=>", StringComparison.Ordinal), "Use >= for greater than or equal.");
        if (line.Contains(".append(", StringComparison.Ordinal)) return (line.Replace(".append(", ".append = ", StringComparison.Ordinal).TrimEnd(')'), "append is a method call, so it needs parentheses.");
        if (line.Contains("print(", StringComparison.Ordinal)) return (line.TrimEnd(')'), "print needs a closing parenthesis.");
        if (line.Contains("[", StringComparison.Ordinal) && line.Contains("]", StringComparison.Ordinal)) return (line.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal), "Lists and indexes use square brackets.");
        if (line.Contains("{", StringComparison.Ordinal) && line.Contains("}", StringComparison.Ordinal)) return (line.Replace("{", "[", StringComparison.Ordinal).Replace("}", "]", StringComparison.Ordinal), "Dictionaries use curly braces for key/value pairs.");
        if (line.Contains("\"", StringComparison.Ordinal)) return (line.Replace("\"", "", StringComparison.Ordinal), "Text strings need quotes.");
        if (line.Contains("=", StringComparison.Ordinal)) return ($"{line} +", "The expression is incomplete. Remove the extra operator or finish the value.");
        return ($"{line} ", "The corrupted code has an extra character.");
    }

    private static RuntimeTraceStep Step(TraceKind kind, string title, string detail, string before, string after)
    {
        if (IsOutputStep(kind, title, after))
        {
            var value = OutputValue(after);
            var source = string.IsNullOrWhiteSpace(before) ? "" : $" Python first reads {FriendlyState(before)}.";
            var output = string.IsNullOrWhiteSpace(value)
                ? "The result is sent to the output console."
                : $"print() sends {value} to the output console.";
            return new(kind, title, $"{source} {output}".Trim(), before, string.IsNullOrWhiteSpace(value) ? after : $"Console shows: {value}");
        }

        return new(kind, title, detail, before, after);
    }

    private static bool IsOutputStep(TraceKind kind, string title, string after)
        => kind is TraceKind.Print or TraceKind.Output
           || title.Contains("print", StringComparison.OrdinalIgnoreCase)
           || after.StartsWith("Output:", StringComparison.OrdinalIgnoreCase);

    private static string OutputValue(string after)
    {
        if (!after.StartsWith("Output:", StringComparison.OrdinalIgnoreCase))
        {
            return after;
        }

        return after["Output:".Length..].Trim();
    }

    private static string FriendlyState(string value)
        => value.Replace("->", "as", StringComparison.Ordinal).Replace("Output:", "the current console output is", StringComparison.Ordinal).Trim();
}
