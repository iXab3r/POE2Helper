# Path Of Exile Frida Investigation Script Catalog

Curated script files are physically stored in [scripts/](scripts/). This catalog is the RE framework index for how those scripts relate to investigations.

## Stable/Reusable Diagnostics

No scripts have been promoted yet.

| Script | Primary tools | Status | RE use |
| --- | --- | --- | --- |
| _none_ | _none_ | _empty scaffold_ | Add entries only after a Path Of Exile probe is reusable, bounded, and linked to evidence. |

## Risky/Manual-Only Diagnostics

No risky/manual-only scripts have been promoted yet.

## Current Slice Links

No build slices have been recorded yet.

## Promotion Checklist

- The script is registered in `docs/PoE/RE/frida/scripts/index.json`.
- The script has bounded capture settings and explicit cleanup/disarm behavior.
- The relevant evidence note explains how to arm it and what observation matters.
- Any live result is marked with target build and confidence.
- Findings are not promoted from script output alone when IDA/disassembly or memory-state confirmation is needed.
