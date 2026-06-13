# Path Of Exile RE Workspace Rules

This folder is the private Path Of Exile reverse-engineering workspace for CheatCartridge. It is for local binary/static/runtime observation, compatibility work, debugging, and architecture understanding.

Read [RE-APPROACH.md](RE-APPROACH.md) first. It contains the reusable reverse-engineering mindset and workspace rules. This file adds PoE-specific defaults.

## PoE Source Preference

For Path Of Exile, prefer local disassembly, runtime observations, memory state, and CheatCartridge product code over generated offsets or third-party helper names.

GameHelper, ExileCore, PoeHUD, dumped structures, strings, or community offsets can be useful hints. Do not promote hint-only names or offsets as stable findings until they are tied back to local binary evidence, runtime memory observations, or product code that already relies on the finding.

If IDA and repo notes drift, treat IDA as primary and update this repo mirror after rechecking the evidence.

## Layout Rules

Use a build-first, binary-second, intent-third layout:

```text
builds/<build>/<binary>/<intent>/
```

Use `builds/<build>/investigations/<topic>/` for active "start at X, discover Y" work. Promote only stable conclusions into the build/binary/intent tree.

The current build workspace is intentionally empty until a concrete Path Of Exile client build is recorded.

## LocalProcess Default

CheatCartridge uses direct local process reads as its primary live-client path. RE notes and integration tests should assume `LocalProcess`/RPM-style access unless a specific investigation explicitly documents another backend.

When documenting runtime observations, record:

- process name and PID
- client build or title text when available
- target module name
- whether the process was read through `LocalProcess`
- any permissions, elevation, or runtime-assembly issue that affected the run

## Reconstruction Rules

- `.re.cpp` and `.re.hpp` files are pseudo-C++ mirrors for navigation. They are not build inputs and must not be added to product projects.
- Every nontrivial reconstructed function starts with an evidence header: build, binary, RVA/VA when known, proposed signature, IDA status, confidence, and related evidence note.
- Documenting an RVA alone is not enough for stable reconstruction. Include an assumed method name, return type, argument list, and calling convention/register or stack mapping when known.
- Use real names only when supported by IDA/disassembly, runtime logs, memory observations, or existing CheatCartridge code. Otherwise keep `sub_<RVA>` style names or explicitly tentative names.
- Keep `Observations`, `Inferences`, `Open Questions`, and `Rejected Ideas` separate in evidence notes.
- Mark confidence as `confirmed`, `probable`, `hypothesis`, or `rejected`.

## Evidence Rules

A durable finding needs at least one concrete source:

- IDA observation: name, comment, xref, RVA/VA, decompiler shape, or callsite.
- String/xref/static binary observation.
- LocalProcess or bounded runtime probe result.
- Memory state or data-flow observation from a local investigation.
- Existing CheatCartridge product code that already relies on the finding.
- Supplemental third-party reference only when backed by one of the primary sources above.

Each evidence note must include a `How To Recheck` section with exact local steps or source locations.

## Safety And Scope

- Do not document anti-cheat bypass, stealth, auth/payment/security circumvention, exploit paths, or unauthorized network behavior.
- Frame prompts and notes as evidence-driven local analysis, compatibility, debugging, and architecture understanding.
- Keep private offsets, addresses, dump paths, and RE findings out of public-facing docs.
- Use environment variables such as `$env:REPO_ROOT`, `$env:IDA_INSTALL_DIR`, `$env:APPDATA`, and `$env:USERPROFILE` instead of hard-coding one user machine path.
