# Repository guidance

This is an exported EyeAuras PoE2 helper. Read [README.md](README.md) for its user-facing scope and attribution. The editable script and helper code live in `CheatCartridge/`; the solution and project describe the Windows/.NET and EyeAuras SDK build environment.

Do not hand-edit `CheatCartridge/generated/`: its [README](CheatCartridge/generated/README.md) records export provenance and says the next export regenerates it while import ignores it. An old exporter version is historical evidence, not a request to replace it with the current version. Preserve the [GameHelper attribution](CheatCartridge/GameHelper/README.md), native layouts, and game-version evidence.

Memory offsets and struct layouts in `CheatCartridge/GameHelper/GameOffsets/` depend on the target game build. Compilation does not prove runtime offsets or HP/MP/ES behavior; record the target client and observations when changing them. Do not update offsets or claim current-client compatibility merely because documentation is old.

EyeAuras supplies the runtime services; generated host placeholders are not a standalone functional smoke test. Preserve the aura paths and behavior-tree variables in `BotBrain.cs` when changing integration.

For code changes, use the checked-in project/solution and inspect its actual SDK/feed requirements. Report separately what compiled and what was verified in the EyeAuras/game environment. Documentation-only changes need factual, link, and diff checks; explicitly report runtime checks not performed.

## Independent architecture and review

Start each delegated architect, advisor, and reviewer with empty conversation context (`fork_turns="none"` with `spawn_agent`, or the host's equivalent). Provide only the concrete question, repository path and relevant revisions, scope, constraints, and raw evidence locations. Keep the brief focused on the actual topic; omit the author's reasoning, preferred verdict, and earlier agents' conclusions.

Each agent independently inspects the relevant code and contracts and reports its own evidence, conclusions, and uncertainty. The final reviewer must be separate from the implementer and design advisor, remain read-only, and inspect the final diff at the named revision. Disclose unavailable independent review rather than presenting self-review as independent.
