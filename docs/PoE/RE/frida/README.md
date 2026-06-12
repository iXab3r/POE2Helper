# Frida Investigation Framework

Frida scripts used for repeatable Path Of Exile investigations are part of the RE workspace. Their executable files live under [scripts/](scripts/).

## Physical Location

The curated script library for this repository lives at:

```text
docs/PoE/RE/frida/scripts/index.json
```

## RE Ownership

This RE framework owns the investigation meaning of those scripts:

- Catalog what each script observes or explores.
- Link scripts to build slices and evidence notes.
- Record whether the script is scratch, active investigation support, stable diagnostic, or risky/manual-only.
- Keep script findings tied to IDA/disassembly or runtime evidence before promoting them into stable `.re.cpp` mirrors.

## Lifecycle

1. Start with scratch tools or one-off Frida scripts for live experiments.
2. Promote only reusable diagnostics into `docs/PoE/RE/frida/scripts`.
3. Register promoted scripts in `docs/PoE/RE/frida/scripts/index.json`.
4. Document the promoted script in [catalog.md](catalog.md).
5. Reference the script from the relevant evidence note or investigation work item.
6. If a script becomes product-critical, move the underlying stable behavior into product code and keep the script as a diagnostic.

## Evidence Rules

Frida output is runtime evidence, not a conclusion by itself. A script-backed finding should record:

- Script path and tool id.
- Required known-state inputs.
- Exact arm/capture settings.
- Expected event names or fields.
- What observation would confirm or reject the hypothesis.
- Whether the run was live-validated for the target build.

Prefer bounded, gated hooks over broad always-on tracing.
