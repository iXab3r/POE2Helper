# Runtime Field Investigation Guide

This guide is for investigations where the goal is to find where a live gameplay, UI, entity, or player fact is stored or updated in Path Of Exile.

Use [RE-APPROACH.md](RE-APPROACH.md) for the evidence mindset. Use this guide for the tactical loop: create a signal, follow that signal through runtime/static evidence, then verify candidate storage with bounded memory observation.

## Core Idea

Do not start by scanning memory for values.

Start by building a reliable causal chain:

```text
controlled stimulus
-> confirmed game/UI response
-> candidate owner objects or update routines
-> small candidate memory samples
-> repeated confirmation under churn
```

Wide scans are allowed only after the causal chain fails and the failure has been recorded. A scan can find a value, but it usually cannot explain ownership, lifetime, update timing, or whether the value is authoritative.

## Investigation Contract

Before touching tools, write down the contract for the run:

- the fact being hunted
- the exact stimulus that should change it
- the expected visible/runtime response
- the reset window before the stimulus can be repeated
- what would confirm the hypothesis
- what would reject the hypothesis

If the stimulus is not reliable yet, finding storage is not the current task. The current task is making the stimulus reliable.

## Standard Workflow

### 1. Establish Live Context

Record the live context at the start of each run:

- client build/title and PID
- target process/module name
- access path, normally `LocalProcess`
- in-world/loading state
- current character, area, UI, target, item, skill, flask, or entity state
- expected event window, such as `flask duration`, `skill cooldown`, `area transition`, or `life value change`

After a restart, area transition, character swap, or UI rebuild, treat child pointers as stale. Re-resolve roots from stable selectors or ownership chains.

### 2. Make A Reliable Stimulus

A useful stimulus is:

- repeatable
- visible or otherwise externally confirmable
- narrow enough to produce a small number of changes
- triggered through one known tool path
- free of extra side effects when possible

Examples:

- take predictable damage or recover life
- use a flask with known duration
- change mana reservation or spend mana with one known skill
- open or close one UI panel
- move between two known positions
- enter a known area and wait for loading to settle

Get operator confirmation once when the trigger is being established. After that, repeat through the same confirmed path.

### 3. Backtrack To Candidate Owners

Use static and runtime context to turn the visible event into concrete candidate owners:

- known CheatCartridge reader and offset chain
- parent object that already owns adjacent facts
- manager or state object that reconciles the affected object
- UI widget or panel object if the value is display-only
- update routine, virtual call, or string/xref anchor discovered in IDA

Candidates must be concrete. Good candidates have an address, owner, range, and reason.

Poor candidate:

```text
some heap area near values that changed
```

Good candidate shape:

```text
<OwnerType> resolved from <stable root/path>, block <offset range>,
because <stimulus> changes <visible fact> and this owner already controls <adjacent fact>.
```

### 4. Sample Small Blocks

Sample bounded blocks around candidate owners, not the whole process.

For each block, define:

- owner name
- how the owner pointer was resolved
- base address and offset range
- byte size
- sample cadence and duration
- expected value shape

Prefer bounded block reads over many field-by-field reads. The memory backend cost is usually better, and the sample preserves local structure.

Common value shapes:

- monotonic countdown
- one-shot flag transition
- pointer swap
- array count change
- bitmask transition
- text backing pointer change
- position/vector update
- current/max resource pair

### 5. Reject Candidates Explicitly

Negative evidence is part of the result. Keep it.

Reject a candidate when:

- it does not change during a confirmed event
- it changes in the wrong phase
- it has the wrong units or range
- it updates before/after the real event window
- it represents display state when the target is model state
- it only works with stale pointers

Do not silently drop rejected paths. Future investigations need to know why attractive names or nearby fields were not used.

### 6. Repeat Under Churn

Before productizing a finding, repeat under at least one churn condition:

- client restart
- area transition
- character select/re-entry
- UI close/reopen
- same action after reset/cooldown
- different item, skill, flask, target, or entity instance

Durable product readers should resolve through live root ownership, not depend on old child addresses.

### 7. Productize Conservatively

When the finding is repeatable:

- put raw facts on the native/live object that owns the field
- keep formatting in UI/tooltips or explicit reports
- avoid compatibility aliases
- avoid native pointers/addresses in script-facing APIs
- add focused live integration coverage when the public surface changes
- update the private RE note with exact recheck steps

If the value is only a display cache, say so. Do not rename it into an authoritative model fact.

## Probe Design Rules

Reusable probes should:

- resolve roots at arm time
- prefer semantic roots or selector-derived roots over hard-coded child pointers
- fail loudly when roots are unavailable
- expose bounded arm/status/stop tools
- use explicit duration, sample count, and event count caps
- include cleanup/disarm behavior
- emit compact summaries with candidate scores and rejected blocks
- document what observation confirms or rejects the hypothesis

Scratch probes are fine during exploration. Promote only probes that are reusable, bounded, and linked from an evidence note.

## Evidence Note Template

A runtime-field finding should include:

- build and binary
- live context
- stimulus
- candidate owner list
- memory sample result
- rejected candidates
- confidence level
- product impact, if any
- exact recheck steps

Keep these sections separate:

- `Observations`: what tools showed.
- `Inferences`: what those observations likely mean.
- `Rejected Ideas`: attractive wrong paths.
- `Open Questions`: what still matters.

## Common Failure Modes

- **Value-first search:** finds many changing numbers but no owner.
- **Name-first search:** trusts a field/widget/function because the name looks right.
- **Third-party-only promotion:** treats community offsets or helper names as truth without runtime/static backing.
- **Stale pointer reuse:** keeps sampling a previous child object after UI/world churn.
- **Unverified stimulus:** samples memory around an action that may not have happened.
- **Over-hooking:** destabilizes the client by hooking broad or instruction-middle routes.
- **No rejection log:** forces the next analyst to rediscover dead ends.
- **Premature productization:** exposes a display cache as if it were authoritative model state.
