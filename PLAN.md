# Implementation plan

Living roadmap for building the guillemets engine against the `/specs` fixture
corpus. This file shrinks as milestones complete — it's *what's left*, not a
history of what's done. Agent/contributor working file, not published
documentation — see `README.md`/`docs/` for that. For *how* it's built, see
`docs/architecture.md`; for *how* to work (TDD discipline, code style), see
`CLAUDE.md`.

## Status

`dotnet test` is green: 228 passed, 0 skipped, 0 failed.
Language/implementation, P1, P2, and P3 (release readiness) milestones are
all done. Nothing left blocks release — only the Explicitly deferred items
below remain, none of which are release blockers.

## Next

Check [Maintenance Issues][] for items to do. If none check [Idea Issues][] and
ask which one to implement.

[Maintenance Issues]: https://github.com/mouseless/guillemets/issues?q=is%3Aissue%20state%3Aopen%20milestone%3Aideas
[Idea Issues]: https://github.com/mouseless/guillemets/issues?q=is%3Aissue%20state%3Aopen%20milestone%3Aidea
