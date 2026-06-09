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
        new("sum(values)", "sum", "Adds numbers from a collection or generator.", "total = sum(numbers)"),
        new("x for x in items", "generator", "Creates values one at a time for a function like sum.", "sum(x for x in items)"),
        new("len(items)", "len", "Counts items in a collection.", "count = len(items)"),
        new("round(value, 2)", "round", "Rounds a number to a set number of decimal places.", "total = round(total, 2)"),
        new("abs(x - y)", "abs", "Returns the positive distance from zero.", "gap = abs(x - y)"),
        new("f\"{price:.2f}\"", "format", "Formats a value for clean display.", "print(f\"{price:,.2f}\")"),
        new("def add(a, b):", "def", "Defines a function.", "Indented lines become the function body."),
        new("return value", "return", "Sends a result out of a function.", "return total"),
        new("input(\"Name: \")", "input", "Receives text from the user.", "name = input(\"Name: \")"),
        new("import random", "import", "Loads tools from a module.", "import random"),
        new("try:", "try/except", "Starts protected code that might fail.", "Use except to handle a known problem.")
    ];

    public static IReadOnlyList<Lesson> BeginnerLessons { get; } = AddBosses(
    [
        TopicMission(1, "Code Order", "See that Python executes one line, then the next.", "Boot\nReady\nGo", Line("print(\"Boot\")", "print()", "print() sends Boot to the output console.", "Python runs this first."), Line("print(\"Ready\")", "order", "The second line runs after the first.", "Execution moves downward."), Line("print(\"Go\")", "order", "The third line runs last.", "Top-to-bottom order matters.")),
        TopicMission(2, "Console Output", "Use print() to show data to the user.", "Hello", Line("print(\"Hello\")", "print()", "print() displays the value inside its parentheses.", "The text appears in the console.")),
        TopicMission(3, "Comments", "Use comments as notes that Python skips.", "Go", Line("# launch note", "comment", "A comment starts with # and does not run.", "Use comments to explain intent."), Line("print(\"Go\")", "print()", "Only the print line creates output.", "Python skips the note and runs this line.")),
        TopicMission(4, "Strings", "Recognize text values by their quotes.", "Ada", Line("print(\"Ada\")", "string", "A string is text wrapped in quotes.", "Quotes tell Python this is data, not a name.")),
        TopicMission(5, "Syntax Symbols", "Practice the symbols that make a print line valid.", "Ready", Line("print(\"Ready\")", "syntax", "Parentheses hold the value, and quotes hold the text.", "A tiny symbol change can break code.")),

        TopicMission(6, "Text Variables", "Put text into a variable and read it later.", "Ada", Line("name = \"Ada\"", "variable", "A variable is a named memory slot.", "The left side names the slot; the right side provides the value."), Line("print(name)", "variable use", "No quotes means read the variable.", "print asks memory for name.")),
        TopicMission(7, "Integer Values", "Store and display a whole number.", "12", Line("age = 12", "int", "An int is a whole number.", "Numbers do not need quotes."), Line("print(age)", "print int", "print can display numbers too.", "The number travels from memory to output.")),
        TopicMission(8, "Float Values", "Store and display a decimal number.", "1.52", Line("height = 1.52", "float", "A float is a decimal number.", "Python uses a dot for decimals."), Line("print(height)", "print float", "The decimal value moves to output.", "The console shows the number.")),
        TopicMission(9, "Boolean Values", "Store a True or False switch.", "True", Line("is_ready = True", "bool", "A bool is True or False.", "Python capitalizes True and False."), Line("print(is_ready)", "print bool", "Booleans can be displayed.", "The switch value moves to output.")),
        TopicMission(10, "Empty Values", "Use None when a value is intentionally empty.", "None", Line("favorite = None", "None", "None means no value yet.", "Use it as a placeholder."), Line("print(favorite)", "print None", "Printing None shows the placeholder.", "This helps while building code.")),

        TopicMission(11, "Readable Names", "Practice Python's snake_case naming style.", "10", Line("player_score = 10", "snake_case", "Python names often use underscores between words.", "Readable names help humans understand code."), Line("print(player_score)", "variable use", "Read a named value.", "Names should describe their data.")),
        TopicMission(12, "Name Rules", "Avoid invalid names and reserved words.", "12", Line("science_classes = 12", "valid name", "Names can use letters and underscores.", "Do not start names with numbers or use spaces."), Line("print(science_classes)", "print", "Display the valid variable.", "The variable name stays readable.")),
        TopicMission(13, "String Joining", "Combine two strings with plus.", "Hello, Ada", Line("greeting = \"Hello, \" + \"Ada\"", "concat", "+ can join strings.", "Both sides must be strings."), Line("print(greeting)", "print", "Display the combined string.", "The finished text is stored in memory.")),
        TopicMission(14, "String Conversion", "Convert a number when joining text.", "12 pizzas", Line("count = 12", "int", "Store a number.", "Numbers are not text yet."), Line("message = str(count) + \" pizzas\"", "str()", "str() converts the number into text.", "Now + can join the pieces."), Line("print(message)", "print", "Display the joined message.", "The console shows readable text.")),
        TopicMission(15, "F-String Basics", "Insert a variable into text.", "Hello, Ada", Line("name = \"Ada\"", "variable", "Store a name.", "The value waits in memory."), Line("message = f\"Hello, {name}\"", "f-string", "An f-string inserts variable values.", "Use f before the opening quote."), Line("print(message)", "print", "Display the formatted message.", "The final message prints.")),

        TopicMission(16, "Addition", "Let Python calculate before storing.", "10", Line("total = 4 + 6", "addition", "Python evaluates the right side first.", "4 + 6 becomes 10."), Line("print(total)", "print", "Display the result.", "The answer comes from memory.")),
        TopicMission(17, "Subtraction", "Subtract one number from another.", "7", Line("remaining = 10 - 3", "subtraction", "Python subtracts on the right side.", "10 - 3 becomes 7."), Line("print(remaining)", "print", "Display the remaining value.", "7 prints.")),
        TopicMission(18, "Multiplication", "Multiply values for a calculated result.", "24", Line("area = 6 * 4", "multiplication", "* multiplies numbers.", "6 times 4 becomes 24."), Line("print(area)", "print", "Display the product.", "24 prints.")),
        TopicMission(19, "Division", "Divide values and keep a decimal result.", "2.5", Line("share = 10 / 4", "division", "/ divides and returns a float.", "10 divided by 4 becomes 2.5."), Line("print(share)", "print", "Display the quotient.", "2.5 prints.")),
        TopicMission(20, "Reassignment", "Change a variable by reading its old value.", "15", Line("score = 10", "assign", "Create a score.", "Memory slot score receives 10."), Line("score = score + 5", "reassign", "Read old score, add 5, store new score.", "Variables can change."), Line("print(score)", "print", "Show the updated value.", "Output sees the new score.")),

        TopicMission(21, "Input Text", "Learn that input() receives text from a user.", "student input", Line("name = input(\"Name: \")", "input()", "input asks the user for text.", "The typed answer enters name."), Line("print(name)", "print", "Display the typed answer.", "The stored text prints.")),
        TopicMission(22, "Integer Casting", "Turn typed text into a whole number.", "12", Line("age_text = \"12\"", "string", "Input starts as text.", "This simulates text from input()."), Line("age = int(age_text)", "int()", "int() converts number text to an integer.", "The value becomes numeric."), Line("print(age)", "print", "Display the converted number.", "12 prints.")),
        TopicMission(23, "Float Casting", "Turn typed text into a decimal number.", "9.5", Line("length_text = \"9.5\"", "string", "Decimal input also starts as text.", "The characters are not numeric yet."), Line("length = float(length_text)", "float()", "float() converts decimal text into a number.", "The value can now be used in math."), Line("print(length)", "print", "Display the converted decimal.", "9.5 prints.")),
        TopicMission(24, "Choosing Casts", "Choose int, float, or str based on the data.", "3", Line("classes_text = \"3\"", "input text", "A count of classes should become a whole number.", "Counts are usually integers."), Line("classes = int(classes_text)", "int()", "int() fits whole-number counts.", "The text becomes numeric."), Line("print(classes)", "print", "Display the selected cast result.", "3 prints.")),
        TopicMission(25, "Type Checking", "Ask Python what kind of value you have.", "<class 'int'>", Line("count = 3", "int", "Store a whole number.", "count is an integer."), Line("print(type(count))", "type()", "type() reports the value kind.", "Useful for debugging.")),

        TopicMission(26, "Decimal Formatting", "Format a decimal to two places.", "3.50", Line("price = 3.5", "float", "Store a decimal money-like value.", "The raw value has one decimal digit."), Line("print(f\"{price:.2f}\")", ".2f", ".2f displays two digits after the decimal.", "3.5 appears as 3.50.")),
        TopicMission(27, "Comma Formatting", "Format a large number with commas.", "12,500.00", Line("balance = 12500", "number", "Store a large balance.", "The value is numeric."), Line("print(f\"{balance:,.2f}\")", "comma format", "The comma adds thousands separators.", "Two decimal places are shown.")),
        TopicMission(28, "Right Alignment", "Right-align a value in a column.", "       42", Line("count = 42", "int", "Store a count.", "The value is ready for a table."), Line("print(f\"{count:>9}\")", "right align", ">9 means right-align inside nine spaces.", "Useful for columns.")),
        TopicMission(29, "Left Alignment", "Left-align text in a column.", "Ada     |", Line("name = \"Ada\"", "string", "Store a name.", "Text can be column-aligned."), Line("print(f\"{name:<8}|\")", "left align", "<8 means left-align inside eight spaces.", "The pipe reveals the column edge.")),
        TopicMission(30, "Centered Alignment", "Center text in a column.", "  Ada   |", Line("name = \"Ada\"", "string", "Store text.", "The value will be centered."), Line("print(f\"{name:^8}|\")", "center align", "^8 centers text inside eight spaces.", "Useful for table headers.")),

        TopicMission(31, "Order Of Operations", "Trace how Python applies math precedence.", "20.0", Line("result = 4 + 9 / 3 * 6 - 2", "precedence", "Division and multiplication happen before addition and subtraction.", "Python follows math order."), Line("print(result)", "print", "Display the traced result.", "20.0 prints.")),
        TopicMission(32, "Square Roots", "Use math.sqrt() after importing math.", "5.0", Line("import math", "import", "import loads the math tools.", "math.sqrt becomes available."), Line("root = math.sqrt(25)", "sqrt()", "sqrt returns the square root.", "25 becomes 5.0."), Line("print(root)", "print", "Display the root.", "5.0 prints.")),
        TopicMission(33, "Absolute Difference", "Use abs() to remove a negative sign.", "7", Line("x = 3", "assign", "Store the first number.", "x receives 3."), Line("y = 10", "assign", "Store the second number.", "y receives 10."), Line("difference = abs(x - y)", "abs()", "abs() returns distance from zero.", "The difference becomes positive."), Line("print(difference)", "print", "Display the absolute difference.", "7 prints.")),
        TopicMission(34, "Increment Shortcut", "Use += to update a number clearly.", "4", Line("count = 3", "assign", "Start count at 3.", "Memory receives the first value."), Line("count += 1", "+=", "+= adds to the current value.", "This means count = count + 1."), Line("print(count)", "print", "Display the updated value.", "4 prints.")),
        TopicMission(35, "Trace Variables", "Follow several assignments in order.", "9", Line("x = 1", "assign", "x starts at 1.", "First memory slot is created."), Line("y = 3", "assign", "y starts at 3.", "Second memory slot is created."), Line("x += y", "+=", "Add y into x.", "x becomes 4."), Line("y = x + 5", "reassign", "Use the new x to update y.", "y becomes 9."), Line("print(y)", "print", "Display the final value.", "9 prints.")),

        TopicMission(36, "Simple Lists", "Store several values in one ordered container.", "['red', 'green', 'blue']", Line("colors = [\"red\", \"green\", \"blue\"]", "list", "A list stores ordered items.", "Square brackets create a list."), Line("print(colors)", "print list", "Print can display the list.", "Items stay in order.")),
        TopicMission(37, "List Indexes", "Read one item from a list.", "red", Line("colors = [\"red\", \"green\", \"blue\"]", "list", "Store three colors.", "Indexes start at 0."), Line("first = colors[0]", "index", "colors[0] gets the first item.", "0 points at red."), Line("print(first)", "print", "Display the selected item.", "Only red prints.")),
        TopicMission(38, "List Append", "Add an item to the end of a list.", "['key', 'map']", Line("items = [\"key\"]", "list", "Start with one item.", "items has one slot."), Line("items.append(\"map\")", "append()", "append adds to the end.", "The list mutates."), Line("print(items)", "print", "Show the changed list.", "Now there are two items.")),
        TopicMission(39, "List Length", "Count how many items are in a list.", "3", Line("items = [\"key\", \"map\", \"coin\"]", "list", "Store three inventory items.", "The list has three entries."), Line("print(len(items))", "len()", "len() counts list items.", "The result is 3.")),
        TopicMission(40, "Mini Inventory", "Build a tiny list-based inventory.", "key", Line("items = [\"key\", \"map\"]", "list", "Store two items.", "The list preserves order."), Line("first_item = items[0]", "index", "Index 0 reads the first item.", "The selected item enters memory."), Line("print(first_item)", "print", "Display one inventory item.", "Only the selected item prints.")),

        TopicMission(41, "Dictionaries", "Store labeled key/value data.", "{'name': 'Ada', 'score': 10}", Line("profile = {\"name\": \"Ada\", \"score\": 10}", "dict", "A dictionary maps keys to values.", "Keys unlock values."), Line("print(profile)", "print dict", "Show the whole mapping.", "Both keys and values print.")),
        TopicMission(42, "Dictionary Lookup", "Read one value by key.", "Ada", Line("profile = {\"name\": \"Ada\", \"score\": 10}", "dict", "Store a profile.", "Keys point to values."), Line("name = profile[\"name\"]", "lookup", "The name key returns Ada.", "Square brackets choose a key."), Line("print(name)", "print", "Display the selected value.", "Only Ada prints.")),
        TopicMission(43, "Dictionary Update", "Change one value inside a dictionary.", "15", Line("profile = {\"score\": 10}", "dict", "Store a score under a key.", "score starts at 10."), Line("profile[\"score\"] = 15", "update", "Assigning through a key changes that value.", "The score key now points to 15."), Line("print(profile[\"score\"])", "lookup", "Read the updated key.", "15 prints.")),
        TopicMission(44, "Record Lists", "Store several dictionaries in one list.", "pencil", Line("basket = [", "list of dicts", "Start a list that will hold dictionary records.", "This is like an array of labeled records."), Line("    {\"item\": \"pencil\", \"quantity\": 4},", "dict record", "One dictionary stores one item and its count.", "pencil record enters the list."), Line("    {\"item\": \"cookie\", \"quantity\": 2}", "dict record", "A second dictionary uses the same keys.", "cookie record enters the list."), Line("]", "list close", "The closing bracket finishes the list.", "basket now has two records."), Line("print(basket[0][\"item\"])", "nested lookup", "basket[0] gets the first dictionary, then item reads its name.", "pencil prints.")),
        TopicMission(45, "Nested Lookup", "Read a value from a dictionary inside a list.", "2", Line("basket = [{\"item\": \"cookie\", \"quantity\": 2}]", "list of dicts", "Store one record inside a list.", "basket[0] is a dictionary."), Line("quantity = basket[0][\"quantity\"]", "nested lookup", "Read the first record, then the quantity key.", "quantity receives 2."), Line("print(quantity)", "print", "Display the nested value.", "2 prints.")),

        TopicMission(46, "Price Records", "Add price fields to each basket record.", "2.5", Line("basket = [", "list of dicts", "Start a list of item dictionaries.", "basket will hold records."), Line("    {\"item\": \"cookie\", \"quantity\": 2, \"unit_price\": 1.25, \"extended_price\": 2.50}", "price record", "The record stores name, count, unit price, and line total.", "cookie record has full price data."), Line("]", "list close", "Close the list after the record.", "basket is ready."), Line("print(basket[0][\"extended_price\"])", "nested lookup", "Read the first record, then its extended price.", "2.5 prints.")),
        TopicMission(47, "Line Totals", "Calculate one record total from quantity and unit price.", "2.0", Line("item = {\"quantity\": 4, \"unit_price\": 0.50}", "dict record", "Store one purchasable item.", "The record has count and price."), Line("line_total = item[\"quantity\"] * item[\"unit_price\"]", "line total", "Multiply quantity by unit price.", "The result is 2.0."), Line("print(line_total)", "print", "Display the line total.", "2.0 prints.")),
        TopicMission(48, "Basket Sum", "Calculate the full basket total from records.", "8.49", Line("basket = [", "list of dicts", "Use a list of dictionary records.", "The basket data is ready to calculate."), Line("    {\"quantity\": 4, \"unit_price\": 0.50},", "dict record", "Pencil quantity and unit price are stored.", "4 times 0.50 will be calculated."), Line("    {\"quantity\": 2, \"unit_price\": 1.25},", "dict record", "Cookie quantity and unit price are stored.", "2 times 1.25 will be calculated."), Line("    {\"quantity\": 1, \"unit_price\": 3.99}", "dict record", "Notebook quantity and unit price are stored.", "1 times 3.99 will be calculated."), Line("]", "list close", "Finish the basket list.", "The records can now be summed."), Line("basket_total = sum(", "sum()", "sum adds several calculated values.", "Python starts the total calculation."), Line("    item[\"quantity\"] * item[\"unit_price\"]", "line total", "Each item multiplies quantity by unit price.", "A line price is produced."), Line("    for item in basket", "generator", "This visits each record in basket.", "Each record becomes item."), Line(")", "call close", "Close the sum call.", "basket_total receives 8.49."), Line("print(basket_total)", "print", "Display the basket total.", "8.49 prints.")),
        TopicMission(49, "Basket Check", "Verify that the basket total matches the expected value.", "True", Line("basket_total = 8.49", "test value", "Store the calculated total.", "This represents the basket result."), Line("print(basket_total == 8.49)", "check", "Compare actual total to expected total.", "True means the total matches.")),
        TopicMission(50, "Basket Message", "Present the final basket total with an f-string.", "Basket total: 8.49", Line("basket_total = 8.49", "assign", "Store the final basket total.", "The total is ready for display."), Line("message = f\"Basket total: {basket_total}\"", "f-string", "Insert the total into a readable message.", "The message is built."), Line("print(message)", "print", "Display the final program output.", "The basket total is shown.")),

        TopicMission(51, "Comparisons", "Create a True or False result from a comparison.", "True", Line("lives = 3", "assign", "Store lives.", "lives gets 3."), Line("has_lives = lives > 0", "comparison", "3 > 0 becomes True.", "The boolean result is stored."), Line("print(has_lives)", "print", "Display the boolean.", "True prints.")),
        TopicMission(52, "If Blocks", "Run code only when a condition is true.", "Launch", Line("is_ready = True", "bool", "Store readiness.", "True means yes."), Line("if is_ready:", "if", "The branch opens when True.", "Colon starts the block."), Line("    print(\"Launch\")", "indented block", "Indented code belongs to the if.", "Runs only when ready.")),
        TopicMission(53, "Indentation", "See that indented code belongs to the line above it.", "Armed", Line("armed = True", "bool", "Store an armed switch.", "True means the branch can run."), Line("if armed:", "if", "The if header starts a block.", "The colon announces indented code."), Line("    print(\"Armed\")", "indent", "Four spaces show this line belongs to the if.", "Indented code runs when the branch passes.")),
        TopicMission(54, "If Else", "Choose between two paths.", "Minor", Line("age = 12", "assign", "Store age.", "age gets 12."), Line("if age >= 18:", "if", "Check adult condition.", "12 >= 18 is false."), Line("    print(\"Adult\")", "true path", "Skipped when false.", "This line does not run."), Line("else:", "else", "Fallback path.", "Runs when if is false."), Line("    print(\"Minor\")", "else body", "This branch runs.", "Minor prints.")),
        TopicMission(55, "Equality", "Use == to compare values.", "True", Line("code = \"red\"", "string", "Store text for checking.", "The string goes into memory."), Line("is_match = code == \"red\"", "==", "== asks whether two values match.", "The answer becomes True."), Line("print(is_match)", "print", "Display the comparison result.", "True prints.")),

        TopicMission(56, "Elif Chains", "Check several conditions in order.", "B", Line("score = 85", "assign", "Store score.", "score gets 85."), Line("if score >= 90:", "if", "Check A grade.", "False for 85."), Line("    grade = \"A\"", "skipped", "Skipped because first condition fails.", "No assignment."), Line("elif score >= 80:", "elif", "Check B grade.", "True for 85."), Line("    grade = \"B\"", "taken", "Stores B.", "This branch wins."), Line("else:", "else", "Skipped after a match.", "Only runs if no prior match."), Line("    grade = \"Keep practicing\"", "else body", "Skipped.", "No change."), Line("print(grade)", "print", "Display final grade.", "B prints.")),
        TopicMission(57, "And Logic", "Require two conditions at once.", "True", Line("has_key = True", "bool", "Store key state.", "Player has a key."), Line("door_open = True", "bool", "Store door state.", "Door is open."), Line("can_enter = has_key and door_open", "and", "and requires both sides to be True.", "The result becomes True."), Line("print(can_enter)", "print", "Display access result.", "True prints.")),
        TopicMission(58, "Or Logic", "Allow either condition to pass.", "True", Line("has_code = False", "bool", "No code is present.", "The first switch is off."), Line("has_badge = True", "bool", "Badge is present.", "The second switch is on."), Line("can_enter = has_code or has_badge", "or", "or passes if either side is True.", "The result becomes True."), Line("print(can_enter)", "print", "Display access result.", "True prints.")),
        TopicMission(59, "Not Logic", "Flip a boolean condition.", "True", Line("door_locked = False", "bool", "Door is not locked.", "False goes into memory."), Line("can_enter = not door_locked", "not", "not flips False into True.", "The result is stored."), Line("print(can_enter)", "print", "Display access result.", "True prints.")),
        TopicMission(60, "Efficient Conditions", "Store a decision result before printing it.", "approved", Line("score = 92", "assign", "Store a score.", "The decision will use this value."), Line("passed = score >= 70", "comparison", "The comparison becomes a boolean.", "passed receives True."), Line("if passed:", "if", "Use the stored boolean as the condition.", "The branch opens."), Line("    print(\"approved\")", "output", "The true path displays the result.", "approved prints.")),

        TopicMission(61, "Range Loops", "Repeat code using range.", "0\n1\n2", Line("for number in range(3):", "for/range", "range(3) creates 0, 1, 2.", "Each value enters number."), Line("    print(number)", "loop body", "Runs once per value.", "Print fires three times.")),
        TopicMission(62, "Loop Variables", "Watch the loop variable change each pass.", "0\n1\n2\n3", Line("for step in range(4):", "loop variable", "step receives 0, 1, 2, then 3.", "The value changes each iteration."), Line("    print(step)", "loop body", "Print the current step.", "Four short outputs appear.")),
        TopicMission(63, "List Loops", "Loop through collection items.", "red\ngreen", Line("colors = [\"red\", \"green\"]", "list", "Store two colors.", "List has two slots."), Line("for color in colors:", "for", "Each list item enters color.", "Loop follows list order."), Line("    print(color)", "loop body", "Print each color.", "Runs twice.")),
        TopicMission(64, "Accumulators", "Add values into a running total.", "6", Line("total = 0", "accumulator", "Start total at zero.", "Memory slot is ready."), Line("for number in [1, 2, 3]:", "for list", "Loop over three numbers.", "Each number enters the loop."), Line("    total = total + number", "update", "Add each number to total.", "Total changes each iteration."), Line("print(total)", "print", "Show final total.", "6 prints.")),
        TopicMission(65, "While Loops", "Repeat while a condition stays true.", "3\n2\n1", Line("count = 3", "assign", "Start a countdown.", "count gets 3."), Line("while count > 0:", "while", "Repeat while count is above zero.", "Condition checked before each loop."), Line("    print(count)", "loop body", "Print current count.", "Runs each pass."), Line("    count = count - 1", "update", "Move count toward stopping.", "Prevents infinite loop.")),

        TopicMission(66, "Traversing Lists", "Use a loop to visit each list item.", "Ada\nLin", Line("names = [\"Ada\", \"Lin\"]", "list", "Store two names.", "The loop will traverse them."), Line("for name in names:", "for", "Each item enters name.", "The loop preserves order."), Line("    print(name)", "loop body", "Print the current item.", "Each name appears once.")),
        TopicMission(67, "Counting Matches", "Count items that meet a condition.", "2", Line("scores = [90, 70, 95]", "list", "Store three scores.", "The loop will inspect each one."), Line("passes = 0", "accumulator", "Start the counter at zero.", "passes counts successful scores."), Line("for score in scores:", "for", "Each score enters the loop.", "There are three passes."), Line("    if score >= 80:", "if", "Check whether this score passes.", "Two scores meet the condition."), Line("        passes += 1", "+=", "Increase the counter for each pass.", "The counter reaches 2."), Line("print(passes)", "print", "Display the count.", "2 prints.")),
        TopicMission(68, "Looped Totals", "Total values from a short list.", "9", Line("prices = [2, 3, 4]", "list", "Store three small prices.", "The list is short for visual tracing."), Line("total = 0", "accumulator", "Start total at zero.", "The total will grow."), Line("for price in prices:", "for", "Each price enters price.", "The loop runs three times."), Line("    total += price", "+=", "Add the current price into total.", "Total becomes 9."), Line("print(total)", "print", "Display the final total.", "9 prints.")),
        TopicMission(69, "Nested Loop Shape", "Use two short loops to make rows and columns.", "cell\ncell\ncell\ncell", Line("for row in range(2):", "outer loop", "The outer loop creates two rows.", "row receives 0 then 1."), Line("    for col in range(2):", "inner loop", "The inner loop creates two columns.", "Only four total cells appear."), Line("        print(\"cell\")", "loop body", "Print one cell per pair.", "The output stays short.")),
        TopicMission(70, "Loop Stop Values", "Stop a while loop after a small count.", "0\n1\n2", Line("n = 0", "assign", "Start n at zero.", "The loop will update n."), Line("while n < 3:", "while", "Run while n is less than 3.", "The condition is checked first."), Line("    print(n)", "loop body", "Print the current value.", "0, 1, and 2 print."), Line("    n += 1", "+=", "Move n toward stopping.", "The loop ends when n becomes 3.")),

        TopicMission(71, "Function Definitions", "Create a reusable code block.", "", Line("def greet():", "def", "Create a reusable function.", "The body waits until called."), Line("    print(\"Hello\")", "function body", "Indented code belongs to the function.", "This runs when greet is called.")),
        TopicMission(72, "Function Calls", "Run the reusable function.", "Hello", Line("def greet():", "def", "Create a reusable function.", "The body waits until called."), Line("    print(\"Hello\")", "function body", "Runs when greet is called.", "Indented under def."), Line("greet()", "call", "Call the function.", "Python jumps into the function body.")),
        TopicMission(73, "Parameters", "Send data into a function.", "Ada", Line("def greet(name):", "parameters", "name receives the input value.", "Parameters are function memory slots."), Line("    print(name)", "function body", "Print the parameter.", "The sent-in value appears."), Line("greet(\"Ada\")", "call", "Send Ada into the function.", "Ada enters name.")),
        TopicMission(74, "Return Values", "Send data back out of a function.", "5", Line("def add(a, b):", "parameters", "a and b receive inputs.", "Function expects two values."), Line("    return a + b", "return", "Send a result back.", "a + b becomes the return value."), Line("result = add(2, 3)", "call", "2 and 3 enter the function.", "Returned value enters result."), Line("print(result)", "print", "Display result.", "5 prints.")),
        TopicMission(75, "Status Functions", "Combine function, if, return, and output.", "pass", Line("def status(score):", "function", "Create a status function.", "score becomes a parameter."), Line("    if score >= 10:", "if", "Check if score passes.", "Branch gate opens for 12."), Line("        return \"pass\"", "return", "Return pass when true.", "This path runs."), Line("    return \"try again\"", "fallback", "Runs only if if did not return.", "Skipped here."), Line("result = status(12)", "call", "Send 12 into status.", "Returned text enters result."), Line("print(result)", "print", "Show final result.", "pass prints.")),

        TopicMission(76, "Total Functions", "Wrap a total calculation in a function.", "6", Line("def total_for(numbers):", "function", "Create a function for totals.", "numbers becomes a parameter."), Line("    total = 0", "accumulator", "Start a local total.", "The value belongs to the function call."), Line("    for number in numbers:", "for", "Loop over each number.", "The list is traversed."), Line("        total += number", "+=", "Add each number into total.", "The total grows."), Line("    return total", "return", "Send the final total back.", "The caller receives 6."), Line("print(total_for([1, 2, 3]))", "call", "Call the function and display the return value.", "6 prints.")),
        TopicMission(77, "Formatted Functions", "Return text that is ready to display.", "Total: 8.49", Line("def total_message(total):", "function", "Create a formatting function.", "total enters as a parameter."), Line("    return f\"Total: {total:.2f}\"", "return f-string", "Return a formatted message.", "The number uses two decimals."), Line("print(total_message(8.49))", "call", "Call the function and print its returned text.", "Total: 8.49 prints.")),
        TopicMission(78, "Validation Functions", "Return True or False from a check.", "True", Line("def is_positive(value):", "function", "Create a validation function.", "value enters as a parameter."), Line("    return value > 0", "return bool", "The comparison result is returned.", "Positive numbers return True."), Line("print(is_positive(4))", "call", "Call the validation function.", "True prints.")),
        TopicMission(79, "Default Values", "Give a function parameter a fallback value.", "Hello, coder", Line("def greet(name=\"coder\"):", "default", "A default runs when no argument is sent.", "name becomes coder."), Line("    print(f\"Hello, {name}\")", "f-string", "Build output from the parameter.", "The default value is inserted."), Line("greet()", "call", "Call without an argument.", "The default is used.")),
        TopicMission(80, "Mini Calculator", "Combine parameters, math, and return.", "14", Line("def subtotal(price, quantity):", "function", "Create a reusable calculation.", "Both inputs enter the function."), Line("    return price * quantity", "return", "Multiply price by quantity.", "The product is returned."), Line("print(subtotal(3.5, 4))", "call", "Call the calculator.", "14 prints.")),

        TopicMission(81, "Starting Balance", "Store the first value in a ledger.", "9938.27", Line("checking_balance = 9938.27", "balance", "Store the starting checking balance.", "This is the first ledger value."), Line("print(checking_balance)", "print", "Display the starting balance.", "9938.27 prints.")),
        TopicMission(82, "Income Deposit", "Add income to a balance.", "18240.28", Line("checking_balance = 9938.27", "balance", "Start with the checking balance.", "The account has money already."), Line("income = 8302.01", "income", "Store the deposit amount.", "Income increases the balance."), Line("checking_balance += income", "+=", "Add income into the balance.", "The new balance is 18240.28."), Line("print(checking_balance)", "print", "Display the updated balance.", "18240.28 prints.")),
        TopicMission(83, "Rent Withdrawal", "Subtract rent from a balance.", "16511.19", Line("checking_balance = 18240.28", "balance", "Store the balance after income.", "This is the current account value."), Line("rent = 1729.09", "rent", "Store the rent withdrawal.", "Rent decreases the balance."), Line("checking_balance -= rent", "-=", "Subtract rent from the balance.", "The new balance is 16511.19."), Line("print(checking_balance)", "print", "Display the updated balance.", "16511.19 prints.")),
        TopicMission(84, "Purchase Withdrawal", "Subtract purchases from a balance.", "13219.05", Line("checking_balance = 16511.19", "balance", "Store the balance after rent.", "This is the current value."), Line("purchases = 3292.14", "purchases", "Store purchase spending.", "Purchases decrease the balance."), Line("checking_balance -= purchases", "-=", "Subtract purchases.", "The new balance is 13219.05."), Line("print(checking_balance)", "print", "Display the updated balance.", "13219.05 prints.")),
        TopicMission(85, "Savings Rule", "Calculate ten percent savings.", "830.2", Line("income = 8302.01", "income", "Store the paycheck amount.", "Savings is based on income."), Line("savings = income * 0.10", "percent", "Multiply by 0.10 to calculate ten percent.", "savings becomes 830.201."), Line("savings = round(savings, 2)", "round()", "round keeps two decimal places.", "The value becomes 830.20."), Line("print(savings)", "print", "Display the savings amount.", "830.2 prints.")),

        TopicMission(86, "Interest Calculation", "Calculate one percent account interest.", "123.89", Line("checking_balance = 12388.85", "balance", "Store the balance before interest.", "Interest uses this amount."), Line("interest = checking_balance * 0.01", "percent", "One percent is 0.01.", "The raw interest is calculated."), Line("interest = round(interest, 2)", "round()", "Round money to two decimals.", "Interest becomes 123.89."), Line("print(interest)", "print", "Display interest.", "123.89 prints.")),
        TopicMission(87, "Ending Balance", "Add interest to get the final balance.", "12512.74", Line("checking_balance = 12388.85", "balance", "Store balance before interest.", "This is the account state."), Line("interest = 123.89", "interest", "Store calculated interest.", "Interest increases the balance."), Line("checking_balance += interest", "+=", "Add interest into checking.", "The final balance is 12512.74."), Line("print(checking_balance)", "print", "Display the ending balance.", "12512.74 prints.")),
        TopicMission(88, "Table Headers", "Print aligned table headings.", "Category  Deposits", Line("left = \"Category\"", "string", "Store the first heading.", "It labels the row type."), Line("right = \"Deposits\"", "string", "Store the second heading.", "It labels money entering."), Line("print(f\"{left:<9} {right:>8}\")", "table format", "Left and right alignment create columns.", "Headings line up.")),
        TopicMission(89, "Money Rows", "Print one formatted money row.", "Income  8,302.01", Line("label = \"Income\"", "string", "Store the row label.", "The label names the transaction."), Line("amount = 8302.01", "float", "Store the money amount.", "The value needs formatting."), Line("print(f\"{label:<8}{amount:>8,.2f}\")", "money format", "Use width, commas, and two decimals.", "The row looks like a table.")),
        TopicMission(90, "Ledger Row Function", "Use a function to format one ledger row.", "Rent   1,729.09", Line("def row(label, amount):", "function", "Create a row formatter.", "label and amount enter the function."), Line("    return f\"{label:<7}{amount:>8,.2f}\"", "return format", "Return aligned text.", "Money uses commas and two decimals."), Line("print(row(\"Rent\", 1729.09))", "call", "Print the formatted row.", "The row lines up.")),

        TopicMission(91, "Import Modules", "Load code from Python's standard library.", "module ready", Line("import random", "import", "import loads a module.", "The random tools become available."), Line("print(\"module ready\")", "print", "Confirm the module is loaded.", "The message prints.")),
        TopicMission(92, "Random Choice", "Use a module tool to select data.", "red", Line("import random", "import", "Load the random module.", "choice becomes available."), Line("colors = [\"red\"]", "list", "Use one item so the example is predictable.", "The list contains red."), Line("print(random.choice(colors))", "choice()", "choice selects one item from a list.", "red prints.")),
        TopicMission(93, "File Paths", "Use a variable to represent a file path.", "notes.txt", Line("path = \"notes.txt\"", "file path", "A file path is text that points to a file.", "The name is stored for later file work."), Line("print(path)", "print", "Display the path value.", "The file name prints.")),
        TopicMission(94, "Try Except", "Handle a known problem without crashing.", "handled", Line("try:", "try", "Start code that might fail.", "Python enters the protected block."), Line("    value = int(\"x\")", "risky cast", "This conversion cannot work.", "A ValueError happens."), Line("except ValueError:", "except", "Catch that specific error.", "The program recovers."), Line("    print(\"handled\")", "recovery", "Show that the error was handled.", "handled prints.")),
        TopicMission(95, "Debug Messages", "Print a clear check while testing.", "checking total", Line("debug_message = \"checking total\"", "debug", "Store a temporary message.", "Debug messages explain what is being checked."), Line("print(debug_message)", "print", "Display the message while testing.", "The developer sees progress.")),

        TopicMission(96, "Ledger Inputs", "Prepare the values a budget program needs.", "ready", Line("starting_balance = 9938.27", "balance", "Store the starting account value.", "The program begins with this data."), Line("income = 8302.01", "income", "Store money coming in.", "Income will be added later."), Line("rent = 1729.09", "rent", "Store one bill.", "Rent will be subtracted later."), Line("print(\"ready\")", "print", "Confirm the input values exist.", "ready prints.")),
        TopicMission(97, "Ledger Calculations", "Combine deposits and withdrawals.", "16511.19", Line("balance = 9938.27", "balance", "Start with the account balance.", "The ledger begins here."), Line("balance += 8302.01", "deposit", "Add income.", "Balance increases."), Line("balance -= 1729.09", "withdrawal", "Subtract rent.", "Balance decreases."), Line("print(balance)", "print", "Display the current balance.", "16511.19 prints.")),
        TopicMission(98, "Ledger Savings And Interest", "Apply savings and interest rules.", "12512.74", Line("balance = 13219.05", "balance", "Store balance after purchases.", "Savings comes next."), Line("balance -= 830.20", "savings", "Move ten percent of income to savings.", "Checking decreases."), Line("balance += 123.89", "interest", "Add one percent interest.", "Checking increases."), Line("balance = round(balance, 2)", "round()", "Round the money value before display.", "The final value becomes 12512.74."), Line("print(balance)", "print", "Display final balance.", "12512.74 prints.")),
        TopicMission(99, "Ledger Report", "Print a compact formatted report.", "Ending  12,512.74", Line("label = \"Ending\"", "string", "Store the report label.", "The row needs text."), Line("balance = 12512.74", "float", "Store the ending balance.", "The row needs money."), Line("print(f\"{label:<8}{balance:>9,.2f}\")", "report format", "Use alignment, commas, and two decimals.", "The report row is readable.")),
        TopicMission(100, "Budget Program", "Combine calculation and formatted output into a small program.", "Budget complete: 12,512.74", Line("balance = 9938.27 + 8302.01", "calculation", "Start with balance plus income.", "The account receives a deposit."), Line("balance = balance - 1729.09 - 3292.14", "withdrawals", "Subtract rent and purchases.", "Spending leaves the account."), Line("balance = balance - 830.20 + 123.89", "final updates", "Subtract savings and add interest.", "The final value is ready."), Line("balance = round(balance, 2)", "round()", "Round the final money value.", "The display value is stable."), Line("print(f\"Budget complete: {balance:,.2f}\")", "formatted output", "Display the final account result.", "The finished program reports the balance."))
    ]);

    private static Lesson TopicMission(int number, string topic, string goal, string output, params CodeLine[] lines)
        => Mission($"Mission {number:00}: {topic}", goal, lines, output);

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
