# CheatCartridge.Tests Project Notes

This project is the test home for CheatCartridge, the Path Of Exile helper.

## Test Scope

Use NUnit, Shouldly, and Moq for focused tests.

Place live-client tests under `Integration/`, mark them with `[Category("integration")]`, and keep them explicit unless the fixture is safe to run without a real Path Of Exile client.

## Live Client Backend

CheatCartridge live-client integration tests should use `EyeAuras.Memory.LocalProcess` as the default process access path.

Do not copy Banka.TL's `CAgentProcess` default into this project. `CAgentProcess` is a TL-specific integration default; PoE tests should start from the same direct-memory backend the product uses.

## Test Shape

Public test classes and test methods should have XML documentation comments with `WHAT:` and `HOW:`.

Structure test bodies with explicit `// Given`, `// When`, and `// Then` sections.

Run regular tests separately from `integration` tests. Live-client tests may skip when no client is running, the process cannot be opened, or the LocalProcess runtime is unavailable.
