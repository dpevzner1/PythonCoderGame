# Python Coder Game Curriculum

This document reflects the active curriculum used by the game engine. The runtime source of truth is `Curriculum.BeginnerLessons` in `Curriculum.cs`; mission select, deploy, boss generation, telemetry, and compile replay all read from that single list.

The curriculum uses 50 learning missions plus 10 recap boss battles. Each boss is generated from the five immediately preceding learning missions, so there is no separate boss curriculum competing with the main Python path.

## Teaching Loop

1. Brief the student on one Python idea.
2. Show a concise line explanation while the target code rises.
3. Let the student type the exact Python line.
4. Stack correct code into the compiler scan viewer.
5. Run a slow compile and runtime trace animation.
6. Explain what each line does to memory, control flow, or console output.
7. Save telemetry for accuracy, attempts, time, help usage, completion, and retry behavior.
8. Continue to the next incomplete mission, or let the student repeat or save/edit.

## Curriculum Sections

### Section 1: First Output And Syntax

Students learn how Python reads code, prints text, ignores comments, recognizes strings, and depends on exact syntax.

- Mission 01: Python reads top to bottom.
- Mission 02: `print()` sends text to the console.
- Mission 03: comments explain code without running.
- Mission 04: strings need quotes.
- Mission 05: parentheses and quotes form valid syntax.
- Boss 01: corrupted code recap for missions 01-05.

### Section 2: First Variables And Values

Students learn that variables are named memory slots and that Python values have basic types.

- Mission 06: store a text value.
- Mission 07: store a whole number.
- Mission 08: store a decimal number.
- Mission 09: store a boolean.
- Mission 10: use `None` as an empty placeholder.
- Boss 02: corrupted code recap for missions 06-10.

### Section 3: Names, Text, And Math

Students make code more readable, combine text, format output, calculate values, and update memory.

- Mission 11: use readable variable names.
- Mission 12: join text.
- Mission 13: use f-strings.
- Mission 14: use math expressions.
- Mission 15: reassign and update a value.
- Boss 03: corrupted code recap for missions 11-15.

### Section 4: Types And Ordered Collections

Students inspect types, create lists, read by index, append values, and combine list skills in a tiny inventory.

- Mission 16: check type awareness.
- Mission 17: create a list.
- Mission 18: read list indexes.
- Mission 19: append to a list.
- Mission 20: build a mini inventory.
- Boss 04: corrupted code recap for missions 16-20.

### Section 5: Dictionaries And Branches

Students learn key/value data, dictionary lookup, comparisons, and the first decision paths.

- Mission 21: create a dictionary.
- Mission 22: look up dictionary values.
- Mission 23: make a comparison.
- Mission 24: write an `if` statement.
- Mission 25: write an `if/else` branch.
- Boss 05: corrupted code recap for missions 21-25.

### Section 6: Comparison Logic

Students expand conditional thinking with equality, multiple branches, and boolean operators.

- Mission 26: check equality.
- Mission 27: use `if/elif/else`.
- Mission 28: combine conditions with `and`.
- Mission 29: allow alternatives with `or`.
- Mission 30: reverse a condition with `not`.
- Boss 06: corrupted code recap for missions 26-30.

### Section 7: Short Loops

Students learn repetition with loops while keeping every visual conveyor pass short. No loop condition exceeds four visible passes.

- Mission 31: loop with `range(3)`.
- Mission 32: use the loop variable.
- Mission 33: loop over a list.
- Mission 34: build an accumulator.
- Mission 35: use a short `while` loop.
- Boss 07: corrupted code recap for missions 31-35.

### Section 8: Functions

Students learn reusable code blocks, calls, parameters, return values, and small status helpers.

- Mission 36: define a function.
- Mission 37: call a function.
- Mission 38: pass a parameter.
- Mission 39: return a value.
- Mission 40: write a status function.
- Boss 08: corrupted code recap for missions 36-40.

### Section 9: Input, Imports, And Stored Data

Students see how programs receive values, convert types, import helpers, and represent file or settings data.

- Mission 41: ask for input.
- Mission 42: convert input with `int()`.
- Mission 43: import a module.
- Mission 44: store a file path value.
- Mission 45: write a settings dictionary.
- Boss 09: corrupted code recap for missions 41-45.

### Section 10: Debugging And Mini Programs

Students practice reading errors, guarding risky code, checking values, designing focused functions, and assembling a small program.

- Mission 46: read an error message.
- Mission 47: use `try/except`.
- Mission 48: make a simple check.
- Mission 49: design a small function.
- Mission 50: complete a final mini program.
- Boss 10: corrupted code recap for missions 46-50.

## Boss Battle Rules

Boss battles are recap rounds, not new lessons. Each battle pulls one repair from each of the five previous learning missions.

Boss flow:

```text
A VIRUS HAS CORRUPTED THE CODE
-> show one orange corrupted snippet
-> attempt compile
-> highlight the error area for a short hint
-> student types the repaired line
-> virus health drops
-> repeat until all five recap repairs are complete
-> run the normal compile/data-flow replay
```

