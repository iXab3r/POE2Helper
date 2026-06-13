# PoE FF Layouts

This folder is the bridge between PoE runtime offset recovery and the shared
FrameFormat/ReClass layout model.

## Model

- `resolved/<build>/<module>/` contains live/generated FF proto documents
  for a specific client build.
- `resolved/current.json` points at the latest generated layout and is the
  handoff file for the C# generator layer.
- Generator tests recover offsets from patterns, serialize the resolved layout
  to FF, then update annotated runtime structs from that document.

## Metadata

The documents follow the existing FF comment dialect:

- `@reclass offset=... length=...` marks byte placement.
- `@fflayout shape=...` marks primitive/class/pointer/array shape.

- Field declarations may use a normal trailing proto comment after `;` for
  human RE notes, for example `int32 current = 10; // Current vital value.`
- Header-level `@ffmeta` stores generic key/value metadata such as layout
  identity, source module, source hash, and capture time.

Resolver details such as patterns, keypoints, and evidence paths live in RE
notes and generator tests. The resolved FF file is the result of that work, not
the transcript of how each value was found.

The explicit integration test
`LocalProcessClientIntegrationTests.ShouldExportResolvedRuntimeLayoutAsFfProto`
is the versioned FF generator. It hashes the live executable, writes the FF
proto under `resolved/sha256-*/<module>/poe-game-model.sha256-*.ff.proto`, and
updates `resolved/current.json`.

The debug MCP tool `poe_export_resolved_ff_layout` can still emit the same
document as an MCP result for quick inspection.
