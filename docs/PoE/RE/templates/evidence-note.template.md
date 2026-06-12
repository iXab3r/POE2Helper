# <Finding Name> Evidence

Build: `<build>`
Binary: `<binary>`
Related reconstruction: `<file.re.cpp>`
Confidence: `<confirmed | probable | hypothesis | rejected>`

## Proposed Signatures

| Anchor | Proposed signature | Confidence | Argument evidence |
| --- | --- | --- | --- |
| `<RVA/VA or runtime anchor>` | `<ReturnType ProposedName(ArgType arg, ...)>` | `<confirmed | probable | hypothesis | rejected>` | `<rcx/rdx/r8/r9/stack/vtable/register observations>` |

## Observations

- `<IDA/disassembly/runtime/memory/product fact>`

## Inferences

- `<what the observations likely mean>`

## How To Recheck

- IDA: `<function/comment/xref/RVA>`
- Runtime: `<LocalProcess sample/Frida script/tool/log>`
- Product: `<repo path/symbol>`
- Supplemental refs: `<third-party offset/string/product note, if any>`

## Open Questions

- `<question>`

## Rejected Ideas

- `<idea and why it was rejected>`
