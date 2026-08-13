# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 173 passed, 0 skipped, 0 failed. Every milestone,
including scope navigation (`.: `/`..: `), is done. See
`docs/architecture.md` for how the engine is built and `docs/specs.md`/
`docs/implementations/dotnet.md` for current behavior — no open
ambiguities remain.

## Remaining milestones

1. Rehumanize `docs/specs.md` and the other published docs (`README.md`,
   `docs/architecture.md`, `docs/implementations/dotnet.md`,
   `docs/README.md`) — a readability/tone pass, not a correctness one,
   after this session's many incremental edits.
