# Path Of Exile RE Frida Scripts

This folder is the curated Frida investigation script library for CheatCartridge and Path Of Exile.

Use scratch tools for live experiments first. Promote scripts here only when they become reusable diagnostics or reverse-engineering helpers.

The investigation catalog for these scripts lives one level up:

- [../README.md](../README.md)
- [../catalog.md](../catalog.md)

CheatCartridge normally uses `LocalProcess` for memory reads. Frida scripts should be treated as targeted investigation helpers, not as the default process access path.
