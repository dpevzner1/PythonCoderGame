# Python Coder Game Curriculum

This document reflects the active curriculum used by the game engine. The runtime source of truth is `Curriculum.BeginnerLessons` in `Curriculum.cs`; mission select, deploy, boss generation, telemetry, and compile replay all read from that single list.

The curriculum uses 100 topic-based learning missions plus 20 recap boss battles. Each boss is generated from the five immediately preceding learning missions, so there is no separate boss curriculum competing with the main Python path.

## Teaching Loop

1. Brief the student on one Python idea.
2. Show a concise line explanation while the target code rises.
3. The student types the current line in the input rail.
4. Correct code stacks into the compiler stack with indentation preserved.
5. Mistyped characters produce immediate red arcade penalties while still allowing correction.
6. At mission completion, the compile replay shows each line scanning and the runtime trace explains memory, output, branches, loops, and returns.
7. Every five learning missions, a boss battle corrupts one snippet from each of those five missions.
8. Continue to the next incomplete mission, repeat, save/edit, or exit.

## Curriculum Sections

- Section 01: Output And Syntax
  Code order, `print()`, comments, strings, and exact syntax symbols.

- Section 02: Values And Variables
  Text variables, integers, floats, booleans, and `None`.

- Section 03: Names And Text
  Readable names, valid naming patterns, string joining, `str()` conversion, and f-strings.

- Section 04: Arithmetic
  Addition, subtraction, multiplication, division, reassignment, and changing stored values.

- Section 05: Input And Casting
  `input()`, `int()`, `float()`, choosing the right cast, and `type()`.

- Section 06: Formatted Output
  Decimal precision, comma formatting, right alignment, left alignment, and centered output.

- Section 07: Math Tracing
  Order of operations, `math.sqrt()`, `abs()`, `+=`, and step-by-step variable tracing.

- Section 08: List Basics
  Lists, indexes, `append()`, `len()`, and small inventory data.

- Section 09: Dictionary Records
  Dictionaries, key lookup, dictionary updates, list-of-dictionary records, and nested lookup.

- Section 10: Basket Math
  Price records, line totals, `sum()`, generator expressions, total checks, and basket messages.

- Section 11: Conditionals
  Comparisons, `if`, indentation, `if/else`, and equality checks.

- Section 12: Boolean Logic
  `elif`, `and`, `or`, `not`, and efficient stored decision values.

- Section 13: Loop Basics
  `range()`, loop variables, list loops, accumulators, and short `while` loops.

- Section 14: Looped Data
  Traversing lists, counting matches, looped totals, short nested loops, and stop values.

- Section 15: Function Basics
  Function definitions, calls, parameters, return values, and status helpers.

- Section 16: Function Patterns
  Total functions, formatted-return functions, validation functions, default values, and mini calculators.

- Section 17: Ledger Calculations
  Starting balances, deposits, rent, purchases, savings rules, and interest.

- Section 18: Ledger Reports
  Table headers, money rows, and reusable ledger row formatting.

- Section 19: Modules And Errors
  Imports, predictable random choice, file path variables, `try/except`, and debug messages.

- Section 20: Budget Capstone
  Input values, ledger calculations, savings, interest, formatted reporting, and final program assembly.

## Boss Battle Rules

Boss battles are recap rounds, not new lessons. Each battle pulls one repair from each of the five previous learning missions.

Boss flow:

1. The active mission header shows `A VIRUS HAS CORRUPTED THE CODE`.
2. The corrupted snippet is orange and the correct answer is hidden.
3. The student types the repaired line in the input rail.
4. Incorrect submissions simulate a compile attempt and briefly highlight a small error region.
5. Each repair lowers the virus health bar.
6. The 60-second virus timer appears only in boss battles.
7. If the timer reaches zero, the boss restarts.
8. A completed boss enters the same telemetry stream as a normal mission.

Boss repairs should stay simple: missing quotes, wrong capitalization, missing colon, wrong comparison operator, incomplete expression, broken print call, wrong list/dictionary brackets, or a loop keyword mistake. Bosses should never introduce a concept that was not present in the five immediately previous learning missions.

## Runtime Trace Standards

The compile replay should explain what Python does in student-readable language:

- assignment: a value enters memory under a name,
- output: `print()` sends data to the output console,
- condition: Python checks whether an expression is true or false,
- branch: a path runs or skips based on the result,
- loop: each short pass updates the loop variable and runs the indented block,
- function: Python stores the function, calls it, receives arguments, and optionally returns a value,
- formatting: Python converts a value into a readable display shape.

For money-style examples, missions should round or format before display so beginner learners see stable, intentional output.

## Future Improvement Goals

These are upcoming improvements that should be considered after the 100-mission beginner path is stable:

1. Add an intermediate collection arc for tuples and sets.
2. Add list comprehensions only after loops and accumulators feel solid.
3. Add file reading/writing as a later practical project sequence.
4. Add lightweight classes after functions and dictionaries are comfortable.
5. Add a final arcade-style project arc using small functions and state.
6. Keep every loop visualization short; no beginner mission should create long waiting patterns.
7. Preserve the five-learning-missions-plus-boss rhythm.
8. Update Mission Select summaries, README, telemetry labels, and installer output whenever the active curriculum changes.

## Single-Curriculum Guarantee

The active game engine has one Python curriculum:

- `Curriculum.cs` declares the 100 learning missions.
- `AddBosses(...)` inserts generated boss battles after each five-mission section.
- `GameForm.cs` mission select and deploy use `Curriculum.BeginnerLessons`.
- Telemetry records the active lesson from the same list.

The files in `NewSource` remain external planning references. They do not replace or execute inside the game unless their ideas are intentionally implemented in `Curriculum.cs`.
