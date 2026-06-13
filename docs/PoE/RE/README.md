# Path Of Exile Reverse Engineering Workspace

Internal reverse-engineering notes for Path Of Exile live here. These files may include local dump paths, addresses, offsets, tool setup, and hypotheses, so keep them out of public-facing CheatCartridge docs.

IDA is the authoritative working notebook. This folder mirrors stable findings into a Markdown-first, code-shaped workspace that is easier for agents and humans to navigate, diff, and hand off.

## Current Layout

- [Workspace rules](AGENTS.md)
- [Reusable RE approach](RE-APPROACH.md)
- [Runtime field investigation guide](RUNTIME-FIELD-INVESTIGATION-GUIDE.md)
- [Offset gap ledger](poe-gaps.md)
- [Templates](templates/)
- [Build workspaces](builds/)

Stable reconstructed knowledge lives under `builds/<build>/<binary>/<intent>/`. Temporary "start at X, discover Y" work lives under `builds/<build>/investigations/<topic>/` until it is promoted into the stable tree.

## Current Status

This workspace has an active `sha256-c5da3833` PathOfExileSteam build slice. Treat build-specific conclusions as scoped to that binary until another client build confirms the same shape or an equivalent keypoint.

CheatCartridge uses `LocalProcess` for live-client memory reads. Treat that as the default process access path in PoE integration tests and runtime investigations unless a note explicitly states otherwise.
