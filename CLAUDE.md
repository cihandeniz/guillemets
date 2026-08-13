# CLAUDE.md

This file stays identical across every .NET repo using this convention —
it only ever points elsewhere, the same way a build file refers out to
other files instead of holding everything itself. Read, in order:

1. `.claude/generic.md` — general .NET/C# conventions and working habits.
   Reusable as-is across any .NET repo.
2. `.claude/specific.md` — this project's own conventions, structure, and
   status pointers.

Both are read on every cold start, the same as this file itself.
