# Implementation plan

> [!NOTE]
>
> ## Migration Notice
>
> Plan is moved to github and this file is now obsolete. Check current git
> branch using, find the matching PR in [pulls][] and get its description. Bring
> that task list to below if not brought already, and fix tasks one by one.
> (Follow issue links when a task contains them).

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

[pulls]: https://github.com/mouseless/guillemets/pulls
