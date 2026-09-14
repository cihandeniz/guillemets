# Generic (.NET)

Reusable across any .NET repo as-is — kept identical everywhere via
`cihandeniz/config-files`. Project-specific rules live in
`.agents/specific.md`; nothing project-specific belongs here.

## Docs and tracking

Three places hold durable knowledge, and each owns a different kind of fact:

- `AGENTS.md` + `.agents/*.md` — how to work here.
- The architecture doc — how it's built. High-level shape and decisions
  only; a diagram wherever structure is easier seen than read. It must not
  restate the behaviour spec, so it doesn't churn on every feature change.
  Rewrite sections in place rather than appending.
- The current branch's PR description — the living *what's left*. Actionable
  work only, never a changelog. On a cold start read it (`gh pr view`, or the
  repo's `pulls` page) and follow any issue links, which carry the real
  detail behind a one-line task.

Nothing here can write that PR description. Queue anything bound for it in
`__PR_DESC_UPDATE__.md` at the repo root (create or append); the user folds it
in and clears it. Check for that file on a cold start too — its presence means
unmerged content. These are working files, not published docs; never link them
from a README or docs site.

Internal working `.md` files are hard-wrapped at 80 columns, filled greedily,
with fenced code, tables and headings untouched. Reflow a whole paragraph when
editing it rather than leaving a ragged line. Use real markdown links between
*published* docs, plain backticked filenames inside working docs.

## Working habits

- **TDD, one case at a time.** Smallest next case, minimal code to pass it,
  full suite to confirm no regressions, then actually refactor before moving
  on. Report and let the case's author weigh in first. Urgency is never
  licence to write the fix before a failing test proves it's needed.
- **A redesign spanning multiple cases gets a checkpoint before any code
  changes.** Do the entire spec/test/doc rewrite first, confirm it's red
  against the old code, then ignore/skip every touched case so the suite is
  green — and stop there for review. Track the milestone in
  `__PR_DESC_UPDATE__.md`, not a comment in the test source. Implementation
  starts only after that's confirmed.
- **No failing tests at commit time.** Unimplemented cases are Ignored or
  Skipped, never Failed; drop the ignore entry once a case goes green. If a
  case is left unimplemented because something is genuinely undecided, say
  what's undecided in `__PR_DESC_UPDATE__.md`.
- **"reviewed" with no further detail means: grep the touched files for
  `TODO` first.** The user reviews by leaving inline `// TODO ...` notes
  rather than typing them out. Address each, delete the comment (it's a
  review note, not documentation), rerun the suite. Say so if there are
  none. Before implementing any terse instruction, check whether it would
  silently undo something already decided earlier in the same conversation —
  if so, state the tension and ask rather than complying or ignoring.
- **After a broad rewrite/rename/migration, audit for leftovers before
  reporting done** — grep the whole tree for the old pattern and cross-check
  the tracking mechanism, rather than waiting to be asked.
- **For "what if X isn't there", prefer consistent absence-propagation over a
  new hard restriction.** Reserve a hard failure for a structural/syntax
  problem, not for data that may simply be absent.
- With `EnforceCodeStyleInBuild`/`TreatWarningsAsErrors`, any `.editorconfig`
  violation or compiler warning fails the build. `IDE0060` (unused parameter)
  escalated to error doesn't bite interface implementations — Roslyn exempts
  those automatically.
- Known sandbox flake: `MSB3374` on an `obj/**/*.Up2Date` file. Just retry.
- Sandbox ownership: the repo belongs to the human user, so plain `git` fails
  with "dubious ownership". Use `git -c safe.directory='*' <command>` rather
  than a persisted `git config` write. The permanent fix needs interactive
  `sudo`, so ask the user to run it. `gh` is unauthenticated — use the public
  GitHub REST API via `curl` for read-only lookups.

## C# code style

Structure and design:

- One type per file; a tightly-coupled nested helper may share its owner's.
- Prefer polymorphic dispatch over a `switch` implementing per-type behaviour
  — the behaviour belongs in its own class. A `switch` that only *selects*
  between existing strategies is fine, as is a sequential parser walking a
  token stream.
- No `new SomeType(...)` in a constructor body unless it's a DTO or `record`;
  inject dependencies and wire them at the composition root. Resolve a cycle
  with a `Lazy<T>`-backed field through a shared registry — applied uniformly
  to every registry-sourced collaborator, so registration order is never a
  hazard.
- A type built at most once per key gets a plain constructor plus
  `public static GetOrCreate(...)` over a `static readonly` cache. C# has no
  private primary constructor, so dropping to a regular one is the only way
  to stop external code bypassing the cache.
- A type whose only external contract is one interface implements it
  explicitly (`string ISomething.Method(...)`), forwarding to a private
  method if its own internals need the logic. Drop an interface once nothing
  needs it polymorphically.
- Write small, single-purpose methods from the start. Prefer a private method
  over a local function closing over another method's locals.
- Give a sequential scanner instance fields only for state that must persist
  across separate calls; otherwise use locals.
- Avoid tuples and one-off DTOs for shuttling values: success/failure returns
  `bool` and mutates state; one meaningful value is returned directly; a
  secondary value uses `out`. At three or more, consolidate into a nested
  result record still returned via `out` from a `bool TryXxx(...)`.
- Fix a bug in the component that owns the knowledge, not with a heuristic
  where the symptom surfaced. Don't reach into a caller's shared state — take
  a parameter. Don't poke a returned object's fields to finish a job — pass
  the callee what it needs.
- A public extension method meant to be broadly discoverable can live in a
  shared root namespace, since C# namespace lookup already sees ancestor
  namespaces (see `.agents/specific.md`).
- When a review names a refactoring by its actual term (e.g. "Method Object",
  "Inappropriate Intimacy"), apply that exact technique. Restate in one
  sentence what it does and check the planned fix matches it, rather than a
  smaller substitute touching the same lines. A bracketed aside
  ("[name can be better]") is a detail, not a replacement for the technique.

Syntax and layout:

- `using` directives sorted alphabetically, no special-casing `System.*`;
  `using static` forms its own group below, after a blank line.
- Never write `private` — it's the default. Never write `sealed`.
- Break a long boolean with `&&`/`||` at the *end* of the line; a lone
  closing `)` gets its own line, like a closing `}`.
- Keep blank lines between statements minimal.
- Inheritance clause on a primary-constructor type: its own indented line
  when the parameter list is single-line, trailing the `)` when it already
  spans lines — even for a single parameter, and for `record` as for `class`.
  A type with no primary constructor keeps `: Base` on the declaration line.
- More than 2 optional/named arguments never go on one line.
- Expression-bodied **methods** (constructors included) put `=>` at the end
  of the signature and the expression on its own line below, however short.
  Expression-bodied **properties** keep `=>` and the expression inline. The
  signal is the parameter list: `()` → method, none → property.
- Every `static` member goes at the top of its class, above instance members.
- Extension methods use C# 14 `extension(Receiver name) { ... }` blocks, with
  no `static`/`this` on the members. Use a second block when a different
  receiver name reads better for some members.
- Target-typed `new(...)` wherever the compiler can infer it. Not where the
  declared type is a base/interface, the target is `var`, or the `new(...)`
  is the receiver of a chained call.
- Don't pack a pattern match, a capture, a negation and a comparison into one
  condition. Split into straight-line guards, one fact each, and name a
  computed count rather than inlining its arithmetic.
- A literal carrying meaning beyond its face value — a delimiter, sentinel or
  syntax marker — gets a named constant. An error message or arbitrary test
  datum doesn't.
- Never use `!`. Use `?? throw new InvalidOperationException(...)` where an
  invariant guarantees non-null, so it fails loudly at the point of use. See
  <https://github.com/mouseless/learn-dotnet/blob/main/nullable-usage/README.md>.
- No comments in source, tests included — WHY-comments too. If code needs
  one, restructure instead. A fact worth keeping goes to
  `__PR_DESC_UPDATE__.md`, `.agents/specific.md` or the architecture doc.

Naming (`.editorconfig` only marks these as suggestions, so they aren't
build-enforced):

- Private instance fields `_camelCase`; any `static` field, whatever its
  accessibility, `SCREAMING_CASE`.
- `[Test]` method names are `Snake_case` — a plain sentence, first letter
  capitalised, underscores where spaces or punctuation would go
  (`Date_filter_formats_with_given_pattern`).

Tests and docs:

- Arrange-Act-Assert as three groups separated by blank lines, never a blank
  line *within* a group. Act is one line assigned to `var actual` (a value,
  or a delegate when it should throw).
- Shouldly, not NUnit's `Assert.That`. Assert a throw by chaining off the
  delegate: `actual.ShouldThrow<T>().Message.ShouldBe(...)`, not the static
  `Should.Throw<T>(() => ...)`.
- When XML docs are required, ground each `<summary>` in the existing prose
  docs rather than inventing a description that can drift; use
  `<inheritdoc/>` for interface implementations; wrap `///` at 80 columns. A
  test project sets `<NoWarn>$(NoWarn);CS1591</NoWarn>` rather than turning
  off `GenerateDocumentationFile`, which would also silently drop `IDE0005`.

## Parking (ending a session)

1. Run the suite; confirm green. Never park on red — flag it clearly.
2. Write the session's task-list changes to `__PR_DESC_UPDATE__.md`: tasks
   done, any discovered mid-session, anything worth noting for the PR
   description. Actionable work only — an accepted tradeoff or known
   limitation with no follow-up belongs in the behaviour or architecture doc
   as a plain fact instead.
3. Update the architecture doc with any structural change — current shape
   only, never a changelog.
4. Record any durable convention or decision in `.agents/specific.md`, or
   `.agents/generic.md` if it isn't project-specific. These files plus the PR
   description are what survive to a cold start; nothing load-bearing should
   live only in chat history.
5. Give a short summary: what's done, what's next, what to double-check.

## Git

Read-only `git` and `gh` commands are fine to run directly. Never run a `git`
command that writes — the user handles all of git themselves, so don't prepare
commands for them or remind them about pending git work. A `gh` command that
writes is visible to others the moment it runs: draft it and let the user
apply it unless they've explicitly asked otherwise.
