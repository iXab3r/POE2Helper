# Reverse Engineering Approach

This file is intentionally project-neutral. It describes the reusable mindset for large binary analysis work where IDA is the primary notebook and the repo stores a durable, searchable mirror of stable findings.

## Mindset

Reverse engineering notes are not source code and not proof by themselves. Treat them as a map of observations, inferences, and open questions.

- Prefer disassembly, decompiler output, xrefs, runtime traces, and memory observations over convenient generated names.
- Keep uncertainty visible. Use `confirmed`, `probable`, `hypothesis`, and `rejected` instead of smoothing over gaps.
- Write for the next analyst. Every stable note should say how to recheck it.
- Avoid giant narrative reports as the only artifact. Promote stable knowledge into small slices that are easy to search, diff, and hand off.
- Do not let attractive names become facts. Names are labels until backed by behavior, callsites, data flow, or runtime evidence.

## Source Of Truth

IDA comments, names, xrefs, decompiler shape, and database state are the primary working notebook. Repo-side files mirror stable conclusions so agents can navigate and reason without reopening the full IDA context every time.

If IDA and repo notes drift, recheck IDA and update the repo mirror.

Generated SDKs, leaked headers, symbol dumps, string tables, third-party offsets, and product code can be useful hints, but they are secondary. Do not promote a finding based only on a convenient generated declaration when the binary has not been checked.

## Workspace Shape

Use a build-first, binary-second, intent-third layout when facts are address- or version-sensitive:

```text
builds/<build>/<binary>/<intent>/
```

Use `investigations/<topic>/` for active "start at X, discover Y" work. Promote only stable conclusions into the build/binary/intent tree.

When analysis jumps from anchor `X` to another substantial anchor `Y`, create or link a separate slice. This keeps parallel work possible and prevents one file from becoming the whole investigation.

## Runtime Probes

Treat reusable Frida scripts and other runtime probes as framework artifacts, not disposable chat leftovers.

- Keep a catalog that explains each promoted script's purpose, inputs, outputs, risks, and related slice.
- Prefer scratch scripts for one-off experiments; promote only reusable diagnostics.
- Keep probes bounded and gated: explicit duration, max event counts, filters, and cleanup/disarm behavior.
- Record what observation would confirm or reject the hypothesis before treating a live run as evidence.
- Do not promote a runtime observation into a stable reconstruction until it is tied back to IDA/disassembly, memory state, or another concrete source where needed.

## Reconstruction Files

Use pseudo-code files such as `.re.cpp` or `.re.hpp` as navigation mirrors, not as build inputs.

Every nontrivial reconstructed function should carry:

- Build and binary.
- RVA/VA or an explicit reason it is runtime-only.
- Proposed signature: assumed method name, return type, argument list, and calling convention/register or stack mapping when known.
- IDA status: named, commented, `sub_<RVA>`, or unknown.
- Confidence level.
- Link to the evidence note.

Pseudo-code should preserve important control flow, data shape, and semantic landmarks. It should not copy raw decompiler noise just to look complete.

## Evidence Notes

Keep evidence notes structured:

- `Proposed Signatures`: tentative function names, return types, arguments, and confidence for each important RVA.
- `Observations`: what IDA, disassembly, runtime probes, memory, or static artifacts show.
- `Inferences`: what those observations likely mean.
- `How To Recheck`: exact IDA locations, scripts, commands, probes, or file references.
- `Open Questions`: unknowns that matter.
- `Rejected Ideas`: plausible wrong paths and why they were rejected.

A stable finding needs at least one concrete primary source: IDA/disassembly, xrefs, runtime trace, memory observation, binary strings tied to code, or a repeatable local probe.

Secondary references can support a finding, but should not be the only reason the finding exists.

## Collaboration Safety

Frame work as local observation, compatibility, debugging, or architecture understanding. Keep prompts and notes evidence-driven.

Do not document bypass, stealth, auth/payment/security circumvention, exploit paths, or unauthorized network behavior. If sensitive areas appear while navigating, record the boundary and move analysis back to the intended subsystem.
