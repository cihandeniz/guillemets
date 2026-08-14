# Parked decisions

Working notes for design forks hit mid-fix, parked until the author decides.
Not published docs, not PLAN.md (which only tracks *what's left*, not *why*).
Delete each entry once its decision is made and folded back into the relevant
PLAN.md item / commit.

## `»»` with no trailing newline never closes (PLAN.md P1 item)

**The bug:** `Symbols.cs` only registers `CloseBlock` as `»»` immediately
followed by `\n`. A block whose final `»»` is the last thing in the file
(no trailing newline) never tokenizes as `CloseBlock` — falls back to two
stray `Close` tokens, producing a wrong "Unclosed «" error at the wrong
position.

**The fork:** what should happen to a `»»` run that doesn't cleanly end its
line (not followed by `\n` or EOF)?

- **Option A — Surgical.** `Symbols.cs` treats `»»` as a real close attempt
  when followed by `\n` **or EOF** only. Fixes exactly the reported bug.
  Anything else glued to `»»` mid-line (matching depth or not) keeps today's
  behavior: silently swallowed as literal text, block stays open, search
  continues. Fixture `specs/10-errors/007-close-run-shares-close-line`
  untouched. Would need to scale back the `docs/specs.md` note already added
  (currently claims "enforced on both sides") to say "own line, or EOF"
  instead.

- **Option B — Broader.** `Symbols.cs` treats *any* `»»` run as a real close
  attempt regardless of what follows; `BlockParser` gets a new check that
  throws if literal content follows on the same line (symmetric to the
  existing before-close check). Matches the `docs/specs.md` note as already
  written. But changes fixture `007`'s behavior: today a block opened with
  `«««` (depth 3) can embed a stray, non-matching `»»` mid-body (fence-style
  escape, like markdown backtick-count fencing) — under Option B that stray
  `»»` becomes an immediate depth-mismatch error instead of being swallowed
  as literal content. Would need to decide whether that fence-escape trick
  is worth keeping, and update `007`'s expected `.error` either way.

**Status:** unresolved, waiting on author call. No implementation touched.
In-progress artifacts from investigating this: `docs/specs.md` Blocks
section has a provisional `[!IMPORTANT]` note (matches Option B's claim, may
need scaling back per Option A); fixture
`specs/02-conditional-blocks/011-no-trailing-newline-at-eof.*` written but
not yet confirmed red.
