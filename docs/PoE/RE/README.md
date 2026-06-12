# Path Of Exile Reverse Engineering Workspace

Internal reverse-engineering notes for Path Of Exile live here. These files may include local dump paths, addresses, offsets, tool setup, and hypotheses, so keep them out of public-facing CheatCartridge docs.

IDA is the authoritative working notebook. This folder mirrors stable findings into a Markdown-first, code-shaped workspace that is easier for agents and humans to navigate, diff, and hand off.

## Current Layout

- [Workspace rules](AGENTS.md)
- [Reusable RE approach](RE-APPROACH.md)
- [Runtime field investigation guide](RUNTIME-FIELD-INVESTIGATION-GUIDE.md)
- [Frida investigation framework](frida/)
- [Templates](templates/)
- [Build workspaces](builds/)

Stable reconstructed knowledge lives under `builds/<build>/<binary>/<intent>/`. Temporary "start at X, discover Y" work lives under `builds/<build>/investigations/<topic>/` until it is promoted into the stable tree.

## Current Status

This workspace is a scaffold. Add the first build folder only after recording the concrete Path Of Exile client build, target binary, and evidence source.

CheatCartridge uses `LocalProcess` for live-client memory reads. Treat that as the default process access path in PoE integration tests and runtime investigations unless a note explicitly states otherwise.