The timer is shown only during boss battles. Boss completion contributes to the standard telemetry model through existing mission attempts, line events, accuracy, duration, completion, retries, and outcome fields.

## Runtime Trace Standards

The compile replay should explain what Python does in student-readable language:

- assignment: value enters memory under a variable name
- print/output: value is sent to the console
- condition: expression is checked as `True` or `False`
- loop: each short pass updates the loop variable and runs the indented block
- function: Python stores the function, calls it, receives arguments, and optionally returns a value
- exception handling: Python tries risky code, then moves to `except` if an error occurs

All output belongs in the runtime trace. The compiler scan pane is only for source code scanning and line progress.

## Day 2 Curriculum Improvement Goals

These are upcoming curriculum improvements. They are intentionally documented as a future Day 2 backlog and do not replace the current active curriculum until implemented in `Curriculum.cs`.

The guiding principle is simple-before-complex: the learner should meet the smallest mental model first, then layer syntax and abstraction only after the earlier concept is visible in the compile/data-flow replay.

### Day 2 Improvement List

1. Reassess the placement of `type()`.
   - Current location: Mission 16.
   - Reason to improve: `print(type(count))` produces `<class 'int'>`, which is accurate but visually strange for a zero beginner.
   - Proposed direction: keep `type()` as a debugging/support concept, or move it slightly later after students have seen several value types.

2. Consider moving comparisons before dictionaries.
   - Current order: dictionaries and lookup appear before first comparison.
   - Reason to improve: `lives > 0` is conceptually simpler than `{ "name": "Ada" }` and key lookup.
   - Proposed direction: teach comparisons immediately after variables/lists/math, then use them to prepare for `if`.

3. Consider moving dictionaries after basic conditionals.
   - Current location: Missions 21-22.
   - Reason to improve: dictionaries require multiple syntax ideas at once: braces, keys, colons, strings, brackets, and lookup.
   - Proposed direction: teach lists first, then comparisons and `if`, then introduce dictionaries as labeled data once branching is familiar.

4. Add an explicit indentation mission before the first `if`.
   - Reason to improve: indentation is one of Python's most important rules and appears in `if`, loops, functions, and `try/except`.
   - Proposed mission goal: show that indented code belongs to the line above it.
   - Example concept: `if is_ready:` followed by an indented `print()` line.

5. Add `+=` only after expanded reassignment is understood.
   - Current curriculum teaches `score = score + 5` and `total = total + number`, which is the right first step.
   - Proposed direction: after accumulator practice, introduce `total += number` as shorthand.
   - Include a beginner warning: `n += 1` updates `n`; `n =+ 1` assigns positive `1` and is usually a typo.

6. Keep tuples as an intermediate follow-up, not an immediate beginner requirement.
   - Reason to defer: lists already teach ordered collections with practical beginner value.
   - Proposed direction: introduce tuples later as fixed ordered values after lists, dictionaries, functions, and returns are stable.

7. Preserve boss recap structure after any reorder.
   - Every boss should still cover the five immediately previous learning missions.
   - If missions move, generated boss repairs should continue to recap only the local section.

8. Keep loop visualizations short.
   - Preserve the current rule: no loop condition should produce more than four visible passes.
   - This keeps compile/data-flow replay educational instead of slow or repetitive.

### Proposed Day 2 Concept Order

If the curriculum is revised later, the recommended order is:

1. Output, comments, strings, and syntax.
2. Variables and basic value types.
3. Naming, reassignment, math, and text formatting.
4. Lists, indexes, and append.
5. Comparisons.
6. `if`, indentation, `if/else`, and `elif`.
7. Boolean logic with `and`, `or`, and `not`.
8. Dictionaries and lookup.
9. Loops over `range()` and lists.
10. Accumulators, `+=`, and short `while` loops.
11. Functions, parameters, and return values.
12. Input and conversion.
13. Imports and simple stored data.
14. Debugging, errors, `try/except`, and checks.
15. Final integrated mini programs.

### Day 2 Delivery Notes

- Each new or moved concept should preserve the one-concept-per-mission teaching loop.
- Compile replay should show memory, branch checks, loop passes, and console output in student-readable language.
- Mission Select section summaries should be updated at the same time as curriculum changes.
- README and this curriculum document should remain aligned with `Curriculum.cs`.
- Telemetry concept labels should remain meaningful after any mission reorder so instructor dashboards continue to make sense.

## Single-Curriculum Guarantee

The active game engine has one Python curriculum:

- `Curriculum.cs` declares the 50 learning missions.
- `AddBosses(...)` inserts generated boss battles after each five-mission section.
- `GameForm.cs` mission select and deploy use `Curriculum.BeginnerLessons`.
- Telemetry records the active lesson from the same list.

Any future curriculum changes should update `Curriculum.cs` first, then this document and `README.md` so the game, instructor documentation, and export interpretation stay aligned.
